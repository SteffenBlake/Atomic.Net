# Proposed Alternative Mutation Format

## Problem
json-everything JsonLogic library doesn't support object literals in rule definitions.

## Solution
Use a flattened mutation format that expresses property changes as path-value pairs instead of nested objects.

## New Format

### Original (doesn't work):
```json
{
  "merge": [
    {"var": ""},
    {
      "properties": {
        "health": {"-": [{"var": "properties.health"}, 10]}
      }
    }
  ]
}
```

### Alternative 1: Array of Mutations
```json
{
  "map": [
    {"var": "entities"},
    {
      "cat": [
        {"var": ""},
        {
          "properties.health": {"-": [{"var": "properties.health"}, 10]}
        }
      ]
    }
  ]
}
```

### Alternative 2: Compute Values, Apply in C#
Store just the computed values in a known structure:
```json
{
  "map": [
    {"var": "entities"},
    {
      "_index": {"var": "_index"},
      "_mutations": {
        "health": {"-": [{"var": "properties.health"}, 10]}
      }
    }
  ]
}
```

Then in C# code, extract `_mutations` and apply them to `properties`.

## Recommendation
Use Alternative 2:
- Keeps JsonLogic for VALUE computation (which it's good at)
- Handles STRUCTURE assembly in C# (which is straightforward)
- Clean separation of concerns
- Works within json-everything's limitations

## Implementation
The DO clause would return:
```json
[
  {
    "_index": 256,
    "_mutations": {
      "health": 95.0,
      "mana": 48.0
    }
  }
]
```

C# code interprets `_mutations` as property updates to apply.
