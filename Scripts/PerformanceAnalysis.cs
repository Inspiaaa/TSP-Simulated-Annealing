using Godot;
using System;
using System.Collections;
using System.IO;
using TspSolver.Util;

namespace TspSolver.Analysis;

public partial class PerformanceAnalysis : Node {

	[ExportSubgroup("TSP")]
	[Export] private int cityCount = 40;

	[ExportSubgroup("Simulated Annealing")]
	[Export] private int maxIterations = 10_000;
	[Export] private float initialTemperature = 100;
	[Export] private float temperatureDecay = 0.999f;

	[ExportSubgroup("Statistics")]
	[Export] private int binning = 100;
	[Export] private int repetitions = 20;
	[Export] private string outputPath = "res://performance.txt";

	private float[] distanceByIteration;

	private IEnumerator benchmark;

    public override void _Ready()
    {
        distanceByIteration = new float[maxIterations / binning];
		benchmark = RunBenchmark();
    }

    public override void _Process(double delta)
    {
        if (!benchmark.MoveNext()) {
			SetProcess(false);
			GetTree().Quit();
		}
    }

    private IEnumerator RunBenchmark() {
		for (int i = 0; i < repetitions; i ++) {
			GD.Print($"Running repetition # {i+1} / {repetitions}...");

			Vector2[] cities = TspUtil.CreateRandomCities(cityCount, seed: i, width: 100, height: 100);
			RunSingleBenchmark(cities);
			yield return null;
		}

		NormalizeRecordedDistances();

		GD.Print($"Finished benchmark. Writing result to {outputPath}...");

		var file = Godot.FileAccess.Open(outputPath, Godot.FileAccess.ModeFlags.Write);
		file.StoreString(string.Join("\n", distanceByIteration));
		file.Close();

		GD.Print($"Finished.");
	}

    private void RunSingleBenchmark(Vector2[] cities) {
		SimulatedAnnealing solver = new SimulatedAnnealing(
			cities,
			initialTemperature: initialTemperature,
			temperatureDecay: temperatureDecay
		);

		for (int i = 0; i < maxIterations; i ++) {
			solver.Simulate();
			distanceByIteration[i / binning] += solver.BestDistance;
		}
	}

	private void NormalizeRecordedDistances() {
		for (int i = 0; i < distanceByIteration.Length; i ++) {
			distanceByIteration[i] /= binning * repetitions;
		}
	}
}