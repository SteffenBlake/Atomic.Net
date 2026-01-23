using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using dotVariant;

namespace Atomic.Net.MonoGame.Scenes;

// TODO : This needs to now be modified by @senior-dev to use the new 
// V2 architecture described below

// Note: this should be backwards compatible with how it interacts with the parent behavior
// it is intentional that we re-use this for the Parent Behavior!
//
// Once fixes are done, the old EntitySelector should be deleted and
// EntitySelectorV2 should just be renamed to EntitySelector, replacing the old
// There should NOT be anything named "EntitySelectorV2" or etc in the codebase
// Once the work is done by senior-dev

[JsonConverter(typeof(EntitySelectorConverter))]
public readonly record struct EntitySelector(
    string? ById = null
)
{
    // @senior-dev: This should now be deleted if migrate the code over right
    public readonly bool TryLocate(
        [NotNullWhen(true)]
        out Entity? entity
    )
    {
        if (!string.IsNullOrEmpty(ById))
        {
            if (EntityIdRegistry.Instance.TryResolve(ById, out entity))
            {
                return true;
            }

            EventBus<ErrorEvent>.Push(
                new($"Unresolved reference: #{ById}")
            );
            return false;
        }

        entity = null;
        return false;
    }
}

// These objects will likely need to get used together to produce the new
// enhanced Entity Selector
// You will need to delete the old one and replace it with this once
// You have completed the work

[Variant]
public partial class EntitySelectorV2
{
    static partial void VariantOf(
        UnionEntitySelector union,
        IdEntitySelector id,
        TaggedEntitySelector tagged,
        CollisionEnterEntitySelector collisionEnter,
        CollisionExitEntitySelector collisionExit
    );

    // @senior-dev: This will be the new method you will use to match on entities instead
    public bool Matches(Entity entity)
    {
        return Visit(
            union => union.Matches(entity),
            id => id.Matches(entity),
            tagged => tagged.Matches(entity),
            collisionEnter => collisionEnter.Matches(entity),
            collisionExit => collisionExit.Matches(entity),
            () => false
        );
    }

    // Important notes: you can use a lot of tricks to very efficiently parse a ReadOnlySpan
    // SomeSpan[x..y] provides a non allocating "window" slice view of the span
    // Without copying the data, its the same memory, which is incredibly efficient
    // For quickly parsing over data
    //
    // You probably will want to use some combination of Dynamic Programming
    // + recursion here to produce an efficient output
    public static bool TryParse(
        ReadOnlySpan<char> tokens, out EntitySelectorV2? entitySelector
    )
    {
        throw new NotImplementedException("To be implemented by @senior-dev");
    }
}

public readonly record struct UnionEntitySelector(
    EntitySelectorV2[] Children
)
{
    public bool Matches(Entity entity)
    {
        for (var n = 0; n < Children.Length; n++)
        {
            if (Children[n].Matches(entity))
            {
                return true;
            }
        }

        return false;
    }
}

public readonly record struct IdEntitySelector(
    string Id, EntitySelectorV2? Next = null
)
{
    public bool Matches(Entity entity)
    {
        if (!EntityIdRegistry.Instance.TryResolve(Id, out var match))
        {
            return false;
        }

        if (match.Value.Index != entity.Index)
        {
            return false;
        }
    
        return Next?.Matches(entity) ?? true;
    }
}

public readonly record struct TaggedEntitySelector(
    string Tag, EntitySelectorV2? Next = null
)
{
    public bool Matches(Entity entity)
    {
        throw new NotImplementedException(
            "TODO: future work, requires TagsBehavior to be implemented first"
        );

        // if (!EntityTagsRegistry.Instance.HasTag(entity, Tag))
        // {
        //     return false;
        // }
        //
        // return Next?.Matches(entity) ?? true;
    }
}


public readonly record struct CollisionEnterEntitySelector(
    EntitySelectorV2? Next = null
)
{
    public bool Matches(Entity entity)
    {
        throw new NotImplementedException(
            "TODO: future work, requires RigidBodyBehavior to be implemented first"
        );

        // if (!CollisionRegistry.Instance.CollisionEntered(entity))
        // {
        //     return false;
        // }
        //
        // return Next?.Matches(entity) ?? true;
    }
}

public readonly record struct CollisionExitEntitySelector(
    EntitySelectorV2? Next = null
)
{
    public bool Matches(Entity entity)
    {
        throw new NotImplementedException(
            "TODO: future work, requires RigidBodyBehavior to be implemented first"
        );

        // if (!CollisionRegistry.Instance.CollisionExited(entity))
        // {
        //     return false;
        // }
        //
        // return Next?.Matches(entity) ?? true;
    }
}
