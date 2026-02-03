namespace Atomic.Net.MonoGame.Core;

/// <summary>
/// Global engine constants.
/// </summary>
public static class Constants
{
#if MAX_GLOBAL_ENTITIES
    /// <summary>
    /// Maximum number of global entities supported (partition 0).
    /// Global entities persist across scene resets.
    /// </summary>
    public const ushort MaxGlobalEntities = MAX_GLOBAL_ENTITIES;
#else
    /// <summary>
    /// Maximum number of global entities supported (partition 0).
    /// Global entities persist across scene resets.
    /// </summary>
    public const ushort MaxGlobalEntities = 256;
#endif

#if MAX_SCENE_ENTITIES
    /// <summary>
    /// Maximum number of scene entities supported (partition 1).
    /// Scene entities are cleared on scene reset.
    /// </summary>
    public const uint MaxSceneEntities = MAX_SCENE_ENTITIES;
#else
    /// <summary>
    /// Maximum number of scene entities supported (partition 1).
    /// Scene entities are cleared on scene reset.
    /// </summary>
    public const uint MaxSceneEntities = 8192;
#endif

#if DEFAULT_ALLOC_PROPERTYBAG 
    /// <summary>
    /// Default allocation size for PropertyBag sparse arrays.
    /// </summary>
    public const ushort DefaultAllocPropertyBag = DEFAULT_ALLOC_PROPERTYBAG;
#else
    /// <summary>
    /// Default allocation size for PropertyBag sparse arrays.
    /// </summary>
    public const ushort DefaultAllocPropertyBag = 32;
#endif

#if ATOMIC_PERSISTENCE_DB_PATH
    /// <summary>
    /// Default path for LiteDB persistence database.
    /// Can be overridden at compile time via -p:DefineConstants=ATOMIC_PERSISTENCE_DB_PATH="path"
    /// </summary>
    public const string DefaultPersistenceDatabasePath = ATOMIC_PERSISTENCE_DB_PATH;
#else
    /// <summary>
    /// Default path for LiteDB persistence database.
    /// Can be overridden at compile time via -p:DefineConstants=ATOMIC_PERSISTENCE_DB_PATH="path"
    /// </summary>
    public const string DefaultPersistenceDatabasePath = "persistence.db";
#endif

#if MAX_GLOBAL_RULES
    /// <summary>
    /// Maximum number of global rules supported (partition 0).
    /// Global rules persist across scene resets.
    /// </summary>
    public const ushort MaxGlobalRules = MAX_GLOBAL_RULES;
#else
    /// <summary>
    /// Maximum number of global rules supported (partition 0).
    /// Global rules persist across scene resets.
    /// </summary>
    public const ushort MaxGlobalRules = 128;
#endif

#if MAX_SCENE_RULES
    /// <summary>
    /// Maximum number of scene rules supported (partition 1).
    /// Scene rules are cleared on scene reset.
    /// </summary>
    public const uint MaxSceneRules = MAX_SCENE_RULES;
#else
    /// <summary>
    /// Maximum number of scene rules supported (partition 1).
    /// Scene rules are cleared on scene reset.
    /// </summary>
    public const uint MaxSceneRules = 1024;
#endif

#if MAX_GLOBAL_SEQUENCES
    /// <summary>
    /// Maximum number of global sequences supported (partition 0).
    /// Global sequences persist across scene resets.
    /// </summary>
    public const ushort MaxGlobalSequences = MAX_GLOBAL_SEQUENCES;
#else
    /// <summary>
    /// Maximum number of global sequences supported (partition 0).
    /// Global sequences persist across scene resets.
    /// </summary>
    public const ushort MaxGlobalSequences = 256;
#endif

#if MAX_SCENE_SEQUENCES
    /// <summary>
    /// Maximum number of scene sequences supported (partition 1).
    /// Scene sequences are cleared on scene reset.
    /// </summary>
    public const uint MaxSceneSequences = MAX_SCENE_SEQUENCES;
#else
    /// <summary>
    /// Maximum number of scene sequences supported (partition 1).
    /// Scene sequences are cleared on scene reset.
    /// </summary>
    public const uint MaxSceneSequences = 512;
#endif


}

