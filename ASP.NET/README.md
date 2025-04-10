# ASP.NET Atomic Coding Example

This solution shows an example of Atomic Coding Principles applied to a template for a production formatted ASP.NET application

# Projects
* Atomic.Net.Asp.Application - This is the Web App layer, which houses Asp.Net specific logic (Routing, DI, etc)
* Atomic.Net.Asp.Domain - This is where the vast majority of the applications logic should go, but is designed to be agnostic to any Application specific implementation details. This project should have no concept of Asp.Net or whatever is consuming it
* Atomic.Net.Asp.IntegrationTests - xUnit project setup to run end-to-end Integration Tests against a full functional Postgresql database
* Atomic.Net.Asp.UnitTests - xUnity project setup for Unit Testing Atomic Code pure functions

# Architecture 
The overal architecture uses the following principles:

## 1. Vertical Sliced Architecture
Domain Slices are grouped together. The main Domain Slice in this example are "Foos" which are an arbitrary abstract concept. All that matters is everything related to a "Foo" is in one place in the Domain project

## 2. CQRS (Command-Query-Response Segregation)
All logic flows are branched into being either: 
* Queries (which do not require full ACID transactional database behavior, and are expected to be ReadOnly Idemptotent)
* Commands (Which do require full ACID transactions and are expected to perform database mutations)

We do **NOT** use unneccessary abstraction tools like Mediatr, a project would need to be **extremely** complicated and have a very intricate pipeline to justify mediator pattern

Instead, we simply just... call our methods directly.

For all intents and purposes, logic flows as such:

1. Entry level `Program.cs`, where application is bootstrapped
2. Route binding occurs in Routing.cs where a route is bound up to a controller method
3. Controllers (which are just static classes) inject the necessary stateful / object pooled objects (like DB Connections, HttpClients, Loggers, HttpContext, etc)
4. And then hand off work directly to the Command/Query handler via the helper method "HandleCommand" / "HandleQuery"

The only difference between HandleCommand vs HandleQuery is simply whether a wrapping Database Transaction gets opened or not.

## 3. NOT using Exception throwing as logic switches
A very common pattern observed in ASP.NET (especially by junior developers) is the naive approach of leveraging a `throw` and a custom `MyApplicationException` in order to communicate Non-Exceptional behaviors upwards to the Controller/HttpContext layer.

This is an anti-pattern, as if you think about it for a moment, this is simply just a GOTO statement with extra steps.

Throwing exceptions should only be used for truly exceptional circumstances when you truly did *not* expect the scenario to even occur.

All other scenarios where you actually expect an error to occur due to invalid input or etc, should return a `Result` from your method which can "smoothly fail"

For the case of this project we utilize the `IResult` interface semaphore, which lets us return either a Success or Fail result, with a matching "why" status code, message, and id field for what caused the failure.

There are many reasons why this is a far cleaner solution. Primarily it boils down to these though:

1. Throwing Exceptions is incredibly expensive, and it really messes up CPU and compiler optimization capability
2. Throwing Exceptions brutalizes your performance, the act of snagging a stack trace (which usually gets discarded) is very expensive, and the disruption of the stack obliterates cycles
3. It will clog up your logs for really no good reason, making it much harder to notice REAL exceptions getting thrown from your "fake" exceptions you keep throwing around.

## Avoiding Mocking
You may notice not a single "mock" is used in any of the Unit or Integration tests. The reason for this is the core principle of Atomic Programming: If you organize your code dependancies well, there is no need to mock anything.

Unit testable code can be broken out to Pure Atomic functions, which have no dependancy on anything that requires mocking (see the example string "Sanitize" extension method in the Domain project)

Integration tested code simply runs against a real, actual database. There's no need for mocks there

The only exception to this is when you depend on external 3rd party APIs of some sort. In which case you still wont do a full on "Mock", but you will want to leverage the [HttpMessageHandler](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpmessagehandler?view=net-9.0) class to simply override behavior of an HttpClient easily

The rare case where your codebase is tightly coupled to some 3rd party library that doesnt expose the ability to do this is the rare case where I would consider Mocking to still be applicable, leveraging Decorator Pattern

# Trying it out for yourself

## Atomic.Net.Asp.Application
1. Make sure you have an existing empty Postgres database setup
2. Set the environment variable `ATOMIC_ASPNET__ConnectionStrings_DefaultConnection` to be the connection string to aformentioned Postgres DB
3. `dotnet run` the Atomic.Net.Asp.Application project
4. `curl localhost:5066/foos/1` (the port may be different for you)

## Atomic.Net.Asp.UnitTests
1. `dotnet test` this project should be all that is required

## Atomic.Net.Asp.IntegrationTests
1. Make sure you have an existing empty Postgres database setup (You will want one different from the one you may have used for the Application)
2. Set the environment variable `ATOMIC_ASPNET_TEST_ConnectionStrings_DefaultConnection` to be the connection string to aformentioned Postgres DB (**NOTE: This Env var has a slightly different name from the application one above!**)
3. `dotnet test` the Atomic.Net.Asp.IntegrationTests project
4. Note the database after doesnt get any data added to its tables, as all individual tests are executed inside of transactions that get cancelled after the test finishes, rolling back any changes
