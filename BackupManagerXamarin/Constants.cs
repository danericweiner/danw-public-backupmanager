using System.IO;

namespace BackupManagerXamarin
{
    public static class Constants
    {        
        public static class Files
        {
            public static string AppBinary {
                get {
                    return Path.Combine(Directory.GetCurrentDirectory(), "DanW Backup Manager");
                }
            }
        }
    }
}
