# JsonExpressions Operator Reference

This document describes every JsonExpression operator supported by this system, in alphabetical file order.
Each entry covers: what the operator does, its JSON shape, what it accepts, what it rejects, and examples.

---

## and

**Key:** `"and"`  
**Output:** `bool`

Logical AND across all operands. All operands must be `bool` expressions that compile to the same type. Uses C# short-circuit `&&` semantics (`AndAlso`). Returns `true` only when all operands are `true`.

**Important constraint:** Unlike standard JSONLogic, this implementation does NOT support "truthiness" across mixed types (no `"and": [1, "a", 3]`). All operands must be boolean expressions. Mixed-type operands will fail to compile.

**Accepts:**
- A JSON array of exactly 2 boolean expressions
- Nested operators (e.g. `>`, `<`, `==`) as operands

**Rejects:**
- Non-array value (e.g. `{"and": {"a": true}}`) → compile failure
- Empty array `{"and": []}` → compile failure
- Single operand `{"and": [true]}` → compile failure
- Three or more operands `{"and": [true, false, true]}` → compile failure
- Mixed-type operands (e.g. bools and numerics) → compile failure
- `TOut` other than `bool` → compile failure

**Examples:**
```json
{"and": [true, true]}
// → true

{"and": [true, false]}
// → false

{"and": [{">": [{"var": "Value"}, 0]}, {"<": [{"var": "Value"}, 100]}]}
// → true when Value is 42, false when Value is 200
```

---

## ArrayLiteral

**Key:** *(bare JSON array, no operator key)*  
**Output:** `T[]` where T is the element type

A JSON array that is not wrapped in an operator object is treated as an array literal. Each element is compiled as an expression of the target element type.

**Accepts:**
- A bare JSON array `[...]` when `TOut` is an array type (`float[]`, `string[]`, `bool[]`, etc.)
- Homogeneous element types (all elements must be the same type as `TOut`'s element)

**Rejects:**
- `TOut` that is not an array type (e.g. `TOut=float` with `[1,2,3]`) → compile failure

**Examples:**
```json
[1, 2, 3]
// TOut=float[] → [1.0, 2.0, 3.0]

["a", "b", "c"]
// TOut=string[] → ["a", "b", "c"]

[true, false, true]
// TOut=bool[] → [true, false, true]
```

---

## BoolLiteral

**Key:** *(bare JSON boolean)*  
**Output:** `bool`

A bare `true` or `false` JSON value is compiled as a constant boolean expression.

**Accepts:**
- `true` or `false` when `TOut=bool`

**Rejects:**
- Any `TOut` other than `bool` (e.g. `TOut=float`, `TOut=string`) → compile failure

**Examples:**
```json
true
// TOut=bool → true

false
// TOut=bool → false
```

---

## contains

**Key:** `"contains"`  
**Output:** `bool`

Tests whether a string (the needle) appears as a substring inside another string (the haystack). The check is **case-sensitive**.

The argument array is `[substring, text]`.

**Accepts:**
- Array of exactly 2 string expressions: `[substring, text]`
  - `substring` is the string to search for
  - `text` is the string to search within
- Either argument may be a `{"var": ...}` or any other string-producing expression
- Empty string substring always returns `true`

**Rejects:**
- Non-array value (e.g. `{"contains": "not-an-array"}`) → compile failure
- Array with fewer than 2 elements → compile failure
- Array with more than 2 elements → compile failure
- `TOut` other than `bool` → compile failure

**Examples:**
```json
{"contains": ["Spring", "Springfield"]}
// → true  (case-sensitive match)

{"contains": ["SPRING", "Springfield"]}
// → false  (case-sensitive, no match)

{"contains": ["test", {"var": "Text"}]}
// → true when Text = "This is a test string"

{"contains": ["", "anything"]}
// → true  (empty substring always matches)
```

---

## if

**Key:** `"if"`  
**Output:** `TOut` (type of the then/else branches)

Conditional expression. Evaluates a boolean condition and returns one of two branches. Simple ternary only (no else-if chaining).

**Shape:** `{"if": [condition, thenValue, elseValue]}`

- Exactly 3 arguments required (condition, then, else)
- Condition **must** be `bool` — non-boolean "truthy" values (e.g. `1`, `0`) are not supported
- For else-if chains, nest `if` operators in the else branch

**Accepts:**
- Array with exactly 3 elements: `[bool, TOut, TOut]`
- Nested operators as condition or branch values

**Rejects:**
- Non-array value → compile failure
- Fewer than 3 arguments → compile failure
- More than 3 arguments → compile failure
- Non-bool condition (e.g. numeric `1` or `0`) → compile failure
- `TOut` mismatch between then/else branches → compile failure

**Examples:**
```json
{"if": [true, "yes", "no"]}
// → "yes"

{"if": [false, "yes", "no"]}
// → "no"

{"if": [{"==": [{"var": "Value"}, 42]}, "found", "not found"]}
// → "found" when Value=42

{"if": [{"<": [{"var": "Value"}, 0]}, "negative", {"if": [{"<": [{"var": "Value"}, 100]}, "small", "large"]}]}
// → "small" when Value=50, "large" when Value=100 (nested if for else-if)
```

---

## in

**Key:** `"in"`  
**Output:** `bool`

Tests whether a value is a member of an array. Uses `Array.Contains` / `Enumerable.Contains` semantics.

**Shape:** `{"in": [item, collection]}`

- First argument: the item to search for
- Second argument: must resolve to an **array type** (`T[]`)

**Accepts:**
- Array of exactly 2 elements
- `item` can be any scalar expression (string, float, etc.)
- `collection` must be an array-producing expression (array literal or `var` pointing to an array field)

**Rejects:**
- Non-array value (e.g. `{"in": "not-an-array"}`) → compile failure
- Fewer than 2 arguments → compile failure
- More than 2 arguments → compile failure
- Collection argument resolving to a scalar (non-array type) → compile failure
- `TOut` other than `bool` → compile failure

**Examples:**
```json
{"in": ["Ringo", ["John", "Paul", "George", "Ringo"]]}
// → true

{"in": ["Pete", ["John", "Paul", "George", "Ringo"]]}
// → false

{"in": [3, [1, 2, 3, 4, 5]]}
// → true

{"in": [1, []]}
// → false  (empty collection)
```

---

## LinqAdd (push)

**Key:** `"push"`  
**Output:** `T[]`

Appends a single item to the **end** of an array and returns the new array. The arguments are `[item, array]` — item first, array second.

**Shape:** `{"push": [item, array]}`

**Accepts:**
- Exactly 2 arguments: `[scalar, array]`
- `item` must be a scalar expression matching the array element type
- `array` must resolve to an array type (`T[]`)
- Either argument may be a `{"var": ...}` expression

**Rejects:**
- Non-array value for the outer JSON (e.g. `{"push": "not-an-array"}`) → compile failure
- Exactly 1 argument → compile failure
- More than 2 arguments → compile failure
- Arguments in wrong order `[array, scalar]` → compile failure (first arg must be scalar)
- Both arguments as arrays (`[array, array]`) → compile failure (use `addRange` instead)
- `TOut` other than an array type → compile failure

**Examples:**
```json
{"push": [5, [1, 2, 3, 4]]}
// TOut=float[] → [1, 2, 3, 4, 5]

{"push": [42, []]}
// TOut=float[] → [42]

{"push": [{"var": "Value"}, [10, 20, 30]]}
// TOut=float[], Value=99 → [10, 20, 30, 99]

{"push": ["World", ["Hello"]]}
// TOut=string[] → ["Hello", "World"]
```

---

## LinqAddRange (addRange)

**Key:** `"addRange"`  
**Output:** `T[]`

Concatenates two arrays and returns the combined result. Both arguments must be arrays of the same element type.

**Shape:** `{"addRange": [array1, array2]}`

**Accepts:**
- Exactly 2 arguments, both array expressions of the same `T[]` element type

**Rejects:**
- Non-array value for the outer JSON (e.g. `{"addRange": "not-an-array"}`) → compile failure
- Exactly 1 argument → compile failure
- More than 2 arguments (e.g. 3 arrays) → compile failure
- Scalar arguments instead of arrays → compile failure
- `TOut` other than an array type → compile failure

**Examples:**
```json
{"addRange": [[1, 2], [3, 4]]}
// TOut=float[] → [1, 2, 3, 4]

{"addRange": [[], []]}
// TOut=float[] → []
```

---

## LinqAggregate (aggregate)

**Key:** `"aggregate"`  
**Output:** `TOut` (the accumulator type)

Reduces an array to a single value by repeatedly applying an accumulator expression. Equivalent to `Enumerable.Aggregate` with a seed.

Inside the accumulator expression, two special `var` names are available:
- `{"var": "current"}` — the current array element
- `{"var": "accumulator"}` — the running accumulated value

**Shape:** `{"aggregate": [source, accumulatorExpression, initialValue]}`

- `source`: must resolve to an array type (`T[]`)
- `accumulatorExpression`: expression using `current` and `accumulator` vars, producing `TOut`
- `initialValue`: seed value expression, must match `TOut`

**Accepts:**
- Exactly 3 arguments in the array
- Source resolving to an array type
- Any expression as accumulator that uses `current` / `accumulator`

**Rejects:**
- Non-array value for the outer JSON → compile failure
- Fewer than 3 arguments → compile failure
- More than 3 arguments → compile failure
- Source that doesn't resolve to an array type (e.g. a number literal) → compile failure
- `TOut` mismatch between accumulator expression and `TOut` → compile failure

**Examples:**
```json
{"aggregate": [{"var": "Numbers"}, {"+": [{"var": "current"}, {"var": "accumulator"}]}, 0]}
// Numbers=[1,2,3,4,5] → 15  (sum)

{"aggregate": [{"var": "Numbers"}, {"*": [{"var": "current"}, {"var": "accumulator"}]}, 1]}
// Numbers=[2,3,4] → 24  (product)

{"aggregate": [{"var": "Numbers"}, {"+": [{"var": "current"}, {"var": "accumulator"}]}, 100]}
// Numbers=[] → 100  (empty array returns initial value)

{"aggregate": [{"var": "Numbers"}, {"if": [{">": [{"var": "current"}, {"var": "accumulator"}]}, {"var": "current"}, {"var": "accumulator"}]}, 0]}
// Numbers=[5,2,8,1,9] → 9  (max via if)
```

---

## `all`

**Key:** `"all"`  
**Output:** `bool`

Tests whether **all** elements in an array satisfy a predicate. The predicate expression uses `{"var": ""}` (empty string) to refer to the current element.

> **Note:** Empty array returns `false` (not `true`). This differs from set-logic "vacuous truth" convention and some JSONLogic implementations.

**Accepts:**
- Exactly 2 arguments: `[source_array, predicate_expr]`
- Source resolving to an array type
- Predicate expression that returns `bool`, using `{"var": ""}` for the current element
- Complex predicates using nested operators (`and`, `<`, `%`, etc.)

**Rejects:**
- Non-array value for the outer JSON → compile failure
- Fewer than 2 arguments → compile failure
- More than 2 arguments → compile failure
- Source that doesn't resolve to an array type (e.g. a float field) → compile failure
- Requesting non-`bool` output type → compile failure

**Examples:**
```json
{"all": [[1, 2, 3], {">": [{"var": ""}, 0]}]}
// → true  (all positive)

{"all": [[-1, 2, 3], {">": [{"var": ""}, 0]}]}
// → false  (one negative)

{"all": [[], {">": [{"var": ""}, 0]}]}
// → false  (empty array always false)

{"all": [[2, 4, 6], {"==": [{"%": [{"var": ""}, 2]}, 0]}]}
// → true  (all even)

{"all": [[10, 20, 30], {"and": [{">": [{"var": ""}, 0]}, {"<": [{"var": ""}, 100]}]}]}
// → true  (all between 0 and 100)
```

---

## `any`

**Key:** `"any"`  
**Output:** `bool`

Tests whether **at least one** element in an array satisfies a predicate. The predicate expression uses `{"var": ""}` (empty string) to refer to the current element.

> **Note:** Empty array returns `false`.

**Accepts:**
- Exactly 2 arguments: `[source_array, predicate_expr]`
- Source resolving to an array type
- Predicate expression that returns `bool`, using `{"var": ""}` for the current element

**Rejects:**
- Non-array value for the outer JSON → compile failure
- Fewer than 2 arguments → compile failure
- More than 2 arguments → compile failure
- Source that doesn't resolve to an array type → compile failure
- Requesting non-`bool` output type → compile failure

**Examples:**
```json
{"any": [[-1, 0, 1], {">": [{"var": ""}, 0]}]}
// → true  (one positive element)

{"any": [[-3, -2, -1], {">": [{"var": ""}, 0]}]}
// → false  (none positive)

{"any": [[], {">": [{"var": ""}, 0]}]}
// → false  (empty array)

{"any": [[1, 2, 5], {"==": [{"%": [{"var": ""}, 2]}, 0]}]}
// → true  (2 is even)
```

---

## `none`

**Key:** `"none"`  
**Output:** `bool`

Tests whether **no** elements in an array satisfy a predicate. The predicate expression uses `{"var": ""}` (empty string) to refer to the current element.

> **Note:** Empty array returns `true` (vacuous truth — zero elements means zero failures).

**Accepts:**
- Exactly 2 arguments: `[source_array, predicate_expr]`
- Source resolving to an array type
- Predicate expression that returns `bool`, using `{"var": ""}` for the current element

**Rejects:**
- Non-array value for the outer JSON → compile failure
- Fewer than 2 arguments → compile failure
- More than 2 arguments → compile failure
- Source that doesn't resolve to an array type → compile failure
- Requesting non-`bool` output type → compile failure

**Examples:**
```json
{"none": [[-3, -2, -1], {">": [{"var": ""}, 0]}]}
// → true  (no elements are positive)

{"none": [[-1, 0, 1], {">": [{"var": ""}, 0]}]}
// → false  (one element is positive)

{"none": [[], {">": [{"var": ""}, 0]}]}
// → true  (empty array → vacuously none)

{"none": [[1, 3, 5], {"==": [{"%": [{"var": ""}, 2]}, 0]}]}
// → true  (no even numbers)
```

---

## `select`

**Key:** `"select"`  
**Output:** `TElement[]` (array of projected elements)

Projects each element of an array through a transform expression, producing a new array of the results. Equivalent to LINQ `Select` / JSONLogic `map`. The transform uses `{"var": ""}` (empty string) to refer to the current element.

**Accepts:**
- Exactly 2 arguments: `[source_array, transform_expr]`
- Source resolving to an array type
- Any transform expression using `{"var": ""}` for the current element
- Empty source array → returns empty array

**Rejects:**
- Non-array value for the outer JSON → compile failure
- Fewer than 2 arguments → compile failure
- More than 2 arguments → compile failure
- Source that doesn't resolve to an array type (e.g. a numeric literal) → compile failure
- `TOut` not being an array type → compile failure

**Examples:**
```json
{"select": [{"var": "Numbers"}, {"*": [{"var": ""}, 2]}]}
// Numbers=[1,2,3,4,5] → [2,4,6,8,10]

{"select": [{"var": "Numbers"}, {"+": [{"var": ""}, 10]}]}
// Numbers=[1,2,3] → [11,12,13]

{"select": [{"var": "Numbers"}, {"*": [{"var": ""}, {"var": ""}]}]}
// Numbers=[2,3,4] → [4,9,16]  (square each element)

{"select": [{"var": "Numbers"}, {"*": [{"var": ""}, 2]}]}
// Numbers=[] → []
```

---

## `selectMany`

**Key:** `"selectMany"`  
**Output:** `TElement[]` (flattened array)

Projects each element of a source array through a selector expression that returns an array, then flattens all the resulting arrays into a single array. Equivalent to LINQ `SelectMany`. The selector uses `{"var": "PropertyName"}` or any expression that returns an array; `{"var": ""}` refers to the current outer element.

> **Note:** Unlike `select`, the selector expression must return an **array type** — this is what gets flattened. Elements with empty nested arrays contribute nothing to the output.

> **Note:** You can nest `{"map": [...]}` or other array operators inside the selector to transform before flattening.

**Accepts:**
- Exactly 2 arguments: `[source_array, selector_expr]`
- Source resolving to an array type
- Selector expression that returns an array type
- Empty source → returns empty; some empty nested arrays → those are skipped

**Rejects:**
- Non-array value for the outer JSON → compile failure
- Fewer than 2 arguments → compile failure
- More than 2 arguments → compile failure
- First argument not resolving to an array type → compile failure
- Selector expression not returning an array type (e.g. `{"var": "Name"}` where `Name` is `string`) → compile failure
- `TOut` not being an array type → compile failure
- `TOut` element type mismatch with selector → compile failure
- Non-existent property path in selector → compile failure

**Examples:**
```json
{"selectMany": [{"var": "Teams"}, {"var": "Members"}]}
// Teams=[{Name:"Alpha",Members:["Alice","Bob"]},{Name:"Beta",Members:["Charlie"]}]
// → ["Alice","Bob","Charlie"]

{"selectMany": [{"var": "People"}, {"var": "Scores"}]}
// People=[{Name:"Alice",Scores:[90,85]},{Name:"Bob",Scores:[78]}]
// → [90,85,78]

{"selectMany": [{"var": "People"}, {"map": [{"var": "Scores"}, {"*": [{"var": ""}, 2]}]}]}
// People=[{Scores:[10,20]},{Scores:[30]}] → [20,40,60]  (transform then flatten)
```

---

## `where`

**Key:** `"where"`  
**Output:** `TElement[]` (same element type as source)

Filters an array to only elements that satisfy a predicate. Equivalent to LINQ `Where` / JSONLogic `filter`. The predicate uses `{"var": ""}` (empty string) to refer to the current element.

> **Important:** The predicate expression **must** return an actual `bool`. This system does not support JSONLogic "truthiness" — expressions like `{"%": [{"var": ""}, 2]}` return a number, not a bool, and will fail. Wrap with a comparison: `{"!=": [{"%": [{"var": ""}, 2]}, 0]}`.

**Accepts:**
- Exactly 2 arguments: `[source_array, predicate_expr]`
- Source resolving to an array type
- Predicate expression that explicitly returns `bool`, using `{"var": ""}` for the current element
- Empty source array → returns empty
- No matching elements → returns empty

**Rejects:**
- Non-array value for the outer JSON → compile failure
- Fewer than 2 arguments → compile failure
- More than 2 arguments → compile failure
- Source that doesn't resolve to an array type (e.g. a numeric literal) → compile failure
- `TOut` not matching the source element type → compile failure

**Examples:**
```json
{"where": [{"var": "Numbers"}, {">": [{"var": ""}, 0]}]}
// Numbers=[-2,-1,0,1,2] → [1,2]  (keep positives)

{"where": [{"var": "Numbers"}, {"!=": [{"%": [{"var": ""}, 2]}, 0]}]}
// Numbers=[1,2,3,4,5] → [1,3,5]  (keep odds — note: must wrap % in != comparison)

{"where": [{"var": "Numbers"}, {">": [{"var": ""}, 100]}]}
// Numbers=[1,2,3,4,5] → []  (no matches)
```

---

## log

**Key:** `"log"`  
**Output:** `TOut` (pass-through - returns same type as inner expression)

Evaluates an expression, logs the result as a side effect, and returns the result unchanged. Used for debugging JSON expression evaluation. The logged value is emitted via the `LogEvent` event bus.

**Shape:** `{"log": expression}`

The expression can be a literal value, a `{"var": ...}`, or any nested operator.

**Accepts:**
- Exactly 1 argument (either as bare value or single-element array)
- Any expression type (string, number, boolean, nested operators)
- `TOut` must match the inner expression's return type

**Rejects:**
- Empty array `{"log": []}` → compile failure
- More than 1 element in array form `{"log": [42, 99]}` → compile failure
- `TOut` mismatch (e.g. inner returns `float`, but requesting `bool`) → compile failure

**Examples:**
```json
{"log": "apple"}
// TOut=string → "apple"  (logs "apple")

{"log": 42}
// TOut=float → 42  (logs "42")

{"log": true}
// TOut=bool → true  (logs "True")

{"log": {"var": "Value"}}
// TOut=float, Value=100 → 100  (logs "100")

{"log": {"+": [1, 2]}}
// TOut=float → 3  (logs "3")
```

---

## max

**Key:** `"max"`  
**Output:** `float`

Returns the maximum value from a list of numeric expressions. Requires at least 1 value.

**Shape:** `{"max": [expr1, expr2, ...]}`

**Accepts:**
- Array of 1 or more numeric expressions
- Numeric values (all coerced to float)
- Nested expressions like `{"var": "A"}` or arithmetic operators
- Negative numbers
- Single value (returns that value)

**Rejects:**
- Non-array value `{"max": "not-an-array"}` → compile failure
- Empty array `{"max": []}` → compile failure
- `TOut` that is not numeric (e.g. `string[]`, `bool`) → compile failure

**Examples:**
```json
{"max": [1, 2, 3]}
// → 3

{"max": [5, 2]}
// → 5

{"max": [-5, -2, -10]}
// → -2

{"max": [{"var": "A"}, {"var": "B"}, {"var": "C"}]}
// A=10, B=50, C=30 → 50

{"max": [1.5, 2.7, 0.3]}
// → 2.7

{"max": [42]}
// → 42  (single value)
```

---

## min

**Key:** `"min"`  
**Output:** `float`

Returns the minimum value from a list of numeric expressions. Requires at least 1 value.

**Shape:** `{"min": [expr1, expr2, ...]}`

**Accepts:**
- Array of 1 or more numeric expressions
- Numeric values (all coerced to float)
- Nested expressions like `{"var": "A"}` or arithmetic operators
- Negative numbers
- Single value (returns that value)

**Rejects:**
- Non-array value `{"min": "not-an-array"}` → compile failure
- Empty array `{"min": []}` → compile failure
- `TOut` that is not numeric (e.g. `bool`, `string`) → compile failure

**Examples:**
```json
{"min": [1, 2, 3]}
// → 1

{"min": [5, 2]}
// → 2

{"min": [-5, -2, -10]}
// → -10

{"min": [{"var": "A"}, {"var": "B"}, {"var": "C"}]}
// A=10, B=50, C=30 → 10

{"min": [1.5, 2.7, 0.3]}
// → 0.3

{"min": [42]}
// → 42  (single value)
```

---

## NumberLiteral

**Key:** *(bare JSON number)*  
**Output:** `float`

A bare numeric value in JSON is compiled as a constant number expression.

**Accepts:**
- Any JSON number value when `TOut=float`
- Numeric literals (e.g. `42`, `-100`, `3.14`, `-2.5`)

**Rejects:**
- `TOut` that is not numeric (e.g. `float[]`, `bool`, `string`) → compile failure
- Non-number JSON tokens (e.g. `true`, `"text"`) → compile failure

**Examples:**
```json
42
// TOut=float → 42.0

-3.14
// TOut=float → -3.14

0
// TOut=float → 0.0
```

---

## or

**Key:** `"or"`  
**Output:** `bool`

Logical OR across all operands. Returns `true` if any operand is `true`. Uses C# short-circuit `||` semantics (`OrElse`). Returns `false` only when all operands are `false`.

**Important constraint:** Unlike standard JSONLogic, this implementation does NOT support "truthiness" across mixed types (no `"or": [false, "a", 1]`). All operands must be boolean expressions. Mixed-type operands will fail to compile.

**Accepts:**
- A JSON array of exactly 2 boolean expressions
- Nested operators (e.g. `>`, `==`, `and`) as operands

**Rejects:**
- Non-array value (e.g. `{"or": "not-an-array"}`) → compile failure
- Empty array `{"or": []}` → compile failure
- Single operand `{"or": [true]}` → compile failure
- Three or more operands `{"or": [true, false, true]}` → compile failure
- Mixed-type operands (e.g. bools and strings) → compile failure
- `TOut` other than `bool` → compile failure

**Examples:**
```json
{"or": [true, false]}
// → true

{"or": [false, true]}
// → true

{"or": [false, false]}
// → false

{"or": [{"==": [{"var": "Value"}, 0]}, {"==": [{"var": "Value"}, 42]}]}
// Value=42 → true, Value=100 → false
```

---

## StringLiteral

**Key:** *(bare JSON string)*  
**Output:** `string`

A bare string value in JSON is compiled as a constant string expression.

**Accepts:**
- Any JSON string value when `TOut=string`
- Empty strings `""`

**Rejects:**
- `TOut` other than `string` (e.g. `float`, `bool`) → compile failure

**Examples:**
```json
"hello"
// TOut=string → "hello"

""
// TOut=string → ""  (empty string)

"Hello, World!"
// TOut=string → "Hello, World!"
```

---

## substring

**Key:** `"substring"`  
**Output:** `string`

Extracts a substring from a string. Takes 2 or 3 arguments: `[source, startIndex]` or `[source, startIndex, length]`. Uses C# `String.Substring()` semantics.

**Shape:** `{"substring": [source, startIndex]}` or `{"substring": [source, startIndex, length]}`

- `startIndex` is 0-based
- If `startIndex` exceeds string length, returns empty string
- If `length` is omitted, returns from `startIndex` to end
- If `length` exceeds remaining characters, returns remainder

**Important:** Unlike JSONLogic's `substr`, this implementation does NOT support negative indices. Both `startIndex` and `length` must be non-negative.

**Accepts:**
- Array of 2 or 3 elements
- First argument: string expression
- Second argument: non-negative integer (start index)
- Third argument (optional): non-negative integer (length)

**Rejects:**
- Non-array value (e.g. `{"substring": "not-an-array"}`) → compile failure
- Fewer than 2 arguments `{"substring": ["hello"]}` → compile failure
- More than 3 arguments `{"substring": ["hello", 0, 3, "extra"]}` → compile failure
- Negative `startIndex` or `length` → compile failure
- `TOut` other than `string` → compile failure

**Examples:**
```json
{"substring": ["jsonlogic", 4]}
// → "logic"  (from index 4 to end)

{"substring": ["jsonlogic", 1, 3]}
// → "son"  (3 characters starting at index 1)

{"substring": ["jsonlogic", 0, 4]}
// → "json"  (first 4 characters)

{"substring": [{"var": "Text"}, 0, 5]}
// Text="Hello World" → "Hello"

{"substring": ["short", 2, 100]}
// → "ort"  (length exceeds, returns remainder)

{"substring": ["short", 100]}
// → ""  (start exceeds length, returns empty)
```

---

## + (SymbolAdd)

**Key:** `"+"`  
**Output:** `float` or `string` (depends on operand types)

Addition for numbers or concatenation for strings. Can handle multiple operands. For single operand (unary `+`), returns the operand unchanged.

**Numeric Mode (`TOut=float`):**
- Sums all numeric operands
- Supports negative numbers
- Unary form `{"+": 42}` returns the number

**String Mode (`TOut=string`):**
- Concatenates all operands as strings
- If operand is numeric, converts to string first
- Does NOT support unary string form

**Shape:** `{"+": [operand1, operand2, ...]}` or `{"+": singleValue}` (unary, numeric only)

**Accepts:**
- Array of 1+ operands (all same type, or mixed numeric/string for string concatenation)
- Single numeric value for unary `+`
- Nested expressions

**Rejects:**
- Empty array `{"+": []}` → compile failure
- Non-array, non-numeric value (e.g. `{"+": true}`) → compile failure
- Unary string form `{"+": "hello"}` → compile failure
- `TOut` mismatch (e.g. returns number but requesting `bool`) → compile failure
- `TOut` as array type → compile failure

**Examples:**
```json
{"+": [4, 2]}
// TOut=float → 6

{"+": [2, 2, 2, 2, 2]}
// TOut=float → 10

{"+": [-5, 3]}
// TOut=float → -2

{"+": [3.14, 2.86]}
// TOut=float → 6.0

{"+": 42}
// TOut=float → 42  (unary)

{"+": ["Hello", " World"]}
// TOut=string → "Hello World"

{"+": ["A", "B", "C", "D"]}
// TOut=string → "ABCD"

{"+": ["Value: ", 42]}
// TOut=string → "Value: 42"  (number converted to string)

{"+": ["", ""]}
// TOut=string → ""
```

---

## / (SymbolDivide)

**Key:** `"/"`  
**Output:** `float`

Division of two numeric values. Always performs floating-point division. Division by zero returns `Infinity` per IEEE 754 (does not throw exception).

**Shape:** `{"/": [dividend, divisor]}`

**Accepts:**
- Exactly 2 numeric operands
- Negative numbers
- Division by zero (returns `Infinity`)

**Rejects:**
- Non-array value `{"/": "not-an-array"}` → compile failure
- Fewer than 2 arguments `{"/": [10]}` → compile failure
- More than 2 arguments `{"/": [10, 2, 5]}` → compile failure
- `TOut` that is not numeric (e.g. `bool`, `string`, array) → compile failure

**Examples:**
```json
{"/": [4, 2]}
// → 2

{"/": [5, 2]}
// → 2.5  (float division)

{"/": [7.5, 2.5]}
// → 3.0

{"/": [-10, 2]}
// → -5

{"/": [42, 0]}
// → Infinity  (division by zero)

{"/": [{"var": "A"}, {"var": "B"}]}
// A=84, B=2 → 42
```

---

## == (SymbolEquals)

**Key:** `"=="`  
**Output:** `bool`

Equality comparison. Tests whether two values are equal. Supports numeric and string comparisons.

**Important constraint:** Unlike standard JSONLogic, this implementation does NOT support type coercion (no `"==": [1, "1"]` or `"==": [0, false]`). Both operands must be the same type.

**Shape:** `{"==": [left, right]}`

**Accepts:**
- Exactly 2 operands of the same type
- Numeric comparisons (float)
- String comparisons (case-sensitive)
- Nested expressions

**Rejects:**
- Non-array value (e.g. `{"==": "not-an-array"}`) → compile failure
- Fewer than 2 arguments `{"==": [1]}` → compile failure
- More than 2 arguments `{"==": [1, 1, 1]}` → compile failure
- Mixed-type operands (e.g. numeric and `string`, numeric and `bool`) → compile failure
- `TOut` other than `bool` → compile failure

**Examples:**
```json
{"==": [1, 1]}
// → true

{"==": [1, 2]}
// → false

{"==": [{"var": "Value"}, 42]}
// Value=42 → true, Value=100 → false

{"==": [{"var": "Name"}, "test"]}
// Name="test" → true, Name="other" → false
```

---

## > (SymbolGreaterThan)

**Key:** `">"`  
**Output:** `bool`

Greater-than comparison. Tests whether the left operand is strictly greater than the right operand. Supports numeric comparisons only.

**Shape:** `{">": [left, right]}`

**Accepts:**
- Exactly 2 numeric operands
- Negative numbers
- Nested expressions

**Rejects:**
- Non-array value (e.g. `{">": "not-an-array"}`) → compile failure
- Fewer than 2 arguments `{">": [1]}` → compile failure
- More than 2 arguments `{">": [1, 2, 3]}` → compile failure
- `TOut` other than `bool` → compile failure

**Examples:**
```json
{">": [2, 1]}
// → true

{">": [1, 2]}
// → false

{">": [1, 1]}
// → false  (equal values)

{">": [{"var": "Value"}, 10]}
// Value=42 → true, Value=5 → false

{">": [-1, -5]}
// → true

{">": [3.14, 2.71]}
// → true
```

---

## >= (SymbolGreaterThanOrEqual)

**Key:** `">="`  
**Output:** `bool`

Greater-than-or-equal comparison. Tests whether the left operand is greater than or equal to the right operand. Supports numeric comparisons only.

**Shape:** `{">=": [left, right]}`

**Accepts:**
- Exactly 2 numeric operands
- Negative numbers
- Nested expressions

**Rejects:**
- Non-array value (e.g. `{">=": "not-an-array"}`) → compile failure
- Fewer than 2 arguments `{">=": [1]}` → compile failure
- More than 2 arguments `{">=": [1, 2, 3]}` → compile failure
- `TOut` other than `bool` → compile failure

**Examples:**
```json
{">=": [2, 1]}
// → true

{">=": [1, 1]}
// → true  (equal values)

{">=": [1, 2]}
// → false

{">=": [{"var": "Value"}, 42]}
// Value=42 → true, Value=50 → true, Value=10 → false
```

---

## < (SymbolLessThan)

**Key:** `"<"`  
**Output:** `bool`

Less-than comparison. Tests whether the left operand is strictly less than the right operand. Supports numeric comparisons only.

**Important constraint:** Unlike standard JSONLogic, this implementation does NOT support chained comparisons (no `"<": [1, 2, 3]`). Only binary comparisons are supported.

**Shape:** `{"<": [left, right]}`

**Accepts:**
- Exactly 2 numeric operands
- Negative numbers
- Nested expressions

**Rejects:**
- Non-array value (e.g. `{"<": "not-an-array"}`) → compile failure
- Fewer than 2 arguments `{"<": [1]}` → compile failure
- More than 2 arguments (chained comparisons) `{"<": [1, 2, 3]}` → compile failure
- `TOut` other than `bool` → compile failure

**Examples:**
```json
{"<": [1, 2]}
// → true

{"<": [2, 1]}
// → false

{"<": [1, 1]}
// → false  (equal values)

{"<": [{"var": "Value"}, 100]}
// Value=42 → true, Value=150 → false
```

---

## <= (SymbolLessThanOrEqual)

**Key:** `"<="`  
**Output:** `bool`

Less-than-or-equal comparison. Tests whether the left operand is less than or equal to the right operand. Supports numeric comparisons only.

**Important constraint:** Unlike standard JSONLogic, this implementation does NOT support chained comparisons (no `"<=": [1, 2, 3]`). Only binary comparisons are supported.

**Shape:** `{"<=": [left, right]}`

**Accepts:**
- Exactly 2 numeric operands
- Negative numbers
- Nested expressions

**Rejects:**
- Non-array value (e.g. `{"<=": "not-an-array"}`) → compile failure
- Fewer than 2 arguments `{"<=": [1]}` → compile failure
- More than 2 arguments (chained comparisons) `{"<=": [1, 2, 3]}` → compile failure
- `TOut` other than `bool` → compile failure

**Examples:**
```json
{"<=": [1, 2]}
// → true

{"<=": [1, 1]}
// → true  (equal values)

{"<=": [2, 1]}
// → false

{"<=": [{"var": "Value"}, 42]}
// Value=42 → true, Value=10 → true, Value=50 → false
```

---

## % (SymbolModulo)

**Key:** `"%"`  
**Output:** `float`

Modulo operation (remainder after division). Returns the remainder when the first operand is divided by the second. Modulo by zero returns `NaN` per IEEE 754 (does not throw exception).

**Shape:** `{"%": [dividend, divisor]}`

**Accepts:**
- Exactly 2 numeric operands
- Negative numbers (result sign matches dividend)
- Modulo by zero (returns `NaN`)

**Rejects:**
- Non-array value `{"%": "not-an-array"}` → compile failure
- Fewer than 2 arguments `{"%": [10]}` → compile failure
- More than 2 arguments `{"%": [10, 3, 2]}` → compile failure
- `TOut` that is not numeric (e.g. `bool`, `string`, array) → compile failure

**Examples:**
```json
{"%": [101, 2]}
// → 1  (odd number)

{"%": [100, 2]}
// → 0  (even number)

{"%": [42, 10]}
// → 2

{"%": [5, 10]}
// → 5  (divisor larger than dividend)

{"%": [-7, 3]}
// → -1  (negative dividend)

{"%": [42, 0]}
// → NaN  (modulo by zero)

{"%": [{"var": "Value"}, 10]}
// Value=42 → 2
```

---

## * (SymbolMultiply)

**Key:** `"*"`  
**Output:** `float`

Multiplication of two numeric values.

**Shape:** `{"*": [left, right]}`

**Accepts:**
- Exactly 2 numeric operands
- Negative numbers
- Zero (multiplying by zero returns zero)

**Rejects:**
- Non-array value `{"*": "not-an-array"}` → compile failure
- Fewer than 2 arguments `{"*": [2]}` → compile failure
- More than 2 arguments `{"*": [2, 3, 4]}` → compile failure
- `TOut` that is not numeric (e.g. `bool`, `string`, array) → compile failure

**Examples:**
```json
{"*": [4, 2]}
// → 8

{"*": [6, 7]}
// → 42

{"*": [42, 0]}
// → 0  (multiply by zero)

{"*": [-3, 4]}
// → -12

{"*": [3.5, 2.0]}
// → 7.0

{"*": [{"var": "A"}, {"var": "B"}]}
// A=6, B=7 → 42
```

---

## ! (SymbolNot)

**Key:** `"!"`  
**Output:** `bool`

Logical NOT. Negates a boolean value. Supports both array syntax `{"!": [value]}` and direct syntax `{"!": value}`.

**Important constraint:** Unlike standard JSONLogic, this implementation does NOT support "truthiness" (no `"!": 0` or `"!": []`). The operand must be a boolean expression.

**Shape:** `{"!": boolExpr}` or `{"!": [boolExpr]}`

**Accepts:**
- Single boolean expression (array or direct syntax)
- Nested boolean expressions (e.g. comparisons)

**Rejects:**
- More than 1 argument `{"!": [true, false]}` → compile failure
- Non-boolean operands (e.g. numbers, arrays) → compile failure
- `TOut` other than `bool` → compile failure

**Examples:**
```json
{"!": true}
// → false

{"!": false}
// → true

{"!": [true]}
// → false  (array syntax)

{"!": {"==": [{"var": "Value"}, 0]}}
// Value=42 → true, Value=0 → false
```

---

## != (SymbolNotEquals)

**Key:** `"!="`  
**Output:** `bool`

Inequality comparison. Tests whether two values are not equal. Supports numeric and string comparisons.

**Important constraint:** Unlike standard JSONLogic, this implementation does NOT support type coercion (no `"!=": [1, "1"]`). Both operands must be the same type.

**Shape:** `{"!=": [left, right]}`

**Accepts:**
- Exactly 2 operands of the same type
- Numeric comparisons (float)
- String comparisons (case-sensitive)
- Nested expressions

**Rejects:**
- Non-array value (e.g. `{"!=": "not-an-array"}`) → compile failure
- Fewer than 2 arguments `{"!=": [1]}` → compile failure
- More than 2 arguments `{"!=": [1, 2, 3]}` → compile failure
- Mixed-type operands (e.g. numeric and `string`) → compile failure
- `TOut` other than `bool` → compile failure

**Examples:**
```json
{"!=": [1, 2]}
// → true

{"!=": [1, 1]}
// → false

{"!=": [{"var": "Value"}, 100]}
// Value=42 → true, Value=100 → false

{"!=": [{"var": "Name"}, "test"]}
// Name="other" → true, Name="test" → false
```

---

## - (SymbolSubtract)

**Key:** `"-"`  
**Output:** `float`

Subtraction of two numeric values, or negation of a single value (unary minus).

**Shape:** `{"-": [left, right]}` (binary) or `{"-": value}` (unary negation)

**Binary mode (2 operands):**
- Subtracts right from left
- Negative results allowed

**Unary mode (1 operand):**
- Negates the operand (changes sign)
- `{"-": 2}` → `-2`
- `{"-": -2}` → `2`

**Accepts:**
- Binary: exactly 2 numeric operands
- Unary: single numeric value
- Nested expressions

**Rejects:**
- Non-array, non-numeric value (e.g. `{"-": "not-an-array"}`) → compile failure
- Single-element array `{"-": [10]}` → compile failure
- More than 2 arguments `{"-": [10, 5, 2]}` → compile failure
- `TOut` that is not numeric (e.g. `bool`, `string`, array) → compile failure

**Examples:**
```json
{"-": [4, 2]}
// → 2

{"-": [2, 5]}
// → -3  (negative result)

{"-": 2}
// → -2  (unary negation)

{"-": -2}
// → 2  (negate negative)

{"-": [5.5, 3.2]}
// → 2.3

{"-": [{"var": "A"}, {"var": "B"}]}
// A=50, B=8 → 42
```

---

## var

**Key:** `"var"`  
**Output:** `TOut` (depends on property type)

Accesses data from the input context. Supports property access, nested property access (dot notation), array indexing, and default values.

**Shape:** `{"var": "propertyName"}` or `{"var": ["propertyName", defaultValue]}` or `{"var": ""}` (entire data) or `{"var": index}` (array indexing)

**Access modes:**
- **Property access:** `{"var": "A"}` — accesses property `A` from input
- **Dot notation:** `{"var": "Child.Value"}` — accesses nested property via dot notation
- **Entire data:** `{"var": ""}` — returns the entire input object
- **Array indexing:** `{"var": 1}` — accesses array element at index 1 (when input is array)
- **With default:** `{"var": ["Name", "fallback"]}` — returns `"fallback"` if `Name` is null

**Accepts:**
- String property name
- Numeric index (for array inputs)
- Array with 1-2 elements: `[propertyName]` or `[propertyName, defaultValue]`
- Empty string `""` (returns entire input)
- Dot notation for nested properties

**Rejects:**
- Empty array `{"var": []}` → compile failure
- More than 2 array elements `{"var": ["A", 0, "extra"]}` → compile failure
- Non-existent property → compile failure
- Boolean value `{"var": true}` → compile failure
- `TOut` mismatch with property type → compile failure

**Examples:**
```json
{"var": "A"}
// TData={A:42, B:100} → 42

{"var": ["B"]}
// TData={A:42, B:100} → 100  (array syntax)

{"var": "Child.Value"}
// TData={Child:{Value:123}} → 123  (dot notation)

{"var": ""}
// Returns entire input object

{"var": 1}
// TData=[10,20,30] → 20  (array indexing)

{"var": ["Name", "fallback"]}
// TData={Name:null} → "fallback"  (null returns default)
// TData={Name:"Alice"} → "Alice"
```

---

## Summary

This JsonExpression system implements a type-safe, compile-time JSONLogic variant with the following key differences from standard JSONLogic:

- **No type coercion:** Operands must be exact types (no `1 == "1"` or `0 == false`)
- **No truthiness:** Logical operators require explicit booleans (no `!1` or `and: [1, "a"]`)
- **Type safety:** `TOut` must match the expression's return type
- **Float-only numerics:** All numeric operations use `float` type exclusively
- **C# semantics:** Follows C# type rules and operator behavior (e.g., float division, IEEE 754 for NaN/Infinity)

All operators are documented above in alphabetical order by their implementation file names.

