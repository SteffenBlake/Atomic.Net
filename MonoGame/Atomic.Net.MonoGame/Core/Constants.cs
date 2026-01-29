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
    public const ushort MaxEntities = 8192;
#endif

#if MAX_GLOBAL_ENTITIES
    /// <summary>
    /// Partition point between global and scene entities.
    /// Entities with indices less than MaxGlobalEntities are global entities.
    /// Entities with indices greater than or equal to MaxGlobalEntities are scene entities.
    /// </summary>
    public const ushort MaxGlobalEntities = MAX_GLOBAL_ENTITIES;
#else
    /// <summary>
    /// Partition point between global and scene entities.
    /// Entities with indices less than MaxGlobalEntities are global entities.
    /// Entities with indices greater than or equal to MaxGlobalEntities are scene entities.
    /// </summary>
    public const ushort MaxGlobalEntities = 256;
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

#if MAX_RULES
    /// <summary>
    /// Maximum number of rules supported.
    /// </summary>
    public const ushort MaxRules = MAX_RULES;
#else
    /// <summary>
    /// Maximum number of rules supported.
    /// </summary>
    public const ushort MaxRules = 1024;
#endif

#if MAX_GLOBAL_RULES
    /// <summary>
    /// Partition point between global and scene rules.
    /// Rules with indices less than MaxGlobalRules are global rules.
    /// Rules with indices greater than or equal to MaxGlobalRules are scene rules.
    /// </summary>
    public const ushort MaxGlobalRules = MAX_GLOBAL_RULES;
#else
    /// <summary>
    /// Partition point between global and scene rules.
    /// Rules with indices less than MaxGlobalRules are global rules.
    /// Rules with indices greater than or equal to MaxGlobalRules are scene rules.
    /// </summary>
    public const ushort MaxGlobalRules = 128;
#endif

#if MAX_SEQUENCES
    /// <summary>
    /// Maximum number of sequences supported.
    /// </summary>
    public const ushort MaxSequences = MAX_SEQUENCES;
#else
    /// <summary>
    /// Maximum number of sequences supported.
    /// </summary>
    public const ushort MaxSequences = 512;
#endif

#if MAX_GLOBAL_SEQUENCES
    /// <summary>
    /// Partition point between global and scene sequences.
    /// Sequences with indices less than MaxGlobalSequences are global sequences.
    /// Sequences with indices greater than or equal to MaxGlobalSequences are scene sequences.
    /// </summary>
    public const ushort MaxGlobalSequences = MAX_GLOBAL_SEQUENCES;
#else
    /// <summary>
    /// Partition point between global and scene sequences.
    /// Sequences with indices less than MaxGlobalSequences are global sequences.
    /// Sequences with indices greater than or equal to MaxGlobalSequences are scene sequences.
    /// </summary>
    public const ushort MaxGlobalSequences = 256;
#endif
}

