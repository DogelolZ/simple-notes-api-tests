using System.Text.Json.Serialization;

namespace SimpleNotesApiTests.DTOs;

public class ApiResponseDto
{
    [JsonPropertyName("msg")]
    public string Msg { get; set; } = string.Empty;
}
