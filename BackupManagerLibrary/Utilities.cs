using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BackupManagerLibrary
{
    public static class Utilities
    {
        //https://stackoverflow.com/questions/17292366/hashing-with-sha1-algorithm-in-c-sharp
        public static string Sha1HashString(string input, int truncateLength = 0) {
            using (SHA1Managed sha1Managed = new SHA1Managed()) {
                byte[] hashBytes = sha1Managed.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder sb = new StringBuilder(hashBytes.Length * 2);
                foreach (byte b in hashBytes) {
                    sb.Append(b.ToString("x2")); // "X2" for uppercase
                }
                string hashedString = sb.ToString();
                if(truncateLength > 0) {
                    return hashedString.Left(truncateLength);
                } else {
                    return hashedString;
                }
            }
        }

        public static void KillExistingProcesses() {
            const int dotnetTruncatedChars = 15; // should be able to use GetProcessesByName but there's a bug
            const int killWaitSeconds = 5; // wait for 5 seconds then fail
            Process currentProcess = Process.GetCurrentProcess();
            string currentProcessNameTruncated = currentProcess.ProcessName.Left(dotnetTruncatedChars);
            Process[] processes = Process.GetProcesses().Where(p => p.ProcessName.Left(dotnetTruncatedChars) == currentProcessNameTruncated).ToArray();
            foreach (Process process in processes) {
                if (process.HasExited || process.Id == currentProcess.Id) { continue; }
                process.Kill();
                if (!process.WaitForExit(killWaitSeconds * 1000)) {
                    throw new Exception($"Unable to kill an existing proces with id {process.Id} after {killWaitSeconds} seconds");
                }
            }
        }

        public static string SubstituteEnvironmentVariables(string input) {
            if (input == null) { return input; }
            return Regex.Replace(input, "%([^=%]+)%", delegate (Match match) {
                string variable = match.Groups[1].Value;
                string value = Environment.GetEnvironmentVariable(variable);
                if (value == null) { return match.Value; }
                return value;
            });
        }

        public static void UpdateObjectProperties<ObjectType, PropertyType>(ObjectType obj, Func<PropertyType, PropertyType> transform, bool recursive = true) {
            typeof(ObjectType).GetProperties()
                            .Where(p => p.PropertyType == typeof(PropertyType))
                            .ToList()
                            .ForEach(c => c.SetValue(obj, transform((PropertyType)c.GetValue(obj))));
            if (recursive) {
                // update enumerable objects
                // Note: This will NOT update an array of non-objects, e.g. ["test1","test2"]
                typeof(ObjectType).GetProperties()
                            .Where(p => p.PropertyType.IsClass && p.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(p.PropertyType))
                            .ToList()
                            .ForEach(c => {
                                if (c.GetValue(obj) == null) { return; }
                                foreach (var item in (IEnumerable)c.GetValue(obj)) {
                                    //https://stackoverflow.com/questions/232535/how-do-i-use-reflection-to-call-a-generic-method
                                    MethodInfo method = typeof(Utilities).GetMethod(nameof(UpdateObjectProperties));
                                    MethodInfo generic = method.MakeGenericMethod(item.GetType(), typeof(PropertyType));
                                    generic.Invoke(null, new object[] { item, transform, recursive });
                                }
                            });
                // update non-enumarable objects
                typeof(ObjectType).GetProperties()
                            .Where(p => p.PropertyType.IsClass && p.PropertyType != typeof(string) && !typeof(IEnumerable).IsAssignableFrom(p.PropertyType))
                            .ToList()
                            .ForEach(c => {
                                if (c.GetValue(obj) == null) { return; }
                                MethodInfo method = typeof(Utilities).GetMethod(nameof(UpdateObjectProperties));
                                MethodInfo generic = method.MakeGenericMethod(c.GetValue(obj).GetType(), typeof(PropertyType));
                                generic.Invoke(null, new object[] { c.GetValue(obj), transform, recursive });
                            });
            }
        }
    }
}
