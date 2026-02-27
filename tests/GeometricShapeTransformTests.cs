using System;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using ParametricMidiSequencer.Models;

namespace ParametricMidiSequencer.Tests
{
    public class GeometricShapeTransformTests
    {
        [Fact]
        public void Apply_NullChord_ReturnsNull()
        {
            // Arrange
            var transform = new ShapeTransform { Type = "rotate", Amount = 2 };

            // Act
            var result = GeometricShapeTransform.Apply(null, transform);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Apply_NullTransform_ReturnsOriginalChord()
        {
            // Arrange
            var chord = new List<int> { 0, 4, 7 };

            // Act
            var result = GeometricShapeTransform.Apply(chord, null);

            // Assert
            Assert.Equal(chord, result);
        }

        [Fact]
        public void Apply_EmptyChord_ReturnsEmptyChord()
        {
            // Arrange
            var chord = new List<int>();
            var transform = new ShapeTransform { Type = "rotate", Amount = 5 };

            // Act
            var result = GeometricShapeTransform.Apply(chord, transform);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Apply_Rotate_TransposesCorrectly()
        {
            // Arrange
            var chord = new List<int> { 0, 4, 7 }; // C major
            var transform = new ShapeTransform { Type = "rotate", Amount = 2 }; // transpose up 2 semitones

            // Act
            var result = GeometricShapeTransform.Apply(chord, transform);

            // Assert
            Assert.Equal(new List<int> { 2, 6, 9 }, result); // D major
        }

        [Fact]
        public void Apply_RotateNegative_TransposesDown()
        {
            // Arrange
            var chord = new List<int> { 0, 4, 7 };
            var transform = new ShapeTransform { Type = "rotate", Amount = -2 };

            // Act
            var result = GeometricShapeTransform.Apply(chord, transform);

            // Assert
            Assert.Equal(new List<int> { 10, 2, 5 }, result);
        }

        [Fact]
        public void Apply_RotateLargeAmount_WrapsCorrectly()
        {
            // Arrange
            var chord = new List<int> { 0, 4, 7 };
            var transform = new ShapeTransform { Type = "rotate", Amount = 14 }; // equivalent to +2

            // Act
            var result = GeometricShapeTransform.Apply(chord, transform);

            // Assert
            Assert.Equal(new List<int> { 2, 6, 9 }, result);
        }

        [Fact]
        public void Apply_ReflectAroundZero_InvertsCorrectly()
        {
            // Arrange
            var chord = new List<int> { 0, 4, 7 }; // C E G
            var transform = new ShapeTransform { Type = "reflect", Axis = 0 };

            // Act
            var result = GeometricShapeTransform.Apply(chord, transform);

            // Assert
            // Reflection around 0: pc -> (2*0 - pc) mod 12 = -pc mod 12
            // 0 -> 0, 4 -> 8, 7 -> 5
            Assert.Equal(new List<int> { 0, 8, 5 }, result);
        }

        [Fact]
        public void Apply_ReflectAround6_InvertsCorrectly()
        {
            // Arrange
            var chord = new List<int> { 0, 4, 7 };
            var transform = new ShapeTransform { Type = "reflect", Axis = 6 };

            // Act
            var result = GeometricShapeTransform.Apply(chord, transform);

            // Assert
            // Reflection around 6: pc -> (2*6 - pc) mod 12
            // 0 -> 12 mod 12 = 0, 4 -> 8, 7 -> 5
            Assert.Equal(new List<int> { 0, 8, 5 }, result);
        }

        [Fact]
        public void Apply_Expand_ExpandsIntervals()
        {
            // Arrange
            var chord = new List<int> { 0, 2, 4 };
            var transform = new ShapeTransform { Type = "expand", Amount = 2 };

            // Act
            var result = GeometricShapeTransform.Apply(chord, transform);

            // Assert
            // Expansion doubles semitone intervals within the chord
            // This is a multiplicative transformation relative to center
            Assert.NotNull(result);
            Assert.Equal(chord.Count, result.Count);
        }

        [Fact]
        public void Apply_UnknownType_ReturnsOriginal()
        {
            // Arrange
            var chord = new List<int> { 0, 4, 7 };
            var transform = new ShapeTransform { Type = "unknown_transform", Amount = 5 };

            // Act
            var result = GeometricShapeTransform.Apply(chord, transform);

            // Assert
            Assert.Equal(chord, result);
        }

        [Fact]
        public void Apply_NullTypeString_ReturnsOriginal()
        {
            // Arrange
            var chord = new List<int> { 0, 4, 7 };
            var transform = new ShapeTransform { Type = null, Amount = 5 };

            // Act
            var result = GeometricShapeTransform.Apply(chord, transform);

            // Assert
            Assert.Equal(chord, result);
        }

        [Fact]
        public void Apply_EmptyTypeString_ReturnsOriginal()
        {
            // Arrange
            var chord = new List<int> { 0, 4, 7 };
            var transform = new ShapeTransform { Type = "", Amount = 5 };

            // Act
            var result = GeometricShapeTransform.Apply(chord, transform);

            // Assert
            Assert.Equal(chord, result);
        }

        [Fact]
        public void Apply_CaseInsensitiveType_Rotate()
        {
            // Arrange
            var chord = new List<int> { 0, 4, 7 };
            var transform = new ShapeTransform { Type = "ROTATE", Amount = 3 };

            // Act
            var result = GeometricShapeTransform.Apply(chord, transform);

            // Assert
            Assert.Equal(new List<int> { 3, 7, 10 }, result);
        }

        [Fact]
        public void Apply_CaseInsensitiveType_Reflect()
        {
            // Arrange
            var chord = new List<int> { 0, 4, 7 };
            var transform = new ShapeTransform { Type = "REFLECT", Axis = 0 };

            // Act
            var result = GeometricShapeTransform.Apply(chord, transform);

            // Assert
            Assert.Equal(new List<int> { 0, 8, 5 }, result);
        }

        [Fact]
        public void Apply_LargeChord_HandlesCorrectly()
        {
            // Arrange
            var chord = new List<int> { 0, 2, 4, 5, 7, 9, 10 };
            var transform = new ShapeTransform { Type = "rotate", Amount = 3 };

            // Act
            var result = GeometricShapeTransform.Apply(chord, transform);

            // Assert
            var expected = new List<int> { 3, 5, 7, 8, 10, 0, 1 };
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_WithDuplicatePitches_HandlesCorrectly()
        {
            // Arrange
            var chord = new List<int> { 0, 0, 4, 7, 7 };
            var transform = new ShapeTransform { Type = "rotate", Amount = 2 };

            // Act
            var result = GeometricShapeTransform.Apply(chord, transform);

            // Assert
            Assert.Equal(new List<int> { 2, 2, 6, 9, 9 }, result);
        }

        [Fact]
        public void Apply_WithAllChromatics_HandlesCorrectly()
        {
            // Arrange
            var chord = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
            var transform = new ShapeTransform { Type = "rotate", Amount = 5 };

            // Act
            var result = GeometricShapeTransform.Apply(chord, transform);

            // Assert
            var expected = new List<int> { 5, 6, 7, 8, 9, 10, 11, 0, 1, 2, 3, 4 };
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_RotateZeroAmount_ReturnsUnchanged()
        {
            // Arrange
            var chord = new List<int> { 0, 4, 7 };
            var transform = new ShapeTransform { Type = "rotate", Amount = 0 };

            // Act
            var result = GeometricShapeTransform.Apply(chord, transform);

            // Assert
            Assert.Equal(chord, result);
        }
    }
}
