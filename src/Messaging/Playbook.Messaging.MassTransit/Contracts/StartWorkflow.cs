namespace Playbook.Messaging.MassTransit.Contracts;

// --- Commands (The "Intent") ---
public record StartWorkflow(Guid CorrelationId, string OrderName);
public record ExecuteState1(Guid CorrelationId);
public record ExecuteState2(Guid CorrelationId);
public record ExecuteState3(Guid CorrelationId);

// --- Compensating Commands (The "Undo") ---
public record UndoState1(Guid CorrelationId, string Reason);
public record UndoState2(Guid CorrelationId, string Reason);

// --- Rollback Completion Events ---
public record State2Undone(Guid CorrelationId);
public record State1Undone(Guid CorrelationId);

// --- Events (The "Fact") ---
public record State1Completed(Guid CorrelationId);
public record State2Completed(Guid CorrelationId);
public record State3Completed(Guid CorrelationId);

// --- Failure Events ---
public record State1Failed(Guid CorrelationId, string ErrorMessage);
public record State2Failed(Guid CorrelationId, string ErrorMessage);
public record State3Failed(Guid CorrelationId, string ErrorMessage);
