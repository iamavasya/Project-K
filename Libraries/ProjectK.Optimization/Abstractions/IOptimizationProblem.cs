namespace ProjectK.Optimization.Abstractions;

public interface IOptimizationProblem
{
    int Dimension { get; }
    double[] LowerBounds { get; }
    double[] UpperBounds { get; }

    // Функція штрафу: 0 - ідеально, >0 - погано
    double CalculateFitness(double[] position);
}