//ProtectorHelper.cs
using System;
using Random = System.Random;

namespace CodeProtector
{
    /// <summary>
    /// 代码保护器辅助工具类
    /// </summary>
    public static class ProtectorHelper
    {
        private static Random random;
        public static Random ProtectRandom => random;

        public static void Init(int seed)
        {
            random = new Random(seed);
        }

        public static string GetRandomString(int length = 8)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            char[] buffer = new char[length];
            for (int i = 0; i < length; i++)
                buffer[i] = chars[random.Next(chars.Length)];
            return new string(buffer);
        }

        public static int GetRandomInt(int min, int max) => random.Next(min, max);
        public static bool GetRandomBool() => random.Next(0, 2) == 1;
        public static T GetRandomElement<T>(T[] array) => array[random.Next(array.Length)];
    }
}