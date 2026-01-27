# JsonLogic Integration Issue

## Problem

The json-everything JsonLogic library (version 5.5.0) parses ALL JSON objects as JsonLogic rules. This causes issues with the canonical test examples from the requirements.

## Example Failure

Canonical Example 1 uses:
```json
{
  "merge": [
    { "var": "" },
    {
      "properties": {
        "totalEnemyHealth": 350
      }
    }
  ]
}
```

When parsed, json-everything tries to find a JsonLogic rule called "properties" and fails with:
```
"Cannot identify rule for properties"
```

## Root Cause

In standard JsonLogic spec, object literals can be used as data. However, json-everything's implementation tries to deserialize ALL objects as Rule types, treating every key as a potential operation name.

## Testing

I tested this with simple examples:
- `{"newProp": "value"}` → ERROR: "Cannot identify rule for newProp"
- `{"a": 1}` → ERROR: "Cannot identify rule for a"

Even within `if` statements and other contexts, object literals fail to parse.

## Options

### Option 1: Custom Rule Implementation
Implement a custom "literal" rule that wraps object literals:
```json
{
  "merge": [
    { "var": "" },
    { "literal": {"properties": {"health": 95}} }
  ]
}
```

**Pros**: Keeps json-everything library
**Cons**: Changes required JSON format from canonical examples

### Option 2: Different Library
Switch to a different JsonLogic implementation that handles object literals correctly.

**Pros**: Canonical examples might work as-is
**Cons**: Sprint file specifically mentions json-everything; may have other issues

### Option 3: Pre-processing
Pre-process rule JSON to convert object literals to supported syntax before passing to json-logic.

**Pros**: Canonical examples work as written
**Cons**: Complex pre-processing logic; potential edge cases

### Option 4: Change Mutation Format
Use a different mutation format that doesn't require object literals:
```json
{
  "map": [
    { "var": "entities" },
    {
      "set": [
        { "var": "" },
        "properties.health",
        95
      ]
    }
  ]
}
```

**Pros**: Works with current library
**Cons**: Requires redefining all canonical examples

## Current Status

- RulesDriver core implementation is complete
- Entity serialization works correctly
- Mutation application logic is implemented
- JsonLogic evaluation fails on all canonical examples due to this issue

## Recommendation Needed

This is an architectural decision that affects:
- JSON scene file format
- All test examples
- Future rule authoring

Need guidance on which option to pursue before continuing implementation.
