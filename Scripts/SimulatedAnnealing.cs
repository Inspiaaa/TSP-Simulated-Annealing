using Godot;
using System;

namespace TspSolver;

public class SimulatedAnnealing
{
	private readonly Vector2[] cities;
	private float currentDistance;
	private float temperature;
	private readonly float temperatureDecay;

	private float reheatTemperatureThreshold;
	private float minReheatAmount;
	private float maxReheatAmount;

	public Vector2[] BestRoute => cities;
	public float BestDistance => currentDistance;
	public float CurrentTemperature => temperature;

	public bool ReheatWhenCool = false;

	public SimulatedAnnealing(
		Vector2[] cities,

		float initialTemperature = 100f,
		float temperatureDecay = 0.99f,

		float reheatTemperatureThreshold = 0.005f,
		float minReheatAmount = 0.5f,
		float maxReheatAmount = 20)
	{
		this.cities = new Vector2[cities.Length];
		Array.Copy(cities, this.cities, cities.Length);

		this.currentDistance = TspUtil.ComputeDistance(cities);
		this.temperature = initialTemperature;
		this.temperatureDecay = temperatureDecay;

		this.reheatTemperatureThreshold = reheatTemperatureThreshold;
		this.minReheatAmount = minReheatAmount;
		this.maxReheatAmount = maxReheatAmount;
	}

	private void SwapCities(int idx1, int idx2)
	{
		(cities[idx1], cities[idx2]) = (cities[idx2], cities[idx1]);
	}

	private int WrapIndex(int idx) => ((idx % cities.Length) + cities.Length) % cities.Length;

	private int RandomIndex() => Random.Shared.Next(cities.Length);

	private void ReverseRange(int startIndex, int count)
	{
		for (int i = 0; i <= count / 2; i++)
		{
			int left = WrapIndex(startIndex + i);
			int right = WrapIndex(startIndex + count - i);
			SwapCities(left, right);
		}
	}

	private float ComputeDistanceDeltaAfterReverse(int startIndex, int count)
	{
		int endIndex = WrapIndex(startIndex + count);

		Vector2 positionBeforeStart = cities[WrapIndex(startIndex - 1)];
		Vector2 startPosition = cities[startIndex];
		Vector2 endPosition = cities[endIndex];
		Vector2 positionAfterEnd = cities[WrapIndex(endIndex + 1)];

		// When reversing a range of cities, the distances between the individual
		// cities remain the same. The only thing that changes are the distances between
		// the start and end positions to their predecessor and successor, respectively.

		return 0
			- positionBeforeStart.DistanceTo(startPosition)
			- endPosition.DistanceTo(positionAfterEnd)
			+ positionBeforeStart.DistanceTo(endPosition)
			+ startPosition.DistanceTo(positionAfterEnd);
	}

	private float ComputeDistanceDeltaAfterSwap(int indexA, int indexB)
	{
		int indexBeforeA = WrapIndex(indexA - 1);
		int indexBeforeB = WrapIndex(indexB - 1);
		int indexAfterA = WrapIndex(indexA + 1);
		int indexAfterB = WrapIndex(indexB + 1);

		Vector2 posBeforeA = cities[indexBeforeA];
		Vector2 posA = cities[indexA];
		Vector2 posAfterA = cities[indexAfterA];

		Vector2 posBeforeB = cities[indexBeforeB];
		Vector2 posB = cities[indexB];
		Vector2 posAfterB = cities[indexAfterB];

		float delta = 0;
		delta -= posBeforeA.DistanceTo(posA);
		delta -= posA.DistanceTo(posAfterA);

		delta -= posBeforeB.DistanceTo(posB);
		delta -= posB.DistanceTo(posAfterB);

		// Positions of predecessors / successors may change due to the swap.
		posBeforeA = indexBeforeA == indexB ? posA : posBeforeA;
		posBeforeB = indexBeforeB == indexA ? posB : posBeforeB;
		posAfterA = indexAfterA == indexB ? posA : posAfterA;
		posAfterB = indexAfterB == indexA ? posB : posAfterB;

		delta += posBeforeA.DistanceTo(posB);
		delta += posB.DistanceTo(posAfterA);

		delta += posBeforeB.DistanceTo(posA);
		delta += posA.DistanceTo(posAfterB);

		return delta;
	}

	private void ShiftRange(int startIndex, int count, int distance) {

	}
	// TODO: Shift

	public void Simulate()
	{
		if (ReheatWhenCool && temperature < reheatTemperatureThreshold) {
			temperature += (float)GD.RandRange(minReheatAmount, maxReheatAmount);
		}

		Action acceptSolution = null;
		float distanceChange = 0;

		switch (Random.Shared.Next(2))
		{
			case 0:
				int swapIndexA = RandomIndex();
				int swapIndexB = RandomIndex();

				acceptSolution = () => SwapCities(swapIndexA, swapIndexB);
				distanceChange = ComputeDistanceDeltaAfterSwap(swapIndexA, swapIndexB);
				break;

			case 1:
				int reverseStartIndex = RandomIndex();
				int reverseCount = Random.Shared.Next(1, cities.Length / 2);

				acceptSolution = () => ReverseRange(reverseStartIndex, reverseCount);
				distanceChange = ComputeDistanceDeltaAfterReverse(reverseStartIndex, reverseCount);
				break;
		}

		if (distanceChange <= 0)
		{
			currentDistance += distanceChange;
			acceptSolution();
		}
		else if (Mathf.Exp(-distanceChange / temperature) >= Random.Shared.NextSingle())
		{
			currentDistance += distanceChange;
			acceptSolution();
		}

		temperature *= temperatureDecay;
	}
}