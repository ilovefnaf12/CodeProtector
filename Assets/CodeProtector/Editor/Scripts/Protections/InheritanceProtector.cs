//InheritanceProtector.cs
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using OpCodes = Mono.Cecil.Cil.OpCodes;
using TypeAttributes = Mono.Cecil.TypeAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace CodeProtector
{
    /// <summary>
    /// 类继承混淆器：为每个类创建多个继承链，并在代码中随机替换引用
    /// </summary>
    public class InheritanceProtector
    {
        private ProtectorConfig config;
        private NameGenerator nameGenerator;
        private Dictionary<string, List<string>> inheritanceMap;
        private Dictionary<string, TypeDefinition> newTypeCache = new Dictionary<string, TypeDefinition>();

        public InheritanceProtector(ProtectorConfig config, NameGenerator nameGenerator)
        {
            this.config = config;
            this.nameGenerator = nameGenerator;
        }

        public void DoProtect(AssemblyDefinition assembly,
            Dictionary<string, string> classNameMap,
            Dictionary<string, List<string>> inheritanceMap)
        {
            this.inheritanceMap = inheritanceMap;

            foreach (var type in assembly.MainModule.Types.ToList())
            {
                if (ShouldSkipType(type)) continue;
                CreateInheritanceChain(type, assembly.MainModule);
            }
            ReplaceTypeReferences(assembly);
        }

        private bool ShouldSkipType(TypeDefinition type)
        {
            if (type.IsInterface) return true;
            if (type.IsAbstract && !config.ObfuscateAbstractClass) return true;
            if (type.IsEnum) return true;
            if (type.Name.StartsWith("<")) return true;

            if (type.BaseType != null &&
                (type.BaseType.FullName == "UnityEngine.MonoBehaviour" ||
                 type.BaseType.FullName == "UnityEngine.ScriptableObject"))
            {
                return !config.ObfuscateMonoBehaviourClassName;
            }
            return false;
        }

        private void CreateInheritanceChain(TypeDefinition baseType, ModuleDefinition module)
        {
            List<string> createdTypes = new List<string>();
            string baseTypeKey = baseType.FullName;
            TypeDefinition currentBase = baseType;

            for (int i = 0; i < config.InheritanceDepth; i++)
            {
                string newClassName = nameGenerator.GenerateInheritanceClassName(baseType.Name);
                var newType = new TypeDefinition(
                    baseType.Namespace,
                    newClassName,
                    TypeAttributes.Public | TypeAttributes.Class,
                    module.ImportReference(currentBase)
                );

                AddDefaultConstructor(newType, module, currentBase);
                module.Types.Add(newType);
                createdTypes.Add(newType.FullName);
                currentBase = newType;
            }
            inheritanceMap[baseTypeKey] = createdTypes;
        }

        private void AddDefaultConstructor(TypeDefinition type, ModuleDefinition module, TypeReference baseType)
        {
            var ctor = new MethodDefinition(
                ".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                module.TypeSystem.Void
            );

            var il = ctor.Body.GetILProcessor();
            il.Append(il.Create(OpCodes.Ldarg_0));

            var baseCtor = baseType.Resolve().Methods.FirstOrDefault(m => m.IsConstructor && !m.IsStatic);
            if (baseCtor != null)
                il.Append(il.Create(OpCodes.Call, module.ImportReference(baseCtor)));

            il.Append(il.Create(OpCodes.Ret));
            type.Methods.Add(ctor);
        }

        private void ReplaceTypeReferences(AssemblyDefinition assembly)
        {
            foreach (var type in assembly.MainModule.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody) continue;
                    var instructions = method.Body.Instructions;
                    foreach (var instruction in instructions)
                    {
                        if (instruction.Operand is TypeReference typeRef)
                        {
                            string fullName = typeRef.FullName;
                            if (inheritanceMap.ContainsKey(fullName))
                            {
                                var options = inheritanceMap[fullName];
                                if (options.Count > 0)
                                {
                                    int index = nameGenerator.GetRandomInt(0, options.Count);
                                    string newTypeName = options[index];
                                    var newType = assembly.MainModule.GetType(newTypeName);
                                    if (newType != null)
                                        instruction.Operand = assembly.MainModule.ImportReference(newType);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}