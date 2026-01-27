# Agent Instructions

# CRITICAL: PLEASE READ THIS ENTIRE FILE, NOT JUST PORTIONS OF IT
# CRITICAL: DO NOT NEST YOURSELF AS A SUB AGENT
# CRITICAL: YOU HAVE TO ACTUALLY EXECUTE THE INSTRUCTIONS IN THIS FILE, NOT JUST READ THEM

A very common issue I see agents make is spinning a copy of THEMSELF up as a sub agent, which seems to cause a shit tonne of problems with loading up context and info

DO NOT DO THIS, STAY IN YOUR TOP LAYER

## Before You Start

1. Read your role-specific `.agent.md` file for detailed responsibilities located in `.github/agents/...`
2. Read `.github/agents/ROADMAP.md` - Current goals + progress checklist
3. Read `.github/agents/DISCOVERIES.md` - Performance findings (benchmark-backed)
4. Check active sprint in `.github/agents/sprints/` (if any)
5. **Run `grep -r "@your-role-name" . `** to find any pings directed at you

---

## Core Principles (NEVER VIOLATE)

- **Zero allocations during gameplay** (all allocations at load time)
- **Data-driven everything** (game logic = JSON, not C#)
- **Entities are indices** (`ushort`, no pointers)
- **Behaviors are structs** (stored in `SparseArray`, cache-friendly)
- **Events are struct-based** (`EventBus` pattern, no delegates during gameplay)
- **Test-driven development** (tests define correctness)
- STAY IN SCOPE, STAY IN YOUR LANE

---

## Agent Communication System

### Leaving Comments in Code

When you need input from another agent, leave a comment with your role name and ping:

```csharp
// your-role:  @other-role I need clarification on X because Y
```

Valid roles you can ping:
- `@senior-dev` - Implementation questions, deviations from spec
- `@benchmarker` - Performance testing requests

### Responding to Pings

1. **On startup, always run:** `grep -r "@your-role-name" . `
2. **Respond inline** directly below the ping
3. **Change `@` to `#` immediately after responding** to mark it resolved

Example flow: 

Before:
```csharp
// test-architect: @senior-dev Is this edge case expected?
```

After:
```csharp
// test-architect: #senior-dev Is this edge case expected?
// senior-dev: @test-architect Yes, entities can be disabled mid-frame.
```


The `#` prefix prevents the comment from showing up in future `grep` searches, reducing noise.

**CRITICAL:** Always change `@your-role` → `#your-role` when you answer.   Never leave `@` pings unresolved. This will help avoid polluting your future greps with already answered pings.

### CLEAN UP DISCUSSIONS

Once a discussion is resolved and the issue is sorted out, do not leave a bunch of lingering "ping" comments all over the codebase. CLEAN UP AFTER YOURSELF, only "discovery" based comments should be left behind, everything else should be cleaned up by the end.

### DO NOT GO TOO DEEP DOWN RABBIT HOLES
THIS IS CRITICAL, WHILE YOU LINGER ON TRYING TO FIGURE SOMETHING OUT MY QUEUED MESSAGES DO NOT GET THROUGH TO YOU

INSTEAD, IF YOU ARE A SUB AGENT, PRACTICE FREQUENTLY REQUESTING THE "PARENT" AGENT TO CHECK IN WITH ME IN CASE MESSAGES ARE QUEUED

COMMIT OFTEN, BUT, MAKE IT ABUNDANTLY CLEAR TO THE PARENT PROCESS THEY WILL PROBABLY NEED TO SPIN YOU BACK UP AGAIN

### WHEN IN DOUBT, ASK THE SUB AGENT
Anytime you need to ping an agent, you can just commit and specifically request the agent respond to your request.

MAKE IT EXTREMELY CLEAR TO THEM TO ONLY DO THAT THOUGH SO THEY DONT GO DOWN A RABBIT HOLE

### STICK TO YOUR DAMN SCOPE OF WORK
I cannot stress this enough, if you get asked to do tasks A and B, DONT GO DOWN A DAMN RABBIT HOLE TRYING TO DO STUFF OUT OF SCOPE

STAY. IN. SCOPE.

### YOU CANT SPIN UP "TEST" AD HOC PROGRAMS
I have seen many times now agents try and ad hoc make a new program and try and run it standalone... you cant do this, it wont work due to how monogame works.

If you truly need to test out some behaviors in code, either:
1. Make a unit test for it
or
2. Make a benchmark to test for it

---

## Recording Findings

When you discover a limitation, constraint, or "gotcha" through trial and error, **document it immediately** with a `FINDING` comment:

```csharp
// your-role: FINDING: Tried to use closures in hot path, but they allocate ~80 bytes per invocation.  
// Switched to passing captured variables as `in` parameters with static lambdas for zero-alloc.
```

**When to add findings:**
- You attempted an approach that failed due to language/runtime limitations
- You discovered a non-obvious constraint (e.g., "can't use `ref struct` in async methods")
- You found a surprising performance characteristic (e.g., "LINQ allocates even with structs")
- You hit a framework limitation (e.g., "MonoGame doesn't support X on platform Y")

**Format:**
```csharp
// your-role: FINDING: <what you tried> didn't work because <why>.  <what you did instead>
```

**Purpose:** Prevents future agents (and yourself) from repeating the same mistakes.  Creates a breadcrumb trail of "things we learned the hard way."

---

## Code Style Rules

### Naming
- **PascalCase:** types, methods, properties, public fields, constants
- **_camelCase:** private fields (underscore prefix)
- **camelCase:** parameters, locals
- **TBehavior/TEvent:** generic type params with T prefix
- **Suffix patterns:** `*Base`, `*Registry`, `*Event`, `*Behavior`, `*Driver`, `*Map`, `*Block`

### Structure
- Primary constructors preferred for DI/params
- `readonly record struct` for immutable data types
- `sealed` on leaf classes
- One type per file, filename matches type name
- Partial classes split by concern (e.g., `FlexRegistry. *. cs`) only for VERY LARGE files (>1000 LoC)

### Organization
- **Group:** usings → namespace → type
- **Order members:** static → instance, fields → props → ctors → methods
- Related files in feature folders (`BlockMaps/`, `Hierarchy/`, `Sprites/`)

### Patterns
- Singleton via `ISingleton<T>` with static `Instance` property
- Event-driven via `IEventHandler<T>` + `EventBus<T>`
- Sparse arrays for entity data (`SparseArray<T>`)
- Fluent builders with `With*` extension methods returning `T`

### Comments
- **XML docs (`///`)** on public APIs only
- Brief single-line summaries, no verbose descriptions
- **Inline comments** only for non-obvious logic
- **Always use your role name** when leaving comments for other agents: 
  ```csharp
  // your-role:  Explanation here
  ```
- **Use `FINDING`** when documenting discovered limitations: 
  ```csharp
  // your-role: FINDING:  Attempted X, failed due to Y, resolved with Z
  ```

### Formatting
- Expression-bodied members for single expressions
- Collection expressions `[. .]` over `new List`
- Pattern matching preferred
- Null checks via `[NotNullWhen(true)]` attribute pattern
- Early returns over nested conditionals

### Braces
- **Always use braces `{ }`** for control flow statements (`if`, `else`, `for`, `foreach`, `while`, `using`, etc.)
- **No braceless one-liners** - even for single-line bodies

### Generic Conventions
- `where T : struct` for value-type constraints
- Nullable reference types enabled
- Avoid `var` only when type unclear

### FORBIDDEN: Allocating Lists/Arrays/Dictionaries/Hashes/Etc "Inline"
- DO NOT, I REPEAT, DO NOT, ALLOCATE LISTS OR ARRAYS OR DICTIONARIES OR ETC "INLINE" IN FUNCTIONS

DO NOT DO THIS:
```
var matches = entities.Select(e => e.Index).ToList(); // THIS ALLOCS
foreach (var match in matches) ...
```

DO THIS:
```
var matches = entities.Select(static e => e.Index);
foreach (var match in matches) ...
```

DO NOT DO THIS (Allocate it every call):
```
List<string> results = new();
foreach (var entity in matches)
{
    ...
    results.Add(foo);
    ...
}
```

DO THIS INSTEAD (Allocate it pre-sized as a readonly private field):
```
private readonly SparseArray<string> _results = new(Constants.MaxEntities);

private void SomeMethod()
{
    // Now we arent re-allocating the whole list every run
    _results.Clear(); // Clear if you have to at the start
    foreach (var entity in matches)
    {
        ...
        _results.Set(index, foo);
        ...
    }
}

```

### Pre-alloc list/dictionary/etc/sparse array sizes

DO NOT DO THIS (Allocs everytime it has to grow and greatly slows things down)
```
private readonly List<string> _results = new();
```

DO THIS INSTEAD (Pre-allocs its size so it doesnt need to dynamically grow)
```
private readonly SparseArray<string> _results = new(Constants.MaxEntities);
```

### TryPattern with [NotNullWhen(true)] Attribute

DO NOT DO THIS (declare unitialized variable out of scope):
```
Foo foo;
if ....
{
 .... foo = some value
}
```

DO THIS INSTEAD (Try pattern with NotNullWhen(true)), and MAKE SURE THE OUT VAR IS NULLABLE:

```
private bool TryGetFoo(
    [NotNullWhen(true)]
    out Foo? foo
)
{
```

### Assert.True on individual Try's in tests

DO NOT DO THIS ("aggregate" bools together on tries in tests)
IT WILL NOT INFORM THE LSP/DEBUGGER THEY ARENT NULL AND YOULL GET A BUNCH OF WARNINGS
AND IT MESSES WITH FIGURING OUT WHICH ONE ACTUALLY FAILED TOO
```
var hasFoo = TryGetFoo(out var foo);
var hasBar = TryGetBar(out var bar);
Assert.True(hasFoo && hasBar);
```

DO THIS INSTEAD:
```
Assert.True(TryGetFoo(out var foo));
Assert.True(TryGetBar(out var bar));
```

---

## Directives from the human are the ultimate source of truth
- Whatever the human declares overrides test assertions, it overrides comments, it overrides existing logic
- If an existing test conflicts with the human's directives, its likely the test was just written wrong
- If existing domain layer code conflicts with the human's directives, its likely the domain layer code having been implemented wrong

## CRITICAL: UNDERSTAND, ABOVE ALL ELSE
Take the time to UNDERSTAND the system. Its INCREDIBLY common you agents will just start making wild assumptions about how things work without first reading and understanding not just the 1 chunk of code but HOW IT FITS INTO EVERYTHING

You need to first ask "what is the commanders intent" of a given system you are working on BEFORE you make changes to it. WHY was it written the way it was, WHY was it asked to be made, WHAT purpose does it serve.

Do not get as focused on "tests must pass", focus more on INTENT behind systems, reason about how they would be consumed and used.

Go back and read sprint files if you dont remember

## ALWAYS ask yourself if something has been done before
Whenever you go to implement something new, its INCREDIBLY likely something similiar has been done before. Perform MULTIPLE searches for similiar code and look at how the problem has been solved in other places in the codebase

Its extremely common agents skip this critical step and proceed to "re-invent the wheel" or just get the solution wrong because they just failed to go learn how it was solved before.

There is a huge massive codebase of already existing example code, read it and follow its patterns and examples.

## Performance Expectations

- **Benchmark-backed decisions:** Major performance choices must be validated via benchmarks
- **Zero allocations in hot paths:** Use `Span<T>`, `stackalloc`, and struct-based patterns
- **SIMD-friendly data layout:** Use `SparseArray` for cache-friendly iteration
- **Event-driven, not polling:** Use `EventBus<T>` for decoupled systems

See `.github/agents/DISCOVERIES.md` for findings from previous benchmarks. 
