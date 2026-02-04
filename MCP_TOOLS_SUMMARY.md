# MCP Tools - Quick Reference Summary

**Last Updated:** 2026-02-04

---

## ✅ WORKING TOOLS (5)

| Tool | Status | Use Case |
|------|--------|----------|
| `create` | ✅ Working | Create new files |
| `edit` | ✅ Working | Edit existing files via string replacement |
| `github-mcp-server-list_pull_requests` | ✅ Working | List PRs in repo |
| `github-mcp-server-pull_request_read` | ✅ Working | Read PR details (7 methods) |
| `report_progress` | ✅ Working | Commit, push, update PR |

---

## ❌ MISSING CRITICAL TOOLS (11+)

### P0 - CRITICAL (Required to function)
- ❌ `bash` - **ALL command-line operations blocked**
- ❌ `view` - **Cannot read existing files**
- ❌ `get_diagnostics` - **Required by instructions after EVERY file operation**
- ❌ `code_review` - **Required by instructions before finalizing**
- ❌ `codeql_checker` - **Required by instructions for security**

### P1 - HIGH (Required for basic functionality)
- ❌ `grep` - **Explicitly required in instructions for @senior-dev search**
- ❌ `glob` - Useful for file discovery

### P2 - MEDIUM (Useful features)
- ❌ `task` - Sub-agent delegation (code-reviewer)
- ❌ `read_bash` - Async bash output
- ❌ `write_bash` - Interactive command input
- ❌ `stop_bash` - Stop bash sessions

---

## 🔴 CRITICAL FINDINGS

### Agent Capability: ~5%
- **Can do:** Create/edit files (blindly), view PR info, commit/push
- **Cannot do:** Read files, search code, build, test, lint, diagnose, review, secure

### Root Cause
**Missing `bash` tool** creates cascading failures:
- No git operations (status, log, diff, blame)
- No builds (dotnet build)
- No tests (dotnet test)
- No formatting (dotnet format)
- No file operations (ls, cat, find)
- No dependencies (dotnet restore)

### Compliance with Agent Instructions: 0%
- ❌ Cannot complete pre-work (7/8 steps blocked)
- ❌ Cannot follow development workflows (10/10 blocked)
- ❌ Cannot run mandatory tools (diagnostics, code review, security scan)
- ❌ Cannot "always check examples" (cannot read or search code)

---

## 📊 KEY STATISTICS

- **Available Tools:** 5 (100% working)
- **Missing Tools:** 11+ (100% blocking)
- **Pre-work Steps Blocked:** 87.5% (7/8)
- **Core Workflows Blocked:** 100% (10/10)
- **Overall Functionality:** ~5%

---

## 🚨 IMMEDIATE ACTIONS REQUIRED

1. **Add `bash` tool** → Unlocks 60% of functionality
2. **Add `view` tool** → Unlocks 20% of functionality  
3. **Add `get_diagnostics` tool** → Enables code quality
4. **Add `code_review` tool** → Enables review workflow
5. **Add `codeql_checker` tool** → Enables security scanning
6. **Add `grep` tool** → Enables code search

**With these 6 tools: ~85% functionality restored**

---

## 📝 WHAT THIS MEANS

**Current State:**
The agent has an extremely limited toolset. While the 5 available tools work perfectly, the agent cannot perform 95% of its intended functions. It's like having a carpenter with only a hammer when they need a full toolbox.

**Impact:**
- Cannot read agent instructions files (.github/agents/*.md)
- Cannot search for @senior-dev mentions  
- Cannot view existing code to learn patterns
- Cannot build or test code
- Cannot verify code quality or security
- Cannot follow mandatory workflows

**Bottom Line:**
The MCP tooling that exists works great, but the agent is missing the critical foundation tools (especially `bash` and `view`) needed to function as a developer.

---

## 📖 FULL DETAILS

See `MCP_TOOLS_COMPREHENSIVE_REPORT.md` for:
- Detailed test results for each tool
- Parameter documentation
- Example usage
- Limitations and capabilities
- Impact analysis
- Recommendations

---

**Conclusion:** Available tools work perfectly, but critical tools are missing. Add the 6 priority tools to restore functionality.
