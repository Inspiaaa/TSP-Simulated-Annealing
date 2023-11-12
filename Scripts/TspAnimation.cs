using Godot;
using System;
using TspSolver.Util;

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

	private BooleanInput isPlayingAnimation;

	private NumericInput cityCount;
	private NumericInput animationSpeed;

	private NumericInput initialTemperature;
	private NumericInput temperatureDecaySpeedModifier;
	private NumericInput reheatThresholdTemperature;
	private NumericInput minReheatAmount;
	private NumericInput maxReheatAmount;

	private BooleanInput showSimulatedAnnealingRoute;
	private BooleanInput showGreedyRoute;
	private BooleanInput showOriginalRoute;

	private BooleanInput reheatWhenCool;
	private BooleanInput useGreedyRouteAsStart;

	// For the animation.
	private float elapsedTimeSinceLastTick = 0;

	public override void _Ready()
	{
		simulatedAnnealingRouteRenderer = GetNode<TspRenderer>("%SimulatedAnnealingRenderer");
		originalRouteRenderer = GetNode<TspRenderer>("%OriginalRouteRenderer");
		greedyRouteRenderer = GetNode<TspRenderer>("%GreedyRouteRenderer");
		cityRenderer = GetNode<TspRenderer>("%CityRenderer");

		isPlayingAnimation = new BooleanInput();

		cityCount = new NumericInput();
		animationSpeed = new NumericInput();

		showSimulatedAnnealingRoute = new BooleanInput(
			onChange: isVisible => simulatedAnnealingRouteRenderer.Visible = isVisible);
		showGreedyRoute = new BooleanInput(
			onChange: isVisible => greedyRouteRenderer.Visible = isVisible);
		showOriginalRoute = new BooleanInput(
			onChange: isVisible => originalRouteRenderer.Visible = isVisible);

		reheatWhenCool = new BooleanInput(
			onChange: reheat => { if (solver != null) solver.ReheatWhenCool = reheat; });
		useGreedyRouteAsStart = new BooleanInput();

		initialTemperature = new NumericInput();
		temperatureDecaySpeedModifier = new NumericInput(
			onChange: value => { if (solver != null) UpdateTemperatureDecay(); });
		reheatThresholdTemperature = new NumericInput(
			onChange: value => { if (solver != null) solver.ReheatThresholdTemperature = value; });
		minReheatAmount = new NumericInput(
			onChange: value => { if (solver != null) solver.MinReheatAmount = value; });
		maxReheatAmount = new NumericInput(
			onChange: value => { if (solver != null) solver.MaxReheatAmount = value; });

		isPlayingAnimation.Bind(GetNode<Button>("%ToggleAnimationButton"));

		cityCount.Bind(GetNode<SpinBox>("%CityCountInput"));
		animationSpeed.Bind(GetNode<Slider>("%SpeedSlider"));

		showSimulatedAnnealingRoute.Bind(GetNode<CheckButton>("%ShowSimulatedAnnealingRouteButton"));
		showGreedyRoute.Bind(GetNode<CheckButton>("%ShowGreedyRouteButton"));
		showOriginalRoute.Bind(GetNode<CheckButton>("%ShowOriginalRouteButton"));

		reheatWhenCool.Bind(GetNode<CheckButton>("%ReheatButton"));
		useGreedyRouteAsStart.Bind(GetNode<CheckButton>("%UseGreedyRouteButton"));

		initialTemperature.Bind(GetNode<SpinBox>("%InitialTemperatureInput"));
		temperatureDecaySpeedModifier.Bind(GetNode<Slider>("%DecaySpeedSlider"));
		reheatThresholdTemperature.Bind(GetNode<SpinBox>("%ReheatThresholdInput"));
		minReheatAmount.Bind(GetNode<SpinBox>("%MinReheatAmountInput"));
		maxReheatAmount.Bind(GetNode<SpinBox>("%MaxReheatAmountInput"));

		RestartWithNewCities();
	}

	private string FormatDistance(float distance) => $"Dist.: {distance / 10:f3}";

	public void UpdateDisplay()
	{
		simulatedAnnealingRouteRenderer.DrawRoute(solver.BestRoute);
		GetNode<Label>("%TemperatureLabel").Text = $"Temp.: {solver.CurrentTemperature:F3}";
		GetNode<Label>("%DistanceLabel").Text = FormatDistance(solver.BestDistance);
		GetNode<Label>("%IterationNumberLabel").Text = $"Iteration #: {solver.IterationCount}";
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

	public void Advance()
	{
		solver.Simulate();
		UpdateDisplay();
	}

	public override void _Process(double delta)
	{
		if (!isPlayingAnimation.Value)
			return;

		elapsedTimeSinceLastTick += (float)delta;

		float ticksPerSecond = animationSpeed.Value - 1;
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
		int count = (int)cityCount.Value;

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

		bool useGreedyRoute = GetNode<Button>("%UseGreedyRouteButton").ButtonPressed;

		solver = new SimulatedAnnealing(
			useGreedyRoute ? greedyRoute : originalRoute,

			initialTemperature: initialTemperature.Value,
			temperatureDecay: 0.99f,  // This is a temporary value which will be overwritten.

			reheatWhenCool: reheatWhenCool.Value,
			reheatThresholdTemperature: reheatThresholdTemperature.Value,
			minReheatAmount: minReheatAmount.Value,
			maxReheatAmount: maxReheatAmount.Value
		);

		UpdateTemperatureDecay();

		UpdateDisplay();
		isPlayingAnimation.Value = false;
	}

	private float ComputeTemperatureDecay() {
		// We're trying to solve for the temperature decay variable, given the initial
		// temperature, the termination temperature (minTemp.), and the target iteration
		// count which scales with the number of cities.

		// The equation which is fulfilled at the target iteration count is:
		//     initialTemp * decay ^ iterationCount = minTemp
		// which can then be solved for the decay variable.

		float speedModifier = 1 / temperatureDecaySpeedModifier.Value;
		float initialTemperature = this.initialTemperature.Value;
		float minTemperature = 0.001f;
		float targetIterationCount = originalRoute.Length * 150 * speedModifier;
		float temperatureDecay = Mathf.Pow(minTemperature / initialTemperature, 1 / targetIterationCount);

		return temperatureDecay;
	}

	private void UpdateTemperatureDecay() {
		float decay = ComputeTemperatureDecay();
		solver.TemperatureDecay = decay;
		GetNode<Label>("%TemperatureDecayValueLabel").Text = $"Decay: {decay :f5}";
	}

	public void ResetAdvancedSettings() {
		initialTemperature.Reset();
		temperatureDecaySpeedModifier.Reset();
		reheatThresholdTemperature.Reset();
		minReheatAmount.Reset();
		maxReheatAmount.Reset();
	}
}