using System.Text.Json.Serialization;

public class JoinPayload {
    [JsonPropertyName("name")]
    public string Name {get; set;}

    [JsonPropertyName("room_code")]
    public string RoomCode {get; set;}
}