using System.Security.Cryptography;
using ProjectK.Optimization.Abstractions;
using ProjectK.Optimization.Models;

namespace ProjectK.Optimization.Algorithms;

internal class GreyWolfOptimizer : IOptimizer, IDisposable
{
    private readonly RandomNumberGenerator _rnd;
    private bool _disposed;

    public GreyWolfOptimizer()
    {
        _rnd = RandomNumberGenerator.Create();
    }

    public OptimizationResult Solve(IOptimizationProblem problem, int wolves = 30, int iterations = 100)
    {
        int dim = problem.Dimension;

        // 1. Ініціалізація
        double[][] population = InitializePopulation(wolves, dim, problem);
        double[] currentFitnesses = new double[wolves];

        // Об'єкт для зберігання лідерів
        var leaders = new LeaderContext(dim);

        // 2. Головний цикл
        for (int t = 0; t < iterations; t++)
        {
            // Крок А: Обмеження + Розрахунок
            CalculateFitnessParallel(population, currentFitnesses, problem);

            // Крок Б: Оновлення лідерів
            UpdateLeaders(population, currentFitnesses, dim, leaders);

            // Крок В: Оновлення позицій
            UpdatePositions(population, dim, t, iterations, leaders);
        }

        return new OptimizationResult { BestPosition = leaders.AlphaPos, BestFitness = leaders.AlphaScore };
    }

    // --- Private Helpers ---

    private double[][] InitializePopulation(int wolvesCount, int dim, IOptimizationProblem problem)
    {
        double[][] population = new double[wolvesCount][];
        for (int i = 0; i < wolvesCount; i++)
        {
            population[i] = new double[dim];
            for (int j = 0; j < dim; j++)
            {
                population[i][j] = GetNextDouble() * (problem.UpperBounds[j] - problem.LowerBounds[j]) + problem.LowerBounds[j];
            }
        }
        return population;
    }

    private static void CalculateFitnessParallel(double[][] population, double[] fitnesses, IOptimizationProblem problem)
    {
        int dim = problem.Dimension;

        Parallel.For(0, population.Length, i =>
        {
            for (int d = 0; d < dim; d++)
            {
                population[i][d] = Math.Clamp(population[i][d], problem.LowerBounds[d], problem.UpperBounds[d]);
            }
            fitnesses[i] = problem.CalculateFitness(population[i]);
        });
    }

    private static void UpdateLeaders(double[][] population, double[] fitnesses, int dim, LeaderContext leaders)
    {
        for (int i = 0; i < population.Length; i++)
        {
            double fitness = fitnesses[i];

            if (fitness < leaders.AlphaScore)
            {
                leaders.AlphaScore = fitness;
                Array.Copy(population[i], leaders.AlphaPos, dim);
            }
            else if (fitness < leaders.BetaScore)
            {
                leaders.BetaScore = fitness;
                Array.Copy(population[i], leaders.BetaPos, dim);
            }
            else if (fitness < leaders.DeltaScore)
            {
                leaders.DeltaScore = fitness;
                Array.Copy(population[i], leaders.DeltaPos, dim);
            }
        }
    }

    private void UpdatePositions(double[][] population, int dim, int currentIter, int maxIter, LeaderContext leaders)
    {
        double a = 2.0 - currentIter * (2.0 / maxIter);

        for (int i = 0; i < population.Length; i++)
        {
            for (int d = 0; d < dim; d++)
            {
                // Alpha influence
                double r1 = GetNextDouble(); double r2 = GetNextDouble();
                double A1 = 2 * a * r1 - a; double C1 = 2 * r2;
                double D_alpha = Math.Abs(C1 * leaders.AlphaPos[d] - population[i][d]);
                double X1 = leaders.AlphaPos[d] - A1 * D_alpha;

                // Beta influence
                r1 = GetNextDouble(); r2 = GetNextDouble();
                double A2 = 2 * a * r1 - a; double C2 = 2 * r2;
                double D_beta = Math.Abs(C2 * leaders.BetaPos[d] - population[i][d]);
                double X2 = leaders.BetaPos[d] - A2 * D_beta;

                // Delta influence
                r1 = GetNextDouble(); r2 = GetNextDouble();
                double A3 = 2 * a * r1 - a; double C3 = 2 * r2;
                double D_delta = Math.Abs(C3 * leaders.DeltaPos[d] - population[i][d]);
                double X3 = leaders.DeltaPos[d] - A3 * D_delta;

                population[i][d] = (X1 + X2 + X3) / 3.0;
            }
        }
    }

    private double GetNextDouble()
    {
        Span<byte> buffer = stackalloc byte[8];
        _rnd.GetBytes(buffer);
        return (double)BitConverter.ToUInt64(buffer) / ulong.MaxValue;
    }

    // --- Inner Class for Parameter Object Pattern ---

    private sealed class LeaderContext
    {
        public double[] AlphaPos;
        public double AlphaScore = double.MaxValue;

        public double[] BetaPos;
        public double BetaScore = double.MaxValue;

        public double[] DeltaPos;
        public double DeltaScore = double.MaxValue;

        public LeaderContext(int dim)
        {
            AlphaPos = new double[dim];
            BetaPos = new double[dim];
            DeltaPos = new double[dim];
        }
    }

    // --- Dispose Pattern ---

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _rnd?.Dispose();
            }
            _disposed = true;
        }
    }
}