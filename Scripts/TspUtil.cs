using Godot;
using System;

namespace TspSolver;

public static class TspUtil
{
	public static float ComputeDistance(Vector2[] cities)
	{
		if (cities.Length <= 1) return 0;

		float distance = 0;
		for (int i = 1; i < cities.Length; i++)
		{
			distance += cities[i - 1].DistanceTo(cities[i]);
		}
		distance += cities[0].DistanceTo(cities[^1]);

		return distance;
	}

	public static Vector2[] CreateRandomCities(int count, float width, float height) {
		Vector2[] cities = new Vector2[count];

		for (int i = 0; i < count; i ++) {
			cities[i] = new Vector2(
				(float)GD.RandRange(0, width),
				(float)GD.RandRange(0, height)
			);
		}

		return cities;
	}
}