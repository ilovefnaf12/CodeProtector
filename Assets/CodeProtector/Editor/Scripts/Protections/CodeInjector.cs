//CodeInjector.cs
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using OpCodes = Mono.Cecil.Cil.OpCodes;
namespace CodeProtector
{
    /// <summary>
    /// 代码注入器：从垃圾代码库注入垃圾方法调用
    /// </summary>
    public class CodeInjector
    {
        private ProtectorConfig config;
        private List<MethodDefinition> garbageMethods = new List<MethodDefinition>();

        public CodeInjector(ProtectorConfig config)
        {
            this.config = config;
        }

        public void DoProtect(AssemblyDefinition assembly, string garbageCodeLibPath)
        {
            LoadGarbageCodeLibrary(garbageCodeLibPath);
            if (garbageMethods.Count == 0) return;

            foreach (var type in assembly.MainModule.Types)
            {
                if (ShouldSkipType(type)) continue;
                foreach (var method in type.Methods)
                {
                    if (ShouldSkipMethod(method)) continue;
                    InjectGarbageMethodCalls(method, assembly.MainModule);
                }
            }
        }

        private void LoadGarbageCodeLibrary(string libPath)
        {
            if (string.IsNullOrEmpty(libPath) || !System.IO.File.Exists(libPath))
                return;
            try
            {
                var garbageAssembly = AssemblyDefinition.ReadAssembly(libPath);
                foreach (var type in garbageAssembly.MainModule.Types)
                {
                    if (type.FullName.Contains("GarbageCode"))
                    {
                        foreach (var method in type.Methods)
                        {
                            if (method.IsStatic && method.IsPublic && !method.IsConstructor)
                                garbageMethods.Add(method);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to load garbage code library: {ex.Message}");
            }
        }

        private bool ShouldSkipType(TypeDefinition type)
        {
            if (type.IsEnum) return true;
            if (type.IsInterface) return true;
            if (type.Name.StartsWith("<")) return true;
            return false;
        }

        private bool ShouldSkipMethod(MethodDefinition method)
        {
            if (!method.HasBody) return true;
            if (method.IsConstructor) return true;
            if (method.Name.StartsWith(".")) return true;
            if (method.ReturnType.FullName == "System.Collections.IEnumerator")
                return true;
            return false;
        }

        private void InjectGarbageMethodCalls(MethodDefinition method, ModuleDefinition module)
        {
            var il = method.Body.GetILProcessor();
            var firstInstruction = method.Body.Instructions.FirstOrDefault();

            for (int i = 0; i < config.InsertMethodCountPerMethod; i++)
            {
                if (garbageMethods.Count == 0) continue;
                var garbageMethod = garbageMethods[new System.Random().Next(garbageMethods.Count)];
                var importedMethod = module.ImportReference(garbageMethod);
                var callInstruction = il.Create(OpCodes.Call, importedMethod);

                if (firstInstruction != null)
                    il.InsertBefore(firstInstruction, callInstruction);
                else
                    il.Append(callInstruction);
            }
        }
    }
}