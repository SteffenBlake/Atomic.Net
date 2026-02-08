# SharpTools Quick Start Guide

**TL;DR:** SharpTools is an excellent MCP toolset for C# development. Rating: **9.5/10**

---

## Required Setup (Do This First!)

```bash
# 1. Load the solution
SharpTool_LoadSolution /path/to/solution.sln

# 2. Build the project (IMPORTANT for NuGet access!)
dotnet build

# Now you can use all SharpTools!
```

**Why build?** NuGet package types are only accessible after build. SharpTools can then **decompile** them!

---

## Most Useful Tools

### 📖 Reading Code

```bash
# View file without indentation (saves tokens)
SharpTool_ReadRawFromRoslynDocument /path/to/file.cs

# List all members of a type (like IntelliSense)
SharpTool_GetMembers "Namespace.ClassName" includePrivateMembers=false

# View method with callers and callees (amazing for understanding flow!)
SharpTool_ViewDefinition "Namespace.ClassName.MethodName"

# Find all references to a symbol
SharpTool_FindReferences "Namespace.ClassName"

# Search with regex across solution
SharpTool_SearchDefinitions "class.*Registry"

# Analyze code complexity
SharpTool_AnalyzeComplexity scope="method" target="Namespace.Class.Method"
```

### ✏️ Writing Code

```bash
# Create new file (auto-adds to project!)
SharpTool_CreateRoslynDocument filePath="/path/to/New.cs" content="using System;..."

# Add member to existing type
SharpTool_AddMember 
  fullyQualifiedTargetName="Namespace.ClassName"
  codeSnippet="public void NewMethod() { }"
  fileNameHint="auto"
  lineNumberHint=-1

# Rename symbol (updates all references!)
SharpTool_RenameSymbol 
  fullyQualifiedSymbolName="Namespace.OldName"
  newName="NewName"

# Find and replace with regex
SharpTool_FindAndReplace
  target="/path/to/file.cs"
  regexPattern="Console\.WriteLine"
  replacementText="Debug.WriteLine"

# Replace entire member
SharpTool_OverwriteMember
  fullyQualifiedMemberName="Namespace.Class.Method"
  newMemberCode="public void Method() { /* new code */ }"

# Move member between classes
SharpTool_MoveMember
  fullyQualifiedMemberName="Source.Class.Method"
  fullyQualifiedDestinationTypeOrNamespaceName="Dest.Class"
```

### 🔍 Navigation

```bash
# Find all implementations of interface
SharpTool_ListImplementations "Namespace.IInterface"

# Get type structure from file
SharpTool_ReadTypesFromRoslynDocument /path/to/file.cs
```

### ⏮️ Safety

```bash
# Undo last change (creates backup branch!)
SharpTool_Undo
```

---

## 🔥 Killer Features

1. **NuGet Decompilation** - View full source of external packages!
   ```bash
   dotnet build  # Required first!
   SharpTool_ViewDefinition "ExternalPackage.ClassName"
   # Returns decompiled C# source!
   ```

2. **Callers & Callees** - ViewDefinition shows WHO calls this and WHAT it calls
   
3. **Auto Project Management** - CreateRoslynDocument adds files to .csproj automatically

4. **Safe Refactoring** - All modification tools check for compile errors

5. **Undo with Backup** - Creates backup branch when undoing changes

---

## Known Limitations

❌ **Source-generated code not visible** (e.g., dotVariant's `TryMatch` method)
- **Workaround:** Use `grep` to find usage patterns

⚠️ **Build required for NuGet types** (not really a limitation, just a workflow step)

---

## Workflow Example

```bash
# 1. Start session
SharpTool_LoadSolution /path/to/MySolution.sln
dotnet build

# 2. Understand existing code
SharpTool_GetMembers "MyNamespace.MyClass" includePrivateMembers=false
SharpTool_ViewDefinition "MyNamespace.MyClass.ComplexMethod"
SharpTool_FindReferences "MyNamespace.IMyInterface"
SharpTool_ListImplementations "MyNamespace.IMyInterface"

# 3. Make changes
SharpTool_AddMember 
  fullyQualifiedTargetName="MyNamespace.MyClass"
  codeSnippet="public void NewFeature() { ... }"
  
SharpTool_RenameSymbol
  fullyQualifiedSymbolName="MyNamespace.OldName"
  newName="BetterName"

# 4. Verify (all tools show <errorCheck> output)
# If something went wrong...
SharpTool_Undo
```

---

## Output Quality

All tools provide:
- ✅ Structured JSON or XML output
- ✅ File paths and line numbers
- ✅ Compilation error checking (for modifications)
- ✅ Helpful context (callers, callees, surrounding code)

---

## Performance

⚡ **All tools execute in < 1 second** for typical codebases

---

## Best Practices

1. **Always build after LoadSolution** if working with NuGet packages
2. **Use ViewDefinition** instead of just reading files - the caller/callee info is invaluable
3. **Test modifications on small files first** - SharpTools is powerful but respect the refactoring!
4. **Use Undo liberally** - it creates backups, so no risk
5. **Trust the error checking** - if a tool says "no compilation issues," it's correct

---

## When NOT to Use SharpTools

- ❌ Source-generated code inspection (use `grep` for `TryMatch`, `Visit`, etc.)
- ❌ Quick file peeks (use `view` tool if you just need to glance)
- ❌ Before building (NuGet types won't be accessible)

---

## Comparison to Standard Tools

SharpTools **beats** standard tools in:
- Code understanding (structured output vs plain text)
- Refactoring (safe, with reference updates)
- NuGet inspection (can decompile vs manual ILSpy)
- Navigation (semantic vs text search)

Standard tools are better for:
- Source-generated code (SharpTools can't see it)
- Quick file reads (less overhead)

---

## Final Verdict

**USE SHARPTOOLS!** It's the best C# tooling available in MCP environments.

Rating: **9.5/10**

Only significant limitation: can't see source-generated code (use `grep` as fallback).

---

## Full Documentation

See `SHARPTOOLS_EFFICACY_REPORT.md` for comprehensive testing results and detailed tool documentation.
