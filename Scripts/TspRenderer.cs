using Godot;
using System;

namespace TspSolver;

public partial class TspRenderer : Node2D
{
	[Export] private float cityRadius = 10;
	[Export] private Color cityColor = new Color("#32a852");
	[Export] private Color routeLineColor = new Color("#2d5496");
	[Export] private float routeLineWidth = 2;

	[Export] private bool renderCities = true;
	[Export] private bool renderRoute = true;

	private Vector2[] cities = Array.Empty<Vector2>();

	public void DrawRoute(Vector2[] cities)
	{
		this.cities = cities;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (cities.Length == 0)
		{
			return;
		}

		if (renderRoute) {
			Vector2[] routeLinePoints = new Vector2[cities.Length + 1];
			for (int i = 0; i < cities.Length; i++)
			{
				Vector2 city = cities[i];
				routeLinePoints[i] = city;
			}
			routeLinePoints[^1] = cities[0];
			DrawPolyline(routeLinePoints, routeLineColor, routeLineWidth, antialiased: true);
		}

		if (renderCities)
		{
			foreach (Vector2 city in cities)
			{
				DrawCircle(city, cityRadius, cityColor);
			}
		}
	}
}
