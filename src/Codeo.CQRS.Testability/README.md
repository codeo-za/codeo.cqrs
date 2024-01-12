Codeo.CQRS.Testability
---

This is a library to make testing code using Codeo.CQRS a little more convenient,
specifically:
- fluent extension methods for IQueryExecutor and ICommandExecutor to mock calls for
  NSubstitute substitutes:
  - `queryExecutor.WithMocked<TQuery, TResult>(...)`
  - `commandExecutor.WithMocked<TCommand, TResult>(...)`

Suggested use is as follows:
```csharp
var queryExecurtor = Substitute.For<IQueryExecutor>()
    .WithMocked<SomeQuery, SomeResult>(
        // see the tests for examples
    ).WithMocked<AnotherQUery, AnotherResult>(
        ...
    );
var commandExecutor = Substitute.For<ICommandExecutor>()
    .WithMocked<SomeCommand, SomeResult>(
        // see tests for examples
    ).WithMocked<AnotherCommand, AnotherResult>(
        ...
    );
```

Commands and queries can have their results mocked in the following ways:
1. With a constant result for all usages
```csharp
var expected = new SomeResult();
var queryExecutor = Substitute.For<IQueryExecutor>()
    .WithMocked<SomeQuery, SomeResult>(expected);
```
2. With a constant result based on the incoming command/query
```csharp
var expected = new SomeResult();
var queryExecutor = Substitute.For<IQueryExecutor>()
    .WithMocked<SomeQuery, SomeResult>(
        q => q.SomeParameter == 1,
        expected
    );
```
3. With full control over mock selection and logic
```csharp
var expected = new SomeResult();
var queryExecutor = Substitute.For<IQueryExecutor>()
    .WithMocked<SomeQuery, SomeResult>(
        q => q.SomeParameter == 1,
        q => 
        {
            // ... perform some other logic
            return expected;
        }
    );
```