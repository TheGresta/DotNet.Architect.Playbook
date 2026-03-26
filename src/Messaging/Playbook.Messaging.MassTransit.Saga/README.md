# 🏛️ Playbook.Messaging.MassTransit.Saga

<div align="left">
    <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet" />
    <img src="https://img.shields.io/badge/MassTransit-v9.0+-004FC7?style=for-the-badge" />
    <img src="https://img.shields.io/badge/RabbitMQ-v3.12+-FF6600?style=for-the-badge&logo=rabbitmq" />
    <img src="https://img.shields.io/badge/Pattern-State_Machine_Saga-B10610?style=for-the-badge" />
    <img src="https://img.shields.io/badge/Complexity-Expert-orange?style=for-the-badge" />
</div>

---

## 📖 1. Executive Summary
> [!NOTE]  
> **The Problem:** Distributed systems often struggle with "Partial Failures" where one microservice succeeds but a subsequent one fails, leaving the system in an inconsistent state. Managing these long-running workflows manually leads to "Distributed Spaghetti" code, race conditions, and lost messages.
> 
> **The Solution:** An **Event-Driven Distributed Saga** architecture utilizing **MassTransit State Machines**. This implementation features a robust orchestration engine that manages sequential states, handles out-of-order messaging, and automates **Backward Compensation (Rollbacks)**. By leveraging the **Transactional Outbox**, we ensure atomic database updates and message dispatching, guaranteeing "Exactly-Once" processing semantics.

---
    
## 🏗️ 2. Design & Strategy

### 📊 System Visualization

<div align="center">
    <img width="4063" height="2952" alt="masstransit-saga-workflow" src="https://github.com/user-attachments/assets/a998d1cc-7f10-427c-8255-650c6cbf8e90" />
</div>

### 🛠️ Technical Decisions   

| Choice | Technology | Rationale  |
|------------|------------|---------|
| Orchestration | `MassTransitStateMachine` | Provides a declarative DSL to define states, events, and transitions, separating business flow from infrastructure logic. |
| Persistence | `EF Core` + `Postgres` | Uses a relational store for Saga state to ensure ACID compliance and facilitate complex queries on active workflows. |
| Reliability | `Transactional Outbox` | Prevents the "Dual Write" problem by persisting messages in the same DB transaction as the state change. |
| Resiliency | `Circuit Breaker` & `Retry` | Protects downstream consumers from cascading failures and handles transient network/DB blips automatically. |
| Compensation | `Sequential Rollback` | Implements the Saga pattern by explicitly undoing previously successful steps in reverse order upon failure. |

## 💻 3. Implementation Blueprint

### 📂 Key Artifacts
* `WorkflowStateMachine.cs`: The core brain. Defines the logic for moving between `ProcessingState` and `RollingBackState` based on success/failure events.
* `WorkflowState.cs`: The data contract for persistence. Tracks `CorrelationId`, `CurrentState`, and business data like `OrderName` and `Version`.
* `MessagingRegistration.cs`: The configuration engine. A clean extension method that encapsulates RabbitMQ setup, KebabCase naming, and global retry policies.
* `UndoStateConsumers.cs`: Specialized workers that execute idempotent compensation logic to revert changes when a later stage fails.
* `CriticalFailureConsumer.cs`: he terminal observer. Acts as a safety net for "Poison Messages" or failed rollbacks that require manual human intervention.

> [!TIP]
> **Architect's Insight:** We use `ISagaVersion` for optimistic concurrency. This is critical in high-concurrency environments to prevent two different messages from overwriting the Saga state simultaneously, ensuring data integrity without heavy database locking.

## 🚦 4. Verification Guide

### 🧪 Execution Steps

1. **Infrastructure:** Spin up RabbitMQ and PostgreSQL (e.g., via Docker).
2. **Initialize:** `dotnet run` (Automatically runs EF Migrations to create Saga and Outbox tables).
3. **Trigger:** `POST /workflow/start?name=Order_123`.
    * **Observe:** An entry is created in the `WorkflowStates` table with state `ProcessingState1`.
4. **Chaos Testing:** The `ChaosService` is injected to randomly fail 50% of requests.
    * **Success Path:** Observe `State1` -> `State2` -> `State3` -> `Finalized`.
    * **Rollback Path:** If `State2` fails, observe the Saga automatically publishing `UndoState1` and transitioning to `RollingBackState1` before finishing in `Failed`.
5. **Observability:** Check logs for `[FATAL ERROR]` to see how terminal compensation failures are caught by the `Fault<T>` consumers.

## ⚖️ 5. Trade-offs & Analysis

*Every architectural choice is a compromise.*

* ✅ **Strengths:** 
    * **High Reliability**: Transactional Outbox ensures no message is ever lost between the DB and Broker.
    * **Self-Healing**: Automated retries and compensation logic minimize manual support tickets.
    * **Decoupled Architecture**: Logic is split into small, testable consumers that don't know about the global state.
* ❌ **Weaknesses:**
    * **Increased Latency**: Sequential execution and DB persistence add overhead compared to simple fire-and-forget messaging.
    * **Complexity**: Debugging distributed sagas requires sophisticated logging and correlation ID tracking across services.
* 🔄 **Alternatives:** 
    * **Temporal / Azure Durable Functions**: Good for extremely long-running (months) tasks, but requires more specialized infrastructure.
    * **Choreographed Sagas**: No central state machine; services react to events. Easier to scale but much harder to visualize and manage complex rollback logic.
