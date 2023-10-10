using System.Text.Json.Serialization;

namespace optimus.duat.lib.model
{
    public class Document
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("document_type_id")]
        public ulong DocumentTypeId { get; set; } = default;
        [JsonPropertyName("department_id")]
        public ulong DepartmentId { get; set; } = default;
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
        [JsonPropertyName("identity")]
        public string Identity { get; set; } = string.Empty;
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("comment")]
        public string Comment { get; set; } = string.Empty;
        [JsonPropertyName("storage")]
        public string Storage { get; set; } = string.Empty;
        [JsonPropertyName("date_document")]
        public string DateDocument { get; set; } = string.Empty;
        [JsonPropertyName("is_done")]
        public bool IsDone { get; set; } = false;
        [JsonPropertyName("uploaded")]
        public uint Uploaded { get; set; } = default;
        [JsonPropertyName("images")]
        public DocumentImage[] Images { get; set; } = Array.Empty<DocumentImage>();
        [JsonPropertyName("error")]
        public ErrorReply Error { get; set; } = new();
    }
}
