using System;
using System.Linq;
using System.Text;

public class WSMessage {
    public ushort version;

    public ushort code;

    public ushort sender;

    public byte[] payload;

    public string payloadStr;

    public WSMessage(ushort version, ushort code, ushort sender, byte[] payload) {
        this.version = version;
        this.code = code;
        this.sender = sender;
        this.payload = payload;
        this.payloadStr = Encoding.UTF8.GetString(payload);
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
		var payloadBytes = packet.Skip(10).ToArray();

        msg = new WSMessage(version, code, sender, payloadBytes);

        return true;
    }

    public override string ToString()
    {
        return $"version: {version}, code: {code}, sender: {sender}, payloadStr: {payloadStr}";
    }
}