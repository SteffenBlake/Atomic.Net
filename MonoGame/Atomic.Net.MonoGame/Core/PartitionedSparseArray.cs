using System.Diagnostics.CodeAnalysis;

namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Wrapper over two SparseArray instances: one for global partition, one for scene partition.
/// Routes access based on PartitionIndex variant type.
/// </summary>
/// <typeparam name="T">The struct type stored in the arrays.</typeparam>
public sealed class PartitionedSparseArray<T>(ushort globalCapacity, uint sceneCapacity)
    where T : struct
{
    /// <summary>
    /// Global partition array (ushort indices 0-255 by default).
    /// </summary>
    public readonly SparseArray<T> Global = new(globalCapacity);

    /// <summary>
    /// Scene partition array (uint indices 0-8191 by default).
    /// </summary>
    public readonly SparseArray<T> Scene = new((int)sceneCapacity);

    /// <summary>
    /// Indexer that routes to the correct partition based on PartitionIndex.
    /// </summary>
    public T this[PartitionIndex index]
    {
        get
        {
            // senior-dev: Can't use static lambda here because we capture Global/Scene
            return index.Visit(
                global => Global[global],
                scene => Scene[(ushort)scene],
                () => throw new InvalidOperationException("Invalid PartitionIndex state")
            );
        }
    }

    /// <summary>
    /// Sets a value at the given partition index.
    /// </summary>
    public void Set(PartitionIndex index, T value)
    {
        // senior-dev: Can't use static lambda here because we capture Global/Scene and value
        index.Visit(
            global => { Global.Set(global, value); return 0; },
            scene => { Scene.Set((ushort)scene, value); return 0; },
            () => throw new InvalidOperationException("Invalid PartitionIndex state")
        );
    }

    /// <summary>
    /// Gets a mutable reference to a value at the given partition index.
    /// </summary>
    public SparseRef<T> GetMut(PartitionIndex index)
    {
        // senior-dev: Visit pattern with ref returns causes type inference issues
        // Use if-else pattern with Visit for type checking instead
        var isGlobal = index.Visit(
            static global => true,
            static scene => false,
            static () => false
        );
        
        if (isGlobal)
        {
            var globalIndex = index.Visit(
                static global => global,
                static scene => throw new InvalidOperationException(),
                static () => throw new InvalidOperationException()
            );
            return Global.GetMut(globalIndex);
        }
        else
        {
            var sceneIndex = index.Visit(
                static global => throw new InvalidOperationException(),
                static scene => (ushort)scene,
                static () => throw new InvalidOperationException()
            );
            return Scene.GetMut(sceneIndex);
        }
    }

    /// <summary>
    /// Removes a value at the given partition index.
    /// </summary>
    public bool Remove(PartitionIndex index)
    {
        // senior-dev: Can't use static lambda here because we capture Global/Scene
        return index.Visit(
            global => Global.Remove(global),
            scene => Scene.Remove((ushort)scene),
            () => throw new InvalidOperationException("Invalid PartitionIndex state")
        );
    }

    /// <summary>
    /// Tries to get a value at the given partition index.
    /// Uses tuple return pattern since Visit doesn't support out parameters in lambdas.
    /// </summary>
    public bool TryGetValue(
        PartitionIndex index,
        [NotNullWhen(true)] out T? value
    )
    {
        // senior-dev: Can't use static lambda here because we capture Global/Scene
        // Also can't capture out parameter in lambda, so use tuple return
        var (found, val) = index.Visit(
            global => {
                var success = Global.TryGetValue(global, out var v);
                return (success, v);
            },
            scene => {
                var success = Scene.TryGetValue((ushort)scene, out var v);
                return (success, v);
            },
            () => (false, default(T?))
        );
        value = val;
        return found;
    }

    /// <summary>
    /// Checks if a value exists at the given partition index.
    /// </summary>
    public bool HasValue(PartitionIndex index)
    {
        // senior-dev: Can't use static lambda here because we capture Global/Scene
        return index.Visit(
            global => Global.HasValue(global),
            scene => Scene.HasValue((ushort)scene),
            () => false
        );
    }
}
