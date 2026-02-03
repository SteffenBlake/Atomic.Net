# LSP Tools Testing Report - Round 2
**Date:** 2026-02-03  
**Agent:** senior-dev  
**Purpose:** Re-test cclsp MCP tools with updated configuration

---

## Executive Summary

**Result:** Tools are PARTIALLY WORKING but with significant limitations.

The updated configuration with `--stdio` flag successfully enables basic LSP functionality:
- ✅ `get_diagnostics` - **WORKS PERFECTLY**
- ⚠️ `find_references` - **PARTIALLY WORKS** (only finds declaration, not references)
- ⚠️ `find_definition` - **PARTIALLY WORKS** (finds usage sites, not actual definitions)
- ❌ `rename_symbol` - **INCOMPLETE** (renames definition only, not all references)
- ❌ `get_hover` - **TIMES OUT** (30s timeout on hover requests)

---

## Test Environment

### Configuration Used
**Location:** `$HOME/.config/claude/cclsp.json`

**Working Configuration:**
```json
{
  "servers": [
    {
      "extensions": ["cs", "csx"],
      "command": [
        "Microsoft.CodeAnalysis.LanguageServer",
        "--stdio",
        "--logLevel",
        "Information",
        "--extensionLogDirectory",
        "$HOME/.local/var/log/roslyn-lsp"
      ],
      "rootDir": "."
    }
  ]
}
```

**Key Change from Round 1:** Added `--stdio` flag to the command array. This was the critical missing piece.

### Build Status
- **Solution:** `Atomic.Net.MonoGame.slnx`
- **Restore:** ✅ SUCCESS (3.16s)
- **Build:** ✅ SUCCESS (10.84s, 0 warnings, 0 errors)

---

## Detailed Test Results

### Test 1: get_diagnostics on TagsBehavior.cs ✅

**Command:**
```
cclsp-get_diagnostics("/home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame/Tags/TagsBehavior.cs")
```

**Result:** ✅ **SUCCESS**

**Output:**
```
Found 1 diagnostic in TagsBehavior.cs:

• Hint [IDE0005]: Using directive is unnecessary.
  Location: Line 2, Column 1 to Line 3, Column 32
```

**Analysis:** The diagnostic correctly identifies that line 2 (`using Atomic.Net.MonoGame.BED;`) is an unnecessary using directive. This is accurate - TagsBehavior doesn't use anything from the BED namespace.

**Verification:**
```csharp
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.BED;  // ← This line is flagged as unnecessary
using Atomic.Net.MonoGame.Core;
```

Looking at the file, only `FluentHashSet` from Core and `JsonConverter` attribute are used, so the BED using is indeed unnecessary.

---

### Test 2: get_diagnostics on TagRegistry.cs ✅

**Command:**
```
cclsp-get_diagnostics("/home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame/Tags/TagRegistry.cs")
```

**Result:** ✅ **SUCCESS**

**Output:**
```
Found 2 diagnostics in TagRegistry.cs:

• Information [IDE0028]: Collection initialization can be simplified
  Location: Line 33, Column 88 to Line 33, Column 91

• Hint [IDE0005]: Using directive is unnecessary.
  Location: Line 2, Column 1 to Line 3, Column 32
```

**Analysis:** 
1. First diagnostic suggests collection initialization can be simplified (likely `new()` instead of explicit type)
2. Second diagnostic is the same unnecessary using directive as TagsBehavior.cs

**Conclusion:** `get_diagnostics` is **fully functional** and provides accurate, actionable code quality hints.

---

### Test 3: find_references on TagsBehavior ⚠️

**Command:**
```
cclsp-find_references(
  file_path="/home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame/Tags/TagsBehavior.cs",
  symbol_name="TagsBehavior",
  include_declaration=true
)
```

**Result:** ⚠️ **PARTIAL SUCCESS** - Only finds declaration

**Output:**
```
Results for TagsBehavior (struct) at /home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame/Tags/TagsBehavior.cs:12:31:
/home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame/Tags/TagsBehavior.cs:12:31
```

**Expected:** Should find 50+ references across multiple files including:
- TagsBehaviorConverter.cs (multiple references)
- TagRegistry.cs (multiple references)
- SelectorRegistry.cs (multiple references)
- JsonEntity.cs (at least one reference)
- Multiple test files (20+ references)

**Actual:** Only found the struct definition at line 12, column 31.

**With `include_declaration=false`:**
```
Found 1 symbol(s) but no references could be retrieved. 
Please ensure the language server is properly configured.
```

**Analysis:** The LSP server can locate the symbol but doesn't have cross-file reference information indexed. This suggests the workspace is not fully loaded/indexed by Roslyn.

---

### Test 4: find_definition on FluentHashSet ⚠️

**Command:**
```
cclsp-find_definition(
  file_path="/home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame/Tags/TagsBehavior.cs",
  symbol_name="FluentHashSet"
)
```

**Result:** ⚠️ **PARTIAL SUCCESS** - Finds usage sites, not definition

**Output:**
```
Results for _tags : FluentHashSet<string>? (field) at TagsBehavior.cs:16:45:
/home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame/Tags/TagsBehavior.cs:16:45

Results for Tags : FluentHashSet<string> (property) at TagsBehavior.cs:18:34:
/home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame/Tags/TagsBehavior.cs:18:34
```

**Expected:** Should return:
```
/home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame/Core/FluentHashSet.cs:5:18
(where FluentHashSet<T> class is defined)
```

**Actual:** Returned the field and property that *use* FluentHashSet, not the class definition itself.

**Analysis:** The tool found all symbols matching "FluentHashSet" in the file, but couldn't navigate to the actual type definition in another file. Again, this points to incomplete cross-file indexing.

---

### Test 5: rename_symbol on TagsBehavior ❌

**Dry Run Command:**
```
cclsp-rename_symbol(
  file_path="/home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame/Tags/TagsBehavior.cs",
  symbol_name="TagsBehavior",
  new_name="TagsBehaviorNew",
  dry_run=true
)
```

**Dry Run Result:**
```
[DRY RUN] Would rename TagsBehavior (struct) to "TagsBehaviorNew":
File: TagsBehavior.cs
  - Line 12, Column 43 to Line 12, Column 43: "New"
```

**Actual Rename Command:**
```
cclsp-rename_symbol(
  file_path="...",
  symbol_name="TagsBehavior",
  new_name="TagsBehaviorNew",
  dry_run=false
)
```

**Actual Rename Result:**
```
Successfully renamed TagsBehavior (struct) to "TagsBehaviorNew".

Modified files:
- /home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame/Tags/TagsBehavior.cs
```

**Git Diff:**
```diff
-public readonly record struct TagsBehavior
+public readonly record struct TagsBehaviorNew
```

**Build After Rename:** ❌ **FAILED** - 20 compilation errors

**Sample Errors:**
```
error CS0246: The type or namespace name 'TagsBehavior' could not be found
  - JsonEntity.cs(27,12)
  - SelectorRegistry.cs(16,38)
  - TagRegistry.cs(13,38)
  - TagsBehaviorConverter.cs(12,60)
  - Plus 16 more errors in other files
```

**Remaining References After Rename:**
```bash
$ grep -r "TagsBehavior" --include="*.cs" | grep -v "TagsBehaviorNew" | wc -l
50+
```

**Analysis:** The rename operation only updated the type definition itself, not the 50+ references to it across the codebase. This makes the tool **dangerous to use** as it will break the build.

**Action Taken:** Reverted the change with `git checkout TagsBehavior.cs`

---

### Test 6: get_hover on FluentHashSet ❌

**Command:**
```
cclsp-get_hover(
  file_path="/home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame/Tags/TagsBehavior.cs",
  line=16,
  character=30
)
```

**Result:** ❌ **TIMEOUT**

**Error:**
```
Error getting hover info: LSP request timeout: textDocument/hover (30000ms)
```

**Analysis:** The hover request times out after 30 seconds. This is a different operation than diagnostics and seems to require more processing.

---

## Configuration Experiments

### Experiment 1: Explicit Solution Path
**Config Change:** Added `initializationOptions` with solution path
```json
"initializationOptions": {
  "solution": "/home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame.slnx"
}
```
**Result:** No change - still only finds declarations, not references

### Experiment 2: Absolute rootDir Path
**Config Change:** `"rootDir": "/home/runner/work/Atomic.Net/Atomic.Net/MonoGame"`  
**Result:** No change

### Experiment 3: Trace Logging
**Config Change:** `"--logLevel", "Trace"`  
**Result:** No log files created in specified directory

### Experiment 4: workspaceFolders
**Config Change:** Added `"workspaceFolders": ["/home/runner/work/Atomic.Net/Atomic.Net/MonoGame"]`  
**Result:** No change

---

## Answer to "Greedy Load" Question

**Question:** Is there a config to make the LSP greedy load the entire solution?

**Answer:** **Not that I could find.**

Based on my investigation:

1. **Command-line options** don't include solution pre-loading flags:
   - Checked `Microsoft.CodeAnalysis.LanguageServer --help`
   - Available options focus on extensions, logging, and paths
   - No `--preload`, `--eager`, or similar flags

2. **LSP initialization options** tested:
   - `initializationOptions.solution` - Accepted but no effect on indexing
   - `workspaceFolders` - No observable change
   - Various rootDir configurations - No impact

3. **Roslyn's behavior** appears to be lazy by design:
   - Single-file operations (diagnostics) work immediately
   - Cross-file operations (find references, go to definition) require indexing
   - Indexing happens on-demand rather than upfront
   - No logs created even with Trace level (suggesting logging doesn't capture indexing progress)

4. **Evidence of lazy loading:**
   - `get_diagnostics` works instantly (single-file analysis)
   - `find_references` fails (requires cross-file index)
   - `find_definition` fails (requires cross-file index)
   - `get_hover` times out (may trigger indexing)

**Potential Solutions (unverified):**

1. **Pre-warm by opening all files:** Send `textDocument/didOpen` for every `.cs` file before running queries
2. **Use build metadata:** The LSP might load faster if a recent build exists (we do have this)
3. **Wait for indexing:** After server start, wait 60-120 seconds before running cross-file operations
4. **Use workspace/symbol instead:** The `find_workspace_symbols` operation might trigger full indexing

**Recommendation:** The current LSP configuration is **not suitable for cross-file operations**. Use `get_diagnostics` for code quality checks, but use `grep` and `glob` for finding references and definitions until a solution for cross-file indexing is found.

---

## LSP Server Logging Investigation

**Expected:** Log files in `/tmp/roslyn-lsp-logs/` or `$HOME/.local/var/log/roslyn-lsp/`  
**Actual:** No log files created, even with `--logLevel Trace`

**Possible reasons:**
1. Logging might be disabled for stdio mode
2. Logs might go to stderr (captured by cclsp)
3. `--extensionLogDirectory` might be for extensions only, not core logs
4. Environment variable expansion (`$HOME`) might not work in command args

**Found:** `/tmp/roslyn-canonical-misc/` contains build artifacts (AssemblyInfo, MSBuild configs) but no logs

---

## Tool-by-Tool Summary

| Tool | Status | Usability | Notes |
|------|--------|-----------|-------|
| `get_diagnostics` | ✅ Working | **USE IT** | Fast, accurate, provides IDE-level code quality hints |
| `find_references` | ⚠️ Partial | **DON'T USE** | Only finds declaration, not actual references |
| `find_definition` | ⚠️ Partial | **DON'T USE** | Finds usage sites instead of definitions |
| `rename_symbol` | ❌ Broken | **DANGEROUS** | Only renames definition, breaks build |
| `get_hover` | ❌ Timeout | **DON'T USE** | Times out after 30 seconds |
| `find_workspace_symbols` | ❓ Untested | Unknown | Not tested in round 2 |

---

## Comparison: Round 1 vs Round 2

| Aspect | Round 1 | Round 2 |
|--------|---------|---------|
| Config | No `--stdio` flag | Added `--stdio` flag |
| Initialization | 30s timeout | **SUCCESS** ✅ |
| get_diagnostics | Timeout | **WORKS** ✅ |
| find_references | Timeout | Partial (declaration only) ⚠️ |
| find_definition | Timeout | Partial (wrong results) ⚠️ |
| rename_symbol | Not attempted | Broken (incomplete rename) ❌ |

**Key Improvement:** Adding `--stdio` enabled basic LSP functionality.

**Remaining Issue:** Cross-file analysis requires workspace indexing that isn't happening.

---

## Recommendations

### For Immediate Use

**✅ DO USE:**
- `get_diagnostics` for code quality analysis
  - Faster than running the compiler
  - Provides IDE-level hints (unnecessary usings, simplification opportunities)
  - Works on single files without full solution context

**❌ DON'T USE:**
- `find_references` - Use `grep` instead
- `find_definition` - Use `grep` or `glob` instead
- `rename_symbol` - **DANGEROUS** - Use manual find/replace
- `get_hover` - Times out, not useful

### For Tool Maintainers

1. **Investigate workspace loading:**
   - Research Roslyn LSP initialization sequence
   - Find how to trigger/wait for full workspace indexing
   - Consider sending `workspace/didChangeWatchedFiles` events

2. **Add indexing status check:**
   - Before cross-file operations, verify workspace is indexed
   - Return helpful error: "Workspace not indexed yet, try get_diagnostics instead"

3. **Improve rename_symbol:**
   - Either: Fail if not all references found
   - Or: Warn user that only N of M references will be renamed
   - Current behavior is **dangerous** - silently creates broken code

4. **Document limitations:**
   - Clearly state which operations require full indexing
   - Set user expectations about which tools work

5. **Consider alternative for references:**
   - Hybrid approach: Use LSP for definition, grep for references
   - Or: Use Roslyn's build output (binary logs) for reference data

---

## Test Files for Future Validation

### TagsBehavior.cs
```csharp
// Line 2: Unnecessary using directive (flagged by diagnostics ✅)
using Atomic.Net.MonoGame.BED;

// Line 12: Struct definition (found by find_references ✅)
public readonly record struct TagsBehavior

// Line 16: FluentHashSet usage (found by find_definition ⚠️ - but not the real definition)
private readonly FluentHashSet<string>? _tags;
```

### Expected FluentHashSet Definition
**File:** `/home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame/Core/FluentHashSet.cs`  
**Not found by:** `find_definition` (should be fixed)

### Expected TagsBehavior References
**Count:** 50+ across multiple files:
- TagsBehaviorConverter.cs
- TagRegistry.cs  
- SelectorRegistry.cs
- JsonEntity.cs
- 20+ test files

**Not found by:** `find_references` (should be fixed)

---

## Conclusion

Round 2 shows **significant progress** from Round 1:
- ✅ Basic LSP connectivity working (thanks to `--stdio`)
- ✅ Single-file analysis fully functional (`get_diagnostics`)
- ❌ Cross-file analysis still broken (no workspace indexing)
- ❌ Refactoring tools dangerous (`rename_symbol` incomplete)

**Root Cause:** Roslyn LSP server loads lazily and hasn't indexed the workspace for cross-file operations. The `--stdio` flag fixed communication, but workspace indexing remains the blocker.

**Next Steps:** Research Roslyn LSP workspace initialization, find how to trigger eager loading, or document current tools as "diagnostics-only" until indexing works.

---

## .editorconfig Integration Testing

**Question:** Does the LSP respect .editorconfig settings?

**Answer:** **PARTIALLY** - Suppression works, severity changes don't.

### Test Setup

Starting diagnostics on TagRegistry.cs:
```
• Information [IDE0028]: Collection initialization can be simplified
  Location: Line 33, Column 88 to Line 33, Column 91

• Hint [IDE0005]: Using directive is unnecessary.
  Location: Line 2, Column 1 to Line 3, Column 32
```

### Test 1: Suppress IDE0028 to "none"

**Configuration Added:**
```ini
dotnet_diagnostic.IDE0028.severity = none
```

**Result:** ✅ **SUCCESS** - IDE0028 disappeared completely

**After Restart:**
```
• Hint [IDE0005]: Using directive is unnecessary.
  (IDE0028 is gone!)
```

### Test 2: Change IDE0005 severity to "error"

**Configuration Added:**
```ini
dotnet_diagnostic.IDE0005.severity = error
```

**Result:** ❌ **FAILED** - Still shows as "Hint", not "Error"

**After Restart:**
```
• Hint [IDE0005]: Using directive is unnecessary.
  (Still "Hint", not "Error")
```

### Test 3: Suppress IDE0005 to "none"

**Configuration Added:**
```ini
dotnet_diagnostic.IDE0005.severity = none
```

**Result:** ❌ **FAILED** - Still shows as "Hint"

**After Restart:**
```
• Hint [IDE0005]: Using directive is unnecessary.
  (Not suppressed)
```

### Test 4: Try "silent" severity

**Configuration Added:**
```ini
dotnet_diagnostic.IDE0005.severity = silent
```

**Result:** ❌ **FAILED** - Still shows as "Hint"

### Verification with dotnet format

Confirmed that .editorconfig is being read:
```bash
$ dotnet format --verify-no-changes --verbosity diagnostic
Project Atomic.Net.MonoGame is using configuration from '/home/runner/work/Atomic.Net/Atomic.Net/.editorconfig'.
```

The .editorconfig file IS being loaded by dotnet tooling, so the LSP should also be reading it.

### Conclusions

**✅ What Works:**
- Suppressing diagnostics to `none` for some rules (IDE0028 disappeared)
- .editorconfig file is being read by the LSP server

**❌ What Doesn't Work:**
- Changing severity levels (hint → warning → error)
- Suppressing IDE0005 specifically (may be handled differently)

**Analysis:**

The LSP respects .editorconfig for *suppressing* diagnostics but appears to have its own logic for determining severity levels. Some diagnostics (like IDE0005) may have hardcoded severity that cannot be overridden, or there may be a more complex configuration hierarchy.

The behavior suggests:
1. LSP reads .editorconfig ✅
2. Suppression (severity = none) works for some rules ✅
3. Severity changes (hint/warning/error) don't apply ❌
4. Some diagnostics resist configuration changes ❌

**Recommendation:** Use .editorconfig to suppress unwanted diagnostics entirely, but don't rely on severity level changes.

---

**Report Generated:** 2026-02-03  
**Agent:** senior-dev  
**Status:** Testing Complete - Partial Functionality  
**Updated:** 2026-02-03 (Added .editorconfig testing)
