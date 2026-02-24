# 🤝 Contributing to DotNet.Architect.Playbook

First off, thank you for wanting to help! To keep our codebase clean and maintainable, we follow a strict workflow inspired by "Big Tech" standards.

---

## 🏗️ Our Development Workflow

We follow an **Issue-First** workflow. No code should be written without an associated issue.

### 1. Create an Issue
Before starting any work, [open a new issue](https://github.com/TheGresta/DotNet.Architect.Playbook/issues/new/choose). 
* Use the **Feature Request** or **Bug Report** templates.
* Once submitted, note the **Issue Number** (e.g., `#42`).

### 2. Branch Naming Convention
Branches must be prefixed based on the work type.
* **Format:** `type/issue-number-short-description`
* **Types:** `feat/`, `fix/`, `docs/`, `refactor/`, `chore/`

**Examples:**
- `feat/42-add-stripe-integration`
- `fix/105-header-mobile-overlap`

### 3. Commit Messages
We use **Conventional Commits**. This allows us to automate our changelogs.
* **Format:** `type(scope): [Issue #ID] description`

**Common Examples:**
- `feat(api): [Issue #42] add post request for checkout`
- `fix(ui): [Issue #105] resolve z-index issue on navbar`
- `docs: update installation instructions`

---

## 🚀 Creating a Pull Request (PR)

Once your code is ready:
1. **Push your branch** to GitHub.
2. **Open a PR** against the `main` branch.
3. **Link the Issue:** In the PR description, use the keyword `Closes #IssueNumber`. 
   * *Example: "This PR adds the payment gateway. Closes #42."*
4. **Wait for CI:** Ensure all automated tests pass (Green checkmark ✅).
5. **Code Review:** At least one maintainer must approve your code before it can be merged.

---

## 🛠️ Local Development Setup
1. Clone the repo: `git clone ...`
2. Install dependencies: `npm install` (or your command)
3. Create your branch: `git checkout -b feat/your-issue-number-description`

---

## ⚖️ Code of Conduct
Be respectful and constructive. We are here to build great software together!
