using System;
using System.Linq.Expressions;

namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// Helper methods for building expression trees with zero-boxing optimization.
/// Strategy: Analyze types at COMPILE TIME using reflection, build optimal expressions
/// that execute with minimal/zero boxing at RUNTIME.
/// OLD IMPLEMENTATION - preserved for reference.
/// </summary>
internal static class ExpressionExtensionsOld
{
    /// <summary>
    /// Unifies two numeric expressions to a common type using widening conversions.
    /// Returns tuple of (left, right) with unified types.
    /// Widening hierarchy: int → long → float → double
    /// </summary>
    public static (Expression Left, Expression Right) UnifyNumericTypes(Expression left, Expression right)
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

        // Convert both to target type using Expression.Convert (safe widening conversion)
        var leftConverted = left.Type == targetType ? left : Expression.Convert(left, targetType);
        var rightConverted = right.Type == targetType ? right : Expression.Convert(right, targetType);

        return (leftConverted, rightConverted);
    }

    /// <summary>
    /// Converts any expression to string by calling ToString() method.
    /// Analyzes type at compile time, builds method call expression.
    /// </summary>
    public static Expression ToStringExpr(Expression expr)
    {
        if (expr.Type == typeof(string))
        {
            return expr;
        }

        // Call .ToString() method (analysis at compile time, execution at runtime)
        return Expression.Call(expr, typeof(object).GetMethod(nameof(ToString))!);
    }

    /// <summary>
    /// Converts any expression to bool following JSONLogic truthiness rules.
    /// Falsy: false, null, 0, "", []
    /// Truthy: everything else
    /// Analyzes type at compile time, builds optimal truthiness check expression.
    /// </summary>
    public static Expression ToBool(Expression expr)
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

        // For any array type, check Length > 0
        if (expr.Type.IsArray)
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
    /// Unifies two expressions to a common type using method calls, not casts.
    /// Returns tuple of (left, right) with unified types.
    /// Strategy: Use reflection OUTSIDE expression tree to analyze types,
    /// then build expressions that call .ToString(), ToBool(), etc.
    /// Returns null if types cannot be unified without boxing.
    /// </summary>
    public static (Expression Left, Expression Right)? UnifyExpressionTypes(Expression left, Expression right)
    {
        if (left.Type == right.Type)
        {
            return (left, right);
        }

        // Check if both are numeric - use numeric unification
        var leftIsNumeric = left.Type == typeof(int) || left.Type == typeof(long) || 
                           left.Type == typeof(float) || left.Type == typeof(double);
        var rightIsNumeric = right.Type == typeof(int) || right.Type == typeof(long) || 
                            right.Type == typeof(float) || right.Type == typeof(double);

        if (leftIsNumeric && rightIsNumeric)
        {
            return UnifyNumericTypes(left, right);
        }

        // If one is string, convert other to string using .ToString()
        if (left.Type == typeof(string))
        {
            return (left, ToStringExpr(right));
        }
        if (right.Type == typeof(string))
        {
            return (ToStringExpr(left), right);
        }

        // If one is bool, convert other to bool using ToBool()
        if (left.Type == typeof(bool))
        {
            return (left, ToBool(right));
        }
        if (right.Type == typeof(bool))
        {
            return (ToBool(left), right);
        }

        // Cannot unify these types without boxing - return null to signal error
        return null;
    }
}
