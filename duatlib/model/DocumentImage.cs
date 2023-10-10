using System.Text.Json.Serialization;
using Duat;
namespace optimus.duat.lib.model
{
    public class DocumentImage
    {
        [JsonPropertyName("document_id")]
        public string DocumentId { get; set; } = string.Empty;

        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("storage_type")]
        public StorageType StorageType { get; set; } = StorageType.Local;

        [JsonPropertyName("started_at")]
        public long StartedAt { get; set; } = default;

        [JsonPropertyName("completed_at")]
        public long CompletedAt { get; set; } = default;

        [JsonPropertyName("running")]
        public bool Running { get; set; } = false;

        [JsonPropertyName("error")]
        public ErrorReply Error { get; set; } = new();
    }
}