<div align="center">

# 🏛️ DotNet Architect Playbook

[![Build Status](https://github.com/TheGresta/DotNet.Architect.Playbook/actions/workflows/pr-integrity.yml/badge.svg)](https://github.com/TheGresta/DotNet.Architect.Playbook/actions)
[![Version](https://img.shields.io/github/v/release/TheGresta/DotNet.Architect.Playbook?color=blue&logo=github)](https://github.com/TheGresta/DotNet.Architect.Playbook/releases)
[![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](#)
[![License](https://img.shields.io/github/license/TheGresta/DotNet.Architect.Playbook?color=orange)](LICENSE)
[![Semantic Release](https://img.shields.io/badge/%20%20%F0%9F%93%A6%F0%9F%9A%80-semantic--release-e10079.svg)](https://github.com/semantic-release/semantic-release)

[![Repository Size](https://img.shields.io/github/repo-size/TheGresta/DotNet.Architect.Playbook?color=success)](#)
[![Last Commit](https://img.shields.io/github/last-commit/TheGresta/DotNet.Architect.Playbook?color=success)](#)
[![Open Issues](https://img.shields.io/github/issues/TheGresta/DotNet.Architect.Playbook)](#)
[![Pull Requests](https://img.shields.io/github/issues-pr/TheGresta/DotNet.Architect.Playbook)](#)

*Building scalable, maintainable, and enterprise-grade .NET applications.*



</div>

---

## 📖 Executive Summary

The **DotNet Architect Playbook** is a curated, living repository of architectural patterns, best practices, and enterprise-grade reference implementations. 

Rather than building a single monolithic sample, this repository is divided into isolated **"Chapters"**. Each chapter focuses on solving a specific domain challenge—from high-performance search infrastructures to decoupled messaging systems—providing developers with clean, copy-pasteable, and production-ready code blocks designed for .NET 8+.

---

## 📚 The Playbook Catalog

Below is the comprehensive directory of architectural chapters. Each module is a standalone implementation designed to demonstrate best-in-class patterns for the modern .NET ecosystem.

### 💾 Persistence Domain
*Advanced data management, indexing strategies, and polyglot persistence.*

| Status | Chapter Name | Technology Focus | Documentation | Tracking |
| :---: | :--- | :--- | :--- | :--- |
| ✅ | **Search Engine** | Elasticsearch | [📖 Readme](./src/Persistence/Playbook.Persistence.ElasticSearch/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Aelasticsearch) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Aelasticsearch) |
| ✅ | **Document Store** | MongoDB | [📖 Readme](./src/Persistence/Playbook.Persistence.MongoDB/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3AmongoDb) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3AmongoDb) |
| ✅ | **Relational Data** | EF Core | [📖 Readme](./src/Persistence/Playbook.Persistence.EntityFramework/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Aefcore) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Aefcore) |
| ✅ | **Distributed Cache** | Redis (Stack) | [📖 Readme](./src/Persistence/Playbook.Persistence.Redis/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Aredis) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Aredis) |
| 📅 | **Hybrid Caching** | .NET 9 HybridCache & Redis | [📖 Readme](./src/Persistence/Playbook.Persistence.HybridCache/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Ahybrid-cache) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Ahybrid-cache) |

### 📨 Messaging & Integration
*Patterns for decoupled communication and event-driven choreography.*

| Status | Chapter Name | Technology Focus | Documentation | Tracking |
| :---: | :--- | :--- | :--- | :--- |
| 📅 | **Message Broker** | RabbitMQ | [📖 Readme](./src/Messaging/Playbook.Messaging.RabbitMQ/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Arabbitmq) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Arabbitmq) |
| 📅 | **Service Bus** | MassTransit | [📖 Readme](./src/Messaging/Playbook.Messaging.MassTransit/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Amasstransit) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Amasstransit) |
| 📅 | **Event Streaming** | Kafka | [📖 Readme](./src/Messaging/Playbook.Messaging.Kafka/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Akafka) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Akafka) |
| ✅ | **Real-Time Pushing** | SignalR & Redis Backplane | [📖 Readme](./src/Messaging/Playbook.Messaging.SignalR/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Asignalr) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Asignalr) |

### 🛡️ Core Infrastructure
*Cross-cutting concerns and internal architecture patterns.*

| Status | Chapter Name | Technology Focus | Documentation | Tracking |
| :---: | :--- | :--- | :--- | :--- |
| ✅ | **CQRS Pattern** | MediatR | [📖 Readme](./src/Infrastructure/Playbook.Architecture.CQRS/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3Acqrs) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3Acqrs) |
| ✅ | **Exception Handling** | Custom Exception Framework | [📖 Readme](./src/Infrastructure/Playbook.Exceptions/README.md) | [🐛 Issues](https://github.com/TheGresta/DotNet.Architect.Playbook/issues?q=is%3Aissue+label%3AexceptionHandling) \| [🔄 PRs](https://github.com/TheGresta/DotNet.Architect.Playbook/pulls?q=is%3Apr+label%3AexceptionHandling) |

### 🤖 AI & Semantic Engineering
*Integrating LLMs and Vector Search into .NET applications.*

| Status | Chapter Name | Technology Focus | Documentation | Tracking |
| :---: | :--- | :--- | :--- | :--- |

### ☁️ DevOps & Cloud-Native
*Observability, Infrastructure as Code, and Containerization.*

| Status | Chapter Name | Technology Focus | Documentation | Tracking |
| :---: | :--- | :--- | :--- | :--- |

---

## 🗺️ Roadmap & Future Horizons

Architecture is an ever-evolving discipline. This repository will continually expand to cover emerging patterns and operational necessities.

- [x] **.NET 8 Fundamentals:** Primary constructors, Result patterns, Record structs.
- [x] **Persistence Abstractions:** Polyglot persistence mechanisms.
- [ ] **🚀 AI & Semantic Web:** Semantic Kernel integrations, RAG pipelines, and Vector DBs.
- [ ] **⚙️ DevOps & Cloud Native:** Bicep/Terraform definitions, OpenTelemetry distributed tracing, and GitHub Actions templates.
- [ ] **🔐 Identity & Security:** OAuth2, JWT Bearer flows, and Keycloak integrations.

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

This playbook is open to the community. If you have an architectural pattern or a better implementation strategy, contributions are highly encouraged.

1. Pick an issue from the trackers above or propose a new Chapter.

2. Ensure your code follows the established Clean Architecture guidelines within the project.

3. Submit a Pull Request using the repository's PR Template.

  * Note on Tracking Links: The Issues/PRs links in the tables above use GitHub search query parameters (e.g., `label:elasticsearch`). Ensure you label your issues and PRs accordingly in the repository so they auto-populate in these views!

  ---

<div align="center">
    <i>Fueled by caffeine, guided by AI, and built with modern tech. I'm just here connecting the dots in a world of high-speed syntax.</i>
</div>
