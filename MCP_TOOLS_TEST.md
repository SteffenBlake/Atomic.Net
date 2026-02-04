# MCP Tools Testing and Documentation

## Test Date
2026-02-04

## Overview
This document contains comprehensive testing results for all available MCP tools in the current environment.

---

## Tool 1: `create`
**Status:** ✅ WORKING

**Description:** Creates new files with specified content at a given path.

**Test Result:** Successfully created this file.

**Parameters:**
- `path`: Full absolute path to file to create (MUST NOT exist)
- `file_text`: The content of the file to be created

**Limitations/Notes:**
- Cannot be used if the file already exists
- Parent directories must exist before creating the file
- Path MUST be absolute

**Example Usage:**
```
create(path="/absolute/path/to/file.txt", file_text="content here")
```

---

## Tool 2: `edit`
**Status:** ✅ WORKING

**Description:** Makes string replacements in files. Replaces exactly one occurrence of `old_str` with `new_str`.

**Test Result:** Successfully edited this file.

**Parameters:**
- `path`: Full absolute path to file to edit (MUST exist)
- `old_str`: The string in the file to replace (must match EXACTLY)
- `new_str`: The new string to replace old_str with

**Limitations/Notes:**
- Replaces exactly ONE occurrence (if `old_str` is not unique, replacement will not be performed)
- Include enough context in `old_str` to make it unique
- Leading and ending whitespaces from file content should be preserved
- Can be called multiple times in a single response for batch edits
- Path MUST be absolute

**Example Usage:**
```
edit(
  path="/absolute/path/to/file.txt",
  old_str="old text here",
  new_str="new text here"
)
```

---

## Tool 3: `github-mcp-server-list_pull_requests`
**Status:** ✅ WORKING

**Description:** Lists pull requests in a GitHub repository with filtering and pagination options.

**Test Result:** Successfully retrieved list of PRs from the repository.

**Parameters:**
- `owner`: Repository owner (required)
- `repo`: Repository name (required)
- `state`: Filter by state - "open", "closed", or "all" (optional)
- `base`: Filter by base branch (optional)
- `head`: Filter by head user/org and branch (optional)
- `sort`: Sort by - "created", "updated", "popularity", "long-running" (optional)
- `direction`: Sort direction - "asc" or "desc" (optional)
- `page`: Page number for pagination (min 1) (optional)
- `perPage`: Results per page (min 1, max 100) (optional)

**Limitations/Notes:**
- Output can be very large (98+ KB for repositories with many PRs)
- Results are returned as JSON
- For large outputs, result is saved to a temp file
- Should use pagination to manage large result sets

**Example Usage:**
```
github-mcp-server-list_pull_requests(
  owner="SteffenBlake",
  repo="Atomic.Net",
  state="open",
  perPage=5
)
```

---

## Tool 4: `github-mcp-server-pull_request_read`
**Status:** ✅ WORKING

**Description:** Gets information on a specific pull request in a GitHub repository with multiple action methods.

**Test Result:** Successfully retrieved PR details, testing multiple methods...

**Parameters:**
- `owner`: Repository owner (required)
- `repo`: Repository name (required)
- `pullNumber`: Pull request number (required)
- `method`: Action to specify what PR data to retrieve (required)
  - `get` - Get details of a specific pull request
  - `get_diff` - Get the diff of a pull request
  - `get_status` - Get status of a head commit (builds/checks)
  - `get_files` - Get list of files changed
  - `get_review_comments` - Get review threads on a PR
  - `get_reviews` - Get reviews on a PR
  - `get_comments` - Get comments on a PR
- `page`: Page number for pagination (min 1) (optional)
- `perPage`: Results per page (min 1, max 100) (optional)

**Limitations/Notes:**
- Returns detailed JSON information about the PR
- Different methods return different data structures
- Use pagination for methods that return lists

**Example Usage:**
```
github-mcp-server-pull_request_read(
  owner="SteffenBlake",
  repo="Atomic.Net",
  pullNumber=92,
  method="get"
)
```

---

## Tool 5: `report_progress`
**Status:** ✅ WORKING

**Description:** Reports progress on the task, commits and pushes changes.

**Test Result:** Successfully reported initial plan.

**Parameters:**
- `commitMessage`: Short single line commit message
- `prDescription`: Markdown checklist showing work completed and remaining

**Limitations/Notes:**
- Runs `git add .`, `git commit -m <msg>`, and `git push`
- Should be used when meaningful progress is made
- Keep checklist structure consistent between updates

**Example Usage:**
```
report_progress(
  commitMessage="Implement feature X",
  prDescription="- [x] Step 1\n- [ ] Step 2"
)
```

---

