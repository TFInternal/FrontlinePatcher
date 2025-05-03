using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Spectre.Console;

namespace FrontlinePatcher.Patch.Patches;

public class GameDebugLogPatch : Patch
{
    public override string Name => "GameDebug Log Implementation";
    
    public override bool Apply(ModuleDefMD module)
    {
        var gameDebugType = module.Find("GameDebug", true);
        if (gameDebugType is null)
        {
            AnsiConsole.MarkupLine("[red]  GameDebug type not found![/]");
            return false;
        }
        
        var logGroupType = module.Find("LogGroup", true);
        if (logGroupType is null)
        {
            AnsiConsole.MarkupLine("[red]  LogGroup type not found![/]");
            return false;
        }
        
        var logLevelType = module.Find("LogLevel", true);
        if (logLevelType is null)
        {
            AnsiConsole.MarkupLine("[red]  LogLevel type not found![/]");
            return false;
        }
        
        var unityEngineObjectType = FindUnityEngineType(module, "UnityEngine.Object");
        if (unityEngineObjectType is null)
        {
            AnsiConsole.MarkupLine("[red]  UnityEngine.Object type not found![/]");
            return false;
        }

        var logGroupSig = logGroupType.ToTypeSig();
        var unityEngineObjectSig = unityEngineObjectType.ToTypeSig();
        var logLevelSig = logLevelType.ToTypeSig();
        var stringSig = module.CorLibTypes.String;
        var objectSig = module.CorLibTypes.Object;
        var objectArraySig = new SZArraySig(objectSig);

        // static void Log(LogGroup, UnityEngine.Object, LogLevel, string, object[])
        var targetSig = MethodSig.CreateStatic(
            module.CorLibTypes.Void,
            logGroupSig,
            unityEngineObjectSig,
            logLevelSig,
            stringSig,
            objectArraySig
        );

        var targetMethod = gameDebugType.FindMethod("Log", targetSig);
        if (targetMethod is null)
        {
            AnsiConsole.MarkupLine("[red]  Log method not found![/]");
            return false;
        }
        
        AnsiConsole.WriteLine($"  Log method found! {targetMethod.FullName}");
        
        var unityDebugType = FindUnityEngineType(module, "UnityEngine.Debug");
        if (unityDebugType is null)
        {
            AnsiConsole.MarkupLine("[red]  UnityEngine.Debug type not found![/]");
            return false;
        }
        
        // Find UnityEngine.Debug.LogFormat
        var logFormatSig = MethodSig.CreateStatic(
            module.CorLibTypes.Void,
            unityEngineObjectSig,
            stringSig,
            objectArraySig
        );
        
        var logFormatMethodDef = unityDebugType.FindMethod("LogFormat", logFormatSig);
        if (logFormatMethodDef is null)
        {
            AnsiConsole.MarkupLine("[red]  UnityEngine.Debug.LogFormat method not found![/]");
            return false;
        }

        var logFormatMethodRef = module.Import(logFormatMethodDef);

        AnsiConsole.WriteLine($"  LogFormat method found! {logFormatMethodRef.FullName}");
        
        // Find UnityEngine.Debug.Log
        var logSig = MethodSig.CreateStatic(
            module.CorLibTypes.Void,
            objectSig,
            unityEngineObjectSig
        );
        
        var logMethodDef = unityDebugType.FindMethod("Log", logSig);
        if (logMethodDef is null)
        {
            AnsiConsole.MarkupLine("[red]  UnityEngine.Debug.Log method not found![/]");
            return false;
        }
        
        var logMethodRef = module.Import(logMethodDef);
        
        AnsiConsole.WriteLine($"  Log method found! {logMethodRef.FullName}");
        
        // Modify method body
        var body = targetMethod.Body;
        var instructions = body.Instructions;
        var argsParameter = targetMethod.Parameters[4];
        var contextParameter = targetMethod.Parameters[1];
        var formatParameter = targetMethod.Parameters[3];
        
        instructions.Clear();
        body.KeepOldMaxStack = false;

        var elseLabel = new Instruction(OpCodes.Nop);
        
        // if (args.Length != 0)
        instructions.Add(OpCodes.Ldarg.ToInstruction(argsParameter));
        instructions.Add(OpCodes.Ldlen.ToInstruction());
        instructions.Add(OpCodes.Brfalse_S.ToInstruction(elseLabel));
        
        // {
        //   Debug.LogFormat(context, format, args);
        //   return;
        // }
        instructions.Add(OpCodes.Ldarg.ToInstruction(contextParameter));
        instructions.Add(OpCodes.Ldarg.ToInstruction(formatParameter));
        instructions.Add(OpCodes.Ldarg.ToInstruction(argsParameter));
        instructions.Add(OpCodes.Call.ToInstruction(logFormatMethodRef));
        instructions.Add(OpCodes.Ret.ToInstruction());
        
        // Debug.Log(format, context);
        // return;
        instructions.Add(elseLabel);
        instructions.Add(OpCodes.Ldarg.ToInstruction(formatParameter));
        instructions.Add(OpCodes.Ldarg.ToInstruction(contextParameter));
        instructions.Add(OpCodes.Call.ToInstruction(logMethodRef));
        instructions.Add(OpCodes.Ret.ToInstruction());
        
        body.OptimizeBranches();
        
        AnsiConsole.WriteLine("  Successfully modified method body!");
        return true;
    }
}