namespace Atomic.Net.MonoGame.JsonExpressions;

/// <summary>
/// Context struct passed to the reducer function in 'aggregate' (reduce) operations.
/// Property names are lowercase to match JSONLogic {"var": "current"} and {"var": "accumulator"} references.
/// </summary>
public readonly record struct JsonAggregateContext<TElement, TAccumulator>(TElement current, TAccumulator accumulator);
