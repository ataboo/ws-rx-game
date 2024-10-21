using Godot;

public partial class RemoteGuyControl : RigidBody2D
{
	[Export]
	Label label;

	bool posSet = false;
	bool firstMove = false;
	private Vector2 _targetPos;
	public Vector2 TargetPos
	{
		get
		{
			return _targetPos;
		}
		set
		{
			_targetPos = value;
			posSet = true;
		}
	}

	public Vector2 TargetVel;

	private ushort _userId;
	private string _userName;

	public void SetIdAndName(ushort userId, string userName) {
		_userId = userId;
		_userName = userName;
		label.Text = $"{userName} ({userId})";
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public override void _IntegrateForces(PhysicsDirectBodyState2D state)
    {
		GD.Print($"{TargetVel.X}, {TargetVel.Y}");
		// state.LinearVelocity = TargetVel;

		var deltaPos = TargetPos - Position;

		var len = deltaPos.Length();

		state.ApplyForce(deltaPos / len * Mathf.Sqrt(len));
		

		Rotation = 0;
    }

    public override void _PhysicsProcess(double delta)
	{
		if (!posSet)
		{
			return;
		}

		if (!firstMove)
		{
			Position = TargetPos;
			firstMove = true;
			return;
		}

		// Rotation = 0;

		// LinearVelocity = TargetVel;

		// MoveAndCollide(LinearVelocity);

		// Position = Position.Lerp(TargetPos, 0.7f);
	}
}
