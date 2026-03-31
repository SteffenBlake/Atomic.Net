# JSONLogic Operators Reference

Comprehensive documentation of all JSONLogic operators supported in Atomic.Net.MonoGame, based on test coverage.

## Table of Contents

- [aggregate](#aggregate) - Reduce array elements
- [all](#all) - Check all elements match condition
- [and](#and) - Logical AND
- [any](#any) - Check any element matches condition
- [append](#append) - Add element to array
- [contains](#contains) - Check array contains element
- [/ (divide)](#divide) - Division operation
- [== (equals)](#equals) - Equality comparison
- [>= (greater-than-or-equal)](#greater-than-or-equal) - Greater than or equal comparison
- [> (greater-than)](#greater-than) - Greater than comparison
- [if](#if) - Conditional expression
- [<= (less-than-or-equal)](#less-than-or-equal) - Less than or equal comparison
- [< (less-than)](#less-than) - Less than comparison
- [linq.add](#linqadd) - LINQ-style addition
- [log](#log) - Logarithm operation
- [max](#max) - Maximum value
- [min](#min) - Minimum value
- [% (modulo)](#modulo) - Modulo operation
- [* (multiply)](#multiply) - Multiplication operation
- [none](#none) - Check no elements match condition
- [!= (not-equals)](#not-equals) - Inequality comparison
- [! (not)](#not) - Logical NOT
- [or](#or) - Logical OR
- [select-many](#select-many) - Flatten nested arrays
- [select](#select) - Map array elements
- [contains (string)](#string-contains) - Check string contains substring
- [substr](#substring) - Extract substring
- [- (subtract)](#subtract) - Subtraction operation
- [+ (symbol-add)](#symbol-add) - Addition/concatenation
- [var](#var) - Variable access
- [where](#where) - Filter array elements

---

## aggregate

**JSON Operator:** `aggregate` (JSONLogic `reduce`)  
**Purpose:** Aggregates/reduces array elements using a specified operation with an accumulator.

**Syntax:**
```json
{"aggregate": [array, operation, initialValue]}
```

**Special Variables:**
- `{"var": "current"}` - Current element being processed
- `{"var": "accumulator"}` - Accumulated value from previous iterations

**Examples:**

Sum array elements:
```json
{
  "aggregate": [
    {"var": "Numbers"},
    {"+": [{"var": "current"}, {"var": "accumulator"}]},
    0
  ]
}
```
Input: `{ Numbers: [1, 2, 3, 4, 5] }`  
Output: `15`

Calculate product:
```json
{
  "aggregate": [
    {"var": "Numbers"},
    {"*": [{"var": "current"}, {"var": "accumulator"}]},
    1
  ]
}
```
Input: `{ Numbers: [2, 3, 4] }`  
Output: `24`

Find maximum value:
```json
{
  "aggregate": [
    {"var": "Numbers"},
    {
      "if": [
        {">": [{"var": "current"}, {"var": "accumulator"}]},
        {"var": "current"},
        {"var": "accumulator"}
      ]
    },
    0
  ]
}
```
Input: `{ Numbers: [5, 2, 8, 1, 9] }`  
Output: `9`

**Edge Cases:**
- Empty array returns initial value
- Initial value is the starting accumulator value

---

## all

**JSON Operator:** `all`  
**Purpose:** Checks if ALL elements in an array pass a specified test condition.

**Syntax:**
```json
{"all": [array, condition]}
```

**Special Variables:**
- `{"var": ""}` - The current element being tested

**Examples:**

Check all elements are positive:
```json
{"all": [[1, 2, 3], {">": [{"var": ""}, 0]}]}
```
Output: `true`

Check all elements are even:
```json
{"all": [[2, 4, 6], {"==": [{"%": [{"var": ""}, 2]}, 0]}]}
```
Output: `true`

Complex condition (all between 0 and 100):
```json
{
  "all": [
    [10, 20, 30],
    {
      "and": [
        {">": [{"var": ""}, 0]},
        {"<": [{"var": ""}, 100]}
      ]
    }
  ]
}
```
Output: `true`

One element fails:
```json
{"all": [[-1, 2, 3], {">": [{"var": ""}, 0]}]}
```
Output: `false`

**Edge Cases:**
- Empty array returns `false`
- All elements must pass for result to be `true`

---

## and

**JSON Operator:** `and`  
**Purpose:** Logical AND operation. Returns boolean result of AND between conditions.

**Syntax:**
```json
{"and": [condition1, condition2, ...]}
```

**Examples:**

Both conditions true:
```json
{"and": [true, true]}
```
Output: `true`

One condition false:
```json
{"and": [true, false]}
```
Output: `false`

With variable data (check value in range):
```json
{
  "and": [
    {">": [{"var": "Value"}, 0]},
    {"<": [{"var": "Value"}, 100]}
  ]
}
```
Input: `{ Value: 42 }`  
Output: `true`

**C# Semantics:**
- Only supports `bool` types (no JavaScript-style "truthiness")
- Cannot mix types like `bool`, `string`, `int` in same AND expression
- Fails to compile if attempting mixed type AND operations

---

## any

**JSON Operator:** `any` (JSONLogic `some`)  
**Purpose:** Checks if AT LEAST ONE element in an array passes a specified test condition.

**Syntax:**
```json
{"any": [array, condition]}
```

**Special Variables:**
- `{"var": ""}` - The current element being tested

**Examples:**

Check if any element is positive:
```json
{"any": [[-1, 0, 1], {">": [{"var": ""}, 0]}]}
```
Output: `true`

All elements fail condition:
```json
{"any": [[-3, -2, -1], {">": [{"var": ""}, 0]}]}
```
Output: `false`

Check if any element is even:
```json
{"any": [[1, 2, 5], {"==": [{"%": [{"var": ""}, 2]}, 0]}]}
```
Output: `true` (2 is even)

**Edge Cases:**
- Empty array returns `false`
- Returns `true` as soon as first matching element is found

---

## append

**JSON Operator:** `addRange`  
**Purpose:** Concatenates two arrays into a single combined array.

**Syntax:**
```json
{"addRange": [array1, array2]}
```

**Examples:**

Combine two arrays:
```json
{"addRange": [[1, 2], [3, 4]]}
```
Output: `[1, 2, 3, 4]`

Empty arrays:
```json
{"addRange": [[], []]}
```
Output: `[]`

**C# Semantics:**
- Takes exactly 2 arrays (no more, no less)
- Does not auto-wrap scalars as arrays
- Both inputs must be arrays of compatible element types

---

## contains

**JSON Operator:** `contains` (JSONLogic `in` for arrays)  
**Purpose:** Tests if a value exists in an array (membership test).

**Syntax:**
```json
{"contains": [valueToFind, array]}
```

**Examples:**

String in array:
```json
{"contains": ["Ringo", ["John", "Paul", "George", "Ringo"]]}
```
Output: `true`

Number in array:
```json
{"contains": [3, [1, 2, 3, 4, 5]]}
```
Output: `true`

Value not present:
```json
{"contains": ["Pete", ["John", "Paul", "George", "Ringo"]]}
```
Output: `false`

Empty array:
```json
{"contains": [1, []]}
```
Output: `false`

**Edge Cases:**
- Empty array always returns `false`
- Type-safe comparison (int 3 ≠ string "3")

---

## divide

**JSON Operator:** `/`  
**Purpose:** Divides two numbers (arithmetic division).

**Syntax:**
```json
{"/": [dividend, divisor]}
```

**Examples:**

Integer division:
```json
{"/": [4, 2]}
```
Output: `2`

Float division:
```json
{"/": [7.5, 2.5]}
```
Output: `3.0`

Division with remainder (as float):
```json
{"/": [5, 2]}
```
Output: `2.5` (when compiled as `<TIn, double>`)

With variable data:
```json
{"/": [{"var": "A"}, {"var": "B"}]}
```
Input: `{ A: 84, B: 2 }`  
Output: `42`

Negative numbers:
```json
{"/": [-10, 2]}
```
Output: `-5`

**Error Handling:**
- Division by zero returns `null` and fires `ErrorEvent`
- Requires nullable output type for divide-by-zero safety: `TryCompile<TIn, double?>`

---

## equals

**JSON Operator:** `==`  
**Purpose:** Tests equality between two values.

**Syntax:**
```json
{"==": [value1, value2]}
```

**Examples:**

Same integers:
```json
{"==": [1, 1]}
```
Output: `true`

Different integers:
```json
{"==": [1, 2]}
```
Output: `false`

String comparison:
```json
{"==": [{"var": "Name"}, "test"]}
```
Input: `{ Name: "test" }`  
Output: `true`

With variable data:
```json
{"==": [{"var": "Value"}, 42]}
```
Input: `{ Value: 42 }`  
Output: `true`

**C# Semantics:**
- **No type coercion** (unlike JavaScript JSONLogic)
- Cannot compare `int` to `string` (e.g., `1` vs `"1"` fails to compile)
- Cannot compare `int` to `bool` (e.g., `0` vs `false` fails to compile)
- Both operands must have compatible types

---

## greater-than-or-equal

**JSON Operator:** `>=`  
**Purpose:** Tests if first value is greater than or equal to second value.

**Syntax:**
```json
{">=": [value1, value2]}
```

**Examples:**

Greater value:
```json
{">=": [2, 1]}
```
Output: `true`

Equal values:
```json
{">=": [1, 1]}
```
Output: `true`

Less value:
```json
{">=": [1, 2]}
```
Output: `false`

With variable data:
```json
{">=": [{"var": "Value"}, 42]}
```
Input: `{ Value: 42 }`  
Output: `true`

---

## greater-than

**JSON Operator:** `>`  
**Purpose:** Tests if first value is strictly greater than second value.

**Syntax:**
```json
{">": [value1, value2]}
```

**Examples:**

Greater value:
```json
{">": [2, 1]}
```
Output: `true`

Less value:
```json
{">": [1, 2]}
```
Output: `false`

Equal values:
```json
{">": [1, 1]}
```
Output: `false` (not strictly greater)

With negative numbers:
```json
{">": [-1, -5]}
```
Output: `true`

With floats:
```json
{">": [3.14, 2.71]}
```
Output: `true`

---

## if

**JSON Operator:** `if`  
**Purpose:** Conditional expression (ternary operator). Returns different values based on condition.

**Syntax:**
```json
{"if": [condition, thenValue, elseValue]}
```

**Else-If Chaining:**
```json
{"if": [condition1, value1, condition2, value2, ..., finalElseValue]}
```

**Examples:**

Simple conditional:
```json
{"if": [true, "yes", "no"]}
```
Output: `"yes"`

Condition false:
```json
{"if": [false, "yes", "no"]}
```
Output: `"no"`

With variable condition:
```json
{
  "if": [
    {"==": [{"var": "Value"}, 42]},
    "found",
    "not found"
  ]
}
```
Input: `{ Value: 42 }`  
Output: `"found"`

Else-if chain:
```json
{
  "if": [
    {"<": [{"var": "Value"}, 0]}, "negative",
    {"<": [{"var": "Value"}, 100]}, "small",
    "large"
  ]
}
```
Input: `{ Value: 50 }`  
Output: `"small"`

Input: `{ Value: 100 }`  
Output: `"large"`

**C# Semantics:**
- Condition must be `bool` type (no JavaScript-style "truthiness")
- Cannot use `int`, `string`, or other types as conditions
- All branches (then/else) must return same type

---

## less-than-or-equal

**JSON Operator:** `<=`  
**Purpose:** Tests if first value is less than or equal to second value.

**Syntax:**
```json
{"<=": [value1, value2]}
```

**Examples:**

Less value:
```json
{"<=": [1, 2]}
```
Output: `true`

Equal values:
```json
{"<=": [1, 1]}
```
Output: `true`

Greater value:
```json
{"<=": [2, 1]}
```
Output: `false`

With variable data:
```json
{"<=": [{"var": "Value"}, 42]}
```
Input: `{ Value: 42 }`  
Output: `true`

**C# Semantics:**
- Does NOT support chained comparisons like `1 <= x <= 100` (fails to compile)
- Use `and` operator for range checks: `{"and": [{"<=": [1, x]}, {"<=": [x, 100]}]}`

---

## less-than

**JSON Operator:** `<`  
**Purpose:** Tests if first value is strictly less than second value.

**Syntax:**
```json
{"<": [value1, value2]}
```

**Examples:**

Less value:
```json
{"<": [1, 2]}
```
Output: `true`

Greater value:
```json
{"<": [2, 1]}
```
Output: `false`

Equal values:
```json
{"<": [1, 1]}
```
Output: `false` (not strictly less)

With variable data:
```json
{"<": [{"var": "Value"}, 100]}
```
Input: `{ Value: 42 }`  
Output: `true`

**C# Semantics:**
- Does NOT support chained comparisons like `1 < x < 100` (fails to compile)
- Use `and` operator for range checks: `{"and": [{"<": [1, x]}, {"<": [x, 100]}]}`

---

## linqadd

**JSON Operator:** `add`  
**Purpose:** Adds a single value to an array (appends element to end).

**Syntax:**
```json
{"add": [value, array]}
```

**Examples:**

Add value to array:
```json
{"add": [5, [1, 2, 3, 4]]}
```
Output: `[1, 2, 3, 4, 5]`

Add to empty array:
```json
{"add": [42, []]}
```
Output: `[42]`

Add string to string array:
```json
{"add": ["World", ["Hello"]]}
```
Output: `["Hello", "World"]`

With variable data:
```json
{"add": [{"var": "Value"}, [10, 20, 30]]}
```
Input: `{ Value: 99 }`  
Output: `[10, 20, 30, 99]`

**Distinctions:**
- `add` - Adds a SINGLE value to an array (this operator)
- `+` (symbol-add) - Adds two numbers OR concatenates two strings
- `addRange` (append) - Concatenates two arrays

**C# Semantics:**
- First argument must be a scalar value (not array)
- Second argument must be an array
- Cannot use for array concatenation (use `addRange` instead)

---

## log

**JSON Operator:** `log`  
**Purpose:** Pass-through operator that logs the value via `LogEvent` and returns it unchanged.

**Syntax:**
```json
{"log": value}
```

**Examples:**

Log string:
```json
{"log": "apple"}
```
Output: `"apple"` (and fires `LogEvent` with "apple")

Log number:
```json
{"log": 42}
```
Output: `42` (and fires `LogEvent` with "42")

Log variable:
```json
{"log": {"var": "Value"}}
```
Input: `{ Value: 100 }`  
Output: `100` (and fires `LogEvent` with "100")

Log expression result:
```json
{"log": {"+": [1, 2]}}
```
Output: `3` (and fires `LogEvent` with "3")

**Purpose:** Debugging and auditing - allows you to inspect intermediate values in complex expressions without changing behavior.

---

## max

**JSON Operator:** `max`  
**Purpose:** Returns the maximum value from a set of numbers.

**Syntax:**
```json
{"max": [value1, value2, ...]}
```

**Examples:**

Three integers:
```json
{"max": [1, 2, 3]}
```
Output: `3`

Two values:
```json
{"max": [5, 2]}
```
Output: `5`

Negative numbers:
```json
{"max": [-5, -2, -10]}
```
Output: `-2`

With variable data:
```json
{"max": [{"var": "A"}, {"var": "B"}, {"var": "C"}]}
```
Input: `{ A: 10, B: 50, C: 30 }`  
Output: `50`

Floating point:
```json
{"max": [1.5, 2.7, 0.3]}
```
Output: `2.7`

**Edge Cases:**
- Single value: returns that value
- Accepts any number of arguments (minimum 1)

---

## min

**JSON Operator:** `min`  
**Purpose:** Returns the minimum value from a set of numbers.

**Syntax:**
```json
{"min": [value1, value2, ...]}
```

**Examples:**

Three integers:
```json
{"min": [1, 2, 3]}
```
Output: `1`

Two values:
```json
{"min": [5, 2]}
```
Output: `2`

Negative numbers:
```json
{"min": [-5, -2, -10]}
```
Output: `-10`

With variable data:
```json
{"min": [{"var": "A"}, {"var": "B"}, {"var": "C"}]}
```
Input: `{ A: 10, B: 50, C: 30 }`  
Output: `10`

Floating point:
```json
{"min": [1.5, 2.7, 0.3]}
```
Output: `0.3`

**Edge Cases:**
- Single value: returns that value
- Accepts any number of arguments (minimum 1)

---

## modulo

**JSON Operator:** `%`  
**Purpose:** Returns the remainder after division (modulo operation).

**Syntax:**
```json
{"%": [dividend, divisor]}
```

**Examples:**

Odd number:
```json
{"%": [101, 2]}
```
Output: `1`

Even number:
```json
{"%": [100, 2]}
```
Output: `0`

With variable data:
```json
{"%": [{"var": "Value"}, 10]}
```
Input: `{ Value: 42 }`  
Output: `2`

Divisor larger than dividend:
```json
{"%": [5, 10]}
```
Output: `5`

Negative dividend:
```json
{"%": [-7, 3]}
```
Output: `-1`

**Error Handling:**
- Modulo by zero returns `null` and fires `ErrorEvent`
- Requires nullable output type for modulo-by-zero safety: `TryCompile<TIn, int?>`

---

## multiply

**JSON Operator:** `*`  
**Purpose:** Multiplies numbers together (arithmetic multiplication).

**Syntax:**
```json
{"*": [value1, value2, ...]}
```

**Examples:**

Two integers:
```json
{"*": [4, 2]}
```
Output: `8`

Multiple integers:
```json
{"*": [2, 2, 2, 2, 2]}
```
Output: `32`

With variable data:
```json
{"*": [{"var": "A"}, {"var": "B"}]}
```
Input: `{ A: 6, B: 7 }`  
Output: `42`

Multiply by zero:
```json
{"*": [42, 0]}
```
Output: `0`

Negative numbers:
```json
{"*": [-3, 4]}
```
Output: `-12`

Floating point:
```json
{"*": [3.5, 2.0]}
```
Output: `7.0`

**Edge Cases:**
- Accepts any number of arguments (minimum 1)
- Multiplying by zero returns zero (not an error condition)

---

## none

**JSON Operator:** `none`  
**Purpose:** Checks if NO elements in an array pass a specified test condition (inverse of `any`).

**Syntax:**
```json
{"none": [array, condition]}
```

**Special Variables:**
- `{"var": ""}` - The current element being tested

**Examples:**

All elements fail condition (all negative):
```json
{"none": [[-3, -2, -1], {">": [{"var": ""}, 0]}]}
```
Output: `true` (none are positive)

One element passes (has positive):
```json
{"none": [[-1, 0, 1], {">": [{"var": ""}, 0]}]}
```
Output: `false` (1 is positive)

Empty array:
```json
{"none": [[], {">": [{"var": ""}, 0]}]}
```
Output: `true` (vacuous truth - no elements to fail)

All odd numbers (none even):
```json
{"none": [[1, 3, 5], {"==": [{"%": [{"var": ""}, 2]}, 0]}]}
```
Output: `true`

Has one even:
```json
{"none": [[1, 2, 5], {"==": [{"%": [{"var": ""}, 2]}, 0]}]}
```
Output: `false` (2 is even)

**Edge Cases:**
- Empty array returns `true` (vacuous truth)
- Returns `true` only if ALL elements fail condition

---

## not-equals

**JSON Operator:** `!=`  
**Purpose:** Tests inequality between two values.

**Syntax:**
```json
{"!=": [value1, value2]}
```

**Examples:**

Different integers:
```json
{"!=": [1, 2]}
```
Output: `true`

Same integers:
```json
{"!=": [1, 1]}
```
Output: `false`

With variable data (not equal):
```json
{"!=": [{"var": "Value"}, 100]}
```
Input: `{ Value: 42 }`  
Output: `true`

With variable data (equal):
```json
{"!=": [{"var": "Value"}, 42]}
```
Input: `{ Value: 42 }`  
Output: `false`

**C# Semantics:**
- **No type coercion** (unlike JavaScript JSONLogic)
- Cannot compare `int` to `string` (e.g., `1` vs `"1"` fails to compile)
- Cannot compare `int` to `bool` (e.g., `0` vs `false` fails to compile)
- Both operands must have compatible types

---

## not

**JSON Operator:** `!`  
**Purpose:** Logical NOT operation. Inverts a boolean value.

**Syntax:**
```json
{"!": [boolValue]}
```

**Unary Syntax:**
```json
{"!": boolValue}
```

**Examples:**

Negate true:
```json
{"!": [true]}
```
Output: `false`

Negate false:
```json
{"!": [false]}
```
Output: `true`

Unary syntax:
```json
{"!": true}
```
Output: `false`

With comparison:
```json
{"!": {"==": [{"var": "Value"}, 0]}}
```
Input: `{ Value: 42 }`  
Output: `true` (42 != 0, so !(42 == 0) = true)

**C# Semantics:**
- Only supports `bool` types (no JavaScript-style "truthiness")
- Cannot use `int`, `string`, arrays, or other types with NOT
- Values like `0`, `""`, `[]` are NOT treated as falsy

---

## or

**JSON Operator:** `or`  
**Purpose:** Logical OR operation. Returns boolean result of OR between conditions.

**Syntax:**
```json
{"or": [condition1, condition2, ...]}
```

**Examples:**

One true condition:
```json
{"or": [true, false]}
```
Output: `true`

Both false:
```json
{"or": [false, false]}
```
Output: `false`

With variable data:
```json
{"or": [{"==": [{"var": "Value"}, 0]}, {"==": [{"var": "Value"}, 42]}]}
```
Input: `{ Value: 42 }`  
Output: `true`

**C# Semantics:**
- Only supports `bool` types (no JavaScript-style "truthiness")
- Cannot mix types like `bool`, `string`, `int` in same OR expression
- Does NOT return "first truthy value" like JavaScript (returns `bool`)

---

## select

**JSON Operator:** `select` (JSONLogic `map`)  
**Purpose:** Transforms array elements by applying an expression to each element.

**Syntax:**
```json
{"select": [array, transformExpression]}
```

**Special Variables:**
- `{"var": ""}` - The current element being transformed

**Examples:**

Multiply by two:
```json
{"select": [{"var": "Numbers"}, {"*": [{"var": ""}, 2]}]}
```
Input: `{ Numbers: [1, 2, 3, 4, 5] }`  
Output: `[2, 4, 6, 8, 10]`

Add constant:
```json
{"select": [{"var": "Numbers"}, {"+": [{"var": ""}, 10]}]}
```
Input: `{ Numbers: [1, 2, 3] }`  
Output: `[11, 12, 13]`

Square numbers:
```json
{"select": [{"var": "Numbers"}, {"*": [{"var": ""}, {"var": ""}]}]}
```
Input: `{ Numbers: [2, 3, 4] }`  
Output: `[4, 9, 16]`

**Edge Cases:**
- Empty array returns empty array
- Transform is applied to each element independently

---

## select-many

**JSON Operator:** `selectMany`  
**Purpose:** Projects each element to an array, then flattens all resulting arrays into a single array (flatten/flatMap).

**Syntax:**
```json
{"selectMany": [array, selectorExpression]}
```

**Special Variables:**
- `{"var": ""}` - The current element being processed

**Examples:**

Flatten nested arrays:
```json
{"selectMany": [{"var": "Teams"}, {"var": "Members"}]}
```
Input: `{ Teams: [{ Members: ["Alice", "Bob"] }, { Members: ["Charlie"] }] }`  
Output: `["Alice", "Bob", "Charlie"]`

Flatten scores:
```json
{"selectMany": [{"var": "People"}, {"var": "Scores"}]}
```
Input: `{ People: [{ Scores: [90, 85] }, { Scores: [78, 88] }] }`  
Output: `[90, 85, 78, 88]`

With transformation:
```json
{"selectMany": [{"var": "People"}, {"select": [{"var": "Scores"}, {"*": [{"var": ""}, 2]}]}]}
```
Input: `{ People: [{ Scores: [10, 20] }, { Scores: [30] }] }`  
Output: `[20, 40, 60]`

**Edge Cases:**
- Empty source array returns empty array
- Empty nested arrays are skipped (not included in result)
- All nested arrays are flattened into single-level array

---

## string-contains

**JSON Operator:** `contains` (for strings)  
**Purpose:** Tests if a substring exists within a string.

**Syntax:**
```json
{"contains": [substring, string]}
```

**Examples:**

Substring present:
```json
{"contains": ["Spring", "Springfield"]}
```
Output: `true`

Substring not present:
```json
{"contains": ["Summer", "Springfield"]}
```
Output: `false`

Empty substring (always true):
```json
{"contains": ["", "test"]}
```
Output: `true`

Case sensitive:
```json
{"contains": ["SPRING", "Springfield"]}
```
Output: `false` (case matters)

With variable data:
```json
{"contains": ["test", {"var": "Text"}]}
```
Input: `{ Text: "This is a test string" }`  
Output: `true`

At beginning:
```json
{"contains": ["Hello", "Hello World"]}
```
Output: `true`

At end:
```json
{"contains": ["World", "Hello World"]}
```
Output: `true`

**C# Semantics:**
- Case-sensitive comparison (no case-insensitive option)
- Uses `string.Contains()` internally

**Note:** Same operator name as array membership test, but distinguished by argument types (string vs array).

---

## substring

**JSON Operator:** `substring` (JSONLogic `substr`)  
**Purpose:** Extracts a portion of a string.

**Syntax:**
```json
{"substring": [string, startIndex]}
{"substring": [string, startIndex, length]}
```

**Examples:**

From index to end:
```json
{"substring": ["jsonlogic", 4]}
```
Output: `"logic"`

With length:
```json
{"substring": ["jsonlogic", 1, 3]}
```
Output: `"son"`

From beginning:
```json
{"substring": ["jsonlogic", 0, 4]}
```
Output: `"json"`

With variable data:
```json
{"substring": [{"var": "Text"}, 0, 5]}
```
Input: `{ Text: "Hello World" }`  
Output: `"Hello"`

Length exceeds string:
```json
{"substring": ["short", 2, 100]}
```
Output: `"ort"` (returns remainder)

Start exceeds length:
```json
{"substring": ["short", 100]}
```
Output: `""` (empty string)

**C# Semantics:**
- Does NOT support negative indices (fails to compile)
- Does NOT support negative lengths (fails to compile)
- Uses 0-based indexing
- Uses `string.Substring()` internally

---

## subtract

**JSON Operator:** `-`  
**Purpose:** Subtracts numbers or negates a single number.

**Syntax:**
```json
{"-": [minuend, subtrahend]}
{"-": value}
```

**Examples:**

Binary subtraction:
```json
{"-": [4, 2]}
```
Output: `2`

With variable data:
```json
{"-": [{"var": "A"}, {"var": "B"}]}
```
Input: `{ A: 50, B: 8 }`  
Output: `42`

Unary negation (positive to negative):
```json
{"-": 2}
```
Output: `-2`

Unary negation (negative to positive):
```json
{"-": -2}
```
Output: `2`

Result is negative:
```json
{"-": [2, 5]}
```
Output: `-3`

Floating point:
```json
{"-": [5.5, 3.2]}
```
Output: `2.3`

**Modes:**
- Binary: `{"-": [a, b]}` → a - b
- Unary: `{"-": a}` → -a (negation)

---

## symbol-add

**JSON Operator:** `+`  
**Purpose:** Adds numbers together OR concatenates strings.

**Syntax:**
```json
{"+": [value1, value2, ...]}
{"+": value}
```

**Examples:**

Add integers:
```json
{"+": [4, 2]}
```
Output: `6`

Add multiple:
```json
{"+": [2, 2, 2, 2, 2]}
```
Output: `10`

With variable data:
```json
{"+": [{"var": "A"}, {"var": "B"}]}
```
Input: `{ A: 10, B: 32 }`  
Output: `42`

Negative numbers:
```json
{"+": [-5, 3]}
```
Output: `-2`

Floating point:
```json
{"+": [3.14, 2.86]}
```
Output: `6.0`

Unary (identity):
```json
{"+": 42}
```
Output: `42`

Concatenate strings:
```json
{"+": ["Hello", " World"]}
```
Output: `"Hello World"`

Multiple strings:
```json
{"+": ["A", "B", "C", "D"]}
```
Output: `"ABCD"`

String and number:
```json
{"+": ["Value: ", 42]}
```
Output: `"Value: 42"`

**Distinctions:**
- `+` (symbol-add) - Adds numbers OR concatenates strings (this operator)
- `add` (linq.add) - Adds a single value to an array
- `addRange` (append) - Concatenates two arrays

**C# Semantics:**
- Cannot use unary `+` on strings (fails to compile)
- Supports native C# string + number concatenation

---

## var

**JSON Operator:** `var`  
**Purpose:** Accesses data from the input object (variable/property lookup).

**Syntax:**
```json
{"var": "propertyName"}
{"var": ["propertyName", defaultValue]}
{"var": ""}
```

**Examples:**

Simple property:
```json
{"var": "A"}
```
Input: `{ A: 42 }`  
Output: `42`

Property with array syntax:
```json
{"var": ["B"]}
```
Input: `{ B: 100 }`  
Output: `100`

With default value (missing property):
```json
{"var": ["Z", 999]}
```
Input: `{ A: 42 }`  
Output: `999` (property Z doesn't exist, returns default)

Nested property (dot notation):
```json
{"var": "Child.Value"}
```
Input: `{ Child: { Value: 123 } }`  
Output: `123`

Empty string (entire data object):
```json
{"var": ""}
```
Input: `{ A: 42, B: 100 }`  
Output: `{ A: 42, B: 100 }` (returns entire input)

Array index:
```json
{"var": 1}
```
Input: `[10, 20, 30]` (array as input)  
Output: `20`

**Error Handling:**
- Invalid/missing property without default returns `null` and fires `ErrorEvent`
- Requires nullable output type for missing properties: `TryCompile<TIn, TOut?>`

**Special Cases:**
- `{"var": ""}` returns the entire input data object
- Supports dot notation for nested properties (`"Child.Value"`)
- Supports array indexing when input is array type

---

## where

**JSON Operator:** `where` (JSONLogic `filter`)  
**Purpose:** Filters array elements by applying a predicate to each element.

**Syntax:**
```json
{"where": [array, predicateExpression]}
```

**Special Variables:**
- `{"var": ""}` - The current element being tested

**Examples:**

Filter positive numbers:
```json
{"where": [{"var": "Numbers"}, {">": [{"var": ""}, 0]}]}
```
Input: `{ Numbers: [-2, -1, 0, 1, 2] }`  
Output: `[1, 2]`

Filter odd numbers:
```json
{"where": [{"var": "Numbers"}, {"!=": [{"%": [{"var": ""}, 2]}, 0]}]}
```
Input: `{ Numbers: [1, 2, 3, 4, 5] }`  
Output: `[1, 3, 5]`

No matches:
```json
{"where": [{"var": "Numbers"}, {">": [{"var": ""}, 100]}]}
```
Input: `{ Numbers: [1, 2, 3] }`  
Output: `[]` (empty array)

All match:
```json
{"where": [{"var": "Numbers"}, {">": [{"var": ""}, 0]}]}
```
Input: `{ Numbers: [1, 2, 3, 4, 5] }`  
Output: `[1, 2, 3, 4, 5]`

**Edge Cases:**
- Empty source array returns empty array
- Predicate must return `bool` type
- No matches returns empty array (not null)

---
