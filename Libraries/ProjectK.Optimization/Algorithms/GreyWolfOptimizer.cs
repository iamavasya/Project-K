using ProjectK.Optimization.Abstractions;
using ProjectK.Optimization.Models;

namespace ProjectK.Optimization.Algorithms;

internal class GreyWolfOptimizer : IOptimizer
{
    private readonly Random _rnd = new Random();

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
                wolves[i][j] = _rnd.NextDouble() * (problem.UpperBounds[j] - problem.LowerBounds[j]) + problem.LowerBounds[j];
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
                    double r1 = _rnd.NextDouble(); double r2 = _rnd.NextDouble();
                    double A1 = 2 * a * r1 - a; double C1 = 2 * r2;
                    double D_alpha = Math.Abs(C1 * alphaPos[d] - wolves[i][d]);
                    double X1 = alphaPos[d] - A1 * D_alpha;

                    r1 = _rnd.NextDouble(); r2 = _rnd.NextDouble();
                    double A2 = 2 * a * r1 - a; double C2 = 2 * r2;
                    double D_beta = Math.Abs(C2 * betaPos[d] - wolves[i][d]);
                    double X2 = betaPos[d] - A2 * D_beta;

                    r1 = _rnd.NextDouble(); r2 = _rnd.NextDouble();
                    double A3 = 2 * a * r1 - a; double C3 = 2 * r2;
                    double D_delta = Math.Abs(C3 * deltaPos[d] - wolves[i][d]);
                    double X3 = deltaPos[d] - A3 * D_delta;

                    wolves[i][d] = (X1 + X2 + X3) / 3.0;
                }
            }
        }

        return new OptimizationResult { BestPosition = alphaPos, BestFitness = alphaScore };
    }
}