namespace Atomic.Net.MonoGame.Core;

public interface IEntitySelector
{
    void Recalc();

    SparseArray<bool> Matches { get; }
}
