using System.Text.Json.Serialization;

namespace optimus.duat.service
{
    internal class DuatConfig
    {
        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        [JsonPropertyName("port")]
        public int? Port { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }
    }
}