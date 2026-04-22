//NameProtector.cs
using System.Collections.Generic;
using Mono.Cecil;

namespace CodeProtector
{
    /// <summary>
    /// 名称混淆器：混淆类名、方法名、字段名、属性名
    /// </summary>
    public class NameProtector
    {
        private ProtectorConfig config;
        private NameGenerator nameGenerator;

        public NameProtector(ProtectorConfig config, NameGenerator nameGenerator)
        {
            this.config = config;
            this.nameGenerator = nameGenerator;
        }

        public void DoProtect(AssemblyDefinition assembly,
            Dictionary<string, string> classNameMap,
            Dictionary<string, string> methodNameMap)
        {
            foreach (var type in assembly.MainModule.Types)
            {
                if (ShouldSkipType(type)) continue;

                ObfuscateFields(type);
                ObfuscateProperties(type);
                ObfuscateMethods(type, methodNameMap);
                ObfuscateClassName(type, classNameMap);
                if (config.NameType == ObfuscateNameType.RandomString)
                    ObfuscateNamespace(type);
            }
        }

        private bool ShouldSkipType(TypeDefinition type)
        {
            if (type.Name.StartsWith("<") || type.Name.Contains("`")) return true;
            if (type.IsEnum && !config.EnableEnumObfuscate) return true;
            if (IsMonoBehaviour(type)) return !config.ObfuscateMonoBehaviourClassName;
            return false;
        }

        private bool IsMonoBehaviour(TypeDefinition type)
        {
            var baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType.FullName == "UnityEngine.MonoBehaviour")
                    return true;
                baseType = baseType.Resolve()?.BaseType;
            }
            return false;
        }

        private void ObfuscateFields(TypeDefinition type)
        {
            foreach (var field in type.Fields)
            {
                if (ShouldSkipField(field)) continue;
                field.Name = nameGenerator.GenerateFieldName();
            }
        }

        private bool ShouldSkipField(FieldDefinition field)
        {
            if (field.IsSpecialName || field.IsRuntimeSpecialName) return true;
            if (field.IsPublic && !config.ObfuscatePublicFields) return true;
            if (field.IsPrivate && !config.ObfuscatePrivateFields) return true;
            if (field.IsFamily && !config.ObfuscateProtectedFields) return true;
            if (field.IsAssembly && !config.ObfuscateInternalFields) return true;

            if (field.HasCustomAttributes)
            {
                foreach (var attr in field.CustomAttributes)
                    if (attr.AttributeType.FullName == "UnityEngine.SerializeField")
                        return !config.ObfuscateMonoBehaviourFields;
            }
            return false;
        }

        private void ObfuscateProperties(TypeDefinition type)
        {
            foreach (var property in type.Properties)
            {
                if (ShouldSkipProperty(property)) continue;
                property.Name = nameGenerator.GeneratePropertyName();
            }
        }

        private bool ShouldSkipProperty(PropertyDefinition property)
        {
            if (property.IsSpecialName || property.IsRuntimeSpecialName) return true;
            if (property.Name == "Item") return true;
            return false;
        }

        private void ObfuscateMethods(TypeDefinition type, Dictionary<string, string> methodNameMap)
        {
            foreach (var method in type.Methods)
            {
                if (ShouldSkipMethod(method)) continue;
                string key = $"{type.FullName}::{method.Name}";
                if (methodNameMap.ContainsKey(key))
                    method.Name = methodNameMap[key];
            }
        }

        private bool ShouldSkipMethod(MethodDefinition method)
        {
            if (method.IsConstructor) return true;
            if (method.IsVirtual) return true;
            if (method.Name.StartsWith(".") || method.Name.Contains("<")) return true;
            if (method.IsRuntimeSpecialName || method.IsSpecialName) return true;

            string[] unityMethods = { "Awake", "Start", "Update", "FixedUpdate", "LateUpdate",
                "OnEnable", "OnDisable", "OnDestroy", "OnGUI", "OnTriggerEnter", "OnTriggerExit",
                "OnCollisionEnter", "OnCollisionExit" };
            foreach (string m in unityMethods)
                if (method.Name == m) return true;
            return false;
        }

        private void ObfuscateClassName(TypeDefinition type, Dictionary<string, string> classNameMap)
        {
            string fullName = type.FullName;
            if (classNameMap.ContainsKey(fullName))
                type.Name = classNameMap[fullName];
        }

        private void ObfuscateNamespace(TypeDefinition type)
        {
            if (!string.IsNullOrEmpty(type.Namespace))
                type.Namespace = nameGenerator.GenerateClassName();
        }
    }
}