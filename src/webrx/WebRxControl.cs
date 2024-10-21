using Godot;
using System.Text.Json;

// For self-signed cert, you'll need to set the Certificate bundle override in Godot to the `cert.pem` used by the server.

public partial class WebRxControl : Node
{
	[Signal]
	public delegate void WSConnectEventHandler();

	[Signal]
	public delegate void WSDisconnectEventHandler();

	[Signal]
	public delegate void WSJoinedEventHandler();

	[Signal]
	public delegate void WSPayloadReceivedEventHandler(int payloadId, string payloadStr);

	private WebSocketPeer ws;

	private WebSocketPeer.State _wsState = WebSocketPeer.State.Closed;
	public WebSocketPeer.State WSState => _wsState;

	private string lastConnectUrl;
	private TlsOptions lastTlsOptions;
	private string lastUserName;
	private string lastRoomCode;
	private ushort userId;
	private WebSocketPeer.State lastState;

	public ushort UserId => userId;
	public string UserName => lastUserName;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		UpdateWS();
	}

	public Error ConnectWS(string url, TlsOptions tlsClientOptions, string userName, string roomCode)
	{
		ws = new WebSocketPeer();
		lastState = WebSocketPeer.State.Connecting;

		if(ws.GetReadyState() != WebSocketPeer.State.Closed) {
			return Error.Busy;
		}

		lastConnectUrl = url;
		lastTlsOptions = tlsClientOptions;
		lastUserName = userName;
		lastRoomCode = roomCode;

		var err = ws.ConnectToUrl(url, tlsClientOptions);
		if (err != Error.Ok)
		{
			EmitSignal(SignalName.WSDisconnect);
			return err;
		}

		return Error.Ok;
	}

	public void SendMsg<T>(WSPayloadId pldId, WSMessageCode code, T payload) {
		var pldString = JsonSerializer.Serialize(payload);
		var msgBytes = new WSMessage(1, code, userId, pldId, pldString).Marshal();

		ws.Send(msgBytes, WebSocketPeer.WriteMode.Binary);
	}

	private void UpdateWS()
	{
		if(ws == null) {
			return;
		}

		ws.Poll();

		var state = ws.GetReadyState();
		var stateChanged = state != lastState;
		lastState = state;

		if(stateChanged) {
			GD.Print($"WebRX changed state: {lastState} => {state}");
			switch(state) {
				case WebSocketPeer.State.Closed:
					EmitSignal(SignalName.WSDisconnect);
					break;
				case WebSocketPeer.State.Open:
					EmitSignal(SignalName.WSConnect);
					break;
			}
		}

		switch (state)
		{
			case WebSocketPeer.State.Open:
				ProcessPackets();
				break;
			case WebSocketPeer.State.Connecting:
			case WebSocketPeer.State.Closing:
			case WebSocketPeer.State.Closed:
				break;
		}
	}

	private void ProcessPackets()
	{
		while (ws.GetAvailablePacketCount() > 0)
		{
			var packet = ws.GetPacket();
			if (WSMessage.TryParse(packet, out var msg))
			{
				switch (msg.code)
				{
					case WSMessageCode.Welcome:
						HandleWelcome(msg);
						break;
					case WSMessageCode.Broadcast:
					case WSMessageCode.BroadcastOthers:
						HandleBroadcast(msg);
						break;
				}
			}
			else
			{
				GD.PrintErr("Failed to parse packet");
			}
		}
	}

	private void HandleWelcome(WSMessage msg)
	{
		try
		{
			var welcome = JsonSerializer.Deserialize<WelcomePayload>(msg.PayloadStr);
			userId = welcome.UserId;

			GD.Print($"Got id {this.userId}");

			var err = SendJoin();
			if(err != Error.Ok) {
				GD.PrintErr("Failed to send join");
				return;
			}

			EmitSignal(SignalName.WSJoined);
		}
		catch
		{
			GD.PrintErr("Failed to parse welcome");
			return;
		}



	}

	private void HandleBroadcast(WSMessage msg) {
		EmitSignal(SignalName.WSPayloadReceived, new Variant[] {(int)msg.payloadId, msg.PayloadStr});

		switch(msg.payloadId) {
			case WSPayloadId.PlayerChange:
				var pld = msg.DeserializePayload<PlayerlistPayload>();
				foreach(var p in pld.Players) {
					GD.Print($"Player: '{p.Name}'");
				}
				
				break;

		}
	}

	private Error SendJoin()
	{
		var payload = new JoinPayload {
			Name = lastUserName,
			RoomCode = lastRoomCode,
		};

		WSMessage.TryEncode(1, WSMessageCode.Join, userId, WSPayloadId.Join, payload, out var bytes);

		return ws.Send(bytes, WebSocketPeer.WriteMode.Binary);
	}
}
