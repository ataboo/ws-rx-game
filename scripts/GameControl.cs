using Godot;
using System;

public partial class GameControl : Node2D
{
	private WebRxControl webRX;

	public static WebRxControl WebRxInstance(Node node) { 
		return node.GetNode<WebRxControl>("/root/MainScene/WebRX");
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	webRX = WebRxInstance(this);

		webRX.WSConnect += () => SetPaused(false);
		webRX.WSDisconnect += () => SetPaused(true);

		SetPaused(true);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void SetPaused(bool paused) {
		GetTree().Paused = paused;
	}
}
