using System.Text.Json.Nodes;
using Json.Logic;
using Xunit;

namespace Atomic.Net.MonoGame.Tests.POCs.JsonLogicTests;

public class JsonLogicObjectLiteralTests
{
    [Fact]
    public void MergeWithObjectLiteral_ShouldWork()
    {
        JsonNode? rule = JsonNode.Parse("""
        {
          "merge": [
            { "var": "" },
            { "newProp": "value" }
          ]
        }
        """);

        JsonNode? data = JsonNode.Parse("""
        {
          "existingProp": "oldValue"
        }
        """);

        JsonNode? result = JsonLogic.Apply(rule, data);
        
        Assert.NotNull(result);
        Console.WriteLine($"Result: {result}");
    }

    [Fact]
    public void CanonicalExample_HealthMutation_ShouldWork()
    {
        JsonNode? rule = JsonNode.Parse("""
        {
          "merge": [
            { "var": "" },
            {
              "properties": {
                "health": 95
              }
            }
          ]
        }
        """);

        JsonNode? data = JsonNode.Parse("""
        {
          "properties": {
            "health": 100
          }
        }
        """);

        JsonNode? result = JsonLogic.Apply(rule, data);
        
        Assert.NotNull(result);
        Console.WriteLine($"Result: {result}");
    }

    [Fact]
    public void SimpleVarAccess_ShouldWork()
    {
        JsonNode? rule = JsonNode.Parse("""
        {
          "var": "properties.health"
        }
        """);

        JsonNode? data = JsonNode.Parse("""
        {
          "properties": {
            "health": 100
          }
        }
        """);

        JsonNode? result = JsonLogic.Apply(rule, data);
        
        Assert.NotNull(result);
        Assert.Equal(100, result?.GetValue<int>());
        Console.WriteLine($"Result: {result}");
    }

    [Fact]
    public void MergeWithOnlyVars_NoObjectLiterals_ShouldWork()
    {
        JsonNode? rule = JsonNode.Parse("""
        {
          "merge": [
            { "var": "" },
            { "var": "" }
          ]
        }
        """);

        JsonNode? data = JsonNode.Parse("""
        {
          "properties": {
            "health": 100
          }
        }
        """);

        JsonNode? result = JsonLogic.Apply(rule, data);
        
        Assert.NotNull(result);
        Console.WriteLine($"Result: {result}");
    }

    [Fact]
    public void FilterOperation_WithObjectLiteral_ShouldWork()
    {
        JsonNode? rule = JsonNode.Parse("""
        {
          "filter": [
            { "var": "entities" },
            { ">": [{ "var": "properties.health" }, 0] }
          ]
        }
        """);

        JsonNode? data = JsonNode.Parse("""
        {
          "entities": [
            { "properties": { "health": 100 } },
            { "properties": { "health": 0 } },
            { "properties": { "health": 50 } }
          ]
        }
        """);

        JsonNode? result = JsonLogic.Apply(rule, data);
        
        Assert.NotNull(result);
        Console.WriteLine($"Result: {result}");
    }

    [Fact]
    public void MapOperation_WithObjectLiteral_ShouldWork()
    {
        JsonNode? rule = JsonNode.Parse("""
        {
          "map": [
            { "var": "entities" },
            {
              "merge": [
                { "var": "" },
                { "modified": true }
              ]
            }
          ]
        }
        """);

        JsonNode? data = JsonNode.Parse("""
        {
          "entities": [
            { "id": "e1", "value": 10 },
            { "id": "e2", "value": 20 }
          ]
        }
        """);

        JsonNode? result = JsonLogic.Apply(rule, data);
        
        Assert.NotNull(result);
        Console.WriteLine($"Result: {result}");
    }

    [Fact]
    public void ReduceOperation_WithObjectLiteral_ShouldWork()
    {
        JsonNode? rule = JsonNode.Parse("""
        {
          "reduce": [
            { "var": "entities" },
            { "+": [{ "var": "accumulator" }, { "var": "current.properties.health" }] },
            0
          ]
        }
        """);

        JsonNode? data = JsonNode.Parse("""
        {
          "entities": [
            { "properties": { "health": 100 } },
            { "properties": { "health": 200 } },
            { "properties": { "health": 50 } }
          ]
        }
        """);

        JsonNode? result = JsonLogic.Apply(rule, data);
        
        Assert.NotNull(result);
        Assert.Equal(350m, result?.GetValue<decimal>());
        Console.WriteLine($"Result: {result}");
    }

    [Fact]
    public void DO_Mutations_BurnDamageOverTime_AppliesCorrectly()
    {
        // Pre-allocated buffers (zero allocations during test execution)
        var results = new List<JsonObject>(capacity: 2); // Pre-sized to expected result count
        
        // World context: deltaTime + entities with burn damage
        var worldData = JsonNode.Parse("""
        {
          "world": { "deltaTime": 0.5 },
          "entities": [
            { "_index": 1, "id": "goblin", "tags": ["burning"], "properties": { "health": 100, "burnDPS": 10 } },
            { "_index": 2, "id": "orc", "tags": ["burning"], "properties": { "health": 200, "burnDPS": 5 } }
          ]
        }
        """);

        // WHERE: Filter burning entities
        var whereRule = JsonNode.Parse("""
        {
          "filter": [
            { "var": "entities" },
            { ">": [{ "var": "properties.burnDPS" }, 0] }
          ]
        }
        """);

        var filteredEntities = JsonLogic.Apply(whereRule, worldData);
        Assert.NotNull(filteredEntities);
        var filtered = filteredEntities as JsonArray;
        Assert.NotNull(filtered);
        Assert.Equal(2, filtered.Count);

        // DO: Mutation definition
        var doMutations = JsonNode.Parse("""
        {
          "mut": [
            {
              "target": { "properties": "health" },
              "value": { "-": [{ "var": "self.properties.health" }, { "*": [{ "var": "self.properties.burnDPS" }, { "var": "world.deltaTime" }] }] }
            }
          ]
        }
        """);

        var mutations = doMutations!["mut"] as JsonArray;
        Assert.NotNull(mutations);

        // Simulate RulesDriver processing each entity
        // CRITICAL: Reuse context object, don't allocate per-entity
        var context = new JsonObject();
        context["world"] = worldData!["world"]!.DeepClone();
        context["entities"] = filtered.DeepClone();
        
        for (var i = 0; i < filtered.Count; i++)
        {
            var entity = filtered[i]!;
            
            // Update context (reuse, don't reallocate)
            context["self"] = entity.DeepClone();

            // Clone entity for mutation
            var mutatedEntity = entity.DeepClone() as JsonObject;
            Assert.NotNull(mutatedEntity);
            
            // Process each mutation
            foreach(var mutation in mutations)
            {
                var mutationObj = mutation as JsonObject;
                Assert.NotNull(mutationObj);

                var target = mutationObj["target"];
                var valueRule = mutationObj["value"];

                // Evaluate the value using JsonLogic with self context
                var computedValue = JsonLogic.Apply(valueRule, context);
                
                Console.WriteLine($"Entity {mutatedEntity["_index"]}: computed value = {computedValue}");
                
                // Navigate target path and set value
                var targetObj = target as JsonObject;
                Assert.NotNull(targetObj);
                var parentKey = targetObj.First().Key; // "properties"
                var leafKey = (string)targetObj.First().Value!; // "health"
                
                var parentNode = mutatedEntity[parentKey] as JsonObject;
                Assert.NotNull(parentNode);
                parentNode[leafKey] = computedValue;
            }

            results.Add(mutatedEntity);
        }

        // Assert results
        Assert.Equal(2, results.Count);
        
        // Goblin: 100 - (10 * 0.5) = 95
        Assert.Equal(1, results[0]["_index"]!.GetValue<int>());
        Assert.Equal(95m, results[0]["properties"]!["health"]!.GetValue<decimal>());
        
        // Orc: 200 - (5 * 0.5) = 197.5
        Assert.Equal(2, results[1]["_index"]!.GetValue<int>());
        Assert.Equal(197.5m, results[1]["properties"]!["health"]!.GetValue<decimal>());
    }

    [Fact]
    public void Apply_ArrayIndexingWithConcatenatedPath_ReturnsCorrectValue()
    {
        // Arrange
        var entities = new JsonArray
        {
            new JsonObject { ["health"] = 100 },
            new JsonObject { ["health"] = 50 },
            new JsonObject { ["health"] = 75 }
        };
        
        var context = new JsonObject
        {
            ["entities"] = entities,
            ["index"] = 1
        };
        
        var rule = new JsonObject
        {
            ["var"] = new JsonObject
            {
                ["cat"] = new JsonArray
                {
                    "entities.",
                    new JsonObject { ["var"] = "index" },
                    ".health"
                }
            }
        };
        
        // Act
        var result = JsonLogic.Apply(rule, context);
        
        // Assert
        Assert.NotNull(result);
        var value = result as JsonValue;
        Assert.True(value is not null, $"Expected JsonValue, actually got: {result.ToJsonString()}");
        var actual = value.GetValue<int>();
        Assert.True(actual == 50, $"Expected 50, got {actual}");
    }
}
