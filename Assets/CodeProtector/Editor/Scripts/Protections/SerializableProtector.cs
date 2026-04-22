//SerializableProtector.cs
using Mono.Cecil;
using System.Collections.Generic;

namespace CodeProtector
{
    /// <summary>
    /// 序列化类混淆器：处理带有[Serializable]特性的类
    /// </summary>
    public class SerializableProtector
    {
        private ProtectorConfig config;
        private NameGenerator nameGenerator;

        public SerializableProtector(ProtectorConfig config, NameGenerator nameGenerator)
        {
            this.config = config;
            this.nameGenerator = nameGenerator;
        }

        public void DoProtect(AssemblyDefinition assembly, Dictionary<string, string> classNameMap)
        {
            foreach (var type in assembly.MainModule.Types)
                if (IsSerializable(type)) ProtectSerializableClass(type, classNameMap);
        }

        private bool IsSerializable(TypeDefinition type)
        {
            if (!type.IsClass) return false;
            foreach (var attr in type.CustomAttributes)
                if (attr.AttributeType.FullName == "System.SerializableAttribute") return true;
            return false;
        }

        private void ProtectSerializableClass(TypeDefinition type, Dictionary<string, string> classNameMap)
        {
            string fullName = type.FullName;
            if (classNameMap.ContainsKey(fullName))
                type.Name = classNameMap[fullName];

            foreach (var field in type.Fields)
                if (ShouldProtectSerializableField(field))
                    field.Name = nameGenerator.GenerateFieldName();
        }

        private bool ShouldProtectSerializableField(FieldDefinition field)
        {
            if (field.IsSpecialName || field.IsRuntimeSpecialName) return false;
            if (field.IsStatic) return false;
            if (field.IsNotSerialized) return false;

            foreach (var attr in field.CustomAttributes)
                if (attr.AttributeType.FullName == "System.NonSerializedAttribute") return false;

            if (field.IsPublic) return config.ObfuscatePublicFields;
            foreach (var attr in field.CustomAttributes)
                if (attr.AttributeType.FullName == "UnityEngine.SerializeField")
                    return config.ObfuscateMonoBehaviourFields;
            return false;
        }
    }
}