using ProjectK.Optimization.Abstractions;

namespace ProjectK.Optimization.Scheduling;

public abstract class BaseCampDateProblem : IOptimizationProblem
{
    protected readonly DateTime SearchStart;
    protected readonly int DurationDays;

    protected BaseCampDateProblem(DateTime searchStart, DateTime searchEnd, int durationDays)
    {
        SearchStart = searchStart;
        DurationDays = durationDays;

        // Вираховуємо максимальне зміщення в днях
        double maxOffset = (searchEnd - searchStart).TotalDays - durationDays;

        // Захист, якщо діапазон менший за тривалість табору
        UpperBounds = new double[] { maxOffset > 0 ? maxOffset : 0 };
    }

    public int Dimension => 1; // У нас тільки одна змінна - день старту
    public double[] LowerBounds => new double[] { 0 };
    public double[] UpperBounds { get; }

    // Цей метод ти реалізуєш у бізнес-логіці
    public abstract double CalculateFitness(double[] position);

    // ХЕЛПЕР: Перетворює "вовче число" (напр. 5.43) в реальну дату
    public DateTime PositionToDate(double[] position)
    {
        int offset = (int)Math.Round(position[0]);
        return SearchStart.AddDays(offset);
    }
}