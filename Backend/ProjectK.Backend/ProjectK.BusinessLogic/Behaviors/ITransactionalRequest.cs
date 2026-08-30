namespace ProjectK.BusinessLogic.Behaviors;

/// <summary>
/// Opt-in marker. A request carrying it runs inside a single database transaction
/// managed by <see cref="TransactionBehavior{TRequest,TResponse}"/>: commit on success,
/// rollback on any exception.
/// </summary>
/// <remarks>
/// Do NOT add this to a handler that opens its own transaction (e.g. RegisterKurinCommandHandler) —
/// SQL Server does not support nested transactions and the behaviour would collide with it.
/// </remarks>
public interface ITransactionalRequest;
