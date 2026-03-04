---
name: tunit
description: Write and run tests using the TUnit testing framework. Invoke this skill when writing tests, creating test projects, debugging test failures, or needing guidance on TUnit attributes, assertions, data sources, or lifecycle hooks.
argument-hint: "[task description]"
allowed-tools: Bash, Read, Write, Edit, Glob, Grep
---

# TUnit Testing Skill

You are an expert at writing tests with **TUnit**, a modern .NET testing framework built on Microsoft.Testing.Platform with source-generated test discovery.

## User Request

$ARGUMENTS

## Project Setup

### Creating a TUnit test project

The `.csproj` must use `OutputType Exe` (TUnit generates its own entry point). Do NOT add `Microsoft.NET.Test.Sdk`, `coverlet.collector`, or `coverlet.msbuild` — they are incompatible.

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="TUnit" Version="*" />
    </ItemGroup>
</Project>
```

No `using` statements needed — TUnit auto-configures global usings for `TUnit.Core`, `TUnit.Assertions`, and `TUnit.Assertions.Extensions`.

### Adding TUnit to an existing project

```bash
dotnet package add TUnit --prerelease --project tests/MyProject.Tests/MyProject.Tests.csproj
```

Remove any `Microsoft.NET.Test.Sdk` or `coverlet` packages if present.

---

## Running Tests

```bash
# Standard way (works with CI)
dotnet test

# Direct run (better output, supports TUnit-specific flags)
dotnet run --project tests/MyProject.Tests/

# With parallel limit
dotnet run --project tests/MyProject.Tests/ -- --maximum-parallel-tests 4

# Filter tests
dotnet test --filter "FullyQualifiedName~ParseTests"

# List tests without running
dotnet test -t

# With code coverage
dotnet run --project tests/MyProject.Tests/ --configuration Release --coverage

# With TRX report
dotnet run --project tests/MyProject.Tests/ -- --report-trx
```

---

## Core Attributes

### Test Definition

| Attribute | Purpose |
|-----------|---------|
| `[Test]` | Marks a method as a test (the only required attribute) |
| `[Arguments(...)]` | Inline parameterized test data |
| `[MethodDataSource(nameof(Method))]` | Data from a static method |
| `[MethodDataSource<T>(nameof(T.Method))]` | Data from method on another class |
| `[ClassDataSource<T>]` | Inject a class instance (on class or parameter) |
| `[MatrixDataSource]` | Combinatorial testing (on method) |
| `[Matrix(...)]` | Values for combinatorial parameters |

### Lifecycle Hooks

| Attribute | Scope | Must be static? |
|-----------|-------|-----------------|
| `[Before(Test)]` | Before each test | No |
| `[After(Test)]` | After each test | No |
| `[Before(Class)]` | Once before all tests in class | Yes |
| `[After(Class)]` | Once after all tests in class | Yes |
| `[Before(Assembly)]` | Once before all tests in assembly | Yes |
| `[After(Assembly)]` | Once after all tests in assembly | Yes |

Multiple `[After(Test)]` methods are guaranteed to run even if one fails.

### Test Control

| Attribute | Purpose |
|-----------|---------|
| `[Skip("reason")]` | Skip a test |
| `[Retry(n)]` | Retry on failure |
| `[Repeat(n)]` | Run n times |
| `[Timeout(ms)]` | Test timeout |
| `[DependsOn(nameof(Other))]` | Test ordering |
| `[NotInParallel]` | Prevent parallel execution |
| `[NotInParallel("key")]` | Prevent parallel within group |
| `[Category("name")]` | Categorize tests |
| `[Property("key", "value")]` | Test metadata |

---

## Assertions

All assertions are **async and must be awaited**. A Roslyn analyzer warns on unawaited assertions.

### Equality & Comparison
```csharp
await Assert.That(result).IsEqualTo(expected);
await Assert.That(result).IsNotEqualTo(0);
await Assert.That(result).IsGreaterThan(3);
await Assert.That(result).IsLessThan(10);
await Assert.That(result).IsBetween(1, 10);
await Assert.That(result).IsGreaterThanOrEqualTo(5);
```

### Null & Type
```csharp
await Assert.That(value).IsNull();
await Assert.That(value).IsNotNull();
await Assert.That(value).IsDefault();
await Assert.That(value).IsTypeOf<string>();
await Assert.That(value).IsAssignableTo<IEnumerable>();
```

### Boolean
```csharp
await Assert.That(condition).IsTrue();
await Assert.That(condition).IsFalse();
```

### Strings
```csharp
await Assert.That(name).Contains("test");
await Assert.That(name).StartsWith("Al");
await Assert.That(name).EndsWith("ice");
await Assert.That(name).Matches("^[A-Z].*");
await Assert.That(name).IsNotEmpty();
await Assert.That(name).HasLength(5);
```

### Collections
```csharp
await Assert.That(list).Contains(item);
await Assert.That(list).DoesNotContain(item);
await Assert.That(list).IsNotEmpty();
await Assert.That(list).HasCount().EqualTo(3);
await Assert.That(list).HasSingleItem();
await Assert.That(list).IsEquivalentTo(expectedList);
await Assert.That(list).IsInOrder();
```

### Exceptions
```csharp
await Assert.That(async () => await method())
    .ThrowsExactly<InvalidOperationException>();

await Assert.That(async () => await method())
    .ThrowsExactly<ArgumentException>()
    .WithMessage("expected message");

await Assert.That(() => method()).ThrowsNothing();
```

### Tolerance
```csharp
await Assert.That(timestamp)
    .IsEqualTo(DateTime.Now)
    .Within(TimeSpan.FromMinutes(1));
```

### Chaining
```csharp
await Assert.That(name)
    .IsNotNull()
    .And.IsNotEmpty()
    .And.StartsWith("Al");
```

### Multiple Assertions (reports all failures)
```csharp
await Assert.Multiple(() =>
{
    await Assert.That(user.Name).IsEqualTo("Alice");
    await Assert.That(user.Age).IsGreaterThan(18);
});
```

### Member Assertions
```csharp
await Assert.That(user).Member(u => u.Email).IsEqualTo("alice@example.com");
```

### Completion Timeout
```csharp
await Assert.That(async () => await longTask())
    .CompletesWithin(TimeSpan.FromSeconds(5));
```

---

## Data-Driven Tests

### Inline Arguments
```csharp
[Test]
[Arguments(1, 1, 2)]
[Arguments(2, 3, 5)]
[Arguments(10, -5, 5)]
public async Task Addition_ReturnsSum(int a, int b, int expected)
{
    await Assert.That(a + b).IsEqualTo(expected);
}
```

### Method Data Source
```csharp
[Test]
[MethodDataSource(nameof(GetTestData))]
public async Task MyTest(int value, string name)
{
    await Assert.That(value).IsGreaterThan(0);
}

// Return Func<T> for reference types to get fresh instances per test
public static IEnumerable<Func<(int, string)>> GetTestData()
{
    yield return () => (1, "one");
    yield return () => (2, "two");
}
```

### Method Data Source from Another Class
```csharp
[Test]
[MethodDataSource<TestDataProvider>(nameof(TestDataProvider.GetData))]
public async Task MyTest(int value) { ... }
```

### Matrix (Combinatorial)
```csharp
[Test]
[MatrixDataSource]
public async Task TestCombinations(
    [Matrix(1, 2, 3)] int x,
    [Matrix("a", "b")] string y)
{
    // Generates 6 test cases
    await Assert.That(x).IsGreaterThan(0);
}
```

---

## Dependency Injection

### Constructor Injection
```csharp
[ClassDataSource<DatabaseFixture>(Shared = SharedType.PerClass)]
public class UserTests(DatabaseFixture db)
{
    [Test]
    public async Task CanCreateUser()
    {
        var repo = new UserRepository(db.Connection);
        var user = await repo.CreateAsync("test@example.com");
        await Assert.That(user).IsNotNull();
    }
}
```

### Sharing Modes
| Mode | Description |
|------|-------------|
| `SharedType.None` | New instance per test (default) |
| `SharedType.PerClass` | Shared within test class |
| `SharedType.PerAssembly` | Shared across assembly |
| `SharedType.PerTestSession` | Shared for entire session |
| `SharedType.Keyed` + `Key = "name"` | Shared by key |

### Async Initialization
```csharp
public class DatabaseFixture : IAsyncInitializer, IAsyncDisposable
{
    public string ConnectionString { get; private set; } = "";

    public async Task InitializeAsync()
    {
        // Setup database, start containers, etc.
    }

    public async ValueTask DisposeAsync()
    {
        // Cleanup
    }
}
```

### Property Injection
```csharp
public class MyTests
{
    [ClassDataSource<MyService>(Shared = SharedType.PerTestSession)]
    public required MyService Service { get; init; }

    [Test]
    public async Task Test() { ... }
}
```

---

## Complete Example

```csharp
// No using statements needed

public class PlayFileParserTests
{
    [Test]
    public async Task Parse_SinglePlayFile_ReturnsOnePlay()
    {
        var json = await File.ReadAllTextAsync("testdata/single-play.bgsplay");

        var plays = PlayFileParser.Parse(json);

        await Assert.That(plays).HasSingleItem();
    }

    [Test]
    public async Task Parse_MultiPlayFile_ReturnsAllPlays()
    {
        var json = await File.ReadAllTextAsync("testdata/multi-play.bgsplay");

        var plays = PlayFileParser.Parse(json);

        await Assert.That(plays).HasCount().GreaterThanOrEqualTo(2);
    }

    [Test]
    [Arguments("7", 7.0)]
    [Arguments("11", 11.0)]
    [Arguments("3+4", 7.0)]
    [Arguments(null, null)]
    public async Task CalculateScore_ReturnsExpected(string? expression, double? expected)
    {
        var score = new Score { ScoreExpression = expression };

        var result = score.CalculateScore();

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task Parse_InvalidJson_ThrowsException()
    {
        await Assert.That(() => PlayFileParser.Parse("not json"))
            .ThrowsExactly<JsonException>();
    }

    [Test]
    [Skip("Pending implementation")]
    public async Task Parse_Scoresheet_ParsesCategories()
    {
        // TODO
    }
}
```

---

## Migration Quick Reference

### From xUnit
| xUnit | TUnit |
|-------|-------|
| `[Fact]` | `[Test]` |
| `[Theory]` | `[Test]` (+ data attributes) |
| `[InlineData(...)]` | `[Arguments(...)]` |
| `[MemberData]` | `[MethodDataSource]` |
| `[Trait("k","v")]` | `[Property("k","v")]` |
| `Assert.Equal(e, a)` | `await Assert.That(a).IsEqualTo(e)` |
| `Assert.True(x)` | `await Assert.That(x).IsTrue()` |
| `Assert.Throws<T>` | `await Assert.That(...).ThrowsExactly<T>()` |
| `Assert.Contains(i, c)` | `await Assert.That(c).Contains(i)` |
| Constructor | `[Before(Test)]` |
| `IDisposable` | `[After(Test)]` |
| `IClassFixture<T>` | `[ClassDataSource<T>(Shared = SharedType.PerClass)]` |

### From NUnit
| NUnit | TUnit |
|-------|-------|
| `[TestFixture]` | (not needed) |
| `[Test]` | `[Test]` |
| `[TestCase(...)]` | `[Arguments(...)]` |
| `[SetUp]` | `[Before(Test)]` |
| `[TearDown]` | `[After(Test)]` |
| `[OneTimeSetUp]` | `[Before(Class)]` (static) |
| `[OneTimeTearDown]` | `[After(Class)]` (static) |
| `[Ignore]` | `[Skip]` |

---

## Key Rules

1. **Always `await` assertions** — they are async
2. **`OutputType` must be `Exe`** in the test .csproj
3. **Never add `Microsoft.NET.Test.Sdk`** — it breaks TUnit
4. **No `using` statements needed** — global usings are auto-configured
5. **New class instance per test** — no shared state between tests
6. **Tests run in parallel by default** — use `[NotInParallel]` to opt out
7. **`[After(Test)]` always runs** — even if setup or test fails
