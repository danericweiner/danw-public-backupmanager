using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

//https://developers.google.com/photos/library/reference/rest/v1/mediaItems
namespace BackupManagerGoogle.Models.Photos
{
    public class MediaItemsList
    {
        [JsonProperty("mediaItems")]
        public MediaItem[] MediaItems { get; set; }

        [JsonProperty("nextPageToken")]
        public string NextPageToken { get; set; }
    }


    public class MediaItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("productUrl")]
        public string ProductUrl { get; set; }

        [JsonProperty("baseUrl")]
        public string BaseUrl { get; set; }

        [JsonProperty("mimeType")]
        public string MimeType { get; set; }

        [JsonProperty("mediaMetadata")]
        public Mediametadata MediaMetadata { get; set; }

        [JsonProperty("contributorInfo")]
        public Contributorinfo ContributorInfo { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }
    }


    public class Mediametadata
    {
        [JsonProperty("creationTime")]
        public string CreationTime { get; set; }

        [JsonProperty("width")]
        public string Width { get; set; }

        [JsonProperty("height")]
        public string Height { get; set; }

        [JsonProperty("photo")]
        public Photo Photo { get; set; }

        [JsonProperty("video")]
        public Video Video { get; set; }
    }

    public class Photo
    {
        [JsonProperty("cameraMake")]
        public string CameraMake { get; set; }

        [JsonProperty("cameraModel")]
        public string CameraModel { get; set; }

        [JsonProperty("focalLength")]
        public double FocalLength { get; set; }

        [JsonProperty("apertureFNumber")]
        public double ApertureFNumber { get; set; }

        [JsonProperty("isoEquivalent")]
        public double IsoEquivalent { get; set; }

        [JsonProperty("exposureTime")]
        public string ExposureTime { get; set; }
    }

    public class Video
    {
        [JsonProperty("cameraMake")]
        public string CameraMake { get; set; }

        [JsonProperty("cameraModel")]
        public string CameraModel { get; set; }

        [JsonProperty("fps")]
        public double Fps { get; set; }

        [JsonProperty("status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public VideoStatus Status { get; set; }
    }

    public enum VideoStatus
    {
        [EnumMember(Value = "UNSPECIFIED")]
        Unspecified,

        [EnumMember(Value = "PROCESSING")]
        Processing,

        [EnumMember(Value = "READY")]
        Ready,

        [EnumMember(Value = "FAILED")]
        Failed
    }

    public class Contributorinfo
    {
        [JsonProperty("profilePictureBaseUrl")]
        public string ProfilePictureBaseUrl { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }


}
