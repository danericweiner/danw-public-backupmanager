using System;
using System.IO;

namespace BackupManagerLibrary
{
    public static class StringExtensions
    {
        public static string Left(this string str, int length) {
            return str.Substring(0, Math.Min(length, str.Length));
        }

        public static string Right(this string str, int length) {
            return str.Substring(str.Length - Math.Min(length, str.Length));
        }

        //https://stackoverflow.com/questions/1879395/how-do-i-generate-a-stream-from-a-string
        public static Stream ToStream(this string str) {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
