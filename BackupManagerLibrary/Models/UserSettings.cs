using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace BackupManagerLibrary.Models
{
    public class UserSettings
    {
        public string UserData { get; set; }
        public EmailSettings EmailSettings { get; set; }
        public ActionItem[] Actions { get; set; }

        public ActionItem GetActionItemByKey(string key) {
            key = key.Trim().ToLower();
            foreach (ActionItem actionItem in Actions) {
                if (actionItem.Key.ToLower() == key) {
                    return actionItem;
                }
            }
            return null;
        }

        public static UserSettings GetDefault() {
            return new UserSettings() {
                UserData = Path.Combine("%UserProfile%", "DanW", "Backup Manager"),
                EmailSettings = new EmailSettings() {
                    ToAddress = "",
                    FromAddress = "",
                    FromName = "DanW Backup",
                    SmtpServer = "",
                    SmtpPort = 587,
                    SmtpUserName = "",
                    SmtpPassword = ""
                },
                Actions = new ActionItem[]{
                    new ActionItem() {
                        Key = "LocalGoogleCloudBackup",
                        Name = "Local Google Cloud Backup",
                        CronSchedule = "0 23 * * *", // At 11pm every day
                        RunNow = false,
                        Processes = new ProcessItem[] {
                            new ProcessItem() {
                                Key = Constants.ProcessKeys.BackupGoogleDocs,
                                Name = "Backup Google Docs to Local Machine",
                                Args = new GoogleDocsArgs(){
                                    OnlyOwnedByMe = true,
                                }
                            },
                            new ProcessItem() {
                                Key = Constants.ProcessKeys.BackupGoogleContacts,
                                Name = "Backup Google Contacts to Local Machine",
                                Args = new GoogleContactsArgs()
                            },
                            new ProcessItem() {
                                Key = Constants.ProcessKeys.BackupGooglePhotos,
                                Name = "Backup Google Photos to Local Machine",
                                Args = new GooglePhotosArgs()
                            },
                            new ProcessItem() {
                                Key = Constants.ProcessKeys.BackupGoogleCalendar,
                                Name = "Backup Google Calendar to Local Machine",
                                Args = new GoogleCalendarArgs() {
                                    Calendars  = new GoogleCalendarArgs.CalendarInfo[] {
                                        new GoogleCalendarArgs.CalendarInfo(){
                                            FileSuffix = "",
                                            SecretAddress = ""
                                        }
                                    }
                                }
                            },
                            new ProcessItem() {
                                Key = Constants.ProcessKeys.LogToEmail,
                                Name = "Log Session to Email",
                                Args = new EmailArgs()
                            }
                        }
                    }
                }
            };
        }
    }

    public class EmailSettings
    {
        public string ToAddress { get; set; }
        public string FromAddress { get; set; }
        public string FromName { get; set; }
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUserName { get; set; }
        public string SmtpPassword { get; set; }
    }

    public class ActionItem
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string CronSchedule { get; set; }
        public bool RunNow { get; set; }
        public ProcessItem[] Processes { get; set; }

        public ProcessItem GetProcessItemByKey(string key) {
            key = key.Trim().ToLower();
            foreach (ProcessItem processItem in Processes) {
                if (processItem.Key.ToLower() == key) {
                    return processItem;
                }
            }
            return null;
        }
    }

    [JsonConverter(typeof(ProcessItemConverter))]
    public class ProcessItem
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public ProcessArgs Args { get; set; }
        public bool Skip { get; set; } = false;

        public TArgsType GetArguments<TArgsType>() where TArgsType : ProcessArgs {
            return Args as TArgsType;
        }
    }

    // base class
    public class ProcessArgs
    {
    }

    public class GoogleDocsArgs : ProcessArgs
    {
        public bool OnlyOwnedByMe { get; set; }
    }

    public class GoogleContactsArgs : ProcessArgs
    {
    }

    public class GooglePhotosArgs : ProcessArgs
    {
    }
    
    public class GoogleCalendarArgs : ProcessArgs
    {
        public CalendarInfo[] Calendars { get; set; }

        public class CalendarInfo
        {
            public string FileSuffix { get; set; }            
            public string SecretAddress { get; set; }  //https://support.google.com/calendar/answer/37648?hl=en
        }
    }

    public class EmailArgs : ProcessArgs
    {
    }

    //https://skrift.io/articles/archive/bulletproof-interface-deserialization-in-jsonnet/
    public class ProcessItemConverter : JsonConverter
    {
        public override bool CanWrite => false;
        public override bool CanRead => true;
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(ProcessItem);
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            throw new InvalidOperationException("Use default serialization.");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            JObject jsonObject = JObject.Load(reader);
            ProcessItem processItem = new ProcessItem();
            switch (jsonObject["Key"].ToString().ToLower()) {
                case string key when key == Constants.ProcessKeys.BackupGoogleDocs.ToLower(): processItem.Args = new GoogleDocsArgs(); break;
                case string key when key == Constants.ProcessKeys.BackupGoogleContacts.ToLower(): processItem.Args = new GoogleContactsArgs(); break;
                case string key when key == Constants.ProcessKeys.BackupGooglePhotos.ToLower(): processItem.Args = new GooglePhotosArgs(); break;
                case string key when key == Constants.ProcessKeys.BackupGoogleCalendar.ToLower(): processItem.Args = new GoogleCalendarArgs(); break;
                case string key when key == Constants.ProcessKeys.LogToEmail.ToLower(): processItem.Args = new EmailArgs(); break;
            }
            serializer.Populate(jsonObject.CreateReader(), processItem);
            return processItem;
        }
    }
}
