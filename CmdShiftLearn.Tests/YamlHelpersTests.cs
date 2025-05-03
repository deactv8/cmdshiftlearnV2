using CmdShiftLearn.Api.Helpers;
using CmdShiftLearn.Api.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CmdShiftLearn.Tests
{
    public class YamlHelpersTests
    {
        private readonly Mock<ILogger> _loggerMock;

        public YamlHelpersTests()
        {
            _loggerMock = new Mock<ILogger>();
        }

        [Fact]
        public void DeserializeTutorial_WithSteps_ShouldDeserializeCorrectly()
        {
            // Arrange
            var yamlContent = @"
id: test-tutorial
title: Test Tutorial
description: A test tutorial
xp: 100
difficulty: Beginner
content: |
  # Test Tutorial
  This is a test tutorial.
steps:
  - instructions: Step 1 instructions
    expectedCommand: command1
    hint: hint1
  - instructions: Step 2 instructions
    expectedCommand: command2
    hint: hint2
";

            // Act
            var tutorial = YamlHelpers.DeserializeTutorial(yamlContent, _loggerMock.Object);

            // Assert
            Assert.NotNull(tutorial);
            Assert.Equal("test-tutorial", tutorial.Id);
            Assert.Equal("Test Tutorial", tutorial.Title);
            Assert.Equal(100, tutorial.Xp);
            Assert.Equal("Beginner", tutorial.Difficulty);
            Assert.Contains("# Test Tutorial", tutorial.Content);

            // Verify steps
            Assert.NotNull(tutorial.Steps);
            Assert.Equal(2, tutorial.Steps.Count);
            
            Assert.Equal("Step 1 instructions", tutorial.Steps[0].Instructions);
            Assert.Equal("command1", tutorial.Steps[0].ExpectedCommand);
            Assert.Equal("hint1", tutorial.Steps[0].Hint);
            
            Assert.Equal("Step 2 instructions", tutorial.Steps[1].Instructions);
            Assert.Equal("command2", tutorial.Steps[1].ExpectedCommand);
            Assert.Equal("hint2", tutorial.Steps[1].Hint);
        }
    }
}