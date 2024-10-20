using System.Text.Json.Serialization;

public class WelcomePayload {
    [JsonPropertyName("user_id")]
    public ushort UserId {get; set;}
}