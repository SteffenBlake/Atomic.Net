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
    public void MapOperation_AccessingIndexProperty_ShouldReturnValue()
    {
        // senior-dev: FINDING: { "var": "_index" } inside map object literal returns JsonObject, not value!
        // This proves the blocker - JsonLogic doesn't evaluate nested var operations in object literals
        JsonNode? rule = JsonNode.Parse("""
        {
          "map": [
            [{"_index": 1, "value": 10}, {"_index": 2, "value": 20}],
            {
              "idx": {"var": "_index"},
              "val": {"var": "value"}
            }
          ]
        }
        """);

        JsonNode? result = JsonLogic.Apply(rule, JsonNode.Parse("{}"));
        
        Assert.NotNull(result);
        Console.WriteLine($"Result: {result}");
        
        // senior-dev: Check if result is array with proper structure
        if (result is JsonArray arr)
        {
            Console.WriteLine($"Array length: {arr.Count}");
            if (arr.Count > 0 && arr[0] is JsonObject obj)
            {
                Console.WriteLine($"First element idx type: {obj["idx"]?.GetType().Name}");
                Console.WriteLine($"First element idx value: {obj["idx"]}");
                
                // senior-dev: Confirmed - returns JsonObject with the operation, not the value!
                Assert.IsType<JsonObject>(obj["idx"]);
            }
        }
    }

    [Fact]
    public void MapOperation_UsingMergeForPropertiesWorks()
    {
        // senior-dev: Testing if merge inside map can create proper objects with evaluated values
        JsonNode? rule = JsonNode.Parse("""
        {
          "map": [
            [{"_index": 1, "value": 10}, {"_index": 2, "value": 20}],
            {
              "merge": [
                {"_index": {"var": "_index"}},
                {"doubled": {"*": [{"var": "value"}, 2]}}
              ]
            }
          ]
        }
        """);

        JsonNode? result = JsonLogic.Apply(rule, JsonNode.Parse("{}"));
        
        Assert.NotNull(result);
        Console.WriteLine($"Result: {result}");
        
        if (result is JsonArray arr && arr.Count > 0)
        {
            Console.WriteLine($"First element: {arr[0]}");
            if (arr[0] is JsonArray mergeResult)
            {
                Console.WriteLine($"Merge returned array with {mergeResult.Count} elements");
            }
            else if (arr[0] is JsonObject obj)
            {
                Console.WriteLine($"Has _index: {obj.ContainsKey("_index")}");
                Console.WriteLine($"_index type: {obj["_index"]?.GetType().Name}");
                Console.WriteLine($"_index value: {obj["_index"]}");
            }
        }
    }

    [Fact]
    public void MapOperation_WithNestedReduceEvaluates()
    {
        // senior-dev: Testing if reduce operations inside map object literals get evaluated
        JsonNode? rule = JsonNode.Parse("""
        {
          "map": [
            [{"val": 10}, {"val": 20}],
            {
              "total": {
                "reduce": [
                  [{"val": 10}, {"val": 20}],
                  {"+": [{"var": "accumulator"}, {"var": "current.val"}]},
                  0
                ]
              }
            }
          ]
        }
        """);

        JsonNode? result = JsonLogic.Apply(rule, JsonNode.Parse("{}"));
        
        Assert.NotNull(result);
        Console.WriteLine($"Result: {result}");
        
        if (result is JsonArray arr && arr.Count > 0 && arr[0] is JsonObject obj)
        {
            Console.WriteLine($"First element total: {obj["total"]}");
            Console.WriteLine($"First element total type: {obj["total"]?.GetType().Name}");
        }
    }
}
