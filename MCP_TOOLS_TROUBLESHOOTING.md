# MCP Tools - Troubleshooting & Best Practices

**Last Updated:** 2026-02-04

---

## TOOL: `create`

### ✅ Best Practices

1. **Always use absolute paths**
   ```
   ✅ GOOD: /home/runner/work/Atomic.Net/Atomic.Net/file.txt
   ❌ BAD:  ./file.txt
   ❌ BAD:  file.txt
   ```

2. **Ensure parent directories exist**
   - Cannot create `/path/to/new/dir/file.txt` if `/path/to/new/dir/` doesn't exist
   - File creation will fail

3. **Check if file already exists**
   - Tool will fail if file exists
   - Use `edit` tool instead for existing files

### 🔧 Common Issues

**Issue:** "File already exists"
- **Solution:** Use `edit` tool instead, or choose a different filename

**Issue:** "Parent directory doesn't exist"  
- **Solution:** Without bash, you cannot create parent directories
- Must use paths where all parent directories already exist

**Issue:** "Relative path used"
- **Solution:** Convert to absolute path

---

## TOOL: `edit`

### ✅ Best Practices

1. **Make old_str unique with context**
   ```
   ✅ GOOD: 
   old_str="public class MyClass\n{\n    private int _value;"
   
   ❌ BAD:
   old_str="private int _value;"  # Might appear multiple times
   ```

2. **Preserve whitespace exactly**
   ```
   If file has:
   "    int x = 5;"
   
   Use:
   old_str="    int x = 5;"  # Include the 4 spaces
   ```

3. **Use multiple edit calls for multiple changes**
   ```
   edit(path=file, old_str=first_occurrence, new_str=replacement1)
   edit(path=file, old_str=second_occurrence, new_str=replacement2)
   ```

### 🔧 Common Issues

**Issue:** "Multiple matches found; no changes made"
- **Solution:** Add more context to old_str to make it unique
- Include surrounding lines or unique identifiers

**Issue:** "String not found"
- **Solution:** 
  - Verify exact whitespace (spaces, tabs, newlines)
  - Check for hidden characters
  - Ensure file hasn't been modified since you last saw it

**Issue:** "Cannot edit file that doesn't exist"
- **Solution:** Use `create` tool first

---

## TOOL: `github-mcp-server-list_pull_requests`

### ✅ Best Practices

1. **Use pagination for large repos**
   ```
   # Get first page
   list_pull_requests(owner=..., repo=..., perPage=10, page=1)
   
   # Get next page
   list_pull_requests(owner=..., repo=..., perPage=10, page=2)
   ```

2. **Filter to reduce output size**
   ```
   # Only open PRs
   list_pull_requests(owner=..., repo=..., state="open")
   
   # Specific base branch
   list_pull_requests(owner=..., repo=..., base="main")
   ```

3. **Handle large output**
   - Tool saves output to temp file if > large size
   - Cannot read that file without bash/view tool
   - Use smaller perPage values

### 🔧 Common Issues

**Issue:** "Output too large to read"
- **Solution:** 
  - Use smaller `perPage` value (e.g., 5-10)
  - Add filters (state, base, head)
  - Note: Currently cannot read the temp file without view tool

**Issue:** "No PRs returned"
- **Solution:** Check filters (state, base, head)
- Verify repo has PRs

---

## TOOL: `github-mcp-server-pull_request_read`

### ✅ Best Practices

1. **Choose the right method for your needs**
   ```
   get          - Basic PR info (title, state, description)
   get_diff     - Code changes
   get_files    - List of changed files
   get_status   - Build/check status
   get_reviews  - PR reviews
   get_review_comments - Review comment threads
   get_comments - General PR comments
   ```

2. **Use pagination for lists**
   ```
   # Get files with pagination
   pull_request_read(
     method="get_files",
     perPage=20,
     page=1
   )
   ```

3. **Check build status before merging**
   ```
   pull_request_read(method="get_status")
   ```

### 🔧 Common Issues

**Issue:** "Different methods return different data structures"
- **Solution:** Check documentation for each method's return format
- Parse JSON accordingly

**Issue:** "No output for get_diff"
- **Solution:** PR might have no file changes yet
- This is normal for new/empty PRs

---

## TOOL: `report_progress`

### ✅ Best Practices

1. **Use meaningful commit messages**
   ```
   ✅ GOOD: "Implement user authentication feature"
   ✅ GOOD: "Fix bug in query parser for null values"
   ❌ BAD:  "Update"
   ❌ BAD:  "Changes"
   ```

2. **Keep checklist structure consistent**
   ```
   Good pattern:
   - [x] Create test file
   - [x] Implement feature
   - [ ] Run code review
   - [ ] Fix any issues
   ```

3. **Use frequently to save progress**
   - Don't wait until the very end
   - Report after each meaningful unit of work
   - Helps track progress and provides checkpoints

4. **Review what gets committed**
   - Tool runs `git add .` (adds ALL changes)
   - No selective staging
   - Make sure you want to commit everything

### 🔧 Common Issues

**Issue:** "Everything up-to-date"
- **Solution:** This is normal if you haven't made any changes since last commit
- Not an error, just informational

**Issue:** "Commits files I don't want"
- **Solution:** 
  - Without bash, cannot use .gitignore effectively
  - Cannot selectively stage files
  - Be careful what files you create/modify

**Issue:** "Checklist formatting broken in PR"
- **Solution:** 
  - Use markdown checklist format: `- [ ]` or `- [x]`
  - Include space after dash
  - Start each item on new line

---

## GENERAL TROUBLESHOOTING

### Working Without bash/view/grep Tools

Since these critical tools are missing, here are workarounds:

1. **Cannot view existing files**
   - ❌ No workaround
   - Must edit blindly or ask user for file contents

2. **Cannot check if file exists**
   - Try `create` - if it fails, file exists
   - Try `edit` - if it fails, file doesn't exist

3. **Cannot list directory contents**
   - ❌ No workaround
   - Must know exact file paths

4. **Cannot search code**
   - ❌ No workaround
   - Must ask user for locations

5. **Cannot run builds/tests**
   - ❌ No workaround
   - Cannot verify changes work

### Error Message Interpretation

**"Tool 'X' does not exist"**
- Tool is not available in current environment
- Check available tools list
- Request tool to be added if critical

**"Available tools that can be called are..."**
- Lists all currently available tools
- Use this list to know what you can do

**"File MUST not exist before creating"**
- `create` tool limitation
- Use `edit` instead

**"File MUST exist to edit"**
- `edit` tool limitation
- Use `create` instead

**"Multiple matches found"**
- `edit` tool needs unique old_str
- Add more context

---

## TIPS & TRICKS

### Using edit for Batch Changes

You can call edit multiple times in one response:

```
# Rename variable in multiple places
edit(path=file, old_str="let oldName = 1;", new_str="let newName = 1;")
edit(path=file, old_str="return oldName;", new_str="return newName;")
edit(path=file, old_str="console.log(oldName)", new_str="console.log(newName)")
```

### Creating Multiple Files

```
create(path="/path/to/file1.txt", file_text="content 1")
create(path="/path/to/file2.txt", file_text="content 2")
create(path="/path/to/file3.txt", file_text="content 3")
```

### GitHub PR Workflow

```
# 1. List PRs to find the one you want
list_pull_requests(owner=..., repo=..., state="open")

# 2. Get details on specific PR
pull_request_read(method="get", pullNumber=X)

# 3. Check build status
pull_request_read(method="get_status", pullNumber=X)

# 4. Review changes
pull_request_read(method="get_diff", pullNumber=X)

# 5. Read comments/reviews
pull_request_read(method="get_comments", pullNumber=X)
```

---

## SAFETY CHECKLIST

Before using `report_progress`:

- [ ] Have you reviewed what files were created?
- [ ] Have you reviewed what files were edited?
- [ ] Are there any temporary files that shouldn't be committed?
- [ ] Is your commit message clear and descriptive?
- [ ] Is your PR checklist up to date?
- [ ] Do you want to commit ALL changes? (no selective staging available)

---

## WHEN TO ASK FOR HELP

Ask the user for help when:

1. ❌ You need to read an existing file (no view tool)
2. ❌ You need to search for something (no grep tool)
3. ❌ You need to run a build or test (no bash tool)
4. ❌ You need to check if files exist (no bash tool)
5. ❌ You need to see directory contents (no bash tool)
6. ❌ You need to verify your changes work (no bash tool)
7. ❌ You're not sure of the exact file path (no glob/find tool)
8. ❌ You need to understand existing code patterns (no view tool)

**Don't struggle silently** - if you lack the tools to do something, explain what you need and ask the user for assistance or for the tool to be enabled.

---

**End of Troubleshooting Guide**
