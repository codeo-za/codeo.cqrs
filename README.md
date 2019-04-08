# Codeo.CQRS

## What is it?
CQRS is a pattern for handling data: the Command Query Responsibility Separation.
see: [Wikipedia](https://en.wikipedia.org/wiki/Command–query_separation)

## Definitions
- Command: unit of work which writes to one or more data sources (may also read from one or more data sources)
- Query: unit of work which reads from one or more data sources (should not write to any, except logging, if necessary)
- Executor: logical construct which executes one or more commands and or queries

## Usage
1. Define commands and / or queries by defining new classes which derive from `Command<T>` or `Query<T>`
2. You *must* override the `Execute` method in your derived class. The logic within there must, if possible,
    set the `Result` property on the command or query it belongs to
3. You _may_ override the `Validate` method in your derived class. Command/Query executors will run this
    logic before attempting to `Execute`. Any exception thrown within the `Validate` method will prevent
    command / query execution.
4. Execute commands with an instance of `CommandExecutor`. Execute queries with an instance of `QueryExecutor`
5. You *must* configure the behavior of Codeo.CQRS with `Fluently.Configure()`
    1. You *must* at least specify a connection factory with `WithConnectionProvider`
    2. You _may_ register Sql mappings via the `WithEntitiesFrom` method, though this is optional:
       unknown entity types will be registered on fist use
    3. You _may_ register exception handlers via the `WithExceptionHandler` method
        1. if your exception handler is invoked and does not throw, the default value for the queried type is returned
        2. if your exception handler throws, that exception bubbles up
        3. if there is no exception handler for the type of exception which is caught by an executor, it bubbles up
        
See [TestQueryExecution.cs](src/Codeo.CQRS.Tests/TestQueryExecution.cs) for examples         
