using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.Core;
using dotVariant;

namespace Atomic.Net.MonoGame.Selectors;

// senior-dev: EntitySelector variant type for Query+Command system (Stage 1: parsing only)
// Supports: Union (,), Id (@), Tag (#), CollisionEnter (!enter), CollisionExit (!exit)
// Refinement chains built right-to-left: "!enter:#enemies" → CollisionEnter(Prior: Tag("enemies"))
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
        // senior-dev: Delegate to the variant's WriteTo method
        if (TryMatch(out UnionEntitySelector? union))
        {
            union.WriteTo(stringBuilder);
        }
        else if (TryMatch(out IdEntitySelector? id))
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
