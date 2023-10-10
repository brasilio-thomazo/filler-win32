using System.Text.Json.Serialization;
using Grpc.Core;
namespace optimus.duat.lib.model
{
    public class ErrorReply
    {
        [JsonPropertyName("detail")]
        public string Detail { get; set; } = string.Empty;

        [JsonPropertyName("status_code")]
        public StatusCode StatusCode { get; set; } = StatusCode.OK;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}