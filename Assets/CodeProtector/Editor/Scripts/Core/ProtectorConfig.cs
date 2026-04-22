//ProtectorConfig.cs
using UnityEngine;

namespace CodeProtector
{
    [CreateAssetMenu(fileName = "CodeProtectorConfig", menuName = "CodeProtector/Config")]
    public class ProtectorConfig : ScriptableObject
    {
        [Header("Basic Settings")]
        public bool EnableCodeObfuscator = true;
        public int RandomSeed = 123;
        public bool UseTimeStamp = false;

        [Header("Name Obfuscation")]
        public bool EnableNameObfuscate = true;
        public ObfuscateNameType NameType = ObfuscateNameType.RandomString;
        public int UnicodeStart = 91;
        public int UnicodeCharCountExponent = 1;
        public bool UseNameListFile = false;
        public string NameListFilePath = "";

        [Header("Code Injection")]
        public bool EnableCodeInject = true;
        public bool EnableJunkMethod = true;
        public int JunkMethodLinesPerClass = 10;
        public int GarbageMethodMultiplePerClass = 2;
        public int InsertMethodCountPerMethod = 1;
        public string GarbageCodeLibPath = "";

        [Header("Coroutine Redirect")]
        public bool EnableCoroutineRedirect = true;

        [Header("Inheritance Obfuscation")]
        public bool EnableInheritanceObfuscate = true;
        public int InheritanceDepth = 3;

        [Header("Enum Obfuscation")]
        public bool EnableEnumObfuscate = true;

        [Header("Serializable Obfuscation")]
        public bool EnableSerializableObfuscate = true;

        [Header("MonoBehaviour Obfuscation")]
        public bool ObfuscateMonoBehaviourClassName = true;
        public bool ObfuscateMonoBehaviourFields = true;

        [Header("Modifier Control")]
        public bool ObfuscatePublicFields = true;
        public bool ObfuscatePrivateFields = true;
        public bool ObfuscateProtectedFields = true;
        public bool ObfuscateInternalFields = true;

        [Header("Advanced")]
        public bool ObfuscateAbstractClass = false;
        public string[] ObfuscateDllPaths;
    }

    public enum ObfuscateNameType
    {
        RandomString,
        NameList
    }
}