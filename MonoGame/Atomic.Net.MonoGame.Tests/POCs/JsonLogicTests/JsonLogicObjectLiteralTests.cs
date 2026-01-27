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
}
