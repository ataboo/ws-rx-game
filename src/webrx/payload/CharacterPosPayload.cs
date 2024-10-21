using System.Text.Json.Serialization;

public class CharacterPosPayload {
    [JsonPropertyName("id")]
    public uint Id {get; set;}

    [JsonPropertyName("x")]
    public float XPos {get; set;}

    [JsonPropertyName("y")]
    public float YPos {get; set;}

    [JsonPropertyName("vx")]
    public float XVel {get; set;}

    [JsonPropertyName("vy")]
    public float YVel {get; set;}
}