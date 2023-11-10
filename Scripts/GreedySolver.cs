using Godot;
using System;
using System.Collections.Generic;

namespace TspSolver;

public class GreedySolver
{
	public static Vector2[] Solve(Vector2[] cities)
	{
		Vector2[] greedyRoute = new Vector2[cities.Length];
		if (cities.Length == 0)
		{
			return greedyRoute;
		}

		HashSet<Vector2> remainingCities = new HashSet<Vector2>(cities);

		Vector2 lastCity = cities[0];
		greedyRoute[0] = lastCity;
		remainingCities.Remove(lastCity);

		int idx = 1;
		while (remainingCities.Count > 0)
		{
			float minDistance = float.MaxValue;
			Vector2 closestCity = default;

			foreach (Vector2 city in remainingCities)
			{
				float distance = lastCity.DistanceSquaredTo(city);

				if (distance < minDistance)
				{
					minDistance = distance;
					closestCity = city;
				}
			}

			remainingCities.Remove(closestCity);

			greedyRoute[idx++] = closestCity;
			lastCity = closestCity;
		}

		return greedyRoute;
	}
}