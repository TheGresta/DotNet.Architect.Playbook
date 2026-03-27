<div align="center">

# 🏛️ DotNet Architect Playbook

[![Build Status](https://github.com/TheGresta/DotNet.Architect.Playbook/actions/workflows/pr-integrity.yml/badge.svg)](https://github.com/TheGresta/DotNet.Architect.Playbook/actions)
[![Version](https://img.shields.io/github/v/release/TheGresta/DotNet.Architect.Playbook?color=blue&logo=github)](https://github.com/TheGresta/DotNet.Architect.Playbook/releases)
[![.NET Version](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-512BD4?logo=dotnet)](#)
[![License](https://img.shields.io/github/license/TheGresta/DotNet.Architect.Playbook?color=orange)](LICENSE)
[![Semantic Release](https://img.shields.io/badge/%20%20%F0%9F%93%A6%F0%9F%9A%80-semantic--release-e10079.svg)](https://github.com/semantic-release/semantic-release)

[![Stars](https://img.shields.io/github/stars/TheGresta/DotNet.Architect.Playbook?style=flat&color=FFD700&logo=github)](https://github.com/TheGresta/DotNet.Architect.Playbook/stargazers)
[![Repository Size](https://img.shields.io/github/repo-size/TheGresta/DotNet.Architect.Playbook?color=success)](#)
[![Last Commit](https://img.shields.io/github/last-commit/TheGresta/DotNet.Architect.Playbook?color=success)](https://github.com/TheGresta/DotNet.Architect.Playbook/commits/main)
<br>
[![Open Issues](https://img.shields.io/github/issues/TheGresta/DotNet.Architect.Playbook?color=blue)](https://github.com/TheGresta/DotNet.Architect.Playbook/issues)
[![Closed Issues](https://img.shields.io/github/issues-closed/TheGresta/DotNet.Architect.Playbook?color=purple)](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+is%3Aclosed)
[![Open PRs](https://img.shields.io/github/issues-pr/TheGresta/DotNet.Architect.Playbook?color=blue)](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls)
[![Merged PRs](https://img.shields.io/github/issues-pr-closed/TheGresta/DotNet.Architect.Playbook?color=purple)](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+is%3Aclosed)

*Building scalable, maintainable, and enterprise-grade .NET applications.*

</div>

---

## 📖 Executive Summary

The **DotNet Architect Playbook** is a curated, living repository of architectural patterns, best practices, and enterprise-grade reference implementations. 

Rather than building a single monolithic sample, this repository is divided into isolated **"Chapters"**. Each chapter focuses on solving a specific domain challenge—from high-performance search infrastructures to decoupled messaging systems—providing developers with clean, copy-pasteable, and production-ready code blocks designed for .NET 8+.

---

## 📚 The Playbook Catalog

Below is the comprehensive directory of architectural chapters. Each module is a standalone implementation designed to demonstrate best-in-class patterns. 

**Legend:** &nbsp;&nbsp; ✅ `Completed` &nbsp;&nbsp; | &nbsp;&nbsp; 🚧 `In Progress` &nbsp;&nbsp; | &nbsp;&nbsp; 📅 `Planned / Up for Grabs`

### 💾 Persistence Domain
*Advanced data management, indexing strategies, and polyglot persistence.*

| Status | Chapter Name | Technology Focus | Documentation | Tracking |
| :---: | :--- | :--- | :--- | :--- |
| ✅ | **Search Engine** | Elasticsearch | [📖 Readme](./src/Persistence/Playbook.Persistence.ElasticSearch/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Aelasticsearch) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Aelasticsearch) |
| ✅ | **Document Store** | MongoDB | [📖 Readme](./src/Persistence/Playbook.Persistence.MongoDB/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3AmongoDb) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3AmongoDb) |
| ✅ | **Relational Data** | EF Core | [📖 Readme](./src/Persistence/Playbook.Persistence.EntityFramework/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Aefcore) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Aefcore) |
| ✅ | **Distributed Cache** | Redis (Stack) | [📖 Readme](./src/Persistence/Playbook.Persistence.Redis/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Aredis) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Aredis) |
| ✅ | **Fast Text Search** | Meilisearch | [📖 Readme](./src/Persistence/Playbook.Persistence.Meilisearch/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Ameilisearch) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Ameilisearch) |
| 📅 | **Hybrid Caching** | .NET 9 HybridCache | [📖 Readme](./src/Persistence/Playbook.Persistence.HybridCache/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Ahybrid-cache) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Ahybrid-cache) |

### 📨 Messaging, Integration & Background Jobs
*Patterns for decoupled communication, event-driven choreography, and asynchronous processing.*

| Status | Chapter Name | Technology Focus | Documentation | Tracking |
| :---: | :--- | :--- | :--- | :--- |
| ✅ | **Message Broker** | RabbitMQ | [📖 Readme](./src/Messaging/Playbook.Messaging.RabbitMQ/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Arabbitmq) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Arabbitmq) |
| ✅ | **State Machine Saga** | MassTransit Saga | [📖 Readme](./src/Messaging/Playbook.Messaging.MassTransit.Saga/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Amasstransit) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Amasstransit) |
| ✅ | **Real-Time Pushing** | SignalR & Redis | [📖 Readme](./src/Messaging/Playbook.Messaging.SignalR/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Asignalr) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Asignalr) |
| ✅ | **Routing Slips** | MassTransit Courier | [📖 Readme](./src/Messaging/Playbook.Messaging.MassTransit.Courier/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Amasstransit) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Amasstransit) |
| 📅 | **Event Streaming** | Kafka | [📖 Readme](./src/Messaging/Playbook.Messaging.Kafka/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Akafka) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Akafka) |
| 📅 | **Background Processing** | Hangfire | *Coming Soon* | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Ahangfire) |

### 🛡️ Core Infrastructure & API Design
*Cross-cutting concerns, internal architecture, data exposure, and routing.*

| Status | Chapter Name | Technology Focus | Documentation | Tracking |
| :---: | :--- | :--- | :--- | :--- |
| ✅ | **CQRS Pattern** | MediatR | [📖 Readme](./src/Infrastructure/Playbook.Architecture.CQRS/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Acqrs) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Acqrs) |
| ✅ | **Exception Handling** | Custom Middleware | [📖 Readme](./src/Infrastructure/Playbook.Exceptions/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3AexceptionHandling) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3AexceptionHandling) |
| 📅 | **Federated Graphs** | GraphQL / Hot Chocolate | [📖 Readme](./src/API/Playbook.API.GraphQL/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Agraphql) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Agraphql) |
| 📅 | **High-Performance RPC** | gRPC | *Coming Soon* | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Agrpc) |
| 📅 | **API Gateways** | YARP & Ocelot | *Coming Soon* | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Aapi-gateway) |
| 📅 | **API Documentation** | Scalar | *Coming Soon* | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Ascalar) |
| 📅 | **Object Mapping** | Manual Mapping Patterns | *Coming Soon* | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Amapping) |

### 🧪 Testing & Quality Assurance
*Ensuring reliability through automated testing and containerized environments.*

| Status | Chapter Name | Technology Focus | Documentation | Tracking |
| :---: | :--- | :--- | :--- | :--- |
| 📅 | **Unit Testing Suite** | xUnit, Moq, FluentAssertions, AutoFixture | *Coming Soon* | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Aunit-testing) |
| 📅 | **Integration Testing** | WebApplicationFactory, Testcontainers | *Coming Soon* | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Aintegration-testing) |

### 🤖 AI & Intelligent Systems
*Modern integrations for LLMs, semantic search, and AI-assisted development.*

| Status | Chapter Name | Technology Focus | Documentation | Tracking |
| :---: | :--- | :--- | :--- | :--- |
| 📅 | **Semantic Engineering** | Semantic Kernel & RAG | *Coming Soon* | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Aai) |
| 📅 | **AI Coding Agents** | Cursor (Agent, Plan, Ask, MCPs) | *Coming Soon* | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Aai-agents) |
| 📅 | **LLM Integration** | Anthropic Claude | *Coming Soon* | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Aclaude) |
| 📅 | **Vector Search** | Qdrant / Milvus | *Coming Soon* | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Avector-db) |

### 🔐 Security & Identity Access
*Securing APIs, managing user identities, and OAuth2/OIDC workflows.*

| Status | Chapter Name | Technology Focus | Documentation | Tracking |
| :---: | :--- | :--- | :--- | :--- |
| 📅 | **Identity Providers** | Keycloak, IdentityServer, OpenIddict, Auth0 | *Coming Soon* | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Asecurity) |

### ☁️ Cloud-Native DevOps & Observability
*Deployment, telemetry, structured logging, and cluster orchestration.*

| Status | Chapter Name | Technology Focus | Documentation | Tracking |
| :---: | :--- | :--- | :--- | :--- |
| 📅 | **Distributed Observability**| OpenTelemetry, .NET Aspire, Jaeger | *Coming Soon* | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Aobservability) |
| 📅 | **Structured Logging** | Serilog, Datadog | *Coming Soon* | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Alogging) |
| 📅 | **Metrics & Monitoring** | Prometheus, Grafana, Zabbix | *Coming Soon* | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Amonitoring) |
| 📅 | **Container Orchestration** | Kubernetes (kubectl, Rancher) | *Coming Soon* | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Akubernetes) |
| 📅 | **CI/CD Automation** | GitHub Actions (Build, Deploy) | *Coming Soon* | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Aci-cd) |
---

## 🗺️ Roadmap & Future Horizons

Architecture is an ever-evolving discipline. This repository will continually expand across structured phases to cover emerging patterns and operational necessities, leveraging the latest features in **.NET 10**.

### Phase 1: Foundation (Completed)
- [x] **.NET Fundamentals:** Primary constructors, Result patterns, Record structs.
- [x] **Persistence Abstractions:** Core polyglot persistence mechanisms (SQL, NoSQL, Elasticsearch, Redis).
- [x] **Baseline Messaging:** RabbitMQ and MassTransit Sagas & Routing Slips.
- [x] **Core Architecture:** CQRS with MediatR and custom Exception Handling.

### Phase 2: Advanced Integration & Microservices (Current Focus)
- [ ] **Data Federation & RPC:** GraphQL integrations, High-Performance gRPC.
- [ ] **API Gateways:** Microservice routing with YARP and Ocelot.
- [ ] **Performance & Processing:** .NET HybridCache, Hangfire background jobs, and Meilisearch.
- [ ] **Event Streaming:** Introducing Kafka for high-throughput scenarios.
- [ ] **API Tooling:** Manual object mapping patterns and Scalar API documentation.

### Phase 3: Quality Assurance & Identity Access (Planned)
- [ ] **🧪 Robust Testing:** Unit testing suite (xUnit, Moq, FluentAssertions, AutoFixture).
- [ ] **📦 Integration Testing:** WebApplicationFactory and Testcontainers for reliable isolated environments.
- [ ] **🔐 Comprehensive Security:** Standardized OAuth2/OIDC flows, JWT Bearer implementation.
- [ ] **🛡️ Identity Providers:** Deep dives into Keycloak, IdentityServer, OpenIddict, and Auth0.

### Phase 4: Intelligence & Observability (Planned)
- [ ] **🚀 AI & Semantic Web:** Integrating Semantic Kernel, RAG pipelines, and Vector DBs (Qdrant/Milvus).
- [ ] **🧠 LLMs & Agents:** Integrating Anthropic Claude and leveraging Cursor (Agent, Plan, Ask, MCPs).
- [ ] **⚙️ Cloud-Native Telemetry:** OpenTelemetry distributed tracing, .NET Aspire orchestration, and Jaeger.
- [ ] **📝 Structured Logging:** Centralized logging strategies with Serilog and Datadog.

### Phase 5: DevOps, Operations & Scaling (Planned)
- [ ] **📈 Metrics & Monitoring:** System health visibility using Prometheus, Grafana, and Zabbix.
- [ ] **🚢 Cluster Orchestration:** Container management with Kubernetes (kubectl) and Rancher.
- [ ] **🔄 CI/CD Automation:** Automated Build and Deploy pipelines using GitHub Actions.

---

## 🚀 Getting Started

To explore the playbook locally, clone the repository and navigate to the specific module of interest. Each module contains a standalone `docker-compose.yml` file to spin up its required dependencies (e.g., Redis, RabbitMQ, Elastic).

```bash
# 1. Clone the repository
git clone https://github.com/TheGresta/DotNet.Architect.Playbook.git

# 2. Navigate to docker compose file
cd DotNet.Architect.Playbook

# 3. Spin up the infrastructure
docker-compose up -d

# 4. Navigate to your desired chapter
cd DotNet.Architect.Playbook/src/Persistence/Playbook.Persistence.ElasticSearch

# 5. Run the project
dotnet run
```

---

## 🤝 Contributing

This playbook is open to the community. If you have an architectural pattern or a better implementation strategy, contributions are highly encouraged!

1. **Find an Issue**: Pick an issue from the trackers above or propose a new Chapter via the issue tab.

2. **Follow Standards**: Ensure your code adheres to the established Clean Architecture guidelines within the project.

3. **Submit**: Open a Pull Request using the repository's PR Template.

  * 💡 Note on Tracking Links: The Issues/PRs links in the tables above use GitHub search query parameters (e.g., `label:elasticsearch`). Ensure you label your issues and PRs accordingly in the repository so they auto-populate in these views!

  ---

<div align="center">
    <i>Fueled by caffeine, guided by AI, and built with modern tech. I'm just here connecting the dots in a world of high-speed syntax.</i>
</div>
