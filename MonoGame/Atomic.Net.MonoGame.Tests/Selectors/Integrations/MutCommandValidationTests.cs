using Xunit;
using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.Scenes;

namespace Atomic.Net.MonoGame.Tests.Selectors.Integrations;

/// <summary>
/// Integration tests for MutCommand validation and error handling.
/// Tests that the new array-of-operations format is enforced and old format is rejected.
/// </summary>
[Collection("NonParallel")]
[Trait("Category", "Integration")]
public sealed class MutCommandValidationTests : IDisposable
{
    private readonly FakeEventListener<ErrorEvent> _errorListener;

    public MutCommandValidationTests()
    {
        // Arrange: Initialize systems before each test
        AtomicSystem.Initialize();
        EventBus<InitializeEvent>.Push(new());
        
        _errorListener = new FakeEventListener<ErrorEvent>();
    }

    public void Dispose()
    {
        // Clean up between tests
        _errorListener.Dispose();
        EventBus<ShutdownEvent>.Push(new());
    }

    [Fact]
    public void LoadGameScene_WithOldMapMergeFormat_FiresErrorEvent()
    {
        // Arrange: Create temp scene file with old format
        var oldFormatJson = """
        {
          "entities": [
            { "id": "goblin", "properties": { "health": 50 } }
          ],
          "rules": [
            {
              "from": "#enemy",
              "where": {},
              "do": {
                "mut": {
                  "map": [
                    { "var": "entities" },
                    {
                      "merge": [
                        { "var": "" },
                        { "properties": { "health": 0 } }
                      ]
                    }
                  ]
                }
              }
            }
          ]
        }
        """;

        var tempPath = Path.Combine(Path.GetTempPath(), $"old-format-{Guid.NewGuid()}.json");
        File.WriteAllText(tempPath, oldFormatJson);

        try
        {
            // Act
            SceneLoader.Instance.LoadGameScene(tempPath);

            // Assert
            Assert.True(_errorListener.ReceivedEvents.Count > 0, "Should fire ErrorEvent for old format");
            
            var hasRelevantError = _errorListener.ReceivedEvents.Any(e => 
                e.Message.Contains("Failed to parse JSON", StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains("mutation", StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains("object", StringComparison.OrdinalIgnoreCase));
            Assert.True(hasRelevantError, "Error should mention parsing failure");
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void LoadGameScene_WithMissingTarget_FiresErrorEvent()
    {
        // Arrange: Create temp scene file with missing target
        var missingTargetJson = """
        {
          "entities": [
            { "id": "goblin", "properties": { "health": 50 } }
          ],
          "rules": [
            {
              "from": "#enemy",
              "where": {},
              "do": {
                "mut": [
                  {
                    "value": 0
                  }
                ]
              }
            }
          ]
        }
        """;

        var tempPath = Path.Combine(Path.GetTempPath(), $"missing-target-{Guid.NewGuid()}.json");
        File.WriteAllText(tempPath, missingTargetJson);

        try
        {
            // Act
            SceneLoader.Instance.LoadGameScene(tempPath);

            // Assert
            Assert.True(_errorListener.ReceivedEvents.Count > 0, "Should fire ErrorEvent for missing target");
            
            var hasRelevantError = _errorListener.ReceivedEvents.Any(e => 
                e.Message.Contains("Missing 'target'", StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains("Failed to parse JSON", StringComparison.OrdinalIgnoreCase));
            Assert.True(hasRelevantError, "Error should mention missing target");
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void LoadGameScene_WithMissingValue_FiresErrorEvent()
    {
        // Arrange: Create temp scene file with missing value
        var missingValueJson = """
        {
          "entities": [
            { "id": "goblin", "properties": { "health": 50 } }
          ],
          "rules": [
            {
              "from": "#enemy",
              "where": {},
              "do": {
                "mut": [
                  {
                    "target": { "properties": "health" }
                  }
                ]
              }
            }
          ]
        }
        """;

        var tempPath = Path.Combine(Path.GetTempPath(), $"missing-value-{Guid.NewGuid()}.json");
        File.WriteAllText(tempPath, missingValueJson);

        try
        {
            // Act
            SceneLoader.Instance.LoadGameScene(tempPath);

            // Assert
            Assert.True(_errorListener.ReceivedEvents.Count > 0, "Should fire ErrorEvent for missing value");
            
            var hasRelevantError = _errorListener.ReceivedEvents.Any(e => 
                e.Message.Contains("Missing 'value'", StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains("Failed to parse JSON", StringComparison.OrdinalIgnoreCase));
            Assert.True(hasRelevantError, "Error should mention missing value");
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void LoadGameScene_WithNullTarget_FiresErrorEvent()
    {
        // Arrange: Create temp scene file with null target
        var nullTargetJson = """
        {
          "entities": [
            { "id": "goblin", "properties": { "health": 50 } }
          ],
          "rules": [
            {
              "from": "#enemy",
              "where": {},
              "do": {
                "mut": [
                  {
                    "target": null,
                    "value": 0
                  }
                ]
              }
            }
          ]
        }
        """;

        var tempPath = Path.Combine(Path.GetTempPath(), $"null-target-{Guid.NewGuid()}.json");
        File.WriteAllText(tempPath, nullTargetJson);

        try
        {
            // Act
            SceneLoader.Instance.LoadGameScene(tempPath);

            // Assert
            Assert.True(_errorListener.ReceivedEvents.Count > 0, "Should fire ErrorEvent for null target");
            
            var hasRelevantError = _errorListener.ReceivedEvents.Any(e => 
                e.Message.Contains("Missing 'target'", StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains("Failed to parse JSON", StringComparison.OrdinalIgnoreCase));
            Assert.True(hasRelevantError, "Error should mention missing/null target");
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void LoadGameScene_WithNullValue_FiresErrorEvent()
    {
        // Arrange: Create temp scene file with null value
        var nullValueJson = """
        {
          "entities": [
            { "id": "goblin", "properties": { "health": 50 } }
          ],
          "rules": [
            {
              "from": "#enemy",
              "where": {},
              "do": {
                "mut": [
                  {
                    "target": { "properties": "health" },
                    "value": null
                  }
                ]
              }
            }
          ]
        }
        """;

        var tempPath = Path.Combine(Path.GetTempPath(), $"null-value-{Guid.NewGuid()}.json");
        File.WriteAllText(tempPath, nullValueJson);

        try
        {
            // Act
            SceneLoader.Instance.LoadGameScene(tempPath);

            // Assert
            Assert.True(_errorListener.ReceivedEvents.Count > 0, "Should fire ErrorEvent for null value");
            
            var hasRelevantError = _errorListener.ReceivedEvents.Any(e => 
                e.Message.Contains("Missing 'value'", StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains("Failed to parse JSON", StringComparison.OrdinalIgnoreCase));
            Assert.True(hasRelevantError, "Error should mention missing/null value");
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void LoadGameScene_WithNonObjectOperation_FiresErrorEvent()
    {
        // Arrange: Create temp scene file with non-object operation
        var nonObjectJson = """
        {
          "entities": [
            { "id": "goblin", "properties": { "health": 50 } }
          ],
          "rules": [
            {
              "from": "#enemy",
              "where": {},
              "do": {
                "mut": [
                  "invalid"
                ]
              }
            }
          ]
        }
        """;

        var tempPath = Path.Combine(Path.GetTempPath(), $"non-object-{Guid.NewGuid()}.json");
        File.WriteAllText(tempPath, nonObjectJson);

        try
        {
            // Act
            SceneLoader.Instance.LoadGameScene(tempPath);

            // Assert
            Assert.True(_errorListener.ReceivedEvents.Count > 0, "Should fire ErrorEvent for non-object operation");
            
            var hasRelevantError = _errorListener.ReceivedEvents.Any(e => 
                e.Message.Contains("object", StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains("Failed to parse JSON", StringComparison.OrdinalIgnoreCase));
            Assert.True(hasRelevantError, "Error should mention parsing failure");
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void LoadGameScene_WithMultipleOperations_LoadsSuccessfully()
    {
        // Arrange: Create temp scene file with multiple operations
        var multiOpJson = """
        {
          "entities": [
            { "id": "goblin", "properties": { "health": 50, "mana": 20 } }
          ],
          "rules": [
            {
              "from": "#enemy",
              "where": {},
              "do": {
                "mut": [
                  {
                    "target": { "properties": "health" },
                    "value": 0
                  },
                  {
                    "target": { "properties": "mana" },
                    "value": 0
                  }
                ]
              }
            }
          ]
        }
        """;

        var tempPath = Path.Combine(Path.GetTempPath(), $"multi-op-{Guid.NewGuid()}.json");
        File.WriteAllText(tempPath, multiOpJson);

        try
        {
            // Act
            SceneLoader.Instance.LoadGameScene(tempPath);

            // Assert
            Assert.Empty(_errorListener.ReceivedEvents);
            
            var rules = RuleRegistry.Instance.Rules.ToList();
            Assert.Single(rules);
            
            var rule = rules[0].Value;
            Assert.True(rule.Do.TryMatch(out MutCommand mutCommand), "Do should be MutCommand");
            Assert.Equal(2, mutCommand.Operations.Length);
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void LoadGameScene_WithEmptyMutArray_LoadsSuccessfully()
    {
        // Arrange: Create temp scene file with empty mut array
        var emptyMutJson = """
        {
          "entities": [
            { "id": "goblin", "properties": { "health": 50 } }
          ],
          "rules": [
            {
              "from": "#enemy",
              "where": {},
              "do": {
                "mut": []
              }
            }
          ]
        }
        """;

        var tempPath = Path.Combine(Path.GetTempPath(), $"empty-mut-{Guid.NewGuid()}.json");
        File.WriteAllText(tempPath, emptyMutJson);

        try
        {
            // Act
            SceneLoader.Instance.LoadGameScene(tempPath);

            // Assert
            Assert.Empty(_errorListener.ReceivedEvents);
            
            var rules = RuleRegistry.Instance.Rules.ToList();
            Assert.Single(rules);
            
            var rule = rules[0].Value;
            Assert.True(rule.Do.TryMatch(out MutCommand mutCommand), "Do should be MutCommand");
            Assert.Empty(mutCommand.Operations);
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
