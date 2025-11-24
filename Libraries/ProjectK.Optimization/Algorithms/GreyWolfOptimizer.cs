using System.Security.Cryptography;
using ProjectK.Optimization.Abstractions;
using ProjectK.Optimization.Models;

namespace ProjectK.Optimization.Algorithms;

internal class GreyWolfOptimizer : IOptimizer, IDisposable
{
    private readonly RandomNumberGenerator _rnd = RandomNumberGenerator.Create();

    public OptimizationResult Solve(IOptimizationProblem problem, int wolvesCount = 30, int maxIter = 100)
    {
        int dim = problem.Dimension;
        double[][] wolves = new double[wolvesCount][];

        // Ініціалізація популяції
        for (int i = 0; i < wolvesCount; i++)
        {
            wolves[i] = new double[dim];
            for (int j = 0; j < dim; j++)
            {
                wolves[i][j] = GetNextDouble() * (problem.UpperBounds[j] - problem.LowerBounds[j]) + problem.LowerBounds[j];
            }
        }

        double[] alphaPos = new double[dim]; double alphaScore = double.MaxValue;
        double[] betaPos = new double[dim]; double betaScore = double.MaxValue;
        double[] deltaPos = new double[dim]; double deltaScore = double.MaxValue;

        // Головний цикл
        for (int t = 0; t < maxIter; t++)
        {
            foreach (var wolf in wolves)
            {
                // Обмеження (стіни)
                for (int d = 0; d < dim; d++)
                {
                    wolf[d] = Math.Clamp(wolf[d], problem.LowerBounds[d], problem.UpperBounds[d]);
                }

                double fitness = problem.CalculateFitness(wolf);

                if (fitness < alphaScore) { alphaScore = fitness; alphaPos = (double[])wolf.Clone(); }
                else if (fitness < betaScore) { betaScore = fitness; betaPos = (double[])wolf.Clone(); }
                else if (fitness < deltaScore) { deltaScore = fitness; deltaPos = (double[])wolf.Clone(); }
            }

            double a = 2.0 - t * (2.0 / maxIter); // Лінійне зменшення від 2 до 0

            for (int i = 0; i < wolvesCount; i++)
            {
                for (int d = 0; d < dim; d++)
                {
                    double r1 = GetNextDouble(); double r2 = GetNextDouble();
                    double A1 = 2 * a * r1 - a; double C1 = 2 * r2;
                    double D_alpha = Math.Abs(C1 * alphaPos[d] - wolves[i][d]);
                    double X1 = alphaPos[d] - A1 * D_alpha;

                    r1 = GetNextDouble(); r2 = GetNextDouble();
                    double A2 = 2 * a * r1 - a; double C2 = 2 * r2;
                    double D_beta = Math.Abs(C2 * betaPos[d] - wolves[i][d]);
                    double X2 = betaPos[d] - A2 * D_beta;

                    r1 = GetNextDouble(); r2 = GetNextDouble();
                    double A3 = 2 * a * r1 - a; double C3 = 2 * r2;
                    double D_delta = Math.Abs(C3 * deltaPos[d] - wolves[i][d]);
                    double X3 = deltaPos[d] - A3 * D_delta;

                    wolves[i][d] = (X1 + X2 + X3) / 3.0;
                }
            }
        }

        return new OptimizationResult { BestPosition = alphaPos, BestFitness = alphaScore };
    }

    private double GetNextDouble()
    {
        // Виділяємо 8 байт (UInt64) прямо на стеку (дуже швидко)
        Span<byte> buffer = stackalloc byte[8];

        // Заповнюємо випадковими даними
        _rnd.GetBytes(buffer);

        // Конвертуємо в число (ulong)
        ulong ul = BitConverter.ToUInt64(buffer);

        // Ділимо на максимальне значення ulong, щоб отримати діапазон 0.0 - 1.0
        return (double)ul / ulong.MaxValue;
    }

    // Оскільки RandomNumberGenerator реалізує IDisposable, 
    // хороший тон - звільнити його ресурси.
    public void Dispose()
    {
        _rnd.Dispose();
    }
}