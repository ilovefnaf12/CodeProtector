//ProtectorWindow.cs
using UnityEditor;
using UnityEngine;

namespace CodeProtector
{
    public class ProtectorWindow : EditorWindow
    {
        private ProtectorConfig config;
        private Vector2 scrollPosition;
        private string unicodePreview = "";

        [MenuItem("Tools/Code Protector/Config Window")]
        public static void ShowWindow()
        {
            GetWindow<ProtectorWindow>("Code Protector");
        }

        private void OnEnable()
        {
            string[] guids = AssetDatabase.FindAssets("t:ProtectorConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                config = AssetDatabase.LoadAssetAtPath<ProtectorConfig>(path);
            }
            if (config == null)
            {
                config = CreateInstance<ProtectorConfig>();
                AssetDatabase.CreateAsset(config, "Assets/CodeProtector/Editor/Res/CodeProtectorConfig.asset");
                AssetDatabase.SaveAssets();
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.LabelField("Code Protector Configuration", EditorStyles.boldLabel);
            DrawBasicSettings();
            DrawNameObfuscationSettings();
            DrawCodeInjectionSettings();
            DrawAdvancedSettings();
            DrawActionButtons();
            EditorGUILayout.EndScrollView();
        }

        private void DrawBasicSettings()
        {
            EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);
            config.EnableCodeObfuscator = EditorGUILayout.Toggle("Enable Protection", config.EnableCodeObfuscator);
            config.RandomSeed = EditorGUILayout.IntField("Random Seed", config.RandomSeed);
            config.UseTimeStamp = EditorGUILayout.Toggle("Use Timestamp", config.UseTimeStamp);
            EditorGUILayout.Space();
        }

        private void DrawNameObfuscationSettings()
        {
            EditorGUILayout.LabelField("Name Obfuscation", EditorStyles.boldLabel);
            config.EnableNameObfuscate = EditorGUILayout.Toggle("Enable Name Obfuscation", config.EnableNameObfuscate);
            EditorGUI.indentLevel++;
            config.NameType = (ObfuscateNameType)EditorGUILayout.EnumPopup("Name Type", config.NameType);
            if (config.NameType == ObfuscateNameType.RandomString)
            {
                config.UnicodeStart = EditorGUILayout.IntField("Unicode Start (decimal)", config.UnicodeStart);
                config.UnicodeCharCountExponent = EditorGUILayout.IntField("Char Count Exponent (2^N)", config.UnicodeCharCountExponent);
                if (GUILayout.Button("Preview Unicode")) UpdateUnicodePreview();
                if (!string.IsNullOrEmpty(unicodePreview))
                    EditorGUILayout.HelpBox($"Preview: {unicodePreview}", MessageType.Info);
            }
            config.UseNameListFile = EditorGUILayout.Toggle("Use Word List File", config.UseNameListFile);
            if (config.UseNameListFile)
                config.NameListFilePath = EditorGUILayout.TextField("Word List Path", config.NameListFilePath);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawCodeInjectionSettings()
        {
            EditorGUILayout.LabelField("Code Injection", EditorStyles.boldLabel);
            config.EnableCodeInject = EditorGUILayout.Toggle("Enable Code Injection", config.EnableCodeInject);
            EditorGUI.indentLevel++;
            config.EnableJunkMethod = EditorGUILayout.Toggle("Enable Junk Methods", config.EnableJunkMethod);
            config.JunkMethodLinesPerClass = EditorGUILayout.IntField("Junk Lines Per Class", config.JunkMethodLinesPerClass);
            config.GarbageMethodMultiplePerClass = EditorGUILayout.IntField("Junk Multiplier", config.GarbageMethodMultiplePerClass);
            config.InsertMethodCountPerMethod = EditorGUILayout.IntField("Calls Per Method", config.InsertMethodCountPerMethod);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawAdvancedSettings()
        {
            EditorGUILayout.LabelField("Advanced Settings", EditorStyles.boldLabel);
            config.EnableCoroutineRedirect = EditorGUILayout.Toggle("Coroutine Redirect", config.EnableCoroutineRedirect);
            config.EnableInheritanceObfuscate = EditorGUILayout.Toggle("Inheritance Obfuscation", config.EnableInheritanceObfuscate);
            if (config.EnableInheritanceObfuscate)
                config.InheritanceDepth = EditorGUILayout.IntField("Inheritance Depth", config.InheritanceDepth);
            config.EnableEnumObfuscate = EditorGUILayout.Toggle("Enum Obfuscation", config.EnableEnumObfuscate);
            config.EnableSerializableObfuscate = EditorGUILayout.Toggle("Serializable Obfuscation", config.EnableSerializableObfuscate);
            config.ObfuscateMonoBehaviourClassName = EditorGUILayout.Toggle("Obfuscate MB Class Names", config.ObfuscateMonoBehaviourClassName);
            config.ObfuscateMonoBehaviourFields = EditorGUILayout.Toggle("Obfuscate MB Fields", config.ObfuscateMonoBehaviourFields);
            EditorGUILayout.Space();
        }

        private void DrawActionButtons()
        {
            if (GUILayout.Button("Save Config", GUILayout.Height(30)))
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                Debug.Log("Config saved.");
            }
            if (GUILayout.Button("Execute Protection", GUILayout.Height(30)))
                ExecuteProtection();
        }

        private void UpdateUnicodePreview()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int count = (int)Mathf.Pow(2, config.UnicodeCharCountExponent);
            for (int i = 0; i < Mathf.Min(20, count); i++)
                try { sb.Append(System.Convert.ToChar(config.UnicodeStart + i)); } catch { }
            unicodePreview = sb.ToString();
        }

        private void ExecuteProtection()
        {
            var protector = new Protector(config);
            string dllPath = Application.dataPath + "/../Library/ScriptAssemblies/Assembly-CSharp.dll";
            if (System.IO.File.Exists(dllPath))
            {
                protector.DoProtect(new string[] { dllPath });
                EditorUtility.DisplayDialog("Complete", "Protection completed!", "OK");
            }
            else
                EditorUtility.DisplayDialog("Error", "Assembly file not found.", "OK");
        }
    }
}