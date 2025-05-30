# ASP.NET Atomic Coding Example

This solution shows an example of Atomic Coding Principles applied to a template for a production formatted ASP.NET application

# Projects
* Atomic.Net.Asp.AppHost - Core .Net Aspire Orchestration project
* Atomic.Net.Asp.DataService - Database Migrations and optional Data Seeding Worker Service
* Atomic.Net.Asp.Application - This is the Web App layer, which houses Asp.Net specific logic (Routing, DI, etc)
* Atomic.Net.Asp.SPWA - Svelte frontend application that connections to the above
* Atomic.Net.Asp.Domain - This is where the vast majority of the applications logic should go, but is designed to be agnostic to any Application specific implementation details. This project should have no concept of Asp.Net or whatever is consuming it
* Atomic.Net.Asp.IntegrationTests - xUnit project setup to run end-to-end Integration Tests against a full functional app stack, using Microsoft.Playwright to perform automated UI tests against the svelte frontend, all the way to the backing database. True end to end!
* Atomic.Net.Asp.UnitTests - xUnity project setup for Unit Testing Atomic Code pure functions
* Atomic.Net.Asp.DevProxy - A small dev only YARP reverse proxy app that enables reverse proxying of the web stack, to work around aspire's localhost only limitation

# Architecture 
The overal architecture uses the following principles:

## 1. Vertical Sliced Architecture
Domain Slices are grouped together. The main Domain Slice in this example are "Foos" which are an arbitrary abstract concept. All that matters is everything related to a "Foo" is in one place in the Domain project

## 2. CQRS (Command-Query-Response Segregation)
All logic flows are branched into being either: 
* Queries (which do not require full ACID transactional database behavior, and are expected to be ReadOnly Idemptotent)
* Commands (Which do require full ACID transactions and are expected to perform database mutations)

We do **NOT** use unnecessary abstraction tools like Mediatr, a project would need to be **extremely** complicated and have a very intricate pipeline to justify mediator pattern

Instead, we simply just... call our methods directly.

For all intents and purposes, logic flows as such:

1. Entry level `Program.cs`, where application is bootstrapped
2. Route binding occurs in Routing.cs where a route is bound up to a Handler Delegate
3. Inner logic that is written to be pure and stateless handles the process of all of:
    a. TransactionHandler, which will wrap Commands (but not Queries) in a DB Transaction that auto rolls back if anything goes wrong
    b. QueryHandler, which pulls metadata out of the HttpContext and composes the RequestContext, which holds any requested additional service injections as well (or just set it to a `Unit` if you want nothing)
    c. ValidationHandler, which will automatically run any detected validation on the request (and short circuit out if validation fails
4. At which point finally logic gets handed to the actual registered delegated handler you built, with a properly scoped DB, metadata, services all bundled in the RequestContext for you, as well as a DB Transaction already opened if needed, and validation completed. 

## 3. NOT using Exception throwing as logic switches
A very common pattern observed in ASP.NET (especially by junior developers) is the naive approach of leveraging a `throw` and a custom `MyApplicationException` in order to communicate Non-Exceptional behaviors upwards to the Controller/HttpContext layer.

This is an anti-pattern, as if you think about it for a moment, this is simply just a GOTO statement with extra steps.

Throwing exceptions should only be used for truly exceptional circumstances when you truly did *not* expect the scenario to even occur.

All other scenarios where you actually expect an error to occur due to invalid input or etc, should return a `Result` from your method which can "smoothly fail"

For the case of this project we utilize the `IDomainResult` interface semaphore, which lets us return either a Success or Fail result, with a matching "why" status code, message, and id field for what caused the failure.

There are many reasons why this is a far cleaner solution. Primarily it boils down to these though:

1. Throwing Exceptions is incredibly expensive, and it really messes up CPU and compiler optimization capability
2. Throwing Exceptions brutalizes your performance, the act of snagging a stack trace (which usually gets discarded) is very expensive, and the disruption of the stack obliterates cycles
3. It will clog up your logs for really no good reason, making it much harder to notice REAL exceptions getting thrown from your "fake" exceptions you keep throwing around.

## Source Gen Validation
Libraries like [Validly](https://github.com/Hookyns/validly) provide extremely performant ways to do validation on Queries / Commands, letting us utilize Attribute based Validation, while reducing overall performance hits they normally would incur.

## Avoiding Mocking
You may notice not a single "mock" is used in any of the Unit or Integration tests. The reason for this is the core principle of Atomic Programming: If you organize your code dependancies well, there is no need to mock anything.

Unit testable code can be broken out to Pure Atomic functions, which have no dependancy on anything that requires mocking (see the example string "Sanitize" extension method in the Domain project)

Integration tested code simply runs against a real, actual database. There's no need for mocks there

The only exception to this is when you depend on external 3rd party APIs of some sort. In which case you still wont do a full on "Mock", but you will want to leverage the [HttpMessageHandler](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpmessagehandler?view=net-9.0) class to simply override behavior of an HttpClient easily

The rare case where your codebase is tightly coupled to some 3rd party library that doesnt expose the ability to do this is the rare case where I would consider Mocking to still be applicable, leveraging Decorator Pattern

# Trying it out for yourself

## Atomic.Net.Asp.AppHost (Running the app)
1. `dotnet run` the Atomic.Net.Asp.AppHost project
2. You'll see something in the console akin to:

```
Login to the dashboard at https://0.0.0.0:17131/login?t=<some token>
```

Open this url in your browser to check out the .Net Aspire dashboard
3. You should see a url for the DevProxy, likely `http://localhost:50001`
4. `curl localhost:5000/foos/1` to see an example success result
5. `curl localhost:5066/foos/1000` to see an example NotFound result
6. `curl localhost:5066/foos/100000` to see an example Validation Error result

## Atomic.Net.Asp.UnitTests
1. `dotnet test` this project should be all that is required

## Atomic.Net.Asp.IntegrationTests
1. `dotnet test` this project should be all that is required
2. Note that via .Net Aspire, this project still stands up a database and runs against it, but the database runs in docker and gets scaffolded then torn down automatically.

# Using a custom Hostname (IE testing from a mobile phone instead of localhost)

1. `cd` to the AppHost project
2. `dotnet user-secrets set HostOverride "$(hostname)"`

This will override the hostnames used for the entire app stack, as well as enable the YARP dev proxy project, which will automatically reverse proxy all of aspire's load balanced apps to your hostname, with new ports for each proxy.

To access the apps, use the URLs displayed under the DEVPROXY resource, instead of the original un-proxied URLs displayed on the apps themselves.
