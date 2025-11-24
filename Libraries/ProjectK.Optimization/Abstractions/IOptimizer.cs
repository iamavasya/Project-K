using ProjectK.Optimization.Models;

namespace ProjectK.Optimization.Abstractions;

public interface IOptimizer
{
    OptimizationResult Solve(IOptimizationProblem problem, int wolves = 30, int iterations = 100);
}