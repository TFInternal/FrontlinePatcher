using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace FrontlinePatcher.Patch.Patches;

public class StorePurchasePatch : Patch
{
    public override string Name => "Fix Store For Devices Without GMS";
    
    public override bool Apply(ModuleDefMD module)
    {
        var storeManagerType = module.Find("StoreManager", true);
        if (storeManagerType is null)
        {
            Console.Error.WriteLine("  StoreManager type not found!");
            return false;
        }

        var storeCurrencyType = storeManagerType.NestedTypes.FirstOrDefault(t => t.Name == "StoreCurrencyType");
        if (storeCurrencyType is null)
        {
            Console.Error.WriteLine("  StoreManager.StoreCurrencyType type not found!");
            return false;
        }

        if (!storeCurrencyType.IsEnum)
        {
            Console.Error.WriteLine("  StoreManager.StoreCurrencyType is not an enum!");
            return false;
        }

        var storeCurrencyRealField = storeCurrencyType.FindField("Real");
        if (storeCurrencyRealField is null)
        {
            Console.Error.WriteLine("  StoreManager.StoreCurrencyType.Real field not found!");
            return false;
        }
        
        if (storeCurrencyRealField.Constant.Value is not int storeCurrencyRealValue)
        {
            Console.Error.WriteLine("  StoreManager.StoreCurrencyType.Real field is not an int!");
            return false;
        }
        
        if (!PatchStoreUi(module, storeCurrencyRealValue))
        {
            Console.Error.WriteLine("  Failed to patch StoreUI!");
            return false;
        }
        
        if (!PatchStoreManager(module, storeManagerType, storeCurrencyRealValue))
        {
            Console.Error.WriteLine("  Failed to patch StoreManager!");
            return false;
        }

        return true;
    }

    private bool PatchStoreUi(ModuleDefMD module, int storeCurrencyRealValue)
    {
        Console.WriteLine("  Patching StoreUI...");
        
        var storeUiType = module.Find("StoreUI", true);
        if (storeUiType is null)
        {
            Console.Error.WriteLine("    StoreUI type not found!");
            return false;
        }
        
        var productType = module.Find("Product", true);
        if (productType is null)
        {
            Console.Error.WriteLine("    Product type not found!");
            return false;
        }
        
        var stateMachineType = storeUiType.NestedTypes.FirstOrDefault(t => t.IsClass && t.Name == "<RunPurchaseFlow>c__Iterator81");
        if (stateMachineType is null)
        {
            Console.Error.WriteLine("    RunPurchaseFlow state machine type not found!");
            return false;
        }
        
        var targetMethod = stateMachineType.FindMethod("MoveNext");
        if (targetMethod is null)
        {
            Console.Error.WriteLine("    RunPurchaseFlow state machine MoveNext method not found!");
            return false;
        }
        
        Console.WriteLine($"    RunPurchaseFlow state machine MoveNext method found! {targetMethod.FullName}");
        
        var currencyField = stateMachineType.FindField("currency");
        if (currencyField is null)
        {
            Console.Error.WriteLine("    RunPurchaseFlow state machine currency field not found!");
            return false;
        }
        
        var body = targetMethod.Body;
        var instructions = body.Instructions;
        
        body.KeepOldMaxStack = false;

        var nextComparisonJumpTargetLabel = instructions.FirstOrDefault(i => i.Offset == 0x004c);
        var equalJumpTargetLabel = instructions.FirstOrDefault(i => i.Offset == 0x005b);

        var newInstructions = new List<Instruction>
        {
            OpCodes.Brtrue_S.ToInstruction(nextComparisonJumpTargetLabel),
            OpCodes.Ldarg_0.ToInstruction(),
            OpCodes.Ldfld.ToInstruction(currencyField),
            OpCodes.Ldc_I4.ToInstruction(storeCurrencyRealValue),
            OpCodes.Beq_S.ToInstruction(equalJumpTargetLabel)
        };
        
        var replaceAt = instructions.IndexOf(instructions.FirstOrDefault(i => i.Offset == 0x0047));
        if (replaceAt == -1)
        {
            Console.Error.WriteLine("    Failed to find instruction to replace!");
            return false;
        }
        
        instructions.RemoveAt(replaceAt);
        
        foreach (var instruction in newInstructions)
        {
            instructions.Insert(replaceAt++, instruction);
        }

        body.OptimizeBranches();
        
        Console.WriteLine("    Successfully modified method body!");
        return true;
    }

    private bool PatchStoreManager(ModuleDefMD module, TypeDef storeManagerType, int storeCurrencyRealValue)
    {
        Console.WriteLine("  Patching StoreManager...");
        
        var targetMethod = storeManagerType.FindMethod("RequestPurchase");
        if (targetMethod is null)
        {
            Console.Error.WriteLine("    RequestPurchase method not found!");
            return false;
        }
        
        Console.WriteLine($"    RequestPurchase method found! {targetMethod.FullName}");
        
        var body = targetMethod.Body;
        var instructions = body.Instructions;
        
        body.KeepOldMaxStack = false;
        
        var notRealJumpTargetLabel = instructions.FirstOrDefault(i => i.Offset == 0x0035);

        var newInstructions = new List<Instruction>
        {
            OpCodes.Ldarg_2.ToInstruction(),
            OpCodes.Ldc_I4.ToInstruction(storeCurrencyRealValue),
            OpCodes.Bne_Un_S.ToInstruction(notRealJumpTargetLabel)
        };
        
        var insertAfter = instructions.IndexOf(instructions.FirstOrDefault(i => i.Offset == 0x0011));
        if (insertAfter == -1)
        {
            Console.Error.WriteLine("    Failed to find where to insert instructions!");
            return false;
        }
        
        for (var i = 0; i < newInstructions.Count; i++)
        {
            instructions.Insert(insertAfter + i + 1, newInstructions[i]);
        }
        
        body.OptimizeBranches();
        
        Console.WriteLine("    Successfully modified method body!");
        return true;
    }
}