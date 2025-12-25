using Microsoft.Xna.Framework;
using R3;

namespace Atomic.Net.MonoGame.Engine;

public class Node
{
    public IReadOnlyList<Node> Children => _children;

    protected readonly List<Node> _children = [];

    public virtual IDisposable Compose()
    {
        return Disposable.Combine(
            [.. _children.Select(c => c.Compose())]
        );
    }

    public void Update(GameTime gameTime)
    {
        foreach (var child in _children)
        {
            child.Update(gameTime);
        }
    }

    public void Draw(GameTime gameTime)
    {
        foreach (var child in _children)
        {
            child.Draw(gameTime);
        }
    }

}

