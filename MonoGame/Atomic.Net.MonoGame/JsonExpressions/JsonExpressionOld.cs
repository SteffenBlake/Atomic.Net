using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// Generic context object for reduce operations with strongly-typed current element and accumulator.
/// </summary>
public sealed class ReduceContext<TCurrent, TAccum>
{
    public TCurrent? Current { get; set; }
    public TAccum? Accumulator { get; set; }
}

/// <summary>
/// Context object for reduce operations with current element and accumulator.
/// </summary>
[Obsolete("Use ReduceContext<TCurrent, TAccum> instead for zero-boxing")]
public sealed class ReduceContext
{
    public object? Current { get; set; }
    public object? Accumulator { get; set; }
}

/// <summary>
/// Compiles JSONLogic rules into strongly-typed Expression trees.
/// Zero-allocation recursive compilation using yoyo pattern.
/// </summary>
public static class JsonExpressionOld
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
            // Try to unify types using our type coercion system first
            var targetDefault = Expression.Default(typeof(TOut));
            var unified = ExpressionExtensionsOld.UnifyExpressionTypes(bodyExpr, targetDefault);
            
            if (unified.HasValue && unified.Value.Left.Type == typeof(TOut))
            {
                bodyExpr = unified.Value.Left;
            }
            else
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot convert expression of type {bodyExpr.Type.Name} to target type {typeof(TOut).Name}"));
                bodyExpr = Expression.Default(typeof(TOut));
            }
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
                return Expression.Constant(null, typeof(string));
            
            case JsonValueKind.Number:
                return CompileLiteralNumber(element);
            
            case JsonValueKind.String:
                return Expression.Constant(element.GetString());
            
            case JsonValueKind.Array:
                // Arrays are valid data values in JSONLogic
                // Infer array element type at compile time, create properly typed array
                var arrayLength = element.GetArrayLength();
                if (arrayLength == 0)
                {
                    return Expression.Constant(Array.Empty<string>(), typeof(string[]));
                }
                
                // Analyze all elements to determine common type
                var hasInt = false;
                var hasDouble = false;
                var hasBool = false;
                var hasString = false;
                var hasNull = false;
                
                foreach (var item in element.EnumerateArray())
                {
                    switch (item.ValueKind)
                    {
                        case JsonValueKind.Number:
                            if (item.TryGetInt32(out _))
                            {
                                hasInt = true;
                            }
                            else
                            {
                                hasDouble = true;
                            }
                            break;
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            hasBool = true;
                            break;
                        case JsonValueKind.String:
                            hasString = true;
                            break;
                        case JsonValueKind.Null:
                        case JsonValueKind.Undefined:
                            hasNull = true;
                            break;
                    }
                }
                
                // Determine target type (prefer most specific)
                if (hasString || hasNull)
                {
                    // String array (can hold nulls)
                    var strArray = new string?[arrayLength];
                    var idx = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        strArray[idx++] = item.ValueKind switch
                        {
                            JsonValueKind.String => item.GetString(),
                            JsonValueKind.Number => item.GetDouble().ToString(),
                            JsonValueKind.True => "true",
                            JsonValueKind.False => "false",
                            _ => null
                        };
                    }
                    return Expression.Constant(strArray, typeof(string[]));
                }
                else if (hasBool && !hasInt && !hasDouble)
                {
                    // Pure bool array
                    var boolArray = new bool[arrayLength];
                    var idx = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        boolArray[idx++] = item.ValueKind == JsonValueKind.True;
                    }
                    return Expression.Constant(boolArray, typeof(bool[]));
                }
                else if (hasDouble || (hasInt && hasBool))
                {
                    // Double array (widest numeric type)
                    var doubleArray = new double[arrayLength];
                    var idx = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        doubleArray[idx++] = item.ValueKind switch
                        {
                            JsonValueKind.Number => item.GetDouble(),
                            JsonValueKind.True => 1.0,
                            JsonValueKind.False => 0.0,
                            _ => 0.0
                        };
                    }
                    return Expression.Constant(doubleArray, typeof(double[]));
                }
                else if (hasInt)
                {
                    // Int array
                    var intArray = new int[arrayLength];
                    var idx = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        intArray[idx++] = item.GetInt32();
                    }
                    return Expression.Constant(intArray, typeof(int[]));
                }
                else
                {
                    // Empty or unknown - default to string[]
                    return Expression.Constant(Array.Empty<string>(), typeof(string[]));
                }
            
            case JsonValueKind.Object:
                return CompileOperation(element, dataParam);
            
            default:
                EventBus<ErrorEvent>.Push(new ErrorEvent($"Unsupported JSON value kind: {element.ValueKind}"));
                return Expression.Constant(null, typeof(string));
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
        string? propertyPath = null;
        int? arrayIndex = null;
        Expression? defaultValueExpr = null;

        if (args.ValueKind == JsonValueKind.String)
        {
            propertyPath = args.GetString();
        }
        else if (args.ValueKind == JsonValueKind.Number)
        {
            // Array index access
            arrayIndex = args.GetInt32();
        }
        else if (args.ValueKind == JsonValueKind.Array)
        {
            var arrayEnum = args.EnumerateArray();
            if (!arrayEnum.MoveNext())
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent("var requires at least one argument"));
                return Expression.Constant(null, typeof(string));
            }

            var firstArg = arrayEnum.Current;
            if (firstArg.ValueKind == JsonValueKind.String)
            {
                propertyPath = firstArg.GetString();
            }
            else if (firstArg.ValueKind == JsonValueKind.Number)
            {
                arrayIndex = firstArg.GetInt32();
            }
            else
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent("var first argument must be string or number"));
                return Expression.Constant(null, typeof(string));
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
            return Expression.Constant(null, typeof(string));
        }

        // Handle array index access
        if (arrayIndex.HasValue)
        {
            if (dataParam.Type.IsArray)
            {
                var lengthProp = Expression.Property(dataParam, nameof(Array.Length));
                var indexConst = Expression.Constant(arrayIndex.Value);
                
                // Check bounds: index >= 0 && index < length
                var inBounds = Expression.AndAlso(
                    Expression.GreaterThanOrEqual(indexConst, Expression.Constant(0)),
                    Expression.LessThan(indexConst, lengthProp)
                );
                
                var elemType = dataParam.Type.GetElementType()!;
                var indexAccess = Expression.ArrayIndex(dataParam, indexConst);
                
                if (defaultValueExpr is not null)
                {
                    // Unify types between index access and default
                    var unified = ExpressionExtensionsOld.UnifyExpressionTypes(indexAccess, defaultValueExpr);
                    if (unified.HasValue)
                    {
                        return Expression.Condition(inBounds, unified.Value.Left, unified.Value.Right);
                    }
                    // Can't unify - return default for element type
                    return Expression.Condition(inBounds, indexAccess, Expression.Default(elemType));
                }
                
                // No default - return default value for element type on out of bounds
                var defaultValue = Expression.Default(elemType);
                return Expression.Condition(inBounds, indexAccess, defaultValue);
            }
            
            // Not an array but got index - return default or null
            return defaultValueExpr ?? Expression.Constant(null, typeof(string));
        }

        // Handle property path access
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
                // Property not found - return default value or null with string type
                return defaultValueExpr ?? Expression.Constant(null, typeof(string));
            }

            // Return property as-is without conversion
            return Expression.Property(dataParam, prop);
        }

        // Nested property access (with dots) - walk the path
        var parts = propertyPath.Split('.');
        Expression currentExpr = dataParam;
        Type? currentType = dataParam.Type;
        
        foreach (var part in parts)
        {
            if (currentType is null)
            {
                break;
            }
            
            var prop = currentType.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop is null)
            {
                // Property not found in chain - return default or null
                return defaultValueExpr ?? Expression.Constant(null, typeof(string));
            }
            
            currentExpr = Expression.Property(currentExpr, prop);
            currentType = prop.PropertyType;
        }
        
        return currentExpr;
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
            
            // Unify types before addition
            (result, next) = ExpressionExtensionsOld.UnifyNumericTypes(result, next);
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
            
            // Unify types before subtraction
            (result, next) = ExpressionExtensionsOld.UnifyNumericTypes(result, next);
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
            (result, next) = ExpressionExtensionsOld.UnifyNumericTypes(result, next);
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
            
            // Unify types before division
            (result, next) = ExpressionExtensionsOld.UnifyNumericTypes(result, next);
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

        // Unify types before modulo
        (first, second) = ExpressionExtensionsOld.UnifyNumericTypes(first, second);
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
            
            // Unify types before comparison
            (first, second) = ExpressionExtensionsOld.UnifyNumericTypes(first, second);
            (first, third) = ExpressionExtensionsOld.UnifyNumericTypes(first, third);
            
            // first > second && first < third
            return Expression.AndAlso(
                Expression.GreaterThan(first, second),
                Expression.LessThan(first, third)
            );
        }

        // Unify types before comparison
        (first, second) = ExpressionExtensionsOld.UnifyNumericTypes(first, second);
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
            
            // Unify types before comparison
            (first, second) = ExpressionExtensionsOld.UnifyNumericTypes(first, second);
            (first, third) = ExpressionExtensionsOld.UnifyNumericTypes(first, third);
            
            // first >= second && first <= third
            return Expression.AndAlso(
                Expression.GreaterThanOrEqual(first, second),
                Expression.LessThanOrEqual(first, third)
            );
        }

        // Unify types before comparison
        (first, second) = ExpressionExtensionsOld.UnifyNumericTypes(first, second);
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
            
            // Unify types before comparison
            (first, second) = ExpressionExtensionsOld.UnifyNumericTypes(first, second);
            (first, third) = ExpressionExtensionsOld.UnifyNumericTypes(first, third);
            
            // first > second && first < third (i.e., second < first < third)
            return Expression.AndAlso(
                Expression.GreaterThan(first, second),
                Expression.LessThan(first, third)
            );
        }

        // Unify types before comparison
        (first, second) = ExpressionExtensionsOld.UnifyNumericTypes(first, second);
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
            
            // Unify types before comparison
            (first, second) = ExpressionExtensionsOld.UnifyNumericTypes(first, second);
            (first, third) = ExpressionExtensionsOld.UnifyNumericTypes(first, third);
            
            // first >= second && first <= third (i.e., second <= first <= third)
            return Expression.AndAlso(
                Expression.GreaterThanOrEqual(first, second),
                Expression.LessThanOrEqual(first, third)
            );
        }

        // Unify types before comparison
        (first, second) = ExpressionExtensionsOld.UnifyNumericTypes(first, second);
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
        expr = ExpressionExtensionsOld.ToBool(expr);
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
        return ExpressionExtensionsOld.ToBool(expr);
    }

    private static Expression CompileAnd(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("and requires array of arguments"));
            return Expression.Constant(false);
        }

        var elements = args.EnumerateArray().ToArray();
        if (elements.Length == 0)
        {
            return Expression.Constant(false);
        }

        if (elements.Length == 1)
        {
            return CompileCore(elements[0], dataParam);
        }

        // And returns first falsy value, or last value if all truthy
        // Compile all elements first to determine final return type
        var compiledExprs = elements.Select(e => CompileCore(e, dataParam)).ToArray();
        
        // Find common type among all expressions
        var firstType = compiledExprs[0].Type;
        var allSameType = compiledExprs.All(e => e.Type == firstType);
        
        if (allSameType)
        {
            // All same type - build nested conditions
            Expression result = compiledExprs[0];
            for (var i = 1; i < compiledExprs.Length; i++)
            {
                var current = result;
                var next = compiledExprs[i];
                result = Expression.Condition(
                    ExpressionExtensionsOld.ToBool(current),
                    next,      // current is truthy, continue to next
                    current    // current is falsy, return it
                );
            }
            return result;
        }
        else
        {
            // Heterogeneous types - unify all to the LAST element's type
            // (since that's what gets returned when all are truthy)
            var targetType = compiledExprs[^1].Type;
            var unifiedExprs = new Expression[compiledExprs.Length];
            
            for (var i = 0; i < compiledExprs.Length; i++)
            {
                if (compiledExprs[i].Type == targetType)
                {
                    unifiedExprs[i] = compiledExprs[i];
                }
                else
                {
                    var unified = ExpressionExtensionsOld.UnifyExpressionTypes(compiledExprs[i], Expression.Default(targetType));
                    if (!unified.HasValue)
                    {
                        EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot unify type {compiledExprs[i].Type.Name} to {targetType.Name} in 'and' operation"));
                        return Expression.Default(targetType);
                    }
                    unifiedExprs[i] = unified.Value.Left;
                }
            }
            
            // Build nested conditions with unified expressions
            Expression result = unifiedExprs[0];
            for (var i = 1; i < unifiedExprs.Length; i++)
            {
                var current = result;
                var next = unifiedExprs[i];
                result = Expression.Condition(
                    ExpressionExtensionsOld.ToBool(compiledExprs[i - 1]),  // Use original for truthiness check
                    next,      // current is truthy, continue to next
                    current    // current is falsy, return it
                );
            }
            return result;
        }
    }

    private static Expression CompileOr(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("or requires array of arguments"));
            return Expression.Constant(false);
        }

        var elements = args.EnumerateArray().ToArray();
        if (elements.Length == 0)
        {
            return Expression.Constant(false);
        }

        if (elements.Length == 1)
        {
            return CompileCore(elements[0], dataParam);
        }

        // Or returns first truthy value, or last value if all falsy
        // Compile all elements first to determine final return type
        var compiledExprs = elements.Select(e => CompileCore(e, dataParam)).ToArray();
        
        // Find common type among all expressions
        var firstType = compiledExprs[0].Type;
        var allSameType = compiledExprs.All(e => e.Type == firstType);
        
        if (allSameType)
        {
            // All same type - build nested conditions
            Expression result = compiledExprs[0];
            for (var i = 1; i < compiledExprs.Length; i++)
            {
                var current = result;
                var next = compiledExprs[i];
                result = Expression.Condition(
                    ExpressionExtensionsOld.ToBool(current),
                    current,   // current is truthy, return it
                    next       // current is falsy, continue to next
                );
            }
            return result;
        }
        else
        {
            // Heterogeneous types - unify pairwise
            Expression result = compiledExprs[0];
            for (var i = 1; i < compiledExprs.Length; i++)
            {
                var current = result;
                var next = compiledExprs[i];
                var unified = ExpressionExtensionsOld.UnifyExpressionTypes(current, next);
                if (!unified.HasValue)
                {
                    EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot unify types {current.Type.Name} and {next.Type.Name} in 'or' operation"));
                    return Expression.Constant(null, typeof(string));
                }
                result = Expression.Condition(
                    ExpressionExtensionsOld.ToBool(current),
                    unified.Value.Left,    // current is truthy, return current
                    unified.Value.Right        // current is falsy, return next
                );
            }
            return result;
        }
    }

    private static Expression CompileIf(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("if requires array of arguments"));
            return Expression.Constant(null, typeof(string));
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("if requires at least one argument"));
            return Expression.Constant(null, typeof(string));
        }

        // Build nested if-then-else chain
        // Pattern: if(condition, then) or if(condition, then, else) or if(cond1, then1, cond2, then2, ..., else)
        var firstCondition = CompileCore(arrayEnum.Current, dataParam);
        var conditionBool = ExpressionExtensionsOld.ToBool(firstCondition);

        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("if requires at least 2 arguments (condition and then)"));
            return Expression.Constant(null, typeof(string));
        }

        var thenBranch = CompileCore(arrayEnum.Current, dataParam);

        // Check if there's an else branch or more condition pairs
        if (!arrayEnum.MoveNext())
        {
            // No else branch - return null with matching type of then branch
            var nullElse = Expression.Constant(null, thenBranch.Type);
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

        // Unify types between then and else branches using type-safe unification
        if (thenBranch.Type != elseBranch.Type)
        {
            var unified = ExpressionExtensionsOld.UnifyExpressionTypes(thenBranch, elseBranch);
            if (!unified.HasValue)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot unify types {thenBranch.Type.Name} and {elseBranch.Type.Name} in 'if' branches"));
                return Expression.Constant(null, typeof(string));
            }
            return Expression.Condition(conditionBool, unified.Value.Left, unified.Value.Right);
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

        // Loose equality: apply JSONLogic coercion rules
        // 1. Both numeric -> compare as numbers
        // 2. Both bool -> compare as bools
        // 3. One numeric, one bool -> convert bool to number (true=1, false=0)
        // 4. Otherwise -> convert both to strings
        
        var firstIsNumeric = first.Type == typeof(int) || first.Type == typeof(long) || 
                            first.Type == typeof(float) || first.Type == typeof(double);
        var secondIsNumeric = second.Type == typeof(int) || second.Type == typeof(long) || 
                             second.Type == typeof(float) || second.Type == typeof(double);
        var firstIsBool = first.Type == typeof(bool);
        var secondIsBool = second.Type == typeof(bool);
        
        if (firstIsNumeric && secondIsNumeric)
        {
            // Both numeric - unify and compare
            var (unifiedFirst, unifiedSecond) = ExpressionExtensionsOld.UnifyNumericTypes(first, second);
            return Expression.Equal(unifiedFirst, unifiedSecond);
        }
        else if (firstIsBool && secondIsBool)
        {
            // Both bool - compare directly
            return Expression.Equal(first, second);
        }
        else if ((firstIsNumeric && secondIsBool) || (firstIsBool && secondIsNumeric))
        {
            // One numeric, one bool - convert bool to number
            if (firstIsBool)
            {
                // Convert first (bool) to number: true=1.0, false=0.0
                var firstAsNumber = Expression.Condition(
                    first,
                    Expression.Constant(1.0),
                    Expression.Constant(0.0)
                );
                var secondAsNumber = ToNumber(second);
                var (unifiedFirst, unifiedSecond) = ExpressionExtensionsOld.UnifyNumericTypes(firstAsNumber, secondAsNumber);
                return Expression.Equal(unifiedFirst, unifiedSecond);
            }
            else
            {
                // Convert second (bool) to number: true=1.0, false=0.0
                var firstAsNumber = ToNumber(first);
                var secondAsNumber = Expression.Condition(
                    second,
                    Expression.Constant(1.0),
                    Expression.Constant(0.0)
                );
                var (unifiedFirst, unifiedSecond) = ExpressionExtensionsOld.UnifyNumericTypes(firstAsNumber, secondAsNumber);
                return Expression.Equal(unifiedFirst, unifiedSecond);
            }
        }
        else
        {
            // Default: convert both to strings
            var firstStr = ExpressionExtensionsOld.ToStringExpr(first);
            var secondStr = ExpressionExtensionsOld.ToStringExpr(second);
            return Expression.Equal(firstStr, secondStr);
        }
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

        // Strict equality: types must match (but numeric types are considered one "type")
        var firstIsNumeric = first.Type == typeof(int) || first.Type == typeof(long) || 
                            first.Type == typeof(float) || first.Type == typeof(double);
        var secondIsNumeric = second.Type == typeof(int) || second.Type == typeof(long) || 
                             second.Type == typeof(float) || second.Type == typeof(double);
        
        if (firstIsNumeric && secondIsNumeric)
        {
            // Both numeric - unify and compare (following JavaScript semantics where 5 === 5.0)
            var (unifiedFirst, unifiedSecond) = ExpressionExtensionsOld.UnifyNumericTypes(first, second);
            return Expression.Equal(unifiedFirst, unifiedSecond);
        }
        else if (first.Type == second.Type)
        {
            // Same type - compare directly
            return Expression.Equal(first, second);
        }
        else
        {
            // Different types - always false for strict equality
            return Expression.Constant(false);
        }
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

        // Loose inequality: apply JSONLogic coercion rules (same as ==, but negated)
        var firstIsNumeric = first.Type == typeof(int) || first.Type == typeof(long) || 
                            first.Type == typeof(float) || first.Type == typeof(double);
        var secondIsNumeric = second.Type == typeof(int) || second.Type == typeof(long) || 
                             second.Type == typeof(float) || second.Type == typeof(double);
        var firstIsBool = first.Type == typeof(bool);
        var secondIsBool = second.Type == typeof(bool);
        
        if (firstIsNumeric && secondIsNumeric)
        {
            var (unifiedFirst, unifiedSecond) = ExpressionExtensionsOld.UnifyNumericTypes(first, second);
            return Expression.NotEqual(unifiedFirst, unifiedSecond);
        }
        else if (firstIsBool && secondIsBool)
        {
            return Expression.NotEqual(first, second);
        }
        else if ((firstIsNumeric && secondIsBool) || (firstIsBool && secondIsNumeric))
        {
            if (firstIsBool)
            {
                var firstAsNumber = Expression.Condition(
                    first,
                    Expression.Constant(1.0),
                    Expression.Constant(0.0)
                );
                var secondAsNumber = ToNumber(second);
                var (unifiedFirst, unifiedSecond) = ExpressionExtensionsOld.UnifyNumericTypes(firstAsNumber, secondAsNumber);
                return Expression.NotEqual(unifiedFirst, unifiedSecond);
            }
            else
            {
                var firstAsNumber = ToNumber(first);
                var secondAsNumber = Expression.Condition(
                    second,
                    Expression.Constant(1.0),
                    Expression.Constant(0.0)
                );
                var (unifiedFirst, unifiedSecond) = ExpressionExtensionsOld.UnifyNumericTypes(firstAsNumber, secondAsNumber);
                return Expression.NotEqual(unifiedFirst, unifiedSecond);
            }
        }
        else
        {
            var firstStr = ExpressionExtensionsOld.ToStringExpr(first);
            var secondStr = ExpressionExtensionsOld.ToStringExpr(second);
            return Expression.NotEqual(firstStr, secondStr);
        }
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

        // Strict inequality: types must match (but numeric types are considered one "type")
        var firstIsNumeric = first.Type == typeof(int) || first.Type == typeof(long) || 
                            first.Type == typeof(float) || first.Type == typeof(double);
        var secondIsNumeric = second.Type == typeof(int) || second.Type == typeof(long) || 
                             second.Type == typeof(float) || second.Type == typeof(double);
        
        if (firstIsNumeric && secondIsNumeric)
        {
            // Both numeric - unify and compare
            var (unifiedFirst, unifiedSecond) = ExpressionExtensionsOld.UnifyNumericTypes(first, second);
            return Expression.NotEqual(unifiedFirst, unifiedSecond);
        }
        else if (first.Type == second.Type)
        {
            // Same type - compare directly
            return Expression.NotEqual(first, second);
        }
        else
        {
            // Different types - always true for strict inequality
            return Expression.Constant(true);
        }
    }

    private static Expression CompileCat(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            // Single argument - convert to string
            var expr = CompileCore(args, dataParam);
            if (expr.Type != typeof(string))
            {
                expr = ExpressionExtensionsOld.ToStringExpr(expr);
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
        first = ExpressionExtensionsOld.ToStringExpr(first);

        Expression result = first;
        while (arrayEnum.MoveNext())
        {
            var next = CompileCore(arrayEnum.Current, dataParam);
            next = ExpressionExtensionsOld.ToStringExpr(next);
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
        str = ExpressionExtensionsOld.ToStringExpr(str);

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
            var safeSubstringMethod = typeof(JsonExpressionOld).GetMethod(nameof(SafeSubstring))!;
            var lengthProp = typeof(string).GetProperty(nameof(string.Length))!;
            return Expression.Call(safeSubstringMethod, str, start, Expression.Property(str, lengthProp));
        }

        var length = CompileCore(arrayEnum.Current, dataParam);
        if (length.Type != typeof(int))
        {
            length = Expression.Convert(ToNumber(length), typeof(int));
        }

        // String, start, and length - use SafeSubstring
        var safeSubstringMethod2 = typeof(JsonExpressionOld).GetMethod(nameof(SafeSubstring))!;
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

        // Check if haystack is an array type
        var haystackIsArray = haystack.Type.IsArray;
        
        if (haystackIsArray)
        {
            // Array contains - check using array methods based on actual array type
            var elemType = haystack.Type.GetElementType()!;
            var arrayIndexOfMethod = typeof(Array).GetMethod(nameof(Array.IndexOf), [typeof(Array), typeof(object)])!;
            
            // Check if needle needs to be unified with array element type
            Expression needleForArray;
            if (needle.Type == elemType)
            {
                needleForArray = needle;
            }
            else
            {
                // Unify types
                var unified = ExpressionExtensionsOld.UnifyExpressionTypes(needle, Expression.Default(elemType));
                if (!unified.HasValue)
                {
                    // Can't unify - will never find match, return false
                    return Expression.Constant(false);
                }
                needleForArray = unified.Value.Left;
            }
            
            var indexOf = Expression.Call(arrayIndexOfMethod, haystack, needleForArray);
            return Expression.GreaterThanOrEqual(indexOf, Expression.Constant(0));
        }
        else
        {
            // String contains - convert both to strings
            var needleStr = ExpressionExtensionsOld.ToStringExpr(needle);
            var haystackStr = ExpressionExtensionsOld.ToStringExpr(haystack);
            var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
            return Expression.Call(haystackStr, containsMethod, needleStr);
        }
    }

    private static Expression CompileMerge(JsonElement args, ParameterExpression dataParam)
    {
        // merge combines multiple arrays into one
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("merge requires array of arguments"));
            return Expression.Constant(Array.Empty<string>(), typeof(string[]));
        }

        var arrays = new List<Expression>();
        
        foreach (var item in args.EnumerateArray())
        {
            var itemExpr = CompileCore(item, dataParam);
            arrays.Add(itemExpr);
        }

        if (arrays.Count == 0)
        {
            return Expression.Constant(Array.Empty<string>(), typeof(string[]));
        }

        if (arrays.Count == 1)
        {
            // Single element - if it's an array return it, otherwise wrap it in array of its type
            var singleExpr = arrays[0];
            if (singleExpr.Type.IsArray)
            {
                return singleExpr;
            }
            return Expression.NewArrayInit(singleExpr.Type, singleExpr);
        }

        // Find common type among all arrays
        Type? commonElemType = null;
        
        foreach (var arr in arrays)
        {
            if (arr.Type.IsArray)
            {
                var elemType = arr.Type.GetElementType()!;
                if (commonElemType is null)
                {
                    commonElemType = elemType;
                }
                else if (commonElemType != elemType)
                {
                    // Different array types - need to unify to string (most general)
                    commonElemType = typeof(string);
                }
            }
            else
            {
                // Non-array element - treat as single-element array of its type
                if (commonElemType is null)
                {
                    commonElemType = arr.Type;
                }
                else if (commonElemType != arr.Type)
                {
                    // Different types - unify to string
                    commonElemType = typeof(string);
                }
            }
        }

        // Default to string[] if we couldn't determine
        commonElemType ??= typeof(string);

        // Build block to merge arrays
        var statements = new List<Expression>();
        var arrayVars = new List<ParameterExpression>();
        var totalLengthVar = Expression.Variable(typeof(int), "totalLength");
        var resultVar = Expression.Variable(commonElemType.MakeArrayType(), "result");
        var indexVar = Expression.Variable(typeof(int), "index");

        Expression lengthSum = Expression.Constant(0);
        
        // Convert each expression to array of common type
        for (var i = 0; i < arrays.Count; i++)
        {
            var arrVar = Expression.Variable(commonElemType.MakeArrayType(), $"arr{i}");
            arrayVars.Add(arrVar);
            
            var arrayExpr = arrays[i];
            if (arrayExpr.Type.IsArray)
            {
                var elemType = arrayExpr.Type.GetElementType()!;
                if (elemType == commonElemType)
                {
                    // Same type - use directly
                    statements.Add(Expression.Assign(arrVar, arrayExpr));
                }
                else
                {
                    // Different type - need to convert each element
                    // Use LINQ Select to convert (will be compiled to efficient loop)
                    var selectMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(elemType, commonElemType);
                    var toArrayMethod = typeof(Enumerable).GetMethod("ToArray")!.MakeGenericMethod(commonElemType);
                    var xParam = Expression.Parameter(elemType, "x");
                    var unified = ExpressionExtensionsOld.UnifyExpressionTypes(xParam, Expression.Default(commonElemType));
                    if (!unified.HasValue)
                    {
                        EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot convert array element type {elemType.Name} to {commonElemType.Name} in merge"));
                        return Expression.Constant(Array.Empty<string>(), typeof(string[]));
                    }
                    var converter = Expression.Lambda(unified.Value.Left, xParam);
                    var selected = Expression.Call(selectMethod, arrayExpr, converter);
                    statements.Add(Expression.Assign(arrVar, Expression.Call(toArrayMethod, selected)));
                }
            }
            else
            {
                // Not an array - wrap as single-element array
                Expression elem = arrayExpr;
                if (arrayExpr.Type != commonElemType)
                {
                    var unified = ExpressionExtensionsOld.UnifyExpressionTypes(arrayExpr, Expression.Default(commonElemType));
                    if (!unified.HasValue)
                    {
                        EventBus<ErrorEvent>.Push(new ErrorEvent($"Cannot convert type {arrayExpr.Type.Name} to {commonElemType.Name} in merge"));
                        return Expression.Constant(Array.Empty<string>(), typeof(string[]));
                    }
                    elem = unified.Value.Left;
                }
                statements.Add(Expression.Assign(arrVar, Expression.NewArrayInit(commonElemType, elem)));
            }
            
            lengthSum = Expression.Add(lengthSum, Expression.Property(arrVar, "Length"));
        }

        statements.Add(Expression.Assign(totalLengthVar, lengthSum));
        statements.Add(Expression.Assign(resultVar, Expression.NewArrayBounds(commonElemType, totalLengthVar)));
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
            
            // Unify types before calling Math.Max
            (result, next) = ExpressionExtensionsOld.UnifyNumericTypes(result, next);
            
            // Math.Max(result, next) - need to use correct overload for unified type
            var mathMaxMethod = typeof(Math).GetMethod(nameof(Math.Max), [result.Type, result.Type])!;
            result = Expression.Call(mathMaxMethod, result, next);
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
            
            // Unify types before calling Math.Min
            (result, next) = ExpressionExtensionsOld.UnifyNumericTypes(result, next);
            
            // Math.Min(result, next) - need to use correct overload for unified type
            var mathMinMethod = typeof(Math).GetMethod(nameof(Math.Min), [result.Type, result.Type])!;
            result = Expression.Call(mathMinMethod, result, next);
        }

        return result;
    }

    private static Expression CompileMap(JsonElement args, ParameterExpression dataParam)
    {
        if (args.ValueKind != JsonValueKind.Array)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("map requires array of arguments"));
            return Expression.Constant(Array.Empty<string>(), typeof(string[]));
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("map requires at least 2 arguments (array and transform)"));
            return Expression.Constant(Array.Empty<string>(), typeof(string[]));
        }

        var arrayExprTest = CompileCore(arrayEnum.Current, dataParam);
        
        Expression arrayExpr;
        Type arrayType;
        
        if (arrayExprTest.Type.IsArray)
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
            return Expression.Constant(Array.Empty<string>(), typeof(string[]));
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
            return Expression.Constant(Array.Empty<string>(), typeof(string[]));
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("filter requires at least 2 arguments (array and condition)"));
            return Expression.Constant(Array.Empty<string>(), typeof(string[]));
        }

        // Get the array expression with its actual type
        var arrayExpr = CompileCore(arrayEnum.Current, dataParam);

        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("filter requires at least 2 arguments (array and condition)"));
            return Expression.Constant(Array.Empty<string>(), typeof(string[]));
        }

        var arrayType = arrayExpr.Type;
        if (!arrayType.IsArray)
        {
            return Expression.Constant(Array.Empty<string>(), typeof(string[]));
        }

        var elementType = arrayType.GetElementType()!;
        var conditionElement = arrayEnum.Current;
        var conditionJson = JsonDocument.Parse(conditionElement.GetRawText());
        
        // Compile condition with actual element type
        var elementParam = Expression.Parameter(elementType, "element");
        var conditionBody = CompileCore(conditionJson.RootElement, elementParam);
        var conditionBodyAsBool = ExpressionExtensionsOld.ToBool(conditionBody);
        
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
            return Expression.Constant(null, typeof(string));
        }

        var arrayEnum = args.EnumerateArray();
        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("reduce requires at least 3 arguments (array, reducer, initial)"));
            return Expression.Constant(null, typeof(string));
        }

        // Get the array expression with its actual type
        var arrayExpr = CompileCore(arrayEnum.Current, dataParam);

        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("reduce requires at least 3 arguments (array, reducer, initial)"));
            return Expression.Constant(null, typeof(string));
        }

        var arrayType = arrayExpr.Type;
        if (!arrayType.IsArray)
        {
            return Expression.Constant(null, typeof(string));
        }

        var elementType = arrayType.GetElementType()!;
        var reducerElement = arrayEnum.Current;
        var reducerJson = JsonDocument.Parse(reducerElement.GetRawText());

        if (!arrayEnum.MoveNext())
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent("reduce requires at least 3 arguments (array, reducer, initial)"));
            return Expression.Constant(null, typeof(string));
        }

        // Get initial value
        var initialValueExpr = CompileCore(arrayEnum.Current, dataParam);
        var accumType = initialValueExpr.Type;
        
        // Build generic ReduceContext<elementType, accumType> with fully typed properties
        var contextType = typeof(ReduceContext<,>).MakeGenericType(elementType, accumType);
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
                        Expression.Assign(Expression.Property(contextVar, currentProp), Expression.ArrayIndex(arrayExpr, indexVar)),
                        Expression.Assign(Expression.Property(contextVar, accumProp), accumVar),
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
        var conditionBodyAsBool = ExpressionExtensionsOld.ToBool(conditionBody);

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
        var conditionBodyAsBool = ExpressionExtensionsOld.ToBool(conditionBody);

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
        var conditionBodyAsBool = ExpressionExtensionsOld.ToBool(conditionBody);

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
            : ExpressionExtensionsOld.ToStringExpr(value);
        
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
