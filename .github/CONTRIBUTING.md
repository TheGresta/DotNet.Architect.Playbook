# 🤝 Contributing to DotNet.Architect.Playbook
Thank you for your interest in contributing! This project aims to provide a high-standard reference for .NET architecture. To maintain the quality and consistency of the codebase, we follow a structured workflow inspired by industry best practices.

---

## 🏗️ Our Development Workflow
We operate on an **Issue-First** basis. To ensure all changes are tracked and discussed, please do not submit a Pull Request without a linked issue.

### 1. Create an Issue
Before starting any work, [open a new issue](https://github.com/TheGresta/DotNet.Architect.Playbook/issues/new/choose). 
* Use provided templates for **Feature Request** or **Bug Report**.
* Note the Issue Number (e.g., `#42`) for use in your branch and commits.

### 2. Branch Naming Convention

We use a structured naming convention to keep the repository organized.

| Prefix | Purpose | Example  |
|------------|------------|---------|
| `feat/`| New features or architecture patterns | `feat/42-stripe-integration` |
| `fix/` | Bug fixes | `fix/105-header-overlap` |
| `docs/` | Documentation changes only | `docs/update-readme` |
| `refactor/` | Code changes that neither fix a bug nor add a feature | `refactor/api-logic` |
| `chore/` | Maintenance tasks (dependencies, CI/CD) | `chore/update-nuget` |

### 3. Commit Standards
We follow **[Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/)**. This allows us to generate automated changelogs.

**Format:** `type(scope): [Issue #ID] description`
* **Example:** `feat(api): [Issue #42] implement idempotency logic for checkout`
* **Example:** `fix(ui): [Issue #105] resolve navbar z-index on mobile`

---

## 🚀 Pull Request (PR) Process
1. **Fork & Clone**: Work on your fork and push your branch there.
2. **Submit PR**: Open a PR against our `main` branch.
3. **Link Issues**: Use GitHub keywords in the description (e.g., Closes `#42`) so the issue is automatically resolved upon merge.
4. **Continuous Integration**: Verify that all CI checks (unit tests, linting, builds) pass.
5. **Review**: A maintainer will review your code. Please be prepared to iterate on feedback.

---

## 🛠️ Local Development Setup
To get started with the codebase:

1. Clone the repo:
```bash
git clone https://github.com/TheGresta/DotNet.Architect.Playbook.git
```
2. Initialize Environment:
```bash
# Add your specific setup commands here, for example:
dotnet restore
dotnet build
```
3. Create your feature branch:
```bash
git checkout -b feat/your-issue-number-description
```

---

## ⚖️ Code of Conduct
We are committed to fostering a welcoming and inclusive community. By participating, you agree to abide by our standards of professional and respectful communication.
