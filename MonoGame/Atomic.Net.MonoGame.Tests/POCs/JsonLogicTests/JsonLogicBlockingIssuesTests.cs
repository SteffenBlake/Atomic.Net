using System.Text.Json.Nodes;
using Json.Logic;
using Xunit;
using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Tests.POCs.JsonLogicTests;

/// <summary>
/// senior-dev: CRITICAL DEMONSTRATION OF JSONLOGIC BLOCKING ISSUES
/// 
/// This test file demonstrates the exact blocking issues with JsonLogic when using the EXACT
/// JSON format specified in issue #50 and sprint-007-rules-driver.sprint.md.
/// 
/// ALL TESTS IN THIS FILE ARE EXPECTED TO FAIL. They demonstrate specific behaviors of JsonLogic
/// that prevent the RulesDriver from working as specified.
/// 
/// Each test:
/// 1. Uses the EXACT JSON format from the sprint specifications
/// 2. Documents EXPECTED behavior per the spec
/// 3. Documents ACTUAL behavior observed from JsonLogic
/// 4. Explains WHY this is a blocking problem
/// 5. Has verbose assert messages showing the issue
/// 
/// This file serves as documentation for investigating JsonLogic library behavior or finding
/// workarounds.
/// </summary>
[Collection("NonParallel")]
public class JsonLogicBlockingIssuesTests : IDisposable
{
    private readonly ErrorEventLogger _errorLogger;

    public JsonLogicBlockingIssuesTests()
    {
        // senior-dev: Enable error logging as requested to see any error events
        _errorLogger = new ErrorEventLogger();
    }

    public void Dispose()
    {
        _errorLogger?.Dispose();
    }

    [Fact]
    [Trait("Category", "POC")]
    public void Issue1_MergeOperationReturnsArrayInsteadOfMergedObject()
    {
        // senior-dev: ISSUE #1 - MERGE RETURNS ARRAY, NOT MERGED OBJECT
        //
        // SPRINT SPECIFICATION (from Example 1):
        // The DO clause should use map with merge to add properties to each entity:
        // {
        //   "map": [
        //     { "var": "entities" },
        //     {
        //       "merge": [
        //         { "var": "" },  // Current element (the entity)
        //         { "properties": { "totalEnemyHealth": 350 } }  // New properties to add
        //       ]
        //     }
        //   ]
        // }
        //
        // EXPECTED BEHAVIOR PER SPEC:
        // merge should combine the current entity ({ "var": "" }) with the new properties,
        // returning a SINGLE OBJECT with all properties from both objects merged together.
        // Expected result: { "_index": 1, "properties": { "health": 100, "totalEnemyHealth": 350 } }
        //
        // ACTUAL BEHAVIOR OBSERVED:
        // JsonLogic's merge operation returns an ARRAY containing the two objects separately,
        // NOT a merged single object.
        // Actual result: [{ "_index": 1, "properties": { "health": 100 } }, { "properties": { "totalEnemyHealth": 350 } }]
        //
        // WHY THIS IS BLOCKING:
        // The sprint spec requires mutations to be objects with _index properties so we can
        // map them back to entities. If merge returns an array, we can't extract _index,
        // and we can't apply the mutations to the correct entities.
        //
        // This is the fundamental blocker that prevents the entire RulesDriver from working
        // as specified.

        // Arrange: Create a simple entity as it would appear in the entities array
        JsonNode? data = JsonNode.Parse("""
        {
          "entities": [
            { "_index": 1, "properties": { "health": 100 } }
          ]
        }
        """);

        // Act: Use the EXACT merge pattern from the sprint specification
        JsonNode? rule = JsonNode.Parse("""
        {
          "map": [
            { "var": "entities" },
            {
              "merge": [
                { "var": "" },
                { "properties": { "totalEnemyHealth": 350 } }
              ]
            }
          ]
        }
        """);

        JsonNode? result = JsonLogic.Apply(rule, data);

        // Assert
        Assert.NotNull(result);
        Assert.True(result is JsonArray, "map should return an array");
        
        var arr = (JsonArray)result!;
        Assert.Single(arr); // One entity in, one result out
        
        var firstResult = arr[0];
        
        // senior-dev: This is where the issue becomes apparent
        // EXPECTED: firstResult should be a JsonObject like:
        //   { "_index": 1, "properties": { "health": 100, "totalEnemyHealth": 350 } }
        //
        // ACTUAL: firstResult is a JsonArray like:
        //   [{ "_index": 1, "properties": { "health": 100 } }, { "properties": { "totalEnemyHealth": 350 } }]
        
        Console.WriteLine($"Result type: {firstResult?.GetType().Name}");
        Console.WriteLine($"Result value: {firstResult}");
        
        Assert.True(
            firstResult is JsonObject,
            $"EXPECTED: merge to return a single merged JsonObject.\n" +
            $"ACTUAL: merge returned {firstResult?.GetType().Name}.\n" +
            $"VALUE: {firstResult}\n" +
            $"PROBLEM: Cannot extract _index from an array. Sprint spec requires mutations to be objects with _index properties."
        );
        
        // This assert will fail, demonstrating the issue
        var obj = firstResult as JsonObject;
        Assert.True(
            obj?.ContainsKey("_index") == true,
            $"EXPECTED: Merged object should contain _index property from original entity.\n" +
            $"ACTUAL: Result is {firstResult?.GetType().Name}, not a JsonObject.\n" +
            $"PROBLEM: Cannot map mutation back to entity without _index."
        );
    }

    [Fact]
    [Trait("Category", "POC")]
    public void Issue2_NestedJsonLogicOperationsInObjectLiteralsAreNotEvaluated()
    {
        // senior-dev: ISSUE #2 - NESTED OPERATIONS IN OBJECT LITERALS NOT EVALUATED
        //
        // SPRINT SPECIFICATION (from Example 1):
        // The DO clause should calculate totalEnemyHealth using reduce and add it as a property:
        // {
        //   "map": [
        //     { "var": "entities" },
        //     {
        //       "merge": [
        //         { "var": "" },
        //         {
        //           "properties": {
        //             "totalEnemyHealth": {
        //               "reduce": [
        //                 { "var": "entities" },
        //                 { "+": [{ "var": "accumulator" }, { "var": "current.properties.health" }] },
        //                 0
        //               ]
        //             }
        //           }
        //         }
        //       ]
        //     }
        //   ]
        // }
        //
        // EXPECTED BEHAVIOR PER SPEC:
        // The reduce operation should be EVALUATED, calculating the sum of all enemy health (350).
        // The result should be: { "properties": { "totalEnemyHealth": 350 } }
        // where 350 is a NUMBER, the result of the reduce calculation.
        //
        // ACTUAL BEHAVIOR OBSERVED:
        // JsonLogic does NOT evaluate nested operations inside object literals.
        // The reduce operation is returned AS-IS as a JsonObject, not evaluated to a number.
        // Actual result: { "properties": { "totalEnemyHealth": { "reduce": [...] } } }
        // where totalEnemyHealth is a JsonObject containing the operation, not the number 350.
        //
        // WHY THIS IS BLOCKING:
        // The sprint spec requires us to apply property values to entities. PropertyValue can only
        // store primitives (bool, string, float). We cannot store a JsonObject containing an operation.
        // Without evaluated values, we cannot update entity properties.

        // Arrange: Create entities with health values as specified in Example 1
        JsonNode? data = JsonNode.Parse("""
        {
          "entities": [
            { "_index": 1, "properties": { "health": 100 } },
            { "_index": 2, "properties": { "health": 200 } },
            { "_index": 3, "properties": { "health": 50 } }
          ]
        }
        """);

        // Act: Use just the properties mutation part (without the full map/merge) to isolate the issue
        JsonNode? rule = JsonNode.Parse("""
        {
          "map": [
            { "var": "entities" },
            {
              "properties": {
                "totalEnemyHealth": {
                  "reduce": [
                    { "var": "entities" },
                    { "+": [{ "var": "accumulator" }, { "var": "current.properties.health" }] },
                    0
                  ]
                }
              }
            }
          ]
        }
        """);

        JsonNode? result = JsonLogic.Apply(rule, data);

        // Assert
        Assert.NotNull(result);
        Assert.True(result is JsonArray, "map should return an array");
        
        var arr = (JsonArray)result!;
        Assert.Equal(3, arr.Count); // Three entities
        
        var firstResult = arr[0] as JsonObject;
        Assert.NotNull(firstResult);
        
        var properties = firstResult!["properties"] as JsonObject;
        Assert.NotNull(properties);
        
        var totalEnemyHealth = properties!["totalEnemyHealth"];
        
        // senior-dev: This is where the issue becomes apparent
        // EXPECTED: totalEnemyHealth should be a JsonValue containing the number 350
        // ACTUAL: totalEnemyHealth is a JsonObject containing the unevaluated reduce operation
        
        Console.WriteLine($"totalEnemyHealth type: {totalEnemyHealth?.GetType().Name}");
        Console.WriteLine($"totalEnemyHealth value: {totalEnemyHealth}");
        
        Assert.True(
            totalEnemyHealth is JsonValue,
            $"EXPECTED: totalEnemyHealth to be a JsonValue containing the evaluated number 350.\n" +
            $"ACTUAL: totalEnemyHealth is {totalEnemyHealth?.GetType().Name}.\n" +
            $"VALUE: {totalEnemyHealth}\n" +
            $"PROBLEM: Nested JsonLogic operations in object literals are not evaluated. " +
            $"Cannot apply non-primitive values to entity properties."
        );
        
        // This will fail because totalEnemyHealth is a JsonObject, not a JsonValue
        if (totalEnemyHealth is JsonValue jsonValue)
        {
            Assert.True(
                jsonValue.TryGetValue<int>(out var numValue) && numValue == 350,
                $"EXPECTED: totalEnemyHealth = 350 (sum of 100 + 200 + 50).\n" +
                $"ACTUAL: Could not extract numeric value.\n" +
                $"PROBLEM: reduce operation was not evaluated."
            );
        }
    }

    [Fact]
    [Trait("Category", "POC")]
    public void Issue3_VarOperationsInObjectLiteralsReturnOperationObject()
    {
        // senior-dev: ISSUE #3 - { "var": "property" } IN OBJECT LITERALS NOT EVALUATED
        //
        // SPRINT SPECIFICATION (Technical Requirements section):
        // Each mutation must include `_index` property to identify target entity.
        // The sprint spec shows using merge with { "var": "" } to preserve the current entity,
        // which should include its _index.
        //
        // However, when trying to explicitly include _index in the result like:
        // { "_index": { "var": "_index" } }
        //
        // EXPECTED BEHAVIOR PER SPEC:
        // { "var": "_index" } should be EVALUATED, extracting the _index value from the current entity.
        // Expected result: { "_index": 1 } where 1 is the actual index number.
        //
        // ACTUAL BEHAVIOR OBSERVED:
        // JsonLogic does NOT evaluate { "var": ... } operations inside object literals.
        // The operation is returned AS-IS as a JsonObject.
        // Actual result: { "_index": { "var": "_index" } }
        // where _index is a JsonObject containing the operation, not the number.
        //
        // WHY THIS IS BLOCKING:
        // The sprint spec requires extracting _index as a ushort to identify which entity to mutate.
        // If _index is a JsonObject instead of a number, we cannot extract it as a ushort,
        // and we cannot map mutations back to entities.

        // Arrange: Create an entity with _index as it would appear in the context
        JsonNode? data = JsonNode.Parse("""
        {
          "entities": [
            { "_index": 42, "properties": { "health": 100 } }
          ]
        }
        """);

        // Act: Try to build a mutation object that preserves _index
        JsonNode? rule = JsonNode.Parse("""
        {
          "map": [
            { "var": "entities" },
            {
              "_index": { "var": "_index" },
              "properties": {
                "newValue": 123
              }
            }
          ]
        }
        """);

        JsonNode? result = JsonLogic.Apply(rule, data);

        // Assert
        Assert.NotNull(result);
        Assert.True(result is JsonArray, "map should return an array");
        
        var arr = (JsonArray)result!;
        Assert.Single(arr);
        
        var mutation = arr[0] as JsonObject;
        Assert.NotNull(mutation);
        
        var indexNode = mutation!["_index"];
        
        // senior-dev: This is where the issue becomes apparent
        // EXPECTED: indexNode should be a JsonValue containing the number 42
        // ACTUAL: indexNode is a JsonObject containing the unevaluated { "var": "_index" } operation
        
        Console.WriteLine($"_index type: {indexNode?.GetType().Name}");
        Console.WriteLine($"_index value: {indexNode}");
        
        Assert.True(
            indexNode is JsonValue,
            $"EXPECTED: _index to be a JsonValue containing the number 42.\n" +
            $"ACTUAL: _index is {indexNode?.GetType().Name}.\n" +
            $"VALUE: {indexNode}\n" +
            $"PROBLEM: JsonLogic does not evaluate {{ \"var\": ... }} operations in object literals. " +
            $"Cannot extract _index as ushort to map mutation back to entity."
        );
        
        // This will fail because indexNode is a JsonObject, not a JsonValue
        if (indexNode is JsonValue jsonValue)
        {
            Assert.True(
                jsonValue.TryGetValue<ushort>(out var indexValue) && indexValue == 42,
                $"EXPECTED: _index = 42.\n" +
                $"ACTUAL: Could not extract ushort value.\n" +
                $"PROBLEM: var operation was not evaluated."
            );
        }
    }

    [Fact]
    [Trait("Category", "POC")]
    public void Issue4_CompleteExampleShowingAllIssuesTogether()
    {
        // senior-dev: ISSUE #4 - COMPLETE EXAMPLE SHOWING HOW ISSUES COMBINE TO BLOCK IMPLEMENTATION
        //
        // SPRINT SPECIFICATION (Example 1: Sum Aggregate):
        // This is the EXACT JSON from the sprint spec for calculating totalEnemyHealth.
        // All 3 enemies should get totalEnemyHealth: 350
        //
        // EXPECTED BEHAVIOR PER SPEC:
        // 1. map processes each entity
        // 2. merge combines current entity with new properties
        // 3. reduce calculates sum (350)
        // 4. Result is array of 3 objects, each with _index and totalEnemyHealth: 350
        //
        // Example expected result:
        // [
        //   { "_index": 0, "properties": { "health": 100, "totalEnemyHealth": 350 } },
        //   { "_index": 1, "properties": { "health": 200, "totalEnemyHealth": 350 } },
        //   { "_index": 2, "properties": { "health": 50, "totalEnemyHealth": 350 } }
        // ]
        //
        // ACTUAL BEHAVIOR OBSERVED:
        // Due to Issues #1, #2, and #3 combining:
        // 1. merge returns ARRAY instead of object (Issue #1)
        // 2. reduce is NOT evaluated, returned as operation object (Issue #2)
        // 3. var operations are NOT evaluated (Issue #3)
        //
        // Actual result structure (simplified):
        // [
        //   [
        //     { "_index": 0, "properties": { "health": 100 } },
        //     { "properties": { "totalEnemyHealth": { "reduce": [...] } } }
        //   ],
        //   ... (similar for other entities)
        // ]
        //
        // WHY THIS IS BLOCKING:
        // - Cannot extract _index because result[0] is an array, not an object
        // - Cannot get numeric value for totalEnemyHealth because it's an unevaluated operation
        // - Cannot apply mutations to entities without both issues resolved
        // This makes the entire RulesDriver implementation impossible as specified.

        // Arrange: EXACT data structure from Example 1 in the sprint spec
        JsonNode? data = JsonNode.Parse("""
        {
          "world": {
            "deltaTime": 0.016667
          },
          "entities": [
            { "_index": 0, "properties": { "health": 100 }, "tags": ["enemy"] },
            { "_index": 1, "properties": { "health": 200 }, "tags": ["enemy"] },
            { "_index": 2, "properties": { "health": 50 }, "tags": ["enemy"] }
          ]
        }
        """);

        // Act: EXACT rule structure from Example 1 in the sprint spec (the DO clause part)
        JsonNode? rule = JsonNode.Parse("""
        {
          "map": [
            { "var": "entities" },
            {
              "merge": [
                { "var": "" },
                {
                  "properties": {
                    "totalEnemyHealth": {
                      "reduce": [
                        { "var": "entities" },
                        { "+": [{ "var": "accumulator" }, { "var": "current.properties.health" }] },
                        0
                      ]
                    }
                  }
                }
              ]
            }
          ]
        }
        """);

        JsonNode? result = JsonLogic.Apply(rule, data);

        // Assert
        Assert.NotNull(result);
        Assert.True(result is JsonArray, "map should return an array");
        
        var arr = (JsonArray)result!;
        Assert.Equal(3, arr.Count); // Three enemies
        
        Console.WriteLine("=== COMPLETE EXAMPLE RESULTS ===");
        for (int i = 0; i < arr.Count; i++)
        {
            Console.WriteLine($"\nResult[{i}] type: {arr[i]?.GetType().Name}");
            Console.WriteLine($"Result[{i}] value: {arr[i]}");
        }
        
        // senior-dev: Try to extract _index and totalEnemyHealth from first result
        var firstResult = arr[0];
        
        // EXPECTED: firstResult is a JsonObject
        // ACTUAL: firstResult is a JsonArray (due to Issue #1 - merge returns array)
        Assert.True(
            firstResult is JsonObject,
            $"EXPECTED: Result to be a JsonObject so we can extract _index.\n" +
            $"ACTUAL: Result is {firstResult?.GetType().Name}.\n" +
            $"DETAILED VALUE: {firstResult}\n" +
            $"ROOT CAUSE: Issue #1 - merge returns array instead of merged object.\n" +
            $"BLOCKING EFFECT: Cannot extract _index to map mutation to entity."
        );
        
        // If we got past the above (we won't), try to extract properties
        if (firstResult is JsonObject obj)
        {
            Assert.True(
                obj.ContainsKey("_index"),
                $"EXPECTED: Object to contain _index property.\n" +
                $"ACTUAL: Object keys: {string.Join(", ", obj.Select(kvp => kvp.Key))}\n" +
                $"PROBLEM: Cannot identify which entity to mutate."
            );
            
            var indexValue = obj["_index"];
            Assert.True(
                indexValue is JsonValue,
                $"EXPECTED: _index to be a JsonValue (number).\n" +
                $"ACTUAL: _index is {indexValue?.GetType().Name}.\n" +
                $"ROOT CAUSE: Issue #3 - var operations not evaluated in object literals."
            );
            
            var props = obj["properties"] as JsonObject;
            Assert.NotNull(props);
            
            var totalHealth = props!["totalEnemyHealth"];
            Assert.True(
                totalHealth is JsonValue,
                $"EXPECTED: totalEnemyHealth to be a JsonValue (number 350).\n" +
                $"ACTUAL: totalEnemyHealth is {totalHealth?.GetType().Name}.\n" +
                $"VALUE: {totalHealth}\n" +
                $"ROOT CAUSE: Issue #2 - nested operations not evaluated in object literals.\n" +
                $"BLOCKING EFFECT: Cannot store non-primitive value in PropertyValue."
            );
            
            if (totalHealth is JsonValue val && val.TryGetValue<float>(out var numVal))
            {
                Assert.Equal(350f, numVal);
            }
        }
    }

    [Fact]
    [Trait("Category", "POC")]
    public void Issue5_MergeDocumentationCheckWhatMergeActuallyDoes()
    {
        // senior-dev: ISSUE #5 - UNDERSTANDING MERGE BEHAVIOR
        //
        // PURPOSE:
        // This test documents exactly what JsonLogic's merge operation does according to
        // the JsonLogic specification, to verify our understanding is correct.
        //
        // JSONLOGIC SPEC FOR MERGE:
        // According to jsonlogic.com:
        // "Takes one or more arrays, and merges them into one array"
        // 
        // KEY INSIGHT:
        // merge is for ARRAYS, not OBJECTS! It concatenates arrays together.
        // It does NOT merge object properties like JavaScript's Object.assign() or spread operator.
        //
        // SPRINT SPEC ASSUMPTION:
        // The sprint spec appears to assume merge will combine object properties:
        // { "merge": [{ "var": "" }, { "properties": {...} }] }
        // 
        // This assumes merge will take the current entity object and add/override properties.
        // But that's not what JsonLogic merge does!
        //
        // ACTUAL BEHAVIOR:
        // When you pass objects to merge, it treats them as separate items and returns
        // an array containing both objects unchanged.
        //
        // WHY THIS IS BLOCKING:
        // The entire mutation strategy in the sprint spec relies on merge combining object
        // properties. If merge doesn't do this, we need a different approach entirely.

        // Arrange: Test what merge actually does with objects
        JsonNode? data = JsonNode.Parse("""
        {
          "obj1": { "a": 1, "b": 2 },
          "obj2": { "b": 3, "c": 4 }
        }
        """);

        // Act: Try to merge two objects
        JsonNode? rule = JsonNode.Parse("""
        {
          "merge": [
            { "var": "obj1" },
            { "var": "obj2" }
          ]
        }
        """);

        JsonNode? result = JsonLogic.Apply(rule, data);

        // Assert
        Assert.NotNull(result);
        
        Console.WriteLine($"Result type: {result?.GetType().Name}");
        Console.WriteLine($"Result value: {result}");
        
        // senior-dev: Document what merge actually returns
        //
        // EXPECTED (if merge worked like Object.assign):
        // { "a": 1, "b": 3, "c": 4 }  // Single merged object
        //
        // ACTUAL (what JsonLogic merge actually does):
        // [{ "a": 1, "b": 2 }, { "b": 3, "c": 4 }]  // Array of both objects
        
        Assert.True(
            result is JsonObject,
            $"EXPECTED (based on sprint spec assumption): merge to return a single JsonObject.\n" +
            $"ACTUAL: merge returned {result?.GetType().Name}.\n" +
            $"VALUE: {result}\n" +
            $"JSONLOGIC SPEC: merge is for arrays, not objects. It returns an array containing the items.\n" +
            $"CONCLUSION: The sprint spec's use of merge is based on incorrect assumption about JsonLogic behavior.\n" +
            $"BLOCKING EFFECT: Need alternative way to combine object properties, merge won't work."
        );
    }
}
