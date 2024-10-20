using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Godot;

public class WSMessage {
    public ushort version;

    public WSMessageCode code;

    public ushort sender;

    public WSPayloadId payloadId;

    public byte[] payload;

    public string PayloadStr {get; private set;}

    public WSMessage(ushort version, WSMessageCode code, ushort sender, WSPayloadId payloadId, byte[] payload) {
        this.version = version;
        this.code = code;
        this.sender = sender;
        this.payload = payload;
        this.payloadId = payloadId;
        this.PayloadStr = Encoding.UTF8.GetString(payload);

        if(!BitConverter.IsLittleEndian) {
            GD.PushError("Big endian not supported");
        }
    }

    public WSMessage(ushort version, WSMessageCode code, ushort sender, WSPayloadId payloadId, string payloadStr) {
        this.version = version;
        this.code = code;
        this.sender = sender;
        this.payloadId = payloadId;
        this.PayloadStr = payloadStr;
        this.payload = Encoding.UTF8.GetBytes(payloadStr);

        if(!BitConverter.IsLittleEndian) {
            GD.PushError("Big endian not supported");
        }
    }

    public static bool TryEncode<T>(ushort version, WSMessageCode code, ushort sender, WSPayloadId payloadId, T payload, out byte[] bytes) {
        var payloadStr = JsonSerializer.Serialize(payload);
        var msg = new WSMessage(version, code, sender, payloadId, payloadStr);
        bytes = msg.Marshal();

        return true;
    }

    public static bool TryParse(byte[] packet, out WSMessage msg) {
        msg = null;
        
        var length = BitConverter.ToUInt32(packet, 0);
        if(packet.Length != length) {
            return false;
        }

		var version = BitConverter.ToUInt16(packet, 4);
		var code = BitConverter.ToUInt16(packet, 6);
		var sender = BitConverter.ToUInt16(packet, 8);
        var payloadId = BitConverter.ToUInt16(packet, 10);
		var payloadBytes = packet.Skip(12).ToArray();

        if(!Enum.IsDefined(typeof(WSMessageCode), code)) {
            return false;
        }

        if(!Enum.IsDefined(typeof(WSPayloadId), payloadId)) {
            return false;
        }

        msg = new WSMessage(version, (WSMessageCode)code, sender, (WSPayloadId)payloadId, payloadBytes);

        return true;
    }

    public T DeserializePayload<T>() {
        return JsonSerializer.Deserialize<T>(PayloadStr);
    }

    public void SetPayload<T>(T payload) {
        var serialized = JsonSerializer.Serialize(payload);
        PayloadStr = serialized;
    }

    public byte[] Marshal() {
        var length = (uint)(12 + PayloadStr.Length);

        var bytes = new byte[length];

        BitConverter.GetBytes(length).CopyTo(bytes, 0);
        BitConverter.GetBytes(version).CopyTo(bytes, 4);
        BitConverter.GetBytes((ushort)code).CopyTo(bytes, 6);
        BitConverter.GetBytes(sender).CopyTo(bytes, 8);
        BitConverter.GetBytes((ushort)payloadId).CopyTo(bytes, 10);
        Encoding.UTF8.GetBytes(PayloadStr).CopyTo(bytes, 12);

        return bytes;
    }

    public override string ToString()
    {
        return $"version: {version}, code: {code}, sender: {sender}, payloadStr: {PayloadStr}";
    }
}