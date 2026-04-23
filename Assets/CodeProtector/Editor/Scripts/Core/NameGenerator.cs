//NameGenerator.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Random = System.Random;

namespace CodeProtector
{
    public class NameGenerator
    {
        private Random random;
        private ProtectorConfig config;
        private int counter = 0;
        private HashSet<string> usedNames = new HashSet<string>();
        private int unicodeStart;
        private int characterCount;
        private List<char> availableChars = new List<char>();
        private List<string> nameWordList = new List<string>();
        private bool useWordList = false;

        public NameGenerator(ProtectorConfig config)
        {
            this.config = config;
            int seed = config.UseTimeStamp ? (int)DateTime.Now.Ticks : config.RandomSeed;
            random = new Random(seed);
            unicodeStart = config.UnicodeStart;
            characterCount = (int)Math.Pow(2, config.UnicodeCharCountExponent);
            InitializeUnicodeChars();
            useWordList = config.UseNameListFile && !string.IsNullOrEmpty(config.NameListFilePath);
            if (useWordList) LoadNameList(config.NameListFilePath);
        }

        private void InitializeUnicodeChars()
        {
            for (int i = 0; i < characterCount; i++)
            {
                try { availableChars.Add(Convert.ToChar(unicodeStart + i)); } catch { }
            }
        }

        private void LoadNameList(string relativePath)
        {
            try
            {
                string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
                if (!File.Exists(fullPath)) { useWordList = false; return; }
                foreach (string line in File.ReadAllLines(fullPath))
                {
                    string word = line.Trim();
                    if (!string.IsNullOrEmpty(word) && !word.StartsWith("#"))
                        nameWordList.Add(word);
                }
                if (nameWordList.Count == 0) useWordList = false;
            }
            catch { useWordList = false; }
        }

        public string GenerateClassName() => GenerateName("C_", 8, 16);
        public string GenerateMethodName() => GenerateName("M_", 6, 12);
        public string GenerateFieldName() => GenerateName("F_", 6, 12);
        public string GeneratePropertyName() => GenerateName("P_", 6, 12);
        public string GenerateJunkMethodName() => GenerateName("Junk_", 8, 16);
        public string GenerateJunkVariableName() => GenerateName("v_", 4, 8);
        public string GenerateInheritanceClassName(string baseName) => GenerateName($"I{baseName.Substring(0, Math.Min(3, baseName.Length))}_", 8, 12);

        private string GenerateName(string prefix, int minLen, int maxLen)
        {
            string name;
            int attempts = 0;
            do
            {
                if (useWordList)
                {
                    string baseWord = nameWordList[random.Next(nameWordList.Count)];
                    name = $"{prefix}{baseWord}_{counter++}";
                }
                else if (config.NameType == ObfuscateNameType.RandomString)
                    name = GenerateRandomUnicodeString(minLen, maxLen);
                else
                    name = $"{prefix}{counter++}";
                attempts++;
            } while (usedNames.Contains(name) && attempts < 1000);
            usedNames.Add(name);
            return name;
        }

        private string GenerateRandomUnicodeString(int minLen, int maxLen)
        {
            int length = random.Next(minLen, maxLen + 1);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
                sb.Append(availableChars.Count > 0 ? availableChars[random.Next(availableChars.Count)] : (char)random.Next(65, 90));
            return sb.ToString();
        }

        public int GetRandomInt(int min, int max) => random.Next(min, max);
        public double GetRandomDouble() => random.NextDouble();
        public bool GetRandomBool() => random.Next(0, 2) == 1;
    }
}