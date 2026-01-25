using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;
using dotVariant;

namespace Atomic.Net.MonoGame.Selectors;

// TODO : This needs to now be modified by @senior-dev to use the new 
// V2 architecture described below

// Note: this should be backwards compatible with how it interacts with the parent behavior
// it is intentional that we re-use this for the Parent Behavior!
//
// Once fixes are done, the old EntitySelector should be deleted and
// EntitySelectorV2 should just be renamed to EntitySelector, replacing the old
// There should NOT be anything named "EntitySelectorV2" or etc in the codebase
// Once the work is done by senior-dev

// These objects will likely need to get used together to produce the new
// enhanced Entity Selector
// You will need to delete the old one and replace it with this once
// You have completed the work
[Variant]
[JsonConverter(typeof(EntitySelectorConverter))]
public partial class EntitySelector
{
    static partial void VariantOf(
        UnionEntitySelector union,
        IdEntitySelector id,
        TagEntitySelector tag,
        CollisionEnterEntitySelector collisionEnter,
        CollisionExitEntitySelector collisionExit
    );

    // Important notes: you can use a lot of tricks to very efficiently parse a ReadOnlySpan
    // SomeSpan[x..y] provides a non allocating "window" slice view of the span
    // Without copying the data, its the same memory, which is incredibly efficient
    // For quickly parsing over data
    //
    // You probably will want to use some combination of Dynamic Programming
    // + recursion here to produce an efficient output
    public static bool TryParse(
        ReadOnlySpan<char> tokens, 
        [NotNullWhen(true)]
        out EntitySelector? entitySelector
    )
    {
        throw new NotImplementedException("To be implemented by @senior-dev");
    }

    public void MarkDirty()
    {
        Visit(
            static union => union.MarkDirty(),
            static id => id.MarkDirty(),
            static tag => tag.MarkDirty(),
            static collisionEnter => collisionEnter.MarkDirty(),
            static collisionExit => collisionExit.MarkDirty(),
            static () => {}
        );
    }

    public bool Recalc()
    {
        return Visit(
            static union => union.Recalc(),
            static id => id.Recalc(),
            static tag => tag.Recalc(),
            static collisionEnter => collisionEnter.Recalc(),
            static collisionExit => collisionExit.Recalc(),
            static () => false
        );
    }

    public SparseArray<bool> Matches => Visit(
        static union => union.Matches,
        static id => id.Matches,
        static tag => tag.Matches,
        static collisionEnter => collisionEnter.Matches,
        static collisionExit => collisionExit.Matches
    );
}

public readonly record struct UnionEntitySelector(
    EntitySelector[] Children
)
{
    public SparseArray<bool> Matches => throw new NotImplementedException();

    public static bool TryParse(
        ReadOnlySpan<char> tokens, 
        [NotNullWhen(true)]
        out UnionEntitySelector? entitySelector
    )
    {
        throw new NotImplementedException("To be implemented by @senior-dev");
    }

    // This also needs to be implemented (and effectively do the reverse of TryParse)
    // Otherwise the database write passes wont succeed
    public override string ToString()
    {
        throw new NotImplementedException("To be implemented by @senior-dev");
    }

    public void MarkDirty()
    {
        throw new NotImplementedException("To be implemented by @senior-dev");
    }

    public readonly bool Recalc()
    {
        var shouldRecalc = false;
        for (var n = 0; n < Children.Length; n++)
        {
            shouldRecalc |= Children[n].Recalc();
        }

        if (shouldRecalc)
        {
            throw new NotImplementedException("To be implemented by @senior-dev");
        }

        return shouldRecalc;
    }
}

public record struct IdEntitySelector(
    string Id, EntitySelector? Prior = null
)
{
    public readonly SparseArray<bool> Matches = new(Constants.MaxEntities);

    private bool _dirty = true;

    public static bool TryParse(
        ReadOnlySpan<char> tokens, 
        [NotNullWhen(true)]
        out IdEntitySelector? entitySelector
    )
    {
        throw new NotImplementedException("To be implemented by @senior-dev");
    }
    
    // This also needs to be implemented (and effectively do the reverse of TryParse)
    // Otherwise the database write passes wont succeed
    public override string ToString()
    {
        throw new NotImplementedException("To be implemented by @senior-dev");
    }

    public void MarkDirty() => _dirty = true;

    public readonly bool Recalc()
    {
        var priorDirty = Prior?.Recalc() ?? false;
        var shouldRecalc = priorDirty || _dirty;

        if (shouldRecalc)
        {
            throw new NotImplementedException("To be implemented by @senior-dev");
        }

        return shouldRecalc;
    }
}

public struct TagEntitySelector(
    string Tag, EntitySelector? Prior = null
)
{
    public SparseArray<bool> Matches => throw new NotImplementedException("Requires Tags registry to be implemented later");

    private bool _dirty = true;

    public static bool TryParse(
        ReadOnlySpan<char> tokens, 
        [NotNullWhen(true)]
        out TagEntitySelector? entitySelector
    )
    {
        throw new NotImplementedException("To be implemented by @senior-dev");
    }
    
    // This also needs to be implemented (and effectively do the reverse of TryParse)
    // Otherwise the database write passes wont succeed
    public override string ToString()
    {
        throw new NotImplementedException("To be implemented by @senior-dev");
    }

    public void MarkDirty() => _dirty = true;

    public readonly bool Recalc()
    {
        var priorDirty = Prior?.Recalc() ?? false;
        var shouldRecalc = priorDirty || _dirty;

        if (shouldRecalc)
        {
            throw new NotImplementedException("Requires Tags registry to be implemented later");
        }

        return shouldRecalc;
    }
}


public record struct CollisionEnterEntitySelector(
    EntitySelector? Prior = null
)
{
    public readonly SparseArray<bool> Matches = new(Constants.MaxEntities);

    private bool _dirty = true;

    public static bool TryParse(
        ReadOnlySpan<char> tokens, 
        [NotNullWhen(true)]
        out CollisionEnterEntitySelector? entitySelector
    )
    {
        throw new NotImplementedException("To be implemented by @senior-dev");
    }
    
    public override string ToString()
    {
        throw new NotImplementedException("To be implemented by @senior-dev");
    }

    public void MarkDirty() => _dirty = true;

    public readonly bool Recalc()
    {
        var priorDirty = Prior?.Recalc() ?? false;
        var shouldRecalc = priorDirty || _dirty;

        if (shouldRecalc)
        {
            throw new NotImplementedException("Requires Collision registry to be implemented later");
        }

        return shouldRecalc;
    }
}

public record struct CollisionExitEntitySelector(
    EntitySelector? Prior = null
)
{
    public readonly SparseArray<bool> Matches = new(Constants.MaxEntities);

    private bool _dirty = true;

    public static bool TryParse(
        ReadOnlySpan<char> tokens, 
        [NotNullWhen(true)]
        out CollisionExitEntitySelector? entitySelector
    )
    {
        throw new NotImplementedException("To be implemented by @senior-dev");
    }
    
    public override string ToString()
    {
        throw new NotImplementedException("To be implemented by @senior-dev");
    }

    public void MarkDirty() => _dirty = true;

    public readonly bool Recalc()
    {
        var priorDirty = Prior?.Recalc() ?? false;
        var shouldRecalc = priorDirty || _dirty;

        if (shouldRecalc)
        {
            throw new NotImplementedException("Requires Collision registry to be implemented later");
        }

        return shouldRecalc;
    }
    
}
