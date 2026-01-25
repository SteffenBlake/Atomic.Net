using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED;

/// <summary>
/// Delegate for mutating a behavior's backing values (cannot reassign the behavior itself).
/// </summary>
public delegate void RefReadonlyAction<T>(ref readonly T value);
