using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Scenes.JsonConverters;

namespace Atomic.Net.MonoGame.Scenes.JsonModels;

[JsonConverter(typeof(EntitySelectorConverter))]
public struct EntitySelector()
{
    public string? ById { get; set; }= null;

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
