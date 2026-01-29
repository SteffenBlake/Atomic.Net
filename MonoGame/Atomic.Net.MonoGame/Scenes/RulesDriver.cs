using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Atomic.Net.MonoGame.BED;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Scenes;

/// <summary>
/// Executes all active rules in a single frame.
/// Processes global and scene rules in SparseArray order.
/// </summary>
public sealed class RulesDriver : 
    ISingleton<RulesDriver>,
    IEventHandler<UpdateFrameEvent>
{
    private readonly JsonObject _ruleContext = new()
    {
        ["world"] = new JsonObject()
        {
            ["deltaTime"] = 0.0f
        },
        ["entities"] = new JsonArray()
    };

    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance = new();
        EventBus<UpdateFrameEvent>.Register(Instance);
    }

    public static RulesDriver Instance { get; private set; } = null!;

    /// <summary>
    /// Handles update frame event by running all rules.
    /// </summary>
    public void OnEvent(UpdateFrameEvent e)
    {
        RunFrame((float)e.Elapsed.TotalSeconds);
    }

    /// <summary>
    /// Executes all active rules for a single frame.
    /// Mutates entities based on WHERE filtering and DO mutation operations.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds</param>
    public void RunFrame(float deltaTime)
    {
        foreach (var (_, rule) in RuleRegistry.Instance.Rules)
        {
            ProcessRule(rule, deltaTime);
        }
    }

    /// <summary>
    /// Processes a single rule: get selector matches, serialize those entities, evaluate WHERE, execute DO mutations.
    /// </summary>
    private void ProcessRule(JsonRule rule, float deltaTime)
    {
        var selectorMatches = rule.From.Matches;
        _ruleContext["world"]!["deltaTime"] = deltaTime;
        _ruleContext["index"] = -1;
        BuildEntitiesArray(selectorMatches);

        if (!TryApplyJsonLogic(rule.Where, _ruleContext, out var filteredResult))
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                "Failed to evaluate WHERE clause"
            ));
            return;
        }

        if (filteredResult is not JsonArray filteredEntities)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"WHERE clause did not return JsonArray (got: {filteredResult?.GetType().Name ?? "null"})"
            ));
            return;
        }

        // Process command for each filtered entity
        for (var i = 0; i < filteredEntities.Count; i++)
        {
            var entityJson = filteredEntities[i];
            // JsonLogic filter can return null entries in sparse results
            if (entityJson == null)
            {
                continue;
            }

            if (!TryGetEntityIndex(entityJson, out var entityIndex))
            {
                continue;
            }

            _ruleContext["index"] = i;
            
            // Execute command on this entity
            ProcessCommand(rule.Do, entityIndex, entityJson, _ruleContext);
        }
    }

    /// <summary>
    /// Processes a command on an entity.
    /// All commands are delegated to SceneCommandDriver.
    /// </summary>
    private void ProcessCommand(SceneCommand command, ushort entityIndex, JsonNode entityJson, JsonObject ruleContext)
    {
        // Execute the command using SceneCommandDriver
        SceneCommandDriver.Execute(command, ruleContext, entityJson, entityIndex);
        
        // For MutCommand, we also need to write the mutated entityJson back to the actual entity
        if (command.TryMatch(out MutCommand _))
        {
            var entity = EntityRegistry.Instance[entityIndex];
            JsonEntityConverter.Write(entityJson, entity);
        }
    }

    /// <summary>
    /// Serializes entities that match the selector into a JsonArray.
    /// Delegates to JsonEntityConverter.Read for entity serialization to avoid code duplication.
    /// 
    /// senior-dev: KNOWN PERFORMANCE ISSUE - This method allocates JsonObject instances per entity per frame.
    /// This is required for JsonLogic evaluation but violates zero-alloc principle.
    /// 
    /// TODO: Investigate alternatives:
    /// 1. Object pooling for JsonObject instances
    /// 2. Custom JsonLogic evaluator that works directly with entity data
    /// 3. Cache serialized entities and only update on mutation
    /// 
    /// Estimated allocation: ~1-2 KB per entity per frame (100 entities = 100-200 KB/frame at 60 FPS = 6-12 MB/s GC pressure)
    /// </summary>
    private void BuildEntitiesArray(SparseArray<bool> selectorMatches)
    {
        var entities = (JsonArray)_ruleContext["entities"]!;
        entities.Clear();
        foreach (var (entityIndex, _) in selectorMatches)
        {
            entities.Add(JsonEntityConverter.Read(entityIndex));
        }
    }

    /// <summary>
    /// Extracts the _index property from entity JSON.
    /// </summary>
    private static bool TryGetEntityIndex(
        JsonNode entityJson,
        out ushort entityIndex
    )
    {
        if (entityJson is not JsonObject entityObj)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                "Entity JSON is not a JsonObject"
            ));
            entityIndex = 0;
            return false;
        }

        if (!entityObj.TryGetPropertyValue("_index", out var indexNode) || indexNode == null)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                "Entity missing _index property"
            ));
            entityIndex = 0;
            return false;
        }

        try
        {
            // Entity index is ushort, but JsonLogic may serialize as int
            if (indexNode is JsonValue jsonValue && 
                jsonValue.TryGetValue<ushort>(out var ushortValue)
            )
            {
                entityIndex = ushortValue;
                return true;
            }

            var indexValue = indexNode.GetValue<int>();
            if (indexValue < 0 || indexValue >= Constants.MaxEntities)
            {
                EventBus<ErrorEvent>.Push(new ErrorEvent(
                    $"Entity _index {indexValue} out of bounds (max: {Constants.MaxEntities})"
                ));
                entityIndex = 0;
                return false;
            }

            entityIndex = (ushort)indexValue;
            return true;
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent(
                $"Failed to parse _index: {ex.Message}"
            ));
            entityIndex = 0;
            return false;
        }
    }

    /// <summary>
    /// Applies JsonLogic rules to data.
    /// </summary>
    private static bool TryApplyJsonLogic(
        JsonNode? rule,
        JsonNode? data,
        [NotNullWhen(true)]
        out JsonNode? result
    )
    {
        if (rule == null)
        {
            result = null;
            return false;
        }

        try
        {
            result = Json.Logic.JsonLogic.Apply(rule, data);
            return result != null;
        }
        catch (Exception ex)
        {
            EventBus<ErrorEvent>.Push(new ErrorEvent($"JsonLogic evaluation failed: {ex.Message}"));
            result = null;
            return false;
        }
    }
}
