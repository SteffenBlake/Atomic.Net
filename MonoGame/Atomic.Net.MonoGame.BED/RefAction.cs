using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.BED;

/// <summary>
/// Delegate for mutating a behavior value by reference.
/// </summary>
public delegate void RefAction<T>(ref T value);

/// <summary>
/// Delegate for mutating a behavior value by reference with helper input.
/// </summary>
public delegate void RefInAction<T, THelper>(ref readonly THelper input, ref T value);
