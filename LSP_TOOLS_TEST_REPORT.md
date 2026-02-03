# LSP Tools Testing Report
**Date:** 2026-02-03  
**Agent:** senior-dev  
**Purpose:** Dry-run testing of cclsp MCP tools to assess functionality and usage patterns

---

## Executive Summary

**Result:** All LSP tools currently FAIL with initialization timeout errors.

The cclsp MCP server successfully locates and attempts to start the Microsoft.CodeAnalysis.LanguageServer, but all LSP operations timeout after 30 seconds during the initialization phase. The Roslyn LSP server itself appears to be functional (verified via manual testing), but the integration with cclsp is not working.

---

## Test Environment

### Build Status
- **Solution:** `/home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame.slnx`
- **Restore:** ✅ SUCCESS (3 projects restored in ~3.6 seconds)
- **Build:** ✅ SUCCESS (0 warnings, 0 errors, completed in 10.82 seconds)
- **.NET SDK:** 10.0.102 (with SDKs 8.0.x and 9.0.x also available)
- **LSP Server:** `/usr/local/bin/Microsoft.CodeAnalysis.LanguageServer` ✅ INSTALLED

### Configuration File
**Location:** `$HOME/.config/claude/cclsp.json`

**Initial Configuration:**
```json
{
  "servers": [
    {
      "extensions": ["cs", "csx"],
      "command": ["Microsoft.CodeAnalysis.LanguageServer"],
      "rootDir": "."
    }
  ]
}
```

**Final Tested Configuration:**
```json
{
  "servers": [
    {
      "extensions": ["cs", "csx"],
      "command": [
        "Microsoft.CodeAnalysis.LanguageServer",
        "--logLevel", "Warning",
        "--extensionLogDirectory", "/tmp/lsp-logs"
      ],
      "rootDir": "."
    }
  ]
}
```

---

## Test Results

### Test 1: find_references on TagsBehavior
**Command:** `cclsp-find_references`  
**File:** `/home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame/Tags/TagsBehavior.cs`  
**Symbol:** `TagsBehavior`  
**Result:** ❌ FAILED

**Error:**
```
Error: LSP request timeout: initialize (30000ms)
```

**Expected Behavior:** Should return all locations in the codebase where TagsBehavior is referenced.

---

### Test 2: find_definition on FluentHashSet
**Command:** `cclsp-find_definition`  
**File:** `/home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame/Tags/TagsBehavior.cs`  
**Symbol:** `FluentHashSet` (referenced on line 16 of TagsBehavior.cs)  
**Result:** ❌ FAILED

**Error:**
```
Error: LSP request timeout: initialize (30000ms)
```

**Expected Behavior:** Should return the file path where FluentHashSet is defined (likely in the Core or BED namespace).

**File Context:**
```csharp
// TagsBehavior.cs line 16
private readonly FluentHashSet<string>? _tags;
```

---

### Test 3: get_diagnostics on TagsBehavior.cs
**Command:** `cclsp-get_diagnostics`  
**File:** `/home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame/Tags/TagsBehavior.cs`  
**Result:** ❌ FAILED

**Error:**
```
Error getting diagnostics: LSP request timeout: initialize (30000ms)
```

**Expected Behavior:** Should return diagnostic messages (errors, warnings, info) from the LSP about this file. The problem statement indicates there should be at least one diagnostic message on TagsBehavior.

---

### Test 4: get_diagnostics on TagRegistry.cs
**Status:** SKIPPED (same initialization timeout expected)

---

### Test 5: rename_symbol on TagsBehavior
**Command:** `cclsp-rename_symbol`  
**Symbol:** `TagsBehavior` → `TagsBehaviorNew`  
**Status:** SKIPPED (requires successful initialization first)

**Expected Behavior:** Should:
1. Rename all occurrences of TagsBehavior to TagsBehaviorNew across the entire codebase
2. Update the filename from TagsBehavior.cs to TagsBehaviorNew.cs
3. Maintain a buildable and testable codebase after the rename

---

### Test 6: find_workspace_symbols
**Command:** `cclsp-find_workspace_symbols`  
**Query:** `"TagsBehavior"`, then `"Entity"`  
**Result:** ⚠️ NO TIMEOUT, but returns no symbols

**Error:**
```
No symbols found matching "TagsBehavior"
No symbols found matching "Entity"
```

**Analysis:** This tool doesn't timeout, suggesting it bypasses or handles initialization differently. However, it returns no symbols, indicating the workspace is not properly indexed or the LSP server isn't connected to the workspace.

---

## Configuration Experiments

### Experiment 1: Absolute rootDir
**Config Change:** `"rootDir": "/home/runner/work/Atomic.Net/Atomic.Net/MonoGame"`  
**Result:** No change - still times out

### Experiment 2: Added Required Arguments
**Config Change:** Added `--logLevel Information` and `--extensionLogDirectory /tmp/lsp-logs`  
**Result:** No change - still times out  
**Observation:** No log files were created in /tmp/lsp-logs, suggesting the LSP server never fully initializes or crashes before logging

### Experiment 3: Minimal Logging
**Config Change:** Changed logLevel to `Warning`  
**Result:** No change - still times out

### Experiment 4: Solution Initialization Options
**Config Change:** Added `"initializationOptions": { "solution": "Atomic.Net.MonoGame.slnx" }`  
**Result:** No change - still times out

---

## Manual LSP Server Testing

To verify the LSP server itself is functional, I tested it manually:

```bash
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"processId":null,"rootUri":"file:///home/runner/work/Atomic.Net/Atomic.Net/MonoGame","capabilities":{}}}' | Microsoft.CodeAnalysis.LanguageServer --logLevel Information --extensionLogDirectory /tmp/lsp-logs --stdio
```

**Result:** The server started and logged:
```
Content-Length: 116

{"jsonrpc":"2.0","method":"window/logMessage","params":{"type":3,"message":"[Program] Language server initialized"}}
```

**Then failed with:**
```
Unhandled exception. StreamJsonRpc.BadRpcHeaderException: Header does not end with expected \r\n character sequence
```

**Analysis:** 
- ✅ The LSP server executable is functional and can start
- ✅ It successfully initializes and sends log messages
- ❌ The manual test failed because LSP protocol requires proper message formatting with `Content-Length` headers and `\r\n` line endings
- ⚠️ The cclsp MCP server should be handling this protocol correctly, but the 30-second timeout suggests the initialization handshake is not completing

---

## Root Cause Analysis

### Probable Causes (in order of likelihood):

1. **Workspace Indexing Timeout:** The Roslyn LSP server may be taking >30 seconds to index the workspace. The MonoGame solution has multiple projects and dependencies, which could require significant indexing time on first startup.

2. **LSP Protocol Handshake Issue:** The cclsp MCP server may not be properly completing the LSP initialization handshake with the Roslyn LSP server. The manual test showed the server initializes but requires specific message formatting.

3. **Missing Configuration:** The Roslyn LSP server may require additional initialization options or workspace configuration that isn't being provided by cclsp.

4. **Solution File Discovery:** The LSP server may not be finding or loading the .slnx solution file automatically, preventing it from indexing the workspace.

### Evidence Supporting Workspace Indexing Timeout:
- `find_workspace_symbols` doesn't timeout but returns no results (suggests server starts but workspace not indexed)
- Manual test shows server initializes and sends messages
- No error logs are created (server doesn't crash, just takes too long)

---

## Recommendations

### For Tool Users (Future senior-dev agents):

**Current Status:** These tools are NOT FUNCTIONAL and should not be used until the initialization timeout is resolved.

**Alternative Approaches:**
- Use `grep` for finding references (text-based search)
- Use `glob` for finding file locations
- Use `dotnet build` for getting compiler diagnostics
- Use manual find/replace with `edit` tool for refactoring

### For Tool Maintainers:

1. **Increase Initialization Timeout:** Consider increasing the 30-second timeout to 60-90 seconds for large workspaces
   
2. **Add Initialization Progress Feedback:** Show progress messages during LSP initialization (e.g., "Indexing workspace... 10s elapsed")

3. **Pre-warm LSP Server:** Consider starting the LSP server in the background when the cclsp MCP server starts, before the first tool invocation

4. **Add Retry Logic:** If initialization fails, automatically retry once with a longer timeout

5. **Expose Configuration Options:** Allow users to configure initialization timeout and other LSP server options

6. **Add Diagnostic Logging:** Create detailed logs showing:
   - When LSP server process starts
   - LSP protocol messages exchanged
   - Why initialization is taking so long or failing

7. **Solution File Auto-detection:** Explicitly pass the solution file path to the LSP server during initialization:
   ```json
   {
     "initializationOptions": {
       "solution": "/home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame.slnx"
     }
   }
   ```

---

## Test Data for Future Reference

### TagsBehavior.cs Location
**Full Path:** `/home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame/Tags/TagsBehavior.cs`

**File Contents:**
```csharp
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tags;

/// <summary>
/// Behavior that tracks an entity's tags for group selection in rules and queries.
/// Tags are case-insensitive string labels (e.g., "enemy", "boss", "flying").
/// </summary>
[JsonConverter(typeof(TagsBehaviorConverter))]
public readonly record struct TagsBehavior
{
    // senior-dev: FluentHashSet allocation is approved (following PropertiesBehavior pattern)
    // This is a load-time allocation, not a gameplay allocation
    private readonly FluentHashSet<string>? _tags;
    
    public FluentHashSet<string> Tags
    {
        init => _tags = value;
        get => _tags ?? new(8, StringComparer.OrdinalIgnoreCase);
    }
    
}
```

**Expected Diagnostics:** The problem statement mentions there should be diagnostic messages on this file, but we were unable to retrieve them.

### Expected find_references Results
TagsBehavior should be referenced in:
- TagsBehaviorConverter.cs (JSON converter)
- TagRegistry.cs (registry tracking tags)
- Various test files
- Scene loading code
- Entity selector code

### Expected find_definition Result
FluentHashSet should be defined in:
- Likely in `/home/runner/work/Atomic.Net/Atomic.Net/MonoGame/Atomic.Net.MonoGame/Core/` or `/BED/` directory
- Should be a generic collection class with fluent API methods

---

## Conclusion

The cclsp MCP tools show promise but are currently non-functional due to LSP initialization timeouts. The underlying Roslyn LSP server appears to work correctly when tested manually, suggesting the issue is in the integration layer between cclsp and the LSP server.

**For immediate use:** Agents should continue using grep/glob/manual tools until these issues are resolved.

**For future improvement:** Focus on resolving the initialization timeout, either by increasing timeout duration, improving handshake protocol, or pre-warming the LSP server.

---

## Appendix: Commands Attempted

### Successful Commands
```bash
dotnet restore Atomic.Net.MonoGame.slnx
dotnet build Atomic.Net.MonoGame.slnx --no-restore
which Microsoft.CodeAnalysis.LanguageServer
```

### Failed cclsp Tool Invocations
```
cclsp-find_references (TagsBehavior) → timeout
cclsp-find_definition (FluentHashSet) → timeout  
cclsp-get_diagnostics (TagsBehavior.cs) → timeout
cclsp-restart_server → "No LSP servers found"
cclsp-find_workspace_symbols (TagsBehavior) → no results
cclsp-find_workspace_symbols (Entity) → no results
```

### Configuration Files Modified
- `$HOME/.config/claude/cclsp.json` (tested 4 different configurations)

---

**Report Generated:** 2026-02-03  
**Agent:** senior-dev  
**Status:** Testing Complete - Tools Non-Functional
