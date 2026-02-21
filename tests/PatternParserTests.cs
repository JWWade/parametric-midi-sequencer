using System;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using ParametricMidiSequencer.Models;
using ParametricMidiSequencer.Parsers;

namespace ParametricMidiSequencer.Tests
{
    public class PatternParserTests
    {
        [Fact]
        public void ParseLine_KickPattern_ReturnsCorrectPatternSpec()
        {
            // Arrange
            var input = "K:1/4";
            var expected = new PatternSpec
            {
                Name = "kick",
                Note = 36,
                Type = "modulo",
                Interval = 4
            };

            // Act
            var result = PatternParser.ParseLine(input);

            // Assert
            Assert.Equal(expected.Name, result.Name);
            Assert.Equal(expected.Note, result.Note);
            Assert.Equal(expected.Type, result.Type);
            Assert.Equal(expected.Interval, result.Interval);
        }

        [Fact]
        public void ParseLine_SnarePattern_ReturnsCorrectPatternSpec()
        {
            // Arrange
            var input = "S:1/3";
            var expected = new PatternSpec
            {
                Name = "snare",
                Note = 38,
                Type = "modulo",
                Interval = 3
            };

            // Act
            var result = PatternParser.ParseLine(input);

            // Assert
            Assert.Equal(expected.Name, result.Name);
            Assert.Equal(expected.Note, result.Note);
            Assert.Equal(expected.Type, result.Type);
            Assert.Equal(expected.Interval, result.Interval);
        }

        [Fact]
        public void ParseLine_HihatPattern_ReturnsCorrectPatternSpec()
        {
            // Arrange
            var input = "H:sin>0.5";
            var expected = new PatternSpec
            {
                Name = "hihat",
                Note = 42,
                Type = "function",
                Expression = "sin(x)>0.5"
            };

            // Act
            var result = PatternParser.ParseLine(input);

            // Assert
            Assert.Equal(expected.Name, result.Name);
            Assert.Equal(expected.Note, result.Note);
            Assert.Equal(expected.Type, result.Type);
            Assert.Equal(expected.Expression, result.Expression);
        }

        [Fact]
        public void ParseLine_UnknownPattern_ThrowsException()
        {
            // Arrange
            var input = "X:1/4";

            // Act & Assert
            Assert.Throws<Exception>(() => PatternParser.ParseLine(input));
        }
    }
}