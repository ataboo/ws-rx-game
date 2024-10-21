using Godot;

public partial class ConnectScreen : CanvasLayer
{
	[Export]
	public LineEdit userNameInput;

	[Export]
	public LineEdit roomInput;

	[Export]
	public Button joinButton;

	[Export]
	public Label errorLabel;

	[Export]
	public WebRxControl webRX;

    public override void _Ready()
    {
		webRX.WSDisconnect += () => HandleDisconnect();
		webRX.WSConnect += () => HandleConnect();
		joinButton.Pressed += () => HandleConnectPush();
    }

    public void HandleConnect() {
		this.Visible = false;
		EnableForm(true);
	}

	public void HandleDisconnect() {
		GD.Print("Disconnect");
		this.Visible = true;
		EnableForm(true);
		errorLabel.Visible = true;
		errorLabel.Text = "Failed to connect";
	}

	public void HandleConnectPush() {
		EnableForm(false);
		errorLabel.Visible = false;

		roomInput.Text = roomInput.Text.ToUpper();
		
		webRX.ConnectWS("wss://localhost:3000/ws", TlsOptions.ClientUnsafe(), userNameInput.Text, roomInput.Text);
	}

	private void EnableForm(bool enabled) {
		userNameInput.Editable = enabled;
		roomInput.Editable = enabled;
		joinButton.Disabled = !enabled;
	}
}
