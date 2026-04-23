//JunkMethodGenerator.cs
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;
using Instruction = Mono.Cecil.Cil.Instruction;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace CodeProtector
{
    public class JunkMethodGenerator
    {
        private ProtectorConfig config;
        private NameGenerator nameGenerator;
        private List<string> allStringLiterals = new List<string>();
        private List<Instruction> instructionFragments = new List<Instruction>();

        public JunkMethodGenerator(ProtectorConfig config, NameGenerator nameGenerator)
        {
            this.config = config;
            this.nameGenerator = nameGenerator;
        }

        public void DoProtect(AssemblyDefinition assembly)
        {
            CollectStringLiterals(assembly);
            CollectInstructionFragments(assembly);

            foreach (var type in assembly.MainModule.Types)
            {
                if (ShouldSkipType(type)) continue;

                int junkMethodCount = config.GarbageMethodMultiplePerClass *
                    (type.Methods.Count(m => m.HasBody && !m.IsConstructor));

                for (int i = 0; i < junkMethodCount; i++)
                    GenerateJunkMethod(type, assembly.MainModule);
            }
        }

        private bool ShouldSkipType(TypeDefinition type)
        {
            if (type.IsEnum) return true;
            if (type.IsInterface) return true;
            if (type.Name.StartsWith("<")) return true;
            return false;
        }

        private void CollectStringLiterals(AssemblyDefinition assembly)
        {
            foreach (var type in assembly.MainModule.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody) continue;
                    foreach (var instruction in method.Body.Instructions)
                    {
                        if (instruction.OpCode == OpCodes.Ldstr)
                        {
                            string str = instruction.Operand as string;
                            if (!string.IsNullOrEmpty(str))
                                allStringLiterals.Add(str);
                        }
                    }
                }
            }
        }

        private void CollectInstructionFragments(AssemblyDefinition assembly)
        {
            foreach (var type in assembly.MainModule.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody) continue;
                    var instructions = method.Body.Instructions;
                    for (int i = 0; i < instructions.Count; i++)
                        if (IsSafeInstruction(instructions[i]))
                            instructionFragments.Add(instructions[i]);
                }
            }
        }

        private bool IsSafeInstruction(Instruction instruction)
        {
            var safeOpcodes = new[]
            {
                OpCodes.Ldc_I4, OpCodes.Ldc_I4_0, OpCodes.Ldc_I4_1, OpCodes.Ldc_I4_2, OpCodes.Ldc_I4_3,
                OpCodes.Ldc_I4_4, OpCodes.Ldc_I4_5, OpCodes.Ldc_I4_6, OpCodes.Ldc_I4_7, OpCodes.Ldc_I4_8,
                OpCodes.Ldc_I4_S, OpCodes.Ldc_I8, OpCodes.Ldc_R4, OpCodes.Ldc_R8,
                OpCodes.Add, OpCodes.Sub, OpCodes.Mul, OpCodes.Div, OpCodes.Rem,
                OpCodes.Ldstr, OpCodes.Stloc_0, OpCodes.Stloc_1, OpCodes.Stloc_2, OpCodes.Stloc_3,
                OpCodes.Ldloc_0, OpCodes.Ldloc_1, OpCodes.Ldloc_2, OpCodes.Ldloc_3
            };
            return safeOpcodes.Contains(instruction.OpCode);
        }

        private void GenerateJunkMethod(TypeDefinition type, ModuleDefinition module)
        {
            string methodName = nameGenerator.GenerateJunkMethodName();

            TypeReference returnType;
            int returnTypeChoice = nameGenerator.GetRandomInt(0, 4);
            if (returnTypeChoice == 0)
                returnType = module.TypeSystem.Void;
            else if (returnTypeChoice == 1)
                returnType = module.TypeSystem.Int32;
            else if (returnTypeChoice == 2)
                returnType = module.TypeSystem.String;
            else
                returnType = module.ImportReference(type);

            var method = new MethodDefinition(
                methodName,
                MethodAttributes.Public | MethodAttributes.Static,
                returnType
            );

            var il = method.Body.GetILProcessor();
            int lineCount = config.JunkMethodLinesPerClass;
            GenerateJunkMethodBody(il, module, lineCount, returnType);
            type.Methods.Add(method);
        }

        private void GenerateJunkMethodBody(ILProcessor il, ModuleDefinition module, int lineCount, TypeReference returnType)
        {
            var locals = new List<VariableDefinition>();
            for (int i = 0; i < 3; i++)
            {
                var local = new VariableDefinition(module.TypeSystem.Int32);
                il.Body.Variables.Add(local);
                locals.Add(local);
            }

            for (int i = 0; i < lineCount; i++)
                GenerateRandomOperation(il, module, locals);

            GenerateReturnValue(il, returnType);
        }

        private void GenerateRandomOperation(ILProcessor il, ModuleDefinition module, List<VariableDefinition> locals)
        {
            int opType = nameGenerator.GetRandomInt(0, 5);
            switch (opType)
            {
                case 0:
                    GenerateArithmeticOperation(il);
                    break;
                case 1:
                    GenerateStringConcat(il, module);
                    break;
                case 2:
                    if (locals.Count > 0)
                    {
                        int randValue = nameGenerator.GetRandomInt(1, 100);
                        il.Append(il.Create(OpCodes.Ldc_I4, randValue));
                        il.Append(il.Create(OpCodes.Stloc, locals[nameGenerator.GetRandomInt(0, locals.Count)]));
                    }
                    break;
                case 3:
                    int a = nameGenerator.GetRandomInt(1, 100);
                    int b = nameGenerator.GetRandomInt(1, 100);
                    il.Append(il.Create(OpCodes.Ldc_I4, a));
                    il.Append(il.Create(OpCodes.Ldc_I4, b));
                    il.Append(il.Create(OpCodes.Add));
                    il.Append(il.Create(OpCodes.Pop));
                    break;
                case 4:
                    il.Append(il.Create(OpCodes.Nop));
                    break;
            }
        }

        private void GenerateArithmeticOperation(ILProcessor il)
        {
            int a = nameGenerator.GetRandomInt(1, 100);
            int b = nameGenerator.GetRandomInt(1, 100);
            il.Append(il.Create(OpCodes.Ldc_I4, a));
            il.Append(il.Create(OpCodes.Ldc_I4, b));
            OpCode[] ops = { OpCodes.Add, OpCodes.Sub, OpCodes.Mul, OpCodes.Div };
            il.Append(il.Create(ops[nameGenerator.GetRandomInt(0, ops.Length)]));
            il.Append(il.Create(OpCodes.Pop));
        }

        private void GenerateStringConcat(ILProcessor il, ModuleDefinition module)
        {
            string str1 = GetRandomStringLiteral();
            string str2 = GetRandomStringLiteral();

            // 导入 string.Concat 方法
            var concatMethod = module.ImportReference(
                typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) })
            );

            il.Append(il.Create(OpCodes.Ldstr, str1));
            il.Append(il.Create(OpCodes.Ldstr, str2));
            il.Append(il.Create(OpCodes.Call, concatMethod));
            il.Append(il.Create(OpCodes.Pop));
        }

        private string GetRandomStringLiteral()
        {
            if (allStringLiterals.Count > 0)
                return allStringLiterals[nameGenerator.GetRandomInt(0, allStringLiterals.Count)];
            return "DefaultString";
        }

        private void GenerateReturnValue(ILProcessor il, TypeReference returnType)
        {
            if (returnType.FullName == "System.Void")
                il.Append(il.Create(OpCodes.Ret));
            else if (returnType.FullName == "System.Int32")
            {
                il.Append(il.Create(OpCodes.Ldc_I4, nameGenerator.GetRandomInt(0, 1000)));
                il.Append(il.Create(OpCodes.Ret));
            }
            else if (returnType.FullName == "System.String")
            {
                il.Append(il.Create(OpCodes.Ldstr, GetRandomStringLiteral()));
                il.Append(il.Create(OpCodes.Ret));
            }
            else
            {
                il.Append(il.Create(OpCodes.Ldnull));
                il.Append(il.Create(OpCodes.Ret));
            }
        }
    }
}