using Atomic.Net.MonoGame.GreedyPool;
using Microsoft.Extensions.ObjectPool;
using R3;

namespace Atomic.Net.MonoGame.Engine;

public class ObjectManager<T>(int capacity)
    where T : class, IResettable, new()
{
    private readonly GreedyObjectPool<T> _objectPool = GreedyObjectPool<T>.Default(capacity);

    public IDisposable Compose(out T result)
    {
        var instance = _objectPool.Get();
        result = instance;
        return Disposable.Create(() =>
        {
            instance.TryReset();
            _objectPool.Return(instance);
        });
    }

}

