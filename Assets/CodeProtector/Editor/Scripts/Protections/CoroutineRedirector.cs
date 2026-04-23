//CoroutineRedirector.cs
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Instruction = Mono.Cecil.Cil.Instruction;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace CodeProtector
{
    public class CoroutineRedirector
    {
        private Dictionary<string, string> methodNameMap;
        public CoroutineRedirector(Dictionary<string, string> map) => methodNameMap = map;

        public void DoProtect(AssemblyDefinition assembly)
        {
            foreach (var type in assembly.MainModule.Types)
                foreach (var method in type.Methods)
                    if (method.HasBody) ProcessMethodBody(method);
        }

        private void ProcessMethodBody(MethodDefinition method)
        {
            var instructions = method.Body.Instructions;
            for (int i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].OpCode == OpCodes.Call || instructions[i].OpCode == OpCodes.Callvirt)
                {
                    var methodRef = instructions[i].Operand as MethodReference;
                    if (methodRef != null && methodRef.DeclaringType.FullName == "UnityEngine.MonoBehaviour"
                        && methodRef.Name == "StartCoroutine"
                        && methodRef.Parameters.Count == 1
                        && methodRef.Parameters[0].ParameterType.FullName == "System.String")
                    {
                        var stringArg = FindStringArgument(instructions, i);
                        if (stringArg != null)
                        {
                            string oldName = (string)stringArg.Operand;
                            string key = FindMatchingKey(oldName);
                            if (methodNameMap.ContainsKey(key))
                                stringArg.Operand = methodNameMap[key];
                        }
                    }
                }
            }
        }

        private Instruction FindStringArgument(IList<Instruction> instructions, int callIndex)
        {
            for (int i = callIndex - 1; i >= 0 && i >= callIndex - 5; i--)
                if (instructions[i].OpCode == OpCodes.Ldstr)
                    return instructions[i];
            return null;
        }

        private string FindMatchingKey(string coroutineName)
        {
            foreach (var kv in methodNameMap)
                if (kv.Key.EndsWith("::" + coroutineName))
                    return kv.Key;
            return coroutineName;
        }
    }
}