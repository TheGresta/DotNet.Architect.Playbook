namespace Playbook.Messaging.MassTransit.Contracts;

// --- Commands (The "Intent") ---

/// <summary>
/// Command to initiate the saga workflow.
/// </summary>
/// <param name="CorrelationId">The unique identifier to track this specific workflow instance.</param>
/// <param name="OrderName">The descriptive name of the order being processed.</param>
public record StartWorkflow(Guid CorrelationId, string OrderName);

/// <summary>
/// Command sent to the consumer responsible for the first processing stage.
/// </summary>
/// <param name="CorrelationId">The saga instance correlation identifier.</param>
public record ExecuteState1(Guid CorrelationId);

/// <summary>
/// Command sent to the consumer responsible for the second processing stage.
/// </summary>
/// <param name="CorrelationId">The saga instance correlation identifier.</param>
public record ExecuteState2(Guid CorrelationId);

/// <summary>
/// Command sent to the consumer responsible for the third processing stage.
/// </summary>
/// <param name="CorrelationId">The saga instance correlation identifier.</param>
public record ExecuteState3(Guid CorrelationId);

// --- Compensating Commands (The "Undo") ---

/// <summary>
/// Command to trigger the compensation logic for State 1, reverting any changes made during that stage.
/// </summary>
/// <param name="CorrelationId">The saga instance correlation identifier.</param>
/// <param name="Reason">The explanation or error message triggering the rollback.</param>
public record UndoState1(Guid CorrelationId, string Reason);

/// <summary>
/// Command to trigger the compensation logic for State 2, reverting any changes made during that stage.
/// </summary>
/// <param name="CorrelationId">The saga instance correlation identifier.</param>
/// <param name="Reason">The explanation or error message triggering the rollback.</param>
public record UndoState2(Guid CorrelationId, string Reason);

// --- Rollback Completion Events ---

/// <summary>
/// Event published by a consumer after the compensation for State 1 has been successfully finalized.
/// </summary>
/// <param name="CorrelationId">The saga instance correlation identifier.</param>
public record State1Undone(Guid CorrelationId);

/// <summary>
/// Event published by a consumer after the compensation for State 2 has been successfully finalized.
/// </summary>
/// <param name="CorrelationId">The saga instance correlation identifier.</param>
public record State2Undone(Guid CorrelationId);

// --- Events (The "Fact") ---

/// <summary>
/// Event signaling that the first processing stage has successfully completed.
/// </summary>
/// <param name="CorrelationId">The saga instance correlation identifier.</param>
public record State1Completed(Guid CorrelationId);

/// <summary>
/// Event signaling that the second processing stage has successfully completed.
/// </summary>
/// <param name="CorrelationId">The saga instance correlation identifier.</param>
public record State2Completed(Guid CorrelationId);

/// <summary>
/// Event signaling that the third processing stage has successfully completed.
/// </summary>
/// <param name="CorrelationId">The saga instance correlation identifier.</param>
public record State3Completed(Guid CorrelationId);

// --- Failure Events ---

/// <summary>
/// Event signaling that a failure occurred during the first processing stage.
/// </summary>
/// <param name="CorrelationId">The saga instance correlation identifier.</param>
/// <param name="ErrorMessage">The detailed error message describing the failure.</param>
public record State1Failed(Guid CorrelationId, string ErrorMessage);

/// <summary>
/// Event signaling that a failure occurred during the second processing stage.
/// </summary>
/// <param name="CorrelationId">The saga instance correlation identifier.</param>
/// <param name="ErrorMessage">The detailed error message describing the failure.</param>
public record State2Failed(Guid CorrelationId, string ErrorMessage);

/// <summary>
/// Event signaling that a failure occurred during the third processing stage.
/// </summary>
/// <param name="CorrelationId">The saga instance correlation identifier.</param>
/// <param name="ErrorMessage">The detailed error message describing the failure.</param>
public record State3Failed(Guid CorrelationId, string ErrorMessage);
