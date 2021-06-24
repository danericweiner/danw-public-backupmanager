using Google.Apis.Drive.v3;
using System;
using System.Collections.Generic;
using System.IO;

namespace BackupManagerLibrary
{
    public static class Constants
    {
        public const string ApplicationShortName = "DanW Backup";
        public const string ApplicationLongName = "DanW Backup Manager";

        public static class ProcessKeys
        {
            public const string BackupGoogleDocs = "BackupGoogleDocs";
            public const string BackupGoogleContacts = "BackupGoogleContacts";
            public const string BackupGooglePhotos = "BackupGooglePhotos";
            public const string BackupGoogleCalendar = "BackupGoogleCalendar";
            public const string LogToEmail = "LogToEmail";
        }

        public static class Folders
        {
            public static string UserProfile {
                get {
                    // UserProfile => Windows: c:\Users\{user}, MacOS: /Users/{user}
                    return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                }
            }

            public static string ApplicationData {
                get {
                    // LocalApplicationData => Windows: c:\Users\{user}\AppData\Local, MacOS: /Users/{user}/.local/share
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DanW", "Backup Manager");
                }
            }

            public static string ApplicationCredentials {
                get {
                    return Path.Combine(ApplicationData, "Credentials");
                }
            }

            public static string UserContacts(string userData) {
                return Path.Combine(userData, "Contacts");
            }

            public static string UserDocs(string userData) {
                return Path.Combine(userData, "Docs");
            }

            public static string UserCalendar(string userData) {
                return Path.Combine(userData, "Calendar");
            }

            public static string UserPhotos(string userData) {
                return Path.Combine(userData, "Photos");
            }
        }

        public static class Output
        {
            public const int EmailedMaximumPerProcessMessages = 200;
        }

        public static class Files
        {
            public static string UserSettings {
                get {
                    return Path.Combine(Folders.ApplicationData, "Settings", "UserSettings.json");
                }
            }            

            public static string ApplicationUserGoogleAccountToken {
                get {
                    return Path.Combine(Folders.ApplicationCredentials, "UserGoogleAccountToken");
                }
            }

            public static string UserGoogleContactsBackup(string userData) {
                return Path.Combine(Folders.UserContacts(userData), "GoogleContactsBackup.xml");
            }

            public static string UserGoogleCalendarBackup(string fileSuffix, string userData) {
                return Path.Combine(Folders.UserCalendar(userData), $"GoogleCalendarBackup{fileSuffix}.ics");
            }
        }

        public static class HttpClientOptions
        {
            public const int MaxConnectionsPerServer = 1000;
            public const int TimeOutSeconds = 30;
        }

        public static class GoogleAccess
        {
            public const string ApplicationName = "DanW Backup Manager";
            public const string ClientId = "";
            public const string ClientSecret = "";
            public static string ApiClientSecretJson { get; } = "";

            public static string[] Scopes { get; } = {DriveService.Scope.DriveReadonly,
                                                      "https://www.googleapis.com/auth/contacts.readonly",
                                                      "https://www.googleapis.com/auth/photoslibrary.readonly"};

            public static class Docs
            {
                public const string FileIdMarker = "@@FILE_ID@@";

                public struct ConversionInfo
                {
                    public string MimeType;
                    public string Extension;
                    public string DirectDownload; // handle files over 10MB
                }

                public static Dictionary<string, ConversionInfo> MimeTypeConversions { get; set; } = new Dictionary<string, ConversionInfo>() {
                    {"application/vnd.google-apps.document", new ConversionInfo(){ MimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document", Extension = ".docx", DirectDownload="https://docs.google.com/document/d/@@FILE_ID@@/export?format=doc" } },
                    {"application/vnd.google-apps.presentation", new ConversionInfo(){ MimeType = "application/vnd.openxmlformats-officedocument.presentationml.presentation", Extension = ".pptx", DirectDownload="https://docs.google.com/presentation/d/@@FILE_ID@@/export/pptx" }},
                    {"application/vnd.google-apps.script", new ConversionInfo(){ MimeType = "application/vnd.google-apps.script+json", Extension = ".json" }},
                    {"application/vnd.google-apps.spreadsheet", new ConversionInfo(){ MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Extension = ".xlsx", DirectDownload="https://docs.google.com/spreadsheets/d/@@FILE_ID@@/export?format=xlsx" }},
                    {"application/vnd.google-apps.drawing", new ConversionInfo(){ MimeType = "image/jpeg", Extension = ".jpg" }},
                };
            }

            public static class Photos
            {
                public const int DownloadedFileNameLength = 10;
                public const int MaximumFailuresBeforeFatal = 10;
            }
        }

        public static (string Variable, string Value)[] EnvironmentVariables {
            get {
                return new (string Variable, string Value)[] {
                    ("ApplicationData", Folders.ApplicationData),
                    ("UserProfile", Folders.UserProfile)
                };
            }
        }
    }
}