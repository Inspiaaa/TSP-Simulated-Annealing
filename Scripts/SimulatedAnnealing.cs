using Godot;
using System;

namespace TspSolver;

public class SimulatedAnnealing
{
	private readonly Vector2[] cities;
	private float currentDistance;
	private float temperature;
	public float TemperatureDecay;

	public bool ReheatWhenCool = false;
	public float ReheatThresholdTemperature;
	public float MinReheatAmount;
	public float MaxReheatAmount;

	private int iterationCount;

	public Vector2[] BestRoute => cities;
	public float BestDistance => currentDistance;
	public float CurrentTemperature => temperature;
	public int IterationCount => iterationCount;

	public SimulatedAnnealing(
		Vector2[] cities,

		float initialTemperature = 100f,
		float temperatureDecay = 0.99f,

		bool reheatWhenCool = false,
		float reheatThresholdTemperature = 0.005f,
		float minReheatAmount = 0.5f,
		float maxReheatAmount = 20)
	{
		this.cities = new Vector2[cities.Length];
		Array.Copy(cities, this.cities, cities.Length);

		this.currentDistance = TspUtil.ComputeDistance(cities);
		this.temperature = initialTemperature;
		this.TemperatureDecay = temperatureDecay;

		this.ReheatWhenCool = reheatWhenCool;
		this.ReheatThresholdTemperature = reheatThresholdTemperature;
		this.MinReheatAmount = minReheatAmount;
		this.MaxReheatAmount = maxReheatAmount;

		this.iterationCount = 0;
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

	private void TransportRange(int startIndex, int count, int distance) {
		Vector2[] citiesToMove = new Vector2[count];
		for (int i = 0; i < count; i ++) {
			citiesToMove[i] = cities[WrapIndex(startIndex + i)];
		}

		// Move the right segment to the left.
		for (int i = 0; i < distance; i ++) {
			cities[WrapIndex(startIndex + i)] = cities[WrapIndex(startIndex + i + count)];
		}

		// Move the previous left segment to the right.
		for (int i = 0; i < count; i ++) {
			cities[WrapIndex(startIndex + distance + i)] = citiesToMove[i];
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

	private float ComputeDistanceDeltaAfterTransport(int startIndex, int count, int distance) {
		int leftSegmentStartIndex = startIndex;
		int leftSegmentEndIndex = WrapIndex(startIndex + count - 1);
		int indexBeforeLeftSegment = WrapIndex(startIndex - 1);

		int rightSegmentStartIndex = WrapIndex(leftSegmentEndIndex + 1);
		int rightSegmentEndIndex = WrapIndex(rightSegmentStartIndex + distance - 1);
		int indexAfterRightSegment =  WrapIndex(rightSegmentEndIndex + 1);

		Vector2 posBeforeLeftSegment = cities[indexBeforeLeftSegment];
		Vector2 leftSegmentStart = cities[leftSegmentStartIndex];
		Vector2 leftSegmentEnd = cities[leftSegmentEndIndex];

		Vector2 rightSegmentStart = cities[rightSegmentStartIndex];
		Vector2 rightSegmentEnd = cities[rightSegmentEndIndex];
		Vector2 posAfterRightSegment = cities[indexAfterRightSegment];

		float delta = 0;
		delta -= posBeforeLeftSegment.DistanceTo(leftSegmentStart);
		delta -= leftSegmentEnd.DistanceTo(rightSegmentStart);
		delta -= rightSegmentEnd.DistanceTo(posAfterRightSegment);

		delta += posBeforeLeftSegment.DistanceTo(rightSegmentStart);
		delta += rightSegmentEnd.DistanceTo(leftSegmentStart);
		delta += leftSegmentEnd.DistanceTo(posAfterRightSegment);

		return delta;
	}

	public void Simulate()
	{
		if (ReheatWhenCool && temperature < ReheatThresholdTemperature) {
			temperature += (float)GD.RandRange(MinReheatAmount, MaxReheatAmount);
		}

		if (cities.Length <= 3) {
			temperature *= TemperatureDecay;
			iterationCount += 1;
			return;
		}

		Action acceptSolution = null;
		float distanceChange = 0;

		switch (Random.Shared.Next(6))
		{
			case 0:
				int swapIndexA = RandomIndex();
				int swapIndexB = RandomIndex();

				acceptSolution = () => SwapCities(swapIndexA, swapIndexB);
				distanceChange = ComputeDistanceDeltaAfterSwap(swapIndexA, swapIndexB);
				break;

			case 1: case 2:
				// This operation only works for more than 3 cities.
				int startIndex = RandomIndex();
				int count = Random.Shared.Next(1, cities.Length/4);
				// Note: count+distance must be LESS than the number of cities for the
				// ComputeDistanceDeltaAfterTransport method to work properly.
				int distance = Random.Shared.Next(1, cities.Length - count);

				acceptSolution = () => TransportRange(startIndex, count, distance);
				distanceChange = ComputeDistanceDeltaAfterTransport(startIndex, count, distance);
				break;

			// Twice as likely as it is more powerful.
			case 3: case 4: case 5:
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

		temperature *= TemperatureDecay;
		iterationCount += 1;
	}
}