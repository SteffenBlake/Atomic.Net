# Agent Instructions

## Code Style Rules

### Naming
- PascalCase:  types, methods, properties, public fields, constants
- _camelCase: private fields (underscore prefix)
- camelCase: parameters, locals
- TBehavior/TEvent: generic type params with T prefix
- Suffix patterns: `*Base`, `*Registry`, `*Event`, `*Behavior`, `*Driver`, `*Map`, `*Block`

### Structure
- Primary constructors preferred for DI/params
- `readonly record struct` for immutable data types
- `sealed` on leaf classes
- One type per file, filename matches type name
- Partial classes split by concern (e.g., `FlexRegistry. *. cs`) (Only for VERY LARGE files that are getting very very big, >1000 LoC)

### Organization
- Group:  usings → namespace → type
- Order members:  static → instance, fields → props → ctors → methods
- Related files in feature folders (`BlockMaps/`, `Hierarchy/`, `Sprites/`)

### Patterns
- Singleton via `ISingleton<T>` with static `Instance` property
- Event-driven via `IEventHandler<T>` + `EventBus<T>`
- Sparse arrays for entity data (`SparseArray<T>`)
- Fluent builders with `With*` extension methods returning `T`

### Comments
- XML docs (`///`) on public APIs only
- Brief single-line summaries, no verbose descriptions
- No inline comments unless non-obvious logic

### Formatting
- Expression-bodied members for single expressions
- Collection expressions `[. .]` over `new List`
- Pattern matching preferred
- Null checks via `[NotNullWhen(true)]` attribute pattern
- Early returns over nested conditionals

### Braces
- Always use braces `{ }` for control flow statements (`if`, `else`, `for`, `foreach`, `while`, `using`, etc.), even for single-line bodies
- No braceless one-liners

### Generic Conventions
- `where T : struct` for value-type constraints
- Nullable reference types enabled
- Avoid `var` only when type unclear

### File Organization
- `.csproj` minimal, sorted:  PropertyGroup → ItemGroup (PackageRef) → ItemGroup (ProjectRef)
- Namespace matches folder path from project root

## Testing Rules

### Test Structure
- All test methods must follow the Arrange/Act/Assert pattern
- Add clear comment sections for each phase:
  ```csharp
  [Fact]
  public void TestName()
  {
      // Arrange
      // ... setup code
      
      // Act
      // ... action being tested
      
      // Assert
      // ... verification
  }
  ```
- If a test doesn't fit this pattern, reorganize it or consider splitting into multiple tests
- Each test should test one specific behavior or scenario

### Test Helpers
- Use `FakeEventListener<TEvent>` for testing event firing
- Wrap `FakeEventListener` in `using` statements for proper cleanup
- Deactivated entities should be treated as non-existent - expect exceptions when accessing them
