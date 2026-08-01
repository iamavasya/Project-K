using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ProjectK.BusinessLogic.Behaviors;

/// <summary>
/// Logs how long each MediatR request takes end to end. Applies to every request
/// (no opt-in), and is registered outermost so the measured time includes the other behaviours.
/// </summary>
public sealed class RequestTimingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<RequestTimingBehavior<TRequest, TResponse>> _logger;

    public RequestTimingBehavior(ILogger<RequestTimingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return await next(cancellationToken);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "{RequestName} handled in {ElapsedMilliseconds} ms",
                typeof(TRequest).Name,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
