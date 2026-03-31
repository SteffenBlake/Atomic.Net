using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// Context object for reduce operations with current element and accumulator.
/// </summary>
public sealed class ReduceContext
{
    public object? Current { get; set; }
    public object? Accumulator { get; set; }
}

/// <summary>
/// Compiles JSONLogic rules into strongly-typed Expression trees.
/// Zero-allocation recursive compilation using yoyo pattern.
/// </summary>
public static class JsonExpression
{
    /// <summary>
    /// Compiles a JSONLogic rule from a JsonDocument into a strongly-typed expression.
    /// </summary>
    /// <typeparam name="TIn">Input data type for the rule</typeparam>
    /// <typeparam name="TOut">Output result type of the rule</typeparam>
    /// <param name="rule">JSONLogic rule as JsonDocument</param>
    /// <returns>Compiled expression tree</returns>
    public static Expression<Func<TIn, TOut>> Compile<TIn, TOut>(JsonDocument rule)
    {
        var dataParam = Expression.Parameter(typeof(TIn), "data");
        var bodyExpr = CompileCore(rule.RootElement, dataParam);
        
        // Convert if necessary (only at the top level)
        if (bodyExpr.Type != typeof(TOut))
        {
            bodyExpr = Expression.Convert(bodyExpr, typeof(TOut));
        }
        
        return Expression.Lambda<Func<TIn, TOut>>(bodyExpr, dataParam);
    }

    /// <summary>
    /// Helper method for substr that handles negative indices and edge cases.
    /// </summary>
    public static string SafeSubstring(string str, int start, int length)
    {
        if (str is null)
        {
            return "";
        }

        var strLen = str.Length;

        // Handle negative start (count from end)
        if (start < 0)
        {
            start = Math.Max(0, strLen + start);
        }

        // Clamp start to string bounds
        if (start >= strLen)
        {
            return "";
        }

        // Handle negative length (stop before end)
        if (length < 0)
        {
            // Negative length means "stop |length| chars before end"
            length = Math.Max(0, strLen - start + length);
        }

        // Clamp length to available chars
        length = Math.Min(length, strLen - start);

        if (length <= 0)
        {
            return "";
        }

        return str.Substring(start, length);
    }

    /// <summary>
    /// Helper for 'all' operation - checks if all array elements satisfy the condition lambda.
    /// </summary>
    public static bool AllElements(object[]? array, Func<object?, bool> condition)
    {
        if (array is null || array.Length == 0)
        {
            return false;
        }

        foreach (var item in array)
        {
            if (!condition(item))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Helper for 'some' operation - checks if any array element satisfies the condition lambda.
    /// </summary>
    public static bool SomeElements(object[]? array, Func<object?, bool> condition)
    {
        if (array is null || array.Length == 0)
        {
            return false;
        }

        foreach (var item in array)
        {
            if (condition(item))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Helper for 'none' operation - checks if no array elements satisfy the condition lambda.
    /// </summary>
    public static bool NoneElements(object[]? array, Func<object?, bool> condition)
    {
        if (array is null || array.Length == 0)
        {
            return true;
        }

        foreach (var item in array)
        {
            if (condition(item))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Helper to box any array type to object[] for runtime polymorphism.
    /// </summary>
    public static object?[]? BoxArray(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is object?[] objArray)
        {
            return objArray;
        }

        if (value is Array array)
        {
            var result = new object?[array.Length];
            for (var i = 0; i < array.Length; i++)
            {
                result[i] = array.GetValue(i);
            }
            return result;
        }

        // Not an array, wrap as single element
        return new object?[] { value };
    }

    /// <summary>
    /// Helper for 'map' operation - transforms each array element using a lambda.
    /// </summary>
    public static object?[] MapElements(object[]? array, Func<object?, object?> transform)
    {
        if (array is null || array.Length == 0)
        {
            return Array.Empty<object?>();
        }

        var result = new object?[array.Length];
        for (var i = 0; i < array.Length; i++)
        {
            result[i] = transform(array[i]);
        }

        return result;
    }

    /// <summary>
    /// Helper for 'filter' operation - filters array elements using a condition lambda.
    /// </summary>
    public static object?[] FilterElements(object[]? array, Func<object?, bool> condition)
    {
        if (array is null || array.Length == 0)
        {
            return Array.Empty<object?>();
        }

        var resultList = new List<object?>();
        foreach (var item in array)
        {
            if (condition(item))
            {
                resultList.Add(item);
            }
        }

        return resultList.ToArray();
    }

    /// <summary>
    /// Helper for 'reduce' operation - aggregates array elements with an accumulator.
    /// </summary>
    public static object? ReduceElements(object[]? array, Func<object?, object?, object?> reducer, object? initialValue)
    {
        if (array is null || array.Length == 0)
        {
            return initialValue;
        }

        var accumulator = initialValue;
        foreach (var item in array)
        {
            accumulator = reducer(item, accumulator);
        }

        return accumulator;
    }

    /// <summary>
    /// Core recursive compilation method. Yoyo pattern: recurse inward to leaves, build outward.
    /// </summary>
    private static Expression CompileCore(JsonElement element, ParameterExpression dataParam)
    {
        // Literal values - base case (center of yoyo)
        switch (element.ValueKind)
        {
            case JsonValueKind.True:
                return Expression.Constant(true);
            
            case JsonValueKind.False:
                return Expression.Constant(false);
            
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return Expression.Constant(null, typeof(object));
            
            case JsonValueKind.Number:
                return CompileLiteralNumber(element);
            
            case JsonValueKind.String:
                return Expression.Constant(element.GetString());
            
            case JsonValueKind.Array:
                // Arrays are valid data values in JSONLogic
                // Convert to object[] runtime constant
                var arrayLength = element.GetArrayLength();
                var arrayElements = new object?[arrayLength];
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    arrayElements[index++] = item.ValueKind switch
                    {
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null or JsonValueKind.Undefined => null,
                        JsonValueKind.Number => item.GetDouble(),
                        JsonValueKind.String => item.GetString(),
                        _ => null
                    };
                }
                return Expression.Constant(arrayElements, typeof(object[]));
            
            case JsonValueKind.Object:
                return CompileOperation(element, dataParam);
            
            default:
                EventBus<ErrorEvent>.Push(new ErrorEvent($"Unsupported JSON value kind: {element.ValueKind}"));
                return Expression.Constant(null, typeof(object));
        }
    }

    /// <summary>
    /// Compile literal number to appropriate type.
    /// </summary>
    private static Expression CompileLiteralNumber(JsonElement element)
    {
        if (element.TryGetInt32(out var intVal))
        {
            return Expression.Constant(intVal);
        }
        if (element.TryGetInt64(out var longVal))
        {
            return Expression.Constant(longVal);
        }
        if (element.TryGetDouble(out var doubleVal))
        {
            return Expression.Constant(doubleVal);
        }
        
        // Fallback
        return Expression.Constant(0);
    }

    /// <summary>
    /// Helper to ensure expression is numeric, converting strings if necessary.
    /// Preserves int/long types instead of forcing to double.
    /// </summary>
    private static Expression ToNumber(Expression expr)
    {
        // Already numeric - keep as-is
        if (expr.Type == typeof(int) || expr.Type == typeof(long) || 
            expr.Type == typeof(float) || expr.Type == typeof(double))
        {
            return expr;
        }
        
        if (expr.Type == typeof(string))
        {
            // Parse string as double (JSONLogic behavior)
            var parseMethod = typeof(double).GetMethod(nameof(double.Parse), [typeof(string)])!;
            return Expression.Call(parseMethod, expr);
        }
        
        // Fallback: convert to double
        return Expression.Convert(expr, typeof(double));
    }

    /// <summary>
    /// Unifies two numeric expressions to a common type for arithmetic operations.
    /// Returns tuple of (left, right) with unified types.
    /// </summary>
    private static (Expression Left, Expression Right) UnifyNumericTypes(Expression left, Expression right)
    {
        if (left.Type == right.Type)
        {
            return (left, right);
        }

        // Determine the wider type (double > float > long > int)
        var targetType = typeof(int);
        
        if (left.Type == typeof(double) || right.Type == typeof(double))
        {
            targetType = typeof(double);
        }
        else if (left.Type == typeof(float) || right.Type == typeof(float))
        {
            targetType = typeof(float);
        }
        else if (left.Type == typeof(long) || right.Type == typeof(long))
        {
            targetType = typeof(long);
        }

        // Convert both to target type
        var leftConverted = left.Type == targetType ? left : Expression.Convert(left, targetType);
        var rightConverted = right.Type == targetType ? right : Expression.Convert(right, targetType);

        return (leftConverted, rightConverted);
    }

    /// <summary>
    /// Helper to convert any expression to bool following JSONLogic truthiness rules.
    /// Falsy: false, null, 0, "", []
    /// Truthy: everything else
    /// </summary>
    private static Expression ToBool(Expression expr)
    {
        if (expr.Type == typeof(bool))
        {
            return expr;
        }

        // For numeric types, check != 0
        if (expr.Type == typeof(int) || expr.Type == typeof(long) || expr.Type == typeof(double) || expr.Type == typeof(float))
        {
            var zero = Expression.Constant(Convert.ChangeType(0, expr.Type));
            return Expression.NotEqual(expr, zero);
        }

        // For string, check Length > 0
        if (expr.Type == typeof(string))
        {
            var lengthProp = typeof(string).GetProperty(nameof(string.Length))!;
            return Expression.GreaterThan(
                Expression.Property(expr, lengthProp),
                Expression.Constant(0)
            );
        }

        // For arrays (object[]), check Length > 0
        if (expr.Type == typeof(object[]))
        {
            var lengthProp = typeof(Array).GetProperty(nameof(Array.Length))!;
            return Expression.GreaterThan(
                Expression.Property(expr, lengthProp),
                Expression.Constant(0)
            );
        }

        // For nullable types, check HasValue && Value is truthy
        if (Nullable.GetUnderlyingType(expr.Type) is Type underlyingType)
        {
            var hasValueProp = expr.Type.GetProperty(nameof(Nullable<int>.HasValue))!;
            var valueProp = expr.Type.GetProperty(nameof(Nullable<int>.Value))!;
            return Expression.AndAlso(
                Expression.Property(expr, hasValueProp),
                ToBool(Expression.Property(expr, valueProp))
            );
        }

        // For objects, check != null
        if (!expr.Type.IsValueType)
        {
            return Expression.NotEqual(expr, Expression.Constant(null, expr.Type));
        }

        // Value types are always truthy (except 0 which is handled above)
        return Expression.Constant(true);
    }

    /// <summary>
    /// Compile JSONLogic operation (object with single property = operator).
    /// Strategy pattern - dispatch to operation-specific handler.
    /// </summary>
    private static Expression CompileOperation(JsonElement element, ParameterExpression dataParam)
    {
        // JSONLogic operations are objects with a single property
        var enumerator = element.EnumerateObject();
        if (!enumerator.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("Empty object in JSONLogic rule"));
            return Expression.Constant(null);
        }

        var property = enumerator.Current;
        var op = property.Name.AsSpan();
        var args = property.Value;

        // Check for multiple properties (invalid)
        if (enumerator.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("JSONLogic operations must have exactly one property"));
            return Expression.Constant(null);
        }

        // Dispatch to operation-specific compiler (state machine pattern)
        // Data access operations
        if (op.SequenceEqual("var".AsSpan()))
        {
            return CompileVar(args, dataParam);
        }
        if (op.SequenceEqual("missing".AsSpan()))
        {
            return CompileMissing(args, dataParam);
        }
        if (op.SequenceEqual("missing_some".AsSpan()))
        {
            return CompileMissingSome(args, dataParam);
        }

        // Arithmetic operations
        if (op.SequenceEqual("+".AsSpan()))
        {
            return CompileAdd(args, dataParam);
        }
        if (op.SequenceEqual("-".AsSpan()))
        {
            return CompileSubtract(args, dataParam);
        }
        if (op.SequenceEqual("*".AsSpan()))
        {
            return CompileMultiply(args, dataParam);
        }
        if (op.SequenceEqual("/".AsSpan()))
        {
            return CompileDivide(args, dataParam);
        }
        if (op.SequenceEqual("%".AsSpan()))
        {
            return CompileModulo(args, dataParam);
        }

        // Comparison operations
        if (op.SequenceEqual(">".AsSpan()))
        {
            return CompileGreaterThan(args, dataParam);
        }
        if (op.SequenceEqual(">=".AsSpan()))
        {
            return CompileGreaterThanOrEqual(args, dataParam);
        }
        if (op.SequenceEqual("<".AsSpan()))
        {
            return CompileLessThan(args, dataParam);
        }
        if (op.SequenceEqual("<=".AsSpan()))
        {
            return CompileLessThanOrEqual(args, dataParam);
        }

        // Logical operations
        if (op.SequenceEqual("!".AsSpan()))
        {
            return CompileNot(args, dataParam);
        }
        if (op.SequenceEqual("!!".AsSpan()))
        {
            return CompileDoubleBang(args, dataParam);
        }
        if (op.SequenceEqual("and".AsSpan()))
        {
            return CompileAnd(args, dataParam);
        }
        if (op.SequenceEqual("or".AsSpan()))
        {
            return CompileOr(args, dataParam);
        }
        if (op.SequenceEqual("if".AsSpan()))
        {
            return CompileIf(args, dataParam);
        }

        // Equality operations
        if (op.SequenceEqual("==".AsSpan()))
        {
            return CompileEquals(args, dataParam);
        }
        if (op.SequenceEqual("===".AsSpan()))
        {
            return CompileStrictEquals(args, dataParam);
        }
        if (op.SequenceEqual("!=".AsSpan()))
        {
            return CompileNotEquals(args, dataParam);
        }
        if (op.SequenceEqual("!==".AsSpan()))
        {
            return CompileStrictNotEquals(args, dataParam);
        }

        // String operations
        if (op.SequenceEqual("cat".AsSpan()))
        {
            return CompileCat(args, dataParam);
        }
        if (op.SequenceEqual("substr".AsSpan()))
        {
            return CompileSubstr(args, dataParam);
        }

        // Array operations
        if (op.SequenceEqual("in".AsSpan()))
        {
            return CompileIn(args, dataParam);
        }
        if (op.SequenceEqual("merge".AsSpan()))
        {
            return CompileMerge(args, dataParam);
        }

        // Aggregate operations
        if (op.SequenceEqual("max".AsSpan()))
        {
            return CompileMax(args, dataParam);
        }
        if (op.SequenceEqual("min".AsSpan()))
        {
            return CompileMin(args, dataParam);
        }

        // Array test operations
        if (op.SequenceEqual("map".AsSpan()))
        {
            return CompileMap(args, dataParam);
        }
        if (op.SequenceEqual("filter".AsSpan()))
        {
            return CompileFilter(args, dataParam);
        }
        if (op.SequenceEqual("reduce".AsSpan()))
        {
            return CompileReduce(args, dataParam);
        }
        if (op.SequenceEqual("all".AsSpan()))
        {
            return CompileAll(args, dataParam);
        }
        if (op.SequenceEqual("none".AsSpan()))
        {
            return CompileNone(args, dataParam);
        }
        if (op.SequenceEqual("some".AsSpan()))
        {
            return CompileSome(args, dataParam);
        }

        // Miscellaneous operations
        if (op.SequenceEqual("log".AsSpan()))
        {
            return CompileLog(args, dataParam);
        }

        // Unknown operation
        EventBus<ErrorEvent>.Push(new ErrorEvent($"Unknown JSONLogic operator: {property.Name}"));
        return Expression.Constant(null);
    }

    // ===========================
    // OPERATION COMPILERS (STUBS)
    // ===========================

    private static Expression CompileVar(JsonElement args, ParameterExpression dataParam)
    {
        // var can be: string, array with string, or number (for array index)
        string? propertyPath;
        Expression? defaultValueExpr = null;

        if (args.ValueKind == JsonValueKind.String)
        {
            propertyPath = args.GetString();
        }
        else if (args.ValueKind == JsonValueKind.Number)
        {
            // Array index access - not implemented yet
            EventBus<ErrorEvent>.Push(new ErrorEvent("var with numeric index not yet implemented"));
            return Expression.Constant(null);
        }
        else if (args.ValueKind == JsonValueKind.Array)
        {
            var arrayEnum = args.EnumerateArray();
            if (!arrayEnum.MoveNext())
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent("var requires at least one argument"));
                return Expression.Constant(null);
            }

            var firstArg = arrayEnum.Current;
            if (firstArg.ValueKind == JsonValueKind.String)
            {
                propertyPath = firstArg.GetString();
            }
            else
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent("var first argument must be string"));
                return Expression.Constant(null);
            }

            // Second argument is default value
            if (arrayEnum.MoveNext())
            {
                defaultValueExpr = CompileCore(arrayEnum.Current, dataParam);
            }
        }
        else
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"var argument must be string or array, got {args.ValueKind}"));
            return Expression.Constant(null, typeof(object));
        }

        // Empty string means return entire data object
        if (string.IsNullOrEmpty(propertyPath))
        {
            return dataParam;
        }

        // Simple property access (no dots)
        if (!propertyPath.Contains('.'))
        {
            var prop = dataParam.Type.GetProperty(propertyPath, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop is null)
            {
                // Property not found - return default value or null (not an error, just missing data)
                return defaultValueExpr ?? Expression.Constant(null, typeof(object));
            }

            // Return property as-is without conversion
            return Expression.Property(dataParam, prop);
        }

        // Nested property access (with dots) - not implemented yet, return default/null
        return defaultValueExpr ?? Expression.Constant(null, typeof(object));
    }

    private static Expression CompileMissing(JsonElement args, ParameterExpression dataParam)
    {
        // missing returns an array of keys that are not present in the data
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("missing requires array of property names"));
            return Expression.Constant(Array.Empty<string>(), typeof(string[]));
        }

        var propertyNames = new List<string>();
        foreach (var item in args.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                propertyNames.Add(item.GetString()!);
            }
        }

        if (propertyNames.Count == 0)
        {
            return Expression.Constant(Array.Empty<string>(), typeof(string[]));
        }

        // Build runtime check expressions for each property
        var missingChecks = new List<Expression>();
        var resultList = Expression.Variable(typeof(List<string>), "missingList");
        var statements = new List<Expression>
        {
            Expression.Assign(resultList, Expression.New(typeof(List<string>)))
        };

        foreach (var propName in propertyNames)
        {
            var prop = dataParam.Type.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop is null)
            {
                // Property doesn't exist at compile time - add to missing list
                statements.Add(
                    Expression.Call(resultList, typeof(List<string>).GetMethod("Add")!, Expression.Constant(propName))
                );
            }
            // If property exists, we don't add it to missing list
        }

        // Return array
        var toArrayMethod = typeof(List<string>).GetMethod("ToArray")!;
        statements.Add(Expression.Call(resultList, toArrayMethod));

        return Expression.Block(new[] { resultList }, statements);
    }

    private static Expression CompileMissingSome(JsonElement args, ParameterExpression dataParam)
    {
        // missing_some returns array of missing keys if less than minimum are present
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("missing_some requires array of arguments"));
            return Expression.Constant(Array.Empty<string>(), typeof(string[]));
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("missing_some requires minimum count and property names"));
            return Expression.Constant(Array.Empty<string>(), typeof(string[]));
        }

        // First arg is minimum count required
        var minimumCount = 0;
        if (arrayEnum.Current.ValueKind == JsonValueKind.Number)
        {
            minimumCount = arrayEnum.Current.GetInt32();
        }

        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("missing_some requires array of property names"));
            return Expression.Constant(Array.Empty<string>(), typeof(string[]));
        }

        // Second arg is array of property names to check
        var propertyNamesElement = arrayEnum.Current;
        if (propertyNamesElement.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("missing_some second argument must be array"));
            return Expression.Constant(Array.Empty<string>(), typeof(string[]));
        }

        var propertyNames = new List<string>();
        foreach (var item in propertyNamesElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                propertyNames.Add(item.GetString()!);
            }
        }

        if (propertyNames.Count == 0)
        {
            return Expression.Constant(Array.Empty<string>(), typeof(string[]));
        }

        // Check which properties are present at compile time
        var missingList = new List<string>();
        var presentCount = 0;
        foreach (var propName in propertyNames)
        {
            var prop = dataParam.Type.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop is null)
            {
                missingList.Add(propName);
            }
            else
            {
                presentCount++;
            }
        }

        // If we have enough present, return empty array; otherwise return missing keys
        if (presentCount >= minimumCount)
        {
            return Expression.Constant(Array.Empty<string>(), typeof(string[]));
        }

        return Expression.Constant(missingList.ToArray(), typeof(string[]));
    }

    private static Expression CompileAdd(JsonElement args, ParameterExpression dataParam)
    {
        // + with 1 arg: cast to number (unary +)
        // + with 2+ args: sum all arguments
        if (args.ValueKind != JsonValueKind.Array)
        {
            // Unary + (cast to number)
            var expr = CompileCore(args, dataParam);
            return ToNumber(expr);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            return Expression.Constant(0);
        }

        var first = CompileCore(arrayEnum.Current, dataParam);
        first = ToNumber(first);

        Expression result = first;
        while (arrayEnum.MoveNext())
        {
            var next = CompileCore(arrayEnum.Current, dataParam);
            next = ToNumber(next);
            result = Expression.Add(result, next);
        }

        return result;
    }

    private static Expression CompileSubtract(JsonElement args, ParameterExpression dataParam)
    {
        // - with 1 arg: negate
        // - with 2+ args: subtract all from first
        if (args.ValueKind != JsonValueKind.Array)
        {
            // Unary - (negate)
            var expr = CompileCore(args, dataParam);
            expr = ToNumber(expr);
            return Expression.Negate(expr);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            return Expression.Constant(0.0);
        }

        var first = CompileCore(arrayEnum.Current, dataParam);
        first = ToNumber(first);

        // Single argument - negate
        if (!arrayEnum.MoveNext())
        {
            return Expression.Negate(first);
        }

        Expression result = first;
        do
        {
            var next = CompileCore(arrayEnum.Current, dataParam);
            next = ToNumber(next);
            result = Expression.Subtract(result, next);
        } while (arrayEnum.MoveNext());

        return result;
    }

    private static Expression CompileMultiply(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("* requires array of arguments"));
            return Expression.Constant(0);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            return Expression.Constant(1);
        }

        var first = CompileCore(arrayEnum.Current, dataParam);
        first = ToNumber(first);

        Expression result = first;
        while (arrayEnum.MoveNext())
        {
            var next = CompileCore(arrayEnum.Current, dataParam);
            next = ToNumber(next);
            
            // Unify types before multiplication
            (result, next) = UnifyNumericTypes(result, next);
            result = Expression.Multiply(result, next);
        }

        return result;
    }

    private static Expression CompileDivide(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("/ requires array of arguments"));
            return Expression.Constant(null);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("/ requires at least one argument"));
            return Expression.Constant(null);
        }

        var first = CompileCore(arrayEnum.Current, dataParam);
        first = ToNumber(first);

        Expression result = first;
        while (arrayEnum.MoveNext())
        {
            var next = CompileCore(arrayEnum.Current, dataParam);
            next = ToNumber(next);
            result = Expression.Divide(result, next);
        }

        return result;
    }

    private static Expression CompileModulo(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("% requires array of arguments"));
            return Expression.Constant(null);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("% requires at least 2 arguments"));
            return Expression.Constant(null);
        }

        var first = CompileCore(arrayEnum.Current, dataParam);
        first = ToNumber(first);

        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("% requires at least 2 arguments"));
            return Expression.Constant(null);
        }

        var second = CompileCore(arrayEnum.Current, dataParam);
        second = ToNumber(second);

        return Expression.Modulo(first, second);
    }

    private static Expression CompileGreaterThan(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("> requires array of arguments"));
            return Expression.Constant(false);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("> requires at least 2 arguments"));
            return Expression.Constant(false);
        }

        var first = CompileCore(arrayEnum.Current, dataParam);
        first = ToNumber(first);

        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("> requires at least 2 arguments"));
            return Expression.Constant(false);
        }

        var second = CompileCore(arrayEnum.Current, dataParam);
        second = ToNumber(second);

        // Special case: if 3rd argument exists, check if first is between second and third
        if (arrayEnum.MoveNext())
        {
            var third = CompileCore(arrayEnum.Current, dataParam);
            third = ToNumber(third);
            // first > second && first < third
            return Expression.AndAlso(
                Expression.GreaterThan(first, second),
                Expression.LessThan(first, third)
            );
        }

        return Expression.GreaterThan(first, second);
    }

    private static Expression CompileGreaterThanOrEqual(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(">= requires array of arguments"));
            return Expression.Constant(false);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(">= requires at least 2 arguments"));
            return Expression.Constant(false);
        }

        var first = CompileCore(arrayEnum.Current, dataParam);
        first = ToNumber(first);

        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(">= requires at least 2 arguments"));
            return Expression.Constant(false);
        }

        var second = CompileCore(arrayEnum.Current, dataParam);
        second = ToNumber(second);

        // Special case: if 3rd argument exists, check if first is between second and third (inclusive)
        if (arrayEnum.MoveNext())
        {
            var third = CompileCore(arrayEnum.Current, dataParam);
            third = ToNumber(third);
            // first >= second && first <= third
            return Expression.AndAlso(
                Expression.GreaterThanOrEqual(first, second),
                Expression.LessThanOrEqual(first, third)
            );
        }

        return Expression.GreaterThanOrEqual(first, second);
    }

    private static Expression CompileLessThan(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("< requires array of arguments"));
            return Expression.Constant(false);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("< requires at least 2 arguments"));
            return Expression.Constant(false);
        }

        var first = CompileCore(arrayEnum.Current, dataParam);
        first = ToNumber(first);

        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("< requires at least 2 arguments"));
            return Expression.Constant(false);
        }

        var second = CompileCore(arrayEnum.Current, dataParam);
        second = ToNumber(second);

        // Special case: if 3rd argument exists, check if second < first < third
        if (arrayEnum.MoveNext())
        {
            var third = CompileCore(arrayEnum.Current, dataParam);
            third = ToNumber(third);
            // first > second && first < third (i.e., second < first < third)
            return Expression.AndAlso(
                Expression.GreaterThan(first, second),
                Expression.LessThan(first, third)
            );
        }

        return Expression.LessThan(first, second);
    }

    private static Expression CompileLessThanOrEqual(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("<= requires array of arguments"));
            return Expression.Constant(false);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("<= requires at least 2 arguments"));
            return Expression.Constant(false);
        }

        var first = CompileCore(arrayEnum.Current, dataParam);
        first = ToNumber(first);

        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("<= requires at least 2 arguments"));
            return Expression.Constant(false);
        }

        var second = CompileCore(arrayEnum.Current, dataParam);
        second = ToNumber(second);

        // Special case: if 3rd argument exists, check if second <= first <= third
        if (arrayEnum.MoveNext())
        {
            var third = CompileCore(arrayEnum.Current, dataParam);
            third = ToNumber(third);
            // first >= second && first <= third (i.e., second <= first <= third)
            return Expression.AndAlso(
                Expression.GreaterThanOrEqual(first, second),
                Expression.LessThanOrEqual(first, third)
            );
        }

        return Expression.LessThanOrEqual(first, second);
    }

    private static Expression CompileNot(JsonElement args, ParameterExpression dataParam)
    {
        // ! operator negates truthy value
        // Handle array argument (extract first element)
        var arg = args.ValueKind == JsonValueKind.Array && args.GetArrayLength() > 0
            ? args.EnumerateArray().First()
            : args;
        
        var expr = CompileCore(arg, dataParam);
        expr = ToBool(expr);
        return Expression.Not(expr);
    }

    private static Expression CompileDoubleBang(JsonElement args, ParameterExpression dataParam)
    {
        // !! operator converts to boolean
        // Handle array argument (extract first element)
        var arg = args.ValueKind == JsonValueKind.Array && args.GetArrayLength() > 0
            ? args.EnumerateArray().First()
            : args;
        
        var expr = CompileCore(arg, dataParam);
        return ToBool(expr);
    }

    private static Expression CompileAnd(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("and requires array of arguments"));
            return Expression.Constant(false);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            return Expression.Constant(false);
        }

        // And returns first falsy value, or last value if all truthy
        // Need to evaluate each arg and return the value (not just bool)
        var first = CompileCore(arrayEnum.Current, dataParam);
        Expression result = first;
        
        while (arrayEnum.MoveNext())
        {
            var next = CompileCore(arrayEnum.Current, dataParam);
            // Ensure both result and next are object type for Expression.Condition
            var resultAsObj = result.Type == typeof(object) ? result : Expression.Convert(result, typeof(object));
            var nextAsObj = next.Type == typeof(object) ? next : Expression.Convert(next, typeof(object));
            // If current result is falsy, return it; else continue to next
            result = Expression.Condition(
                ToBool(result),
                nextAsObj,      // result is truthy, continue to next
                resultAsObj     // result is falsy, short-circuit and return it
            );
        }

        return result;
    }

    private static Expression CompileOr(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("or requires array of arguments"));
            return Expression.Constant(false);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            return Expression.Constant(false);
        }

        // Or returns first truthy value, or last value if all falsy
        // Need to evaluate each arg and return the value (not just bool)
        var first = CompileCore(arrayEnum.Current, dataParam);
        Expression result = first;
        
        while (arrayEnum.MoveNext())
        {
            var next = CompileCore(arrayEnum.Current, dataParam);
            // Ensure both result and next are object type for Expression.Condition
            var resultAsObj = result.Type == typeof(object) ? result : Expression.Convert(result, typeof(object));
            var nextAsObj = next.Type == typeof(object) ? next : Expression.Convert(next, typeof(object));
            // If current result is truthy, return it; else continue to next
            result = Expression.Condition(
                ToBool(result),
                resultAsObj,    // result is truthy, short-circuit and return it
                nextAsObj       // result is falsy, continue to next
            );
        }

        return result;
    }

    private static Expression CompileIf(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("if requires array of arguments"));
            return Expression.Constant(null, typeof(object));
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("if requires at least one argument"));
            return Expression.Constant(null, typeof(object));
        }

        // Build nested if-then-else chain
        // Pattern: if(condition, then) or if(condition, then, else) or if(cond1, then1, cond2, then2, ..., else)
        var firstCondition = CompileCore(arrayEnum.Current, dataParam);
        var conditionBool = ToBool(firstCondition);

        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("if requires at least 2 arguments (condition and then)"));
            return Expression.Constant(null, typeof(object));
        }

        var thenBranch = CompileCore(arrayEnum.Current, dataParam);

        // Check if there's an else branch or more condition pairs
        if (!arrayEnum.MoveNext())
        {
            // No else branch - need to create null with matching type
            var nullElse = Expression.Constant(null, typeof(object));
            if (thenBranch.Type != typeof(object))
            {
                // Unify types - convert then to object
                thenBranch = Expression.Convert(thenBranch, typeof(object));
            }
            return Expression.Condition(conditionBool, thenBranch, nullElse);
        }

        // Recursively compile remaining args as else-if chain
        var elseArgs = new List<JsonElement>();
        elseArgs.Add(arrayEnum.Current);
        while (arrayEnum.MoveNext())
        {
            elseArgs.Add(arrayEnum.Current);
        }

        Expression elseBranch;
        if (elseArgs.Count == 1)
        {
            // Single else value
            elseBranch = CompileCore(elseArgs[0], dataParam);
        }
        else
        {
            // Multiple args - treat as nested if-then-else
            // Reconstruct as JSON array for recursive call
            var elseArgsJson = JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(elseArgs)).RootElement;
            elseBranch = CompileIf(elseArgsJson, dataParam);
        }

        // Unify types between then and else branches
        if (thenBranch.Type != elseBranch.Type)
        {
            // Convert both to object for compatibility
            if (thenBranch.Type != typeof(object))
            {
                thenBranch = Expression.Convert(thenBranch, typeof(object));
            }
            if (elseBranch.Type != typeof(object))
            {
                elseBranch = Expression.Convert(elseBranch, typeof(object));
            }
        }

        return Expression.Condition(conditionBool, thenBranch, elseBranch);
    }

    private static Expression CompileEquals(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("== requires array of arguments"));
            return Expression.Constant(false);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("== requires at least 2 arguments"));
            return Expression.Constant(false);
        }

        var first = CompileCore(arrayEnum.Current, dataParam);
        
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("== requires at least 2 arguments"));
            return Expression.Constant(false);
        }

        var second = CompileCore(arrayEnum.Current, dataParam);

        // Loose equality: convert both to strings for type-coerced comparison
        var firstStr = first.Type == typeof(string) ? first : Expression.Call(first, typeof(object).GetMethod(nameof(ToString))!);
        var secondStr = second.Type == typeof(string) ? second : Expression.Call(second, typeof(object).GetMethod(nameof(ToString))!);
        return Expression.Equal(firstStr, secondStr);
    }

    private static Expression CompileStrictEquals(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("=== requires array of arguments"));
            return Expression.Constant(false);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("=== requires at least 2 arguments"));
            return Expression.Constant(false);
        }

        var first = CompileCore(arrayEnum.Current, dataParam);
        
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("=== requires at least 2 arguments"));
            return Expression.Constant(false);
        }

        var second = CompileCore(arrayEnum.Current, dataParam);

        // Strict equality: types must match
        return Expression.Equal(first, second);
    }

    private static Expression CompileNotEquals(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("!= requires array of arguments"));
            return Expression.Constant(true);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("!= requires at least 2 arguments"));
            return Expression.Constant(true);
        }

        var first = CompileCore(arrayEnum.Current, dataParam);
        
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("!= requires at least 2 arguments"));
            return Expression.Constant(true);
        }

        var second = CompileCore(arrayEnum.Current, dataParam);

        // Loose inequality: convert both to strings for type-coerced comparison
        var firstStr = first.Type == typeof(string) ? first : Expression.Call(first, typeof(object).GetMethod(nameof(ToString))!);
        var secondStr = second.Type == typeof(string) ? second : Expression.Call(second, typeof(object).GetMethod(nameof(ToString))!);
        return Expression.NotEqual(firstStr, secondStr);
    }

    private static Expression CompileStrictNotEquals(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("!== requires array of arguments"));
            return Expression.Constant(true);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("!== requires at least 2 arguments"));
            return Expression.Constant(true);
        }

        var first = CompileCore(arrayEnum.Current, dataParam);
        
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("!== requires at least 2 arguments"));
            return Expression.Constant(true);
        }

        var second = CompileCore(arrayEnum.Current, dataParam);

        return Expression.NotEqual(first, second);
    }

    private static Expression CompileCat(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            // Single argument - convert to string
            var expr = CompileCore(args, dataParam);
            if (expr.Type != typeof(string))
            {
                expr = Expression.Call(expr, typeof(object).GetMethod(nameof(ToString))!);
            }
            return expr;
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            return Expression.Constant("");
        }

        // Build string concatenation
        var stringConcatMethod = typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string)])!;
        
        var first = CompileCore(arrayEnum.Current, dataParam);
        if (first.Type != typeof(string))
        {
            first = Expression.Call(first, typeof(object).GetMethod(nameof(ToString))!);
        }

        Expression result = first;
        while (arrayEnum.MoveNext())
        {
            var next = CompileCore(arrayEnum.Current, dataParam);
            if (next.Type != typeof(string))
            {
                next = Expression.Call(next, typeof(object).GetMethod(nameof(ToString))!);
            }
            result = Expression.Call(stringConcatMethod, result, next);
        }

        return result;
    }

    private static Expression CompileSubstr(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("substr requires array of arguments"));
            return Expression.Constant("");
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("substr requires at least 1 argument (string)"));
            return Expression.Constant("");
        }

        var str = CompileCore(arrayEnum.Current, dataParam);
        if (str.Type != typeof(string))
        {
            str = Expression.Call(str, typeof(object).GetMethod(nameof(ToString))!);
        }

        if (!arrayEnum.MoveNext())
        {
            // Only string provided - return as-is
            return str;
        }

        var start = CompileCore(arrayEnum.Current, dataParam);
        if (start.Type != typeof(int))
        {
            start = Expression.Convert(ToNumber(start), typeof(int));
        }

        if (!arrayEnum.MoveNext())
        {
            // String and start - use SafeSubstring with length = string.Length
            var safeSubstringMethod = typeof(JsonExpression).GetMethod(nameof(SafeSubstring))!;
            var lengthProp = typeof(string).GetProperty(nameof(string.Length))!;
            return Expression.Call(safeSubstringMethod, str, start, Expression.Property(str, lengthProp));
        }

        var length = CompileCore(arrayEnum.Current, dataParam);
        if (length.Type != typeof(int))
        {
            length = Expression.Convert(ToNumber(length), typeof(int));
        }

        // String, start, and length - use SafeSubstring
        var safeSubstringMethod2 = typeof(JsonExpression).GetMethod(nameof(SafeSubstring))!;
        return Expression.Call(safeSubstringMethod2, str, start, length);
    }

    private static Expression CompileIn(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("in requires array of arguments"));
            return Expression.Constant(false);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("in requires at least 2 arguments"));
            return Expression.Constant(false);
        }

        var needle = CompileCore(arrayEnum.Current, dataParam);

        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("in requires at least 2 arguments"));
            return Expression.Constant(false);
        }

        var haystack = CompileCore(arrayEnum.Current, dataParam);

        // Check if haystack is an array (array contains) or string (string contains)
        // Use runtime type check with Expression.TypeIs
        var isArrayCheck = Expression.TypeIs(haystack, typeof(object[]));
        
        // Array contains branch using Array.IndexOf - only convert if it's actually an array
        var arrayIndexOfMethod = typeof(Array).GetMethod(nameof(Array.IndexOf), [typeof(Array), typeof(object)])!;
        var haystackAsArray = Expression.TypeAs(haystack, typeof(object[]));
        var indexOf = Expression.Call(arrayIndexOfMethod, haystackAsArray, needle);
        var arrayContains = Expression.GreaterThanOrEqual(indexOf, Expression.Constant(0));

        // String contains branch - convert both to strings
        var needleStr = needle.Type == typeof(string) ? needle : Expression.Call(needle, typeof(object).GetMethod(nameof(ToString))!);
        var haystackStr = haystack.Type == typeof(string) ? haystack : Expression.Call(haystack, typeof(object).GetMethod(nameof(ToString))!);
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        var stringContains = Expression.Call(haystackStr, containsMethod, needleStr);

        // Return conditional: if haystack is array, use array contains, else use string contains
        return Expression.Condition(isArrayCheck, arrayContains, stringContains);
    }

    private static Expression CompileMerge(JsonElement args, ParameterExpression dataParam)
    {
        // merge combines multiple arrays into one
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("merge requires array of arguments"));
            return Expression.Constant(Array.Empty<object>(), typeof(object[]));
        }

        var arrays = new List<Expression>();
        
        foreach (var item in args.EnumerateArray())
        {
            var itemExpr = CompileCore(item, dataParam);
            arrays.Add(itemExpr);
        }

        if (arrays.Count == 0)
        {
            return Expression.Constant(Array.Empty<object>(), typeof(object[]));
        }

        if (arrays.Count == 1)
        {
            // Single element - if it's an array return it, otherwise wrap it
            var singleExpr = arrays[0];
            if (singleExpr.Type.IsArray)
            {
                return singleExpr;
            }
            return Expression.NewArrayInit(typeof(object), Expression.Convert(singleExpr, typeof(object)));
        }

        // Multiple arrays - merge them all as object[] (common base type)
        var statements = new List<Expression>();
        var arrayVars = new List<ParameterExpression>();
        var totalLengthVar = Expression.Variable(typeof(int), "totalLength");
        var resultVar = Expression.Variable(typeof(object[]), "result");
        var indexVar = Expression.Variable(typeof(int), "index");

        Expression lengthSum = Expression.Constant(0);
        
        // For each array argument, convert to object[] and track length
        for (var i = 0; i < arrays.Count; i++)
        {
            var arrVar = Expression.Variable(typeof(object[]), $"arr{i}");
            arrayVars.Add(arrVar);
            
            var arrayExpr = arrays[i];
            if (arrayExpr.Type.IsArray)
            {
                // Convert typed array to object[]
                var elemType = arrayExpr.Type.GetElementType()!;
                var convertAllMethod = typeof(Array).GetMethod("ConvertAll")!.MakeGenericMethod(elemType, typeof(object));
                var xParam = Expression.Parameter(elemType, "x");
                var converterType = typeof(Converter<,>).MakeGenericType(elemType, typeof(object));
                var converter = Expression.Lambda(
                    converterType,
                    Expression.Convert(xParam, typeof(object)),
                    xParam
                ).Compile();
                statements.Add(Expression.Assign(arrVar, Expression.Call(convertAllMethod, arrayExpr, Expression.Constant(converter, converterType))));
            }
            else
            {
                // Not an array - wrap as single element
                statements.Add(Expression.Assign(arrVar, Expression.NewArrayInit(typeof(object), Expression.Convert(arrayExpr, typeof(object)))));
            }
            
            lengthSum = Expression.Add(lengthSum, Expression.Property(arrVar, "Length"));
        }

        statements.Add(Expression.Assign(totalLengthVar, lengthSum));
        statements.Add(Expression.Assign(resultVar, Expression.NewArrayBounds(typeof(object), totalLengthVar)));
        statements.Add(Expression.Assign(indexVar, Expression.Constant(0)));

        // Copy each array using Array.Copy
        var arrayCopyMethod = typeof(Array).GetMethod("Copy", new[] { typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int) })!;
        foreach (var arrVar in arrayVars)
        {
            statements.Add(
                Expression.Call(
                    arrayCopyMethod,
                    arrVar,
                    Expression.Constant(0),
                    resultVar,
                    indexVar,
                    Expression.Property(arrVar, "Length")
                )
            );
            statements.Add(Expression.AddAssign(indexVar, Expression.Property(arrVar, "Length")));
        }

        statements.Add(resultVar);

        var allVars = new List<ParameterExpression> { totalLengthVar, resultVar, indexVar };
        allVars.AddRange(arrayVars);

        return Expression.Block(allVars, statements);
    }

    private static Expression CompileMax(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("max requires array of arguments"));
            return Expression.Constant(0.0);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("max requires at least one argument"));
            return Expression.Constant(0.0);
        }

        var first = CompileCore(arrayEnum.Current, dataParam);
        first = ToNumber(first);

        Expression result = first;
        while (arrayEnum.MoveNext())
        {
            var next = CompileCore(arrayEnum.Current, dataParam);
            next = ToNumber(next);
            // Math.Max(result, next)
            result = Expression.Call(
                typeof(Math).GetMethod(nameof(Math.Max), [typeof(double), typeof(double)])!,
                result,
                next
            );
        }

        return result;
    }

    private static Expression CompileMin(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("min requires array of arguments"));
            return Expression.Constant(0.0);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("min requires at least one argument"));
            return Expression.Constant(0.0);
        }

        var first = CompileCore(arrayEnum.Current, dataParam);
        first = ToNumber(first);

        Expression result = first;
        while (arrayEnum.MoveNext())
        {
            var next = CompileCore(arrayEnum.Current, dataParam);
            next = ToNumber(next);
            // Math.Min(result, next)
            result = Expression.Call(
                typeof(Math).GetMethod(nameof(Math.Min), [typeof(double), typeof(double)])!,
                result,
                next
            );
        }

        return result;
    }

    private static Expression CompileMap(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("map requires array of arguments"));
            return Expression.Constant(Array.Empty<object>(), typeof(object[]));
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("map requires at least 2 arguments (array and transform)"));
            return Expression.Constant(Array.Empty<object>(), typeof(object[]));
        }

        // Get the array expression - try to preserve actual array type
        // First compile without type constraint to see actual type
        var arrayExprTest = CompileCore(arrayEnum.Current, dataParam);
        
        Expression arrayExpr;
        Type arrayType;
        
        // If result was boxed to object, we need to handle it specially
        if (arrayExprTest.Type == typeof(object))
        {
            // Try to get array type through reflection on the property/var access
            // For now, treat as object[] - will need runtime type checking
            arrayExpr = arrayExprTest;
            arrayType = typeof(object[]);
        }
        else if (arrayExprTest.Type.IsArray)
        {
            arrayExpr = arrayExprTest;
            arrayType = arrayExprTest.Type;
        }
        else
        {
            // Treat non-array as single-element array
            arrayExpr = Expression.NewArrayInit(arrayExprTest.Type, arrayExprTest);
            arrayType = arrayExpr.Type;
        }
        
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("map requires at least 2 arguments (array and transform)"));
            return Expression.Constant(Array.Empty<object>(), typeof(object[]));
        }

        var elementType = arrayType.GetElementType()!;
        var transformElement = arrayEnum.Current;
        var transformJson = JsonDocument.Parse(transformElement.GetRawText());
        
        // Compile transform with element as parameter, let it infer actual return type
        var elementParam = Expression.Parameter(elementType, "element");
        var transformBody = CompileCore(transformJson.RootElement, elementParam);
        
        // Output array type is based on what transform actually returns
        var outputElementType = transformBody.Type;
        var outputArrayType = outputElementType.MakeArrayType();
        
        // Build inline map loop
        var resultVar = Expression.Variable(outputArrayType, "result");
        var lengthExpr = Expression.ArrayLength(arrayExpr);
        var indexVar = Expression.Variable(typeof(int), "i");
        var breakLabel = Expression.Label("break");
        
        var loop = Expression.Block(
            new[] { resultVar, indexVar },
            Expression.Assign(resultVar, Expression.NewArrayBounds(outputElementType, lengthExpr)),
            Expression.Assign(indexVar, Expression.Constant(0)),
            Expression.Loop(
                Expression.IfThenElse(
                    Expression.LessThan(indexVar, lengthExpr),
                    Expression.Block(
                        Expression.Assign(
                            Expression.ArrayAccess(resultVar, indexVar),
                            Expression.Invoke(
                                Expression.Lambda(transformBody, elementParam),
                                Expression.ArrayIndex(arrayExpr, indexVar)
                            )
                        ),
                        Expression.PostIncrementAssign(indexVar)
                    ),
                    Expression.Break(breakLabel)
                ),
                breakLabel
            ),
            resultVar
        );
        
        return loop;
    }

    private static Expression CompileFilter(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("filter requires array of arguments"));
            return Expression.Constant(Array.Empty<object>(), typeof(object[]));
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("filter requires at least 2 arguments (array and condition)"));
            return Expression.Constant(Array.Empty<object>(), typeof(object[]));
        }

        // Get the array expression with its actual type
        var arrayExpr = CompileCore(arrayEnum.Current, dataParam);

        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("filter requires at least 2 arguments (array and condition)"));
            return Expression.Constant(Array.Empty<object>(), typeof(object[]));
        }

        var arrayType = arrayExpr.Type;
        if (!arrayType.IsArray)
        {
            return Expression.Constant(Array.Empty<object>(), typeof(object[]));
        }

        var elementType = arrayType.GetElementType()!;
        var conditionElement = arrayEnum.Current;
        var conditionJson = JsonDocument.Parse(conditionElement.GetRawText());
        
        // Compile condition with actual element type
        var elementParam = Expression.Parameter(elementType, "element");
        var conditionBody = CompileCore(conditionJson.RootElement, elementParam);
        var conditionBodyAsBool = ToBool(conditionBody);
        
        // Build inline filter using List<T>
        var listType = typeof(List<>).MakeGenericType(elementType);
        var listVar = Expression.Variable(listType, "result");
        var lengthExpr = Expression.ArrayLength(arrayExpr);
        var indexVar = Expression.Variable(typeof(int), "i");
        var breakLabel = Expression.Label("break");
        var addMethod = listType.GetMethod("Add")!;
        var toArrayMethod = listType.GetMethod("ToArray")!;
        
        var loop = Expression.Block(
            new[] { listVar, indexVar },
            Expression.Assign(listVar, Expression.New(listType)),
            Expression.Assign(indexVar, Expression.Constant(0)),
            Expression.Loop(
                Expression.IfThenElse(
                    Expression.LessThan(indexVar, lengthExpr),
                    Expression.Block(
                        Expression.IfThen(
                            Expression.Invoke(
                                Expression.Lambda(conditionBodyAsBool, elementParam),
                                Expression.ArrayIndex(arrayExpr, indexVar)
                            ),
                            Expression.Call(listVar, addMethod, Expression.ArrayIndex(arrayExpr, indexVar))
                        ),
                        Expression.PostIncrementAssign(indexVar)
                    ),
                    Expression.Break(breakLabel)
                ),
                breakLabel
            ),
            Expression.Call(listVar, toArrayMethod)
        );
        
        return loop;
    }

    private static Expression CompileReduce(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("reduce requires array of arguments"));
            return Expression.Constant(null, typeof(object));
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("reduce requires at least 3 arguments (array, reducer, initial)"));
            return Expression.Constant(null, typeof(object));
        }

        // Get the array expression with its actual type
        var arrayExpr = CompileCore(arrayEnum.Current, dataParam);

        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("reduce requires at least 3 arguments (array, reducer, initial)"));
            return Expression.Constant(null, typeof(object));
        }

        var arrayType = arrayExpr.Type;
        if (!arrayType.IsArray)
        {
            return Expression.Constant(null, typeof(object));
        }

        var elementType = arrayType.GetElementType()!;
        var reducerElement = arrayEnum.Current;
        var reducerJson = JsonDocument.Parse(reducerElement.GetRawText());

        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("reduce requires at least 3 arguments (array, reducer, initial)"));
            return Expression.Constant(null, typeof(object));
        }

        // Get initial value
        var initialValueExpr = CompileCore(arrayEnum.Current, dataParam);
        var accumType = initialValueExpr.Type;
        
        // Build ReduceContext type with properties matching element type
        var contextType = typeof(ReduceContext);
        var contextParam = Expression.Parameter(contextType, "context");
        var reducerBody = CompileCore(reducerJson.RootElement, contextParam);
        
        // Build inline reduce loop
        var accumVar = Expression.Variable(accumType, "accumulator");
        var lengthExpr = Expression.ArrayLength(arrayExpr);
        var indexVar = Expression.Variable(typeof(int), "i");
        var breakLabel = Expression.Label("break");
        var contextVar = Expression.Variable(contextType, "context");
        var currentProp = contextType.GetProperty("Current")!;
        var accumProp = contextType.GetProperty("Accumulator")!;
        
        var loop = Expression.Block(
            new[] { accumVar, indexVar, contextVar },
            Expression.Assign(accumVar, initialValueExpr),
            Expression.Assign(indexVar, Expression.Constant(0)),
            Expression.Assign(contextVar, Expression.New(contextType)),
            Expression.Loop(
                Expression.IfThenElse(
                    Expression.LessThan(indexVar, lengthExpr),
                    Expression.Block(
                        Expression.Assign(Expression.Property(contextVar, currentProp), Expression.Convert(Expression.ArrayIndex(arrayExpr, indexVar), typeof(object))),
                        Expression.Assign(Expression.Property(contextVar, accumProp), Expression.Convert(accumVar, typeof(object))),
                        Expression.Assign(
                            accumVar,
                            Expression.Convert(
                                Expression.Invoke(Expression.Lambda(reducerBody, contextParam), contextVar),
                                accumType
                            )
                        ),
                        Expression.PostIncrementAssign(indexVar)
                    ),
                    Expression.Break(breakLabel)
                ),
                breakLabel
            ),
            accumVar
        );
        
        return loop;
    }

    private static Expression CompileAll(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("all requires array of arguments"));
            return Expression.Constant(false);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("all requires at least 2 arguments (array and condition)"));
            return Expression.Constant(false);
        }

        // Get array with actual type
        var arrayExpr = CompileCore(arrayEnum.Current, dataParam);

        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("all requires at least 2 arguments (array and condition)"));
            return Expression.Constant(false);
        }

        var arrayType = arrayExpr.Type;
        if (!arrayType.IsArray)
        {
            return Expression.Constant(false);
        }

        var elementType = arrayType.GetElementType()!;
        var conditionElement = arrayEnum.Current;
        var conditionJson = JsonDocument.Parse(conditionElement.GetRawText());
        
        var elementParam = Expression.Parameter(elementType, "element");
        var conditionBody = CompileCore(conditionJson.RootElement, elementParam);
        var conditionBodyAsBool = ToBool(conditionBody);

        // Build inline all check
        var lengthExpr = Expression.ArrayLength(arrayExpr);
        var indexVar = Expression.Variable(typeof(int), "i");
        var resultLabel = Expression.Label(typeof(bool), "result");
        
        var loop = Expression.Block(
            new[] { indexVar },
            Expression.Assign(indexVar, Expression.Constant(0)),
            Expression.IfThen(
                Expression.Equal(lengthExpr, Expression.Constant(0)),
                Expression.Return(resultLabel, Expression.Constant(false))
            ),
            Expression.Loop(
                Expression.IfThenElse(
                    Expression.LessThan(indexVar, lengthExpr),
                    Expression.Block(
                        Expression.IfThen(
                            Expression.Not(
                                Expression.Invoke(
                                    Expression.Lambda(conditionBodyAsBool, elementParam),
                                    Expression.ArrayIndex(arrayExpr, indexVar)
                                )
                            ),
                            Expression.Return(resultLabel, Expression.Constant(false))
                        ),
                        Expression.PostIncrementAssign(indexVar)
                    ),
                    Expression.Return(resultLabel, Expression.Constant(true))
                )
            ),
            Expression.Label(resultLabel, Expression.Constant(true))
        );
        
        return loop;
    }

    private static Expression CompileNone(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("none requires array of arguments"));
            return Expression.Constant(true);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("none requires at least 2 arguments (array and condition)"));
            return Expression.Constant(true);
        }

        // Get array with actual type
        var arrayExpr = CompileCore(arrayEnum.Current, dataParam);

        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("none requires at least 2 arguments (array and condition)"));
            return Expression.Constant(true);
        }

        var arrayType = arrayExpr.Type;
        if (!arrayType.IsArray)
        {
            return Expression.Constant(true);
        }

        var elementType = arrayType.GetElementType()!;
        var conditionElement = arrayEnum.Current;
        var conditionJson = JsonDocument.Parse(conditionElement.GetRawText());
        
        var elementParam = Expression.Parameter(elementType, "element");
        var conditionBody = CompileCore(conditionJson.RootElement, elementParam);
        var conditionBodyAsBool = ToBool(conditionBody);

        // Build inline none check (opposite of some)
        var lengthExpr = Expression.ArrayLength(arrayExpr);
        var indexVar = Expression.Variable(typeof(int), "i");
        var resultLabel = Expression.Label(typeof(bool), "result");
        
        var loop = Expression.Block(
            new[] { indexVar },
            Expression.Assign(indexVar, Expression.Constant(0)),
            Expression.IfThen(
                Expression.Equal(lengthExpr, Expression.Constant(0)),
                Expression.Return(resultLabel, Expression.Constant(true))
            ),
            Expression.Loop(
                Expression.IfThenElse(
                    Expression.LessThan(indexVar, lengthExpr),
                    Expression.Block(
                        Expression.IfThen(
                            Expression.Invoke(
                                Expression.Lambda(conditionBodyAsBool, elementParam),
                                Expression.ArrayIndex(arrayExpr, indexVar)
                            ),
                            Expression.Return(resultLabel, Expression.Constant(false))
                        ),
                        Expression.PostIncrementAssign(indexVar)
                    ),
                    Expression.Return(resultLabel, Expression.Constant(true))
                )
            ),
            Expression.Label(resultLabel, Expression.Constant(true))
        );
        
        return loop;
    }

    private static Expression CompileSome(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("some requires array of arguments"));
            return Expression.Constant(false);
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("some requires at least 2 arguments (array and condition)"));
            return Expression.Constant(false);
        }

        // Get array with actual type
        var arrayExpr = CompileCore(arrayEnum.Current, dataParam);

        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("some requires at least 2 arguments (array and condition)"));
            return Expression.Constant(false);
        }

        var arrayType = arrayExpr.Type;
        if (!arrayType.IsArray)
        {
            return Expression.Constant(false);
        }

        var elementType = arrayType.GetElementType()!;
        var conditionElement = arrayEnum.Current;
        var conditionJson = JsonDocument.Parse(conditionElement.GetRawText());
        
        var elementParam = Expression.Parameter(elementType, "element");
        var conditionBody = CompileCore(conditionJson.RootElement, elementParam);
        var conditionBodyAsBool = ToBool(conditionBody);

        // Build inline some check
        var lengthExpr = Expression.ArrayLength(arrayExpr);
        var indexVar = Expression.Variable(typeof(int), "i");
        var resultLabel = Expression.Label(typeof(bool), "result");
        
        var loop = Expression.Block(
            new[] { indexVar },
            Expression.Assign(indexVar, Expression.Constant(0)),
            Expression.IfThen(
                Expression.Equal(lengthExpr, Expression.Constant(0)),
                Expression.Return(resultLabel, Expression.Constant(false))
            ),
            Expression.Loop(
                Expression.IfThenElse(
                    Expression.LessThan(indexVar, lengthExpr),
                    Expression.Block(
                        Expression.IfThen(
                            Expression.Invoke(
                                Expression.Lambda(conditionBodyAsBool, elementParam),
                                Expression.ArrayIndex(arrayExpr, indexVar)
                            ),
                            Expression.Return(resultLabel, Expression.Constant(true))
                        ),
                        Expression.PostIncrementAssign(indexVar)
                    ),
                    Expression.Return(resultLabel, Expression.Constant(false))
                )
            ),
            Expression.Label(resultLabel, Expression.Constant(false))
        );
        
        return loop;
    }

    private static Expression CompileLog(JsonElement args, ParameterExpression dataParam)
    {
        // log operation pushes a LogEvent with the value and returns the value
        var value = CompileCore(args, dataParam);
        
        // Create expression to push LogEvent
        var logEventType = typeof(LogEvent);
        var eventBusType = typeof(EventBus<LogEvent>);
        var pushMethod = eventBusType.GetMethod(nameof(EventBus<LogEvent>.Push))!;
        
        // Convert value to string for logging
        var valueAsString = value.Type == typeof(string) 
            ? value 
            : Expression.Call(value, typeof(object).GetMethod(nameof(ToString))!);
        
        // Create LogEvent instance
        var logEvent = Expression.New(
            logEventType.GetConstructor([typeof(string)])!,
            valueAsString
        );
        
        // Push the event
        var pushCall = Expression.Call(pushMethod, logEvent);
        
        // Return the original value (log is pass-through)
        return Expression.Block(pushCall, value);
    }
}
