# Agent Instructions

## Before You Start

1. Read `.github/agents/ROADMAP.md` - Current goals + progress checklist
2. Read `.github/agents/DISCOVERIES.md` - Performance findings (benchmark-backed)
3. Check active sprint in `.github/agents/sprints/` (if any)
4. **Run `grep -r "@your-role-name" . `** to find any pings directed at you
5. Read your role-specific `.agent. md` file for detailed responsibilities

---

## Core Principles (NEVER VIOLATE)

- **Zero allocations during gameplay** (all allocations at load time)
- **Data-driven everything** (game logic = JSON, not C#)
- **Entities are indices** (`ushort`, no pointers, no IDs in JSON)
- **Behaviors are structs** (stored in `SparseArray`, cache-friendly)
- **Events are struct-based** (`EventBus` pattern, no delegates during gameplay)
- **Test-driven development** (tests define correctness)

---

## Agent Communication System

### Leaving Comments in Code

When you need input from another agent, leave a comment with your role name and ping:

```csharp
// your-role:  @other-role I need clarification on X because Y
```

Valid roles you can ping:
- `@senior-dev` - Implementation questions, deviations from spec
- `@test-architect` - Test coverage, expected behavior
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

---

## Performance Expectations

- **Benchmark-backed decisions:** Major performance choices must be validated via benchmarks
- **Zero allocations in hot paths:** Use `Span<T>`, `stackalloc`, and struct-based patterns
- **SIMD-friendly data layout:** Use `SparseArray` for cache-friendly iteration
- **Event-driven, not polling:** Use `EventBus<T>` for decoupled systems

See `.github/agents/DISCOVERIES.md` for findings from previous benchmarks. 
