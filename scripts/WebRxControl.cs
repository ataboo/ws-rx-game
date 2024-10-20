using Godot;
using System;
using System.Linq;
using System.Text;

// For self-signed cert, you'll need to set the Certificate bundle override in Godot to the `cert.pem` used by the server.

public partial class WebRxControl : Node
{
	private WebSocketPeer ws;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Ready?");
		ConnectWS();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		ws.Poll();

		var state = ws.GetReadyState();

		if (state == WebSocketPeer.State.Open)
		{
			while (ws.GetAvailablePacketCount() > 0)
			{
				var packet = ws.GetPacket();
				GD.Print("Got packet");

				if(WSMessage.TryParse(packet, out var msg)) {
					GD.Print(msg);

					switch(msg.code) {
						case 1:
							this.SendJoin();
						break;
					}
				}

				// var sb = new StringBuilder();
				// foreach (var b in packet)
				// {
				// 	sb.Append(b + ", ");
				// }
				// GD.Print(sb.ToString());
			}
		}
	}

	public Error ConnectWS()
	{
		// if (Joined)
		// {
		//     return Error.AlreadyInUse;
		// }

		this.ws = new WebSocketPeer();

		// ws.VerifySsl = false;
		// ws.Connect("connection_established", this, nameof(HandleConnected));
		// ws.Connect("connection_error", this, nameof(HandleConnectionError));
		// ws.Connect("connection_closed", this, nameof(HandleConnectionEnded));
		// ws.Connect("data_received", this, nameof(HandleDataReceived));


		var err = ws.ConnectToUrl("wss://localhost:3000/ws", tlsClientOptions: TlsOptions.ClientUnsafe());
		if (err != Error.Ok)
		{
			GD.Print(err);
			SetProcess(false);
		}

		return Error.Ok;
	}

	private void SendJoin()
	{
		var payload = Encoding.UTF8.GetBytes(@"{""name"": ""ataboo"", ""room_code"": ""ABCDEF""}");

		var len = (byte)(payload.Length + 10);
		var testMsg = new byte[] { len, 0, 0, 0, 1, 0, 2, 0, 3, 0 };
		testMsg = testMsg.Concat(payload).ToArray();

		ws.Send(testMsg, WebSocketPeer.WriteMode.Binary);
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
