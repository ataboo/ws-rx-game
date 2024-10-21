using Godot;
using CSCol = System.Collections.Generic;
using System.Text.Json;
using System.Linq;

public partial class RemotePlayers : Node2D
{
	public WebRxControl _webRX;

	[Export]
	public PackedScene remoteGuyPrefab;

	private CSCol.Dictionary<uint, RemoteGuyControl> remoteGuyInstances = new CSCol.Dictionary<uint, RemoteGuyControl>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_webRX = GameControl.WebRxInstance(this);
		_webRX.WSPayloadReceived += HandlePayload;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void HandlePayload(int payloadId, string payloadStr)
	{		
		switch((WSPayloadId)payloadId) {
			case WSPayloadId.CharacterPos:
				HandlePosChange(payloadStr);
				break;
			case WSPayloadId.PlayerChange:
				HandlePlayerChange(payloadStr);
				break;
		}

		if(payloadId == (int)WSPayloadId.CharacterPos) {
			HandlePosChange(payloadStr);
		} else if (payloadId == (int)WSPayloadId.PlayerChange) {
			HandlePlayerChange(payloadStr);
		}
	}

	private void HandlePosChange(string payloadStr) {
		var pld = JsonSerializer.Deserialize<CharacterPosPayload>(payloadStr);
		if(!remoteGuyInstances.ContainsKey(pld.Id)) {
			return;
		}

		var guy = remoteGuyInstances[pld.Id];

		guy.TargetPos = new Vector2(pld.XPos, pld.YPos);
		guy.TargetVel = new Vector2(pld.XVel, pld.YVel);
		guy.Rotation = 0;
	}

	private void HandlePlayerChange(string payloadStr) {
		var pld = JsonSerializer.Deserialize<PlayerlistPayload>(payloadStr);

		var newPlayers = pld.Players.Where(p => p.ID != _webRX.UserId && !remoteGuyInstances.ContainsKey(p.ID));
		foreach(var p in newPlayers) {
			var newGuy = remoteGuyPrefab.Instantiate<RemoteGuyControl>();
			remoteGuyInstances.Add(p.ID, newGuy);
			
			AddChild(newGuy);
			
			newGuy.SetIdAndName(p.ID, p.Name);
		}
	}
}
