namespace ProjectK.Optimization.Models;

public class OptimizationResult
{
    public double[] BestPosition { get; set; } = Array.Empty<double>();
    public double BestFitness { get; set; }
}