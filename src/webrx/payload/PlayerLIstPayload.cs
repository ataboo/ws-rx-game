using System.Text.Json.Serialization;

public class PlayerlistPayload
{
    [JsonPropertyName("players")]
    public PlayerPayload[] Players { get; set; }
}