using BackupManagerLibrary.Models;
using Newtonsoft.Json;
using System;
using System.IO;

namespace BackupManagerLibrary
{
    public class IOUtilities
    {
        public static void WriteStreamToFile(Stream stream, string path, DateTime? lastWriteTime = null) {
            string folder = Path.GetDirectoryName(path);
            if (!Directory.Exists(folder)) { Directory.CreateDirectory(folder); }
            using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write)) {
                if (stream.CanSeek) { stream.Position = 0; }
                stream.CopyTo(fileStream);
            }
            if (lastWriteTime != null) {
                File.SetLastWriteTime(path, (DateTime)lastWriteTime);
            }
        }

        public static void WriteObjectJson<Model>(Model model, string path, bool overwrite) {
            try {
                if (File.Exists(path)) {
                    if (overwrite) {
                        File.Delete(path);
                    } else {
                        return;
                    }
                }
                string formattedJson = JsonConvert.SerializeObject(model, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, formattedJson);
            } catch (Exception ex) {
                throw new Exception($"Exception in WriteDefaultObjectJson. {ex.Message}");
            }
        }
    }
}
