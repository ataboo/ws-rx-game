using Godot;

public partial class PlayerControl : RigidBody2D
{
	[Export]
	Label label;

	[Export]
	Timer timer;

	public float speed = 400;

	private WebRxControl _webRx;
	private Vector2 _movement = Vector2.Zero;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_webRx = GameControl.WebRxInstance(this);

		_webRx.WSJoined += () => {
			SetName(_webRx.UserName, _webRx.UserId);
		};

		timer.Timeout += TimerTick;
		timer.OneShot = false;
		timer.Start(0.5f);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_movement = Vector2.Zero;

		if(Input.IsActionPressed("MoveRight")) {
			_movement += new Vector2(1, 0);
		}
		if(Input.IsActionPressed("MoveLeft")) {
			_movement += new Vector2(-1, 0);
		}
		if(Input.IsActionPressed("MoveUp")) {
			_movement += new Vector2(0, -1);
		}
		if(Input.IsActionPressed("MoveDown")) {
			_movement += new Vector2(0, 1);
		}

		_movement = _movement.Clamp(-1, 1);
	}

    public override void _IntegrateForces(PhysicsDirectBodyState2D state)
    {
		state.ApplyForce(_movement * speed);
		Rotation = 0;
    }

    public override void _PhysicsProcess(double delta)
    {
		// ApplyForce(_movement * speed);
		// Rotation = 0;
    }

	private void SetName(string name, ushort id) {
		label.Text = $"{name} ({id})";
	}

	private void TimerTick() {
		var pld = new CharacterPosPayload(){
			Id = _webRx.UserId,
			XPos = Position.X,
			YPos = Position.Y,
			XVel = LinearVelocity.X,
			YVel = LinearVelocity.Y,
		};
		
		_webRx.SendMsg(WSPayloadId.CharacterPos, WSMessageCode.BroadcastOthers, pld);
	}
}
