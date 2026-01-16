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
}

