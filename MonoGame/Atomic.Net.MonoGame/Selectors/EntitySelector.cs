using System.Diagnostics.CodeAnalysis;
using System.Text;
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

    internal void WriteTo(StringBuilder stringBuilder)
    {
        // To avoid closures we have to do this the long way
        if (TryMatch(out IdEntitySelector? id))
        {
            id.WriteTo(stringBuilder);
        }
        else if (TryMatch(out TagEntitySelector? tag))
        {
            tag.WriteTo(stringBuilder);
        }
        else if (TryMatch(out CollisionEnterEntitySelector? collisionEnter))
        {
            collisionEnter.WriteTo(stringBuilder);
        }
        else if (TryMatch(out CollisionExitEntitySelector? collisionExit))
        {
            collisionExit.WriteTo(stringBuilder);
        }
    }
}
