# AGENT.md

## Purpose

This document defines how the coding agent should work in this repository.
The goal is to keep the codebase simple, maintainable, modern, and production-ready for **C# 14 / .NET 10**.

The agent must prefer clarity and consistency over cleverness.
The agent must avoid unnecessary code, abstractions, dependencies, comments, files, and patterns.

---

## Core Principles

* Write the **simplest correct solution**.
* Prefer **readability over cleverness**.
* Prefer **small focused changes** over large rewrites.
* Keep the architecture **boring, standard, and easy to maintain**.
* Follow existing project conventions unless they are clearly harmful.
* Remove duplication when it improves clarity, but do not abstract too early.
* Use modern .NET features only when they **improve clarity, safety, or maintainability**.
* Avoid speculative design for hypothetical future requirements.

---

## Non-Goals

The agent must **not**:

* Add unnecessary layers or design patterns.
* Introduce new dependencies without strong justification.
* Create helper methods, wrappers, or extension methods unless they are reused or clearly improve readability.
* Split code into many files when one file is easier to understand.
* Add comments that merely restate the code.
* Write overly generic code for requirements that do not exist yet.
* Use reflection, dynamic behavior, or metaprogramming unless explicitly required.
* Replace stable standard library features with custom implementations.

---

## Target Stack

Default assumptions unless the repository says otherwise:

* **.NET 10**
* **C# 14**
* **ASP.NET Core / Worker / Console** depending on project type
* **Nullable reference types enabled**
* **Implicit usings enabled**
* **SDK-style projects**
* **Dependency Injection via Microsoft.Extensions.DependencyInjection**
* **Configuration via appsettings + environment variables + options pattern**
* **Logging via Microsoft.Extensions.Logging**
* **Testing with xUnit or the repository’s existing test framework**

Use the built-in .NET platform first. Prefer Microsoft-supported libraries and framework features over third-party packages where reasonable. .NET 10 is currently supported, and C# 14 ships with .NET 10. ([learn.microsoft.com](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14?utm_source=chatgpt.com))

---

## Architecture Guidance

### General

* Prefer a **simple layered structure**:

    * Domain or core logic
    * Application or service logic
    * Infrastructure or external integrations
    * API / host / UI
* Keep business rules out of controllers, endpoints, and infrastructure code.
* Depend on abstractions only where they provide real value.
* Use interfaces mainly for:

    * external dependencies
    * test seams
    * multiple implementations that actually exist
* Do not create interfaces for every class by default.

### For ASP.NET Core

* Prefer **Minimal APIs** for small and straightforward APIs.
* Use controllers only when they improve organization for larger HTTP surfaces.
* Keep endpoints thin.
* Put validation, orchestration, and business logic in dedicated services.
* Use async end-to-end for I/O-bound work.
* Do not block async code with `.Result`, `.Wait()`, or sync-over-async. ASP.NET Core guidance explicitly recommends asynchronous APIs and warns against blocking calls because they can cause thread pool starvation and poor throughput. ([learn.microsoft.com](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices?view=aspnetcore-10.0&utm_source=chatgpt.com))

### For Data Access

* Prefer **EF Core** when already in use or when it fits the application.
* Keep queries explicit and easy to reason about.
* Avoid hidden lazy-loading behavior unless the project intentionally uses it.
* Use projections for read models when full entities are unnecessary.
* Keep transaction boundaries clear.

### For Background Processing

* Use `IHostedService` / `BackgroundService` for hosted background work.
* Keep jobs idempotent where possible.
* Respect cancellation tokens.
* Log failures with enough context for diagnosis.

---

## Coding Style

### General Style

* Use clear, descriptive names.
* Prefer short methods with a single responsibility.
* Keep nesting shallow.
* Prefer guard clauses and early returns.
* Prefer composition over inheritance.
* Prefer immutable data where practical.
* Use `record` / `record class` / `record struct` when value semantics are appropriate.
* Use `sealed` on classes that are not designed for inheritance when it improves correctness.
* Use file-scoped namespaces.
* Keep one public type per file unless a small grouping is clearly better.

### Language Features

Use modern C# features when they improve the codebase, including nullable reference types, pattern matching, primary constructors where appropriate, collection expressions where they improve clarity, and C# 14 features where they make code simpler and more expressive. C# 14 adds new extension member capabilities and ships with .NET 10. ([learn.microsoft.com](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14?utm_source=chatgpt.com))

Do not use a feature just because it is new.
If a traditional form is clearer for the team, use the clearer form.

### Nullability

* Treat nullable warnings as real design feedback.
* Avoid using `!` unless there is no better option.
* Model nullability honestly in APIs and DTOs.
* Validate external inputs at boundaries.

### Exceptions

* Use exceptions for exceptional cases, not normal control flow.
* Throw specific exceptions when useful.
* Do not swallow exceptions.
* Add context when rethrowing only if it materially helps.

---

## Dependency Injection

* Register only what is needed.
* Prefer constructor injection.
* Keep service lifetimes correct:

    * Singleton for stateless shared services
    * Scoped for request/unit-of-work scoped services
    * Transient for lightweight stateless services when appropriate
* Do not use the service locator pattern.
* Do not inject `IServiceProvider` unless absolutely necessary.
* Avoid overly deep dependency graphs.

---

## Configuration and Options

* Use the **options pattern** for structured settings.
* Keep configuration strongly typed.
* Validate configuration on startup when the app cannot run correctly without it.
* Never hardcode secrets.
* Use environment variables, secret stores, or platform secret management.

---

## Logging and Observability

* Use structured logging.
* Log at appropriate levels:

    * Trace / Debug for diagnostics
    * Information for normal important flow
    * Warning for recoverable issues
    * Error for failures
    * Critical for severe system failures
* Do not log secrets or sensitive data.
* Include correlation or request identifiers when relevant.
* Prefer built-in observability integrations and OpenTelemetry-friendly patterns when the project uses them.

---

## Validation

* Validate at system boundaries.
* Fail fast for invalid inputs.
* Keep validation explicit and easy to find.
* Avoid scattering validation rules across many layers.

---

## API Guidance

* Return consistent response shapes where the project expects them.
* Use standard HTTP status codes correctly.
* Version APIs only when necessary.
* Generate and keep OpenAPI metadata accurate when the project exposes APIs.
* Use pagination for large collections. ASP.NET Core guidance recommends returning large collections across multiple smaller pages to avoid excessive memory use and poor performance. ([learn.microsoft.com](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices?view=aspnetcore-10.0&utm_source=chatgpt.com))
* Add rate limiting, caching, and request timeout policies where appropriate. These are part of the current ASP.NET Core 10 performance guidance. ([learn.microsoft.com](https://learn.microsoft.com/en-us/aspnet/core/performance/overview?view=aspnetcore-10.0&utm_source=chatgpt.com))

---

## Performance Guidance

* Measure before optimizing.
* Prioritize algorithmic simplicity and correct data access patterns.
* Avoid unnecessary allocations in hot paths.
* Use streaming for large payloads where appropriate.
* Cache only when there is a demonstrated benefit.
* Avoid blocking calls in ASP.NET Core request paths. Microsoft’s guidance calls this out as a common source of thread pool starvation and degraded response times. ([learn.microsoft.com](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices?view=aspnetcore-10.0&utm_source=chatgpt.com))

Do not introduce micro-optimizations that make the code harder to maintain unless profiling shows they are needed.

---

## Testing Guidance

* Add or update tests for behavior that changed.
* Prefer **unit tests for business logic**.
* Prefer **integration tests for infrastructure and API behavior**.
* Test observable behavior, not implementation details.
* Keep tests readable and deterministic.
* Avoid excessive mocking.
* Use real objects where practical and cheap.
* One test should verify one behavior conceptually, even if setup is shared.

---

## Persistence and Migrations

* Keep database migrations focused and reviewable.
* Do not mix unrelated schema changes.
* Ensure destructive changes are explicit.
* Prefer backward-compatible changes for production systems when possible.

---

## Security Guidance

* Validate and sanitize external input.
* Use authentication and authorization provided by the platform.
* Apply least-privilege access.
* Never log secrets, tokens, or credentials.
* Use HTTPS and secure defaults.
* Keep dependencies updated and minimize package count.

---

## Package and Dependency Rules

* Prefer the .NET base class library and official Microsoft packages first.
* Add a third-party package only when it provides clear value.
* Do not add packages for small utilities already handled well by the platform.
* Avoid overlapping dependencies.
* Remove unused packages.

---

## Project File and Repository Hygiene

* Keep `.csproj` files minimal and tidy.
* Centralize shared package versions when the repository already uses central package management.
* Keep analyzers and formatting rules consistent across the repository.
* Do not add build complexity without clear payoff.

.NET 10 continues SDK and tooling improvements, including build and task execution improvements in the SDK/tooling stack. ([learn.microsoft.com](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/sdk?utm_source=chatgpt.com))

---

## Documentation Rules

* Document **why**, not **what**, when code alone is not enough.
* Keep README and setup instructions accurate.
* Prefer short, actionable documentation.
* Do not create documentation files unless they solve a real need.

---

## When Making Changes

The agent should usually follow this order:

1. Understand the requirement.
2. Inspect the existing code and conventions.
3. Make the smallest clean change that solves the problem.
4. Update tests.
5. Ensure formatting, analyzer warnings, and build quality remain clean.
6. Avoid opportunistic refactoring unless it is directly helpful.

---

## What the Agent Should Avoid Writing

Avoid introducing any of the following unless clearly justified:

* Marker interfaces
* Generic repository wrappers over EF Core
* Pass-through service layers with no logic
* Over-engineered CQRS for small applications
* Custom result frameworks when standard responses are enough
* Deep inheritance hierarchies
* Static global state
* Premature caching
* Premature event-driven decomposition
* Large utility classes
* Comment noise
* Dead code
* Placeholder abstractions for future plans

---

## Preferred Defaults

When several valid choices exist, prefer:

* **Simple over flexible**
* **Explicit over magical**
* **Built-in over custom**
* **Composition over inheritance**
* **Concrete types over unnecessary abstractions**
* **Small files over huge files**, but not fragmented files
* **Focused tests over broad brittle tests**
* **Clear APIs over clever APIs**

---

## Pull Request Expectations

Changes produced by the agent should:

* Build cleanly
* Pass relevant tests
* Match repository conventions
* Contain no unrelated refactors
* Contain no unnecessary dependencies
* Contain no speculative abstractions
* Be easy for another developer to review quickly

---

## Final Rule

The best solution is usually the one that is:

* easy to read
* easy to test
* easy to change
* hard to misuse
* aligned with standard .NET practices
* no more complicated than necessary

When unsure, choose the more maintainable and more standard approach.
