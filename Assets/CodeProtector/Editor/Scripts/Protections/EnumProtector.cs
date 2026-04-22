//EnumProtector.cs
using Mono.Cecil;

namespace CodeProtector
{
    /// <summary>
    /// Ã¶¾Ù»ìÏýÆ÷
    /// </summary>
    public class EnumProtector
    {
        private ProtectorConfig config;
        private NameGenerator nameGenerator;

        public EnumProtector(ProtectorConfig config, NameGenerator nameGenerator)
        {
            this.config = config;
            this.nameGenerator = nameGenerator;
        }

        public void DoProtect(AssemblyDefinition assembly)
        {
            foreach (var type in assembly.MainModule.Types)
                if (type.IsEnum) ProtectEnum(type);
        }

        private void ProtectEnum(TypeDefinition enumType)
        {
            enumType.Name = nameGenerator.GenerateClassName();
            foreach (var field in enumType.Fields)
            {
                if (field.IsSpecialName || field.IsRuntimeSpecialName) continue;
                field.Name = nameGenerator.GenerateFieldName();
            }
        }
    }
}