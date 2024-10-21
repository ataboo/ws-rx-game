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
			GD.Print("Bad state: ", ws.GetReadyState());

			return Error.Busy;
		} else {
			GD.Print("State fine");
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
				GD.Print(msg);

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
		GD.Print(msg.PayloadStr);

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

	// public Error SendRequest(WSRequest request)
	// {
	//     if (!Joined)
	//     {
	//         return Error.Unavailable;
	//     }

	//     return SendMessage(request);
	// }

	// public Error SendGameEvent<TPayload>(string name, TPayload payload) where TPayload : class
	// {
	//     if (!Joined)
	//     {
	//         return Error.Unavailable;
	//     }

	//     var req = new WSRequest
	//     {
	//         type = RequestType.GameEvtReq,
	//         id = new Godot.Object().GetInstanceId().ToString(),
	//         name = name,
	//         send = System.DateTimeOffset.Now.ToUnixTimeMilliseconds(),
	//     };

	//     req.MarshalPayload(payload);

	//     return SendRequest(req);
	// }

	// public void WSDisconnect(bool sendDisconnect = true)
	// {
	//     if (sendDisconnect)
	//     {
	//         ws.DisconnectFromHost();
	//     }
	//     SetProcess(false);
	//     EmitSignal(nameof(OnDisconnect));
	//     Joined = false;
	// }

	// private void HandleConnected(string protocol)
	// {
	//     GD.Print("Connected");
	//     SendMessage(new WSJoinRequest
	//     {
	//         create = true,
	//         game_id = gameID,
	//         player_name = playerName,
	//         room_code = roomCode,
	//         room_size = 12
	//     });
	// }

	// private void HandleConnectionEnded(bool wasCleanClose)
	// {
	//     GD.Print("Connection ended");
	//     WSDisconnect(false);
	// }

	// private void HandleConnectionError()
	// {
	//     GD.Print("Connection error");
	//     WSDisconnect(false);
	// }

	// private void HandleDataReceived()
	// {
	//     var res = ReadMessage();
	//     if (res == null)
	//     {
	//         return;
	//     }

	//     if (responsesWithPlayers.Contains(res.type))
	//     {
	//         var playerPayload = res.ParsePayload<PlayerPayload>();
	//         PlayerNames = playerPayload.players.ToDictionary(p => p.id, p => p.name);

	//         if (res.type == ResponseType.YouJoinRes)
	//         {
	//             PlayerID = playerPayload.subject;
	//             EmitSignal(nameof(OnJoined));
	//             Joined = true;
	//         }

	//         EmitSignal(nameof(OnPlayerChange));
	//     }

	//     EmitSignal(nameof(OnResponse), res);
	// }

	// private Error SendMessage(object req)
	// {
	//     try
	//     {
	//         var msgBytes = JsonConvert.SerializeObject(req).ToUTF8();
	//         return ws.GetPeer(1).PutPacket(msgBytes);
	//     }
	//     catch (System.Exception e)
	//     {
	//         GD.PrintErr(e);
	//         return Error.Failed;
	//     }
	// }

	// private WSResponse ReadMessage()
	// {
	//     try
	//     {
	//         var msgJSON = ws.GetPeer(1).GetPacket().GetStringFromUTF8();
	//         return JsonConvert.DeserializeObject<WSResponse>(msgJSON);
	//     }
	//     catch (System.Exception e)
	//     {
	//         GD.PrintErr(e);
	//         return null;
	//     }
	// }

	// private (WSResponse response, TPayload payload) ReadMessage<TPayload>() where TPayload : class
	// {
	//     var response = ReadMessage();
	//     var payload = response.ParsePayload<TPayload>();

	//     return (response, payload);
	// }
}
