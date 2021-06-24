using System.Diagnostics;

namespace BackupManagerXamarin
{
    static class MainClass
    {
        static void Main(string[] args) {
            using Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = Constants.Files.AppBinary;
            p.Start();
        }
    }
}
