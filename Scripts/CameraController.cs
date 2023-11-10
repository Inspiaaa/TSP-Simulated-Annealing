using Godot;

public partial class CameraController : Camera2D
{
	[Export] private float zoomAmount = 1.1f;

	private Vector2 lastPanMousePos;

	public override void _UnhandledInput(InputEvent e)
	{
		if (e.IsActionPressed("camera_zoom_in"))
		{
			Zoom *= zoomAmount;
		}

		if (e.IsActionPressed("camera_zoom_out"))
		{
			Zoom /= zoomAmount;
		}
	}

	public override void _Process(double delta)
	{
		Vector2 mousePos = GetLocalMousePosition();
		if (Input.IsActionJustPressed("camera_pan"))
		{
			lastPanMousePos = mousePos;
		}

		if (Input.IsActionPressed("camera_pan"))
		{
			Position -= mousePos - lastPanMousePos;
			lastPanMousePos = mousePos;
		}
	}
}
