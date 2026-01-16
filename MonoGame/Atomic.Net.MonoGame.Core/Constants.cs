namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Global engine constants.
/// </summary>
public static class Constants
{
#if MAX_ENTITIES
    /// <summary>
    /// Maximum number of entities supported.
    /// </summary>
    public const ushort MaxEntities = MAX_ENTITIES;
#else
    /// <summary>
    /// Maximum number of entities supported.
    /// </summary>
    public const ushort MaxEntities = 512;
#endif

    /// <summary>
    /// Partition point between loading and scene entities.
    /// Entities with indices &lt; MaxLoadingEntities are loading entities.
    /// Entities with indices &gt;= MaxLoadingEntities are scene entities.
    /// </summary>
    public const ushort MaxLoadingEntities = MaxEntities / 2;
}

