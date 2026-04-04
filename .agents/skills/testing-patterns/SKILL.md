---
name: testing-patterns
description: Provides testing recipes, mocks setup, and syntax conventions for InventoryAlert.Tests. Trigger this skill whenever writing unit or integration tests, validating test coverage, or testing controllers/services/repositories using xUnit, Moq, or FluentAssertions.
---

# Testing Patterns for InventoryAlert.Tests

This skill dictates how to structure test suites, mock dependencies, and write assertions cleanly.

## Core Toolkit
- **xUnit**: Framework runner (`[Fact]`, `[Theory]`).
- **Moq**: For isolated dependency mocking (`_dep.Setup(...)`).
- **FluentAssertions**: For human-readable assertions (`result.Should().Be(...)`).

## Test Class Anatomy

```csharp
public class SubjectTests
{
    // 1. Declare mocks
    private readonly Mock<IDependency> _dep = new();
    private readonly SubjectClass _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    // 2. Wire SUT
    public SubjectTests()
    {
        _sut = new SubjectClass(_dep.Object);
    }
}
```

## Deep-Dives & Recipes

Based on what you are testing, use the following references for explicit code patterns:

- **Mocking Strategy & Moq Gotchas**: See [references/mock-recipes.md](references/mock-recipes.md). Specifically details how to properly mock `ExecuteTransactionAsync`.
- **Repository Integration (EF InMemory)**: See [references/ef-inmemory.md](references/ef-inmemory.md) to avoid state leakage between tests.
- **FluentAssertions Syntax**: See [references/fluent-assertions.md](references/fluent-assertions.md) for quick evaluation matching templates.

## Naming Convention
Strictly use: `<Method>_<ExpectedOutcome>_<Condition>`
*(Example: `GetById_ReturnsNull_WhenProductNotFound`)*
