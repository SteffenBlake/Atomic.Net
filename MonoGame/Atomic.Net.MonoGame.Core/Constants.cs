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

#if MAX_LOADING_ENTITIES
    /// <summary>
    /// Partition point between loading and scene entities.
    /// Entities with indices less than MaxLoadingEntities are loading entities.
    /// Entities with indices greater than or equal to MaxLoadingEntities are scene entities.
    /// </summary>
    public const ushort MaxLoadingEntities = MAX_LOADING_ENTITIES;
#else
    /// <summary>
    /// Partition point between loading and scene entities.
    /// Entities with indices less than MaxLoadingEntities are loading entities.
    /// Entities with indices greater than or equal to MaxLoadingEntities are scene entities.
    /// </summary>
    public const ushort MaxLoadingEntities = 32;
#endif
}

