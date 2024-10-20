using System.Text.Json.Serialization;

public class PlayerPayload
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("id")]
    public ushort ID { get; set; }
}