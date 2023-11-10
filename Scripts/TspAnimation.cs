using Godot;
using System;

namespace TspSolver;

public partial class TspAnimation : Node2D
{
	private Vector2[] originalRoute;
	private Vector2[] greedyRoute;

	private SimulatedAnnealing solver;

	private TspRenderer simulatedAnnealingRouteRenderer;
	private TspRenderer originalRouteRenderer;
	private TspRenderer greedyRouteRenderer;
	private TspRenderer cityRenderer;

	private float elapsedTimeSinceLastTick = 0;
	private bool isAnimationRunning = false;

	public override void _Ready()
	{
		simulatedAnnealingRouteRenderer = GetNode<TspRenderer>("%SimulatedAnnealingRenderer");
		originalRouteRenderer = GetNode<TspRenderer>("%OriginalRouteRenderer");
		greedyRouteRenderer = GetNode<TspRenderer>("%GreedyRouteRenderer");
		cityRenderer = GetNode<TspRenderer>("%CityRenderer");
		RestartWithNewCities();
	}

	public void Advance()
	{
		solver.Simulate();
		UpdateDisplay();
	}

	private string FormatDistance(float distance) => $"Dist.: {distance / 10:f3}";

	public void UpdateDisplay()
	{
		simulatedAnnealingRouteRenderer.DrawRoute(solver.BestRoute);
		GetNode<Label>("%TemperatureLabel").Text = $"Temp.: {solver.CurrentTemperature:F3}";
		GetNode<Label>("%DistanceLabel").Text = FormatDistance(solver.BestDistance);
	}

	private void SetupOriginalRouteDisplay()
	{
		float distance = TspUtil.ComputeDistance(originalRoute);

		GetNode<Label>("%OriginalDistanceLabel").Text = FormatDistance(distance);
		originalRouteRenderer.DrawRoute(originalRoute);
	}

	private void SetupGreedyRouteDisplay()
	{
		greedyRoute = GreedySolver.Solve(originalRoute);
		float distance = TspUtil.ComputeDistance(greedyRoute);

		GetNode<Label>("%GreedyDistanceLabel").Text = FormatDistance(distance);
		greedyRouteRenderer.DrawRoute(greedyRoute);
	}

	public void SetGreedyRouteVisible(bool isVisible)
	{
		greedyRouteRenderer.Visible = isVisible;
	}

	public void SetOriginalRouteVisible(bool isVisible)
	{
		originalRouteRenderer.Visible = isVisible;
	}

	public void SetSimulatedAnnealingRouteVisible(bool isVisible)
	{
		simulatedAnnealingRouteRenderer.Visible = isVisible;
	}

	public void SetReheatWhenCool(bool shouldReheat) {
		solver.ReheatWhenCool = shouldReheat;
	}

	public void ToggleAnimation()
	{
		isAnimationRunning = !isAnimationRunning;
	}

	public override void _Process(double delta)
	{
		if (!isAnimationRunning)
			return;

		elapsedTimeSinceLastTick += (float)delta;

		float ticksPerSecond = (float)GetNode<Slider>("%SpeedSlider").Value - 1;
		int ticks = Mathf.FloorToInt(elapsedTimeSinceLastTick * ticksPerSecond);

		// Prevent vicious circle.
		ticks = Mathf.Min(ticks, Mathf.CeilToInt(ticksPerSecond));

		if (ticks > 0)
		{
			elapsedTimeSinceLastTick -= ticks / ticksPerSecond;

			for (int i = 0; i <= ticks; i++)
			{
				solver.Simulate();
			}
			UpdateDisplay();
		}
	}

	public void RestartWithNewCities()
	{
		int count = (int)GetNode<SpinBox>("%CityCountInput").Value;

		// This dynamically scales the canvas according to the number of cities.
		// The underlying idea is to keep the density of cities somewhat the same,
		// but to also slightly increase the density for large number of cities.
		// (That's why it's not a sqrt, but another scaling function.)
		// The constants were chosen to create a good-looking result.
		float width = 100 * Mathf.Pow(count, 0.4f);

		originalRoute = TspUtil.CreateRandomCities(count, width, width);

		cityRenderer.DrawRoute(originalRoute);

		SetupOriginalRouteDisplay();
		SetupGreedyRouteDisplay();

		RestartAnimation();
	}

	public void RestartAnimation()
	{
		// We're trying to solve for the temperature decay variable, given the initial
		// temperature, the termination temperature (minTemp.), and the target iteration
		// count which scales with the number of cities.

		// The equation which is fulfilled at the target iteration count is:
		//     initialTemp * decay ^ iterationCount = minTemp
		// which can then be solved for the decay variable.

		float initialTemperature = 100;
		float minTemperature = 0.01f;
		float targetIterationCount = originalRoute.Length * 100;
		float temperatureDecay = Mathf.Pow(minTemperature / initialTemperature, 1 / targetIterationCount);

		GD.Print($"Using temp. decay: {temperatureDecay}");

		bool useGreedyRoute = GetNode<Button>("%UseGreedyRouteButton").ButtonPressed;

		solver = new SimulatedAnnealing(
			useGreedyRoute ? greedyRoute : originalRoute,
			initialTemperature: initialTemperature,
			temperatureDecay: temperatureDecay
		);

		solver.ReheatWhenCool = GetNode<Button>("%ReheatButton").ButtonPressed;

		UpdateDisplay();
		isAnimationRunning = false;
		GetNode<Button>("%ToggleAnimationButton").ButtonPressed = false;
	}
}