using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using System;
using System.Text.Json;
using Xunit;

namespace BuckScience.Tests.Domain
{
    public class PropertySeasonMonthsOverrideTests
    {
        [Fact]
        public void PropertySeasonMonthsOverride_Constructor_WithValidParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            var propertyId = 1;
            var season = Season.PreRut;
            var months = new[] { 9, 10 };

            // Act
            var override_ = new PropertySeasonMonthsOverride(propertyId, season, months);

            // Assert
            Assert.Equal(propertyId, override_.PropertyId);
            Assert.Equal(season, override_.Season);
            Assert.Equal(months, override_.GetMonths());
            Assert.True(override_.CreatedAt <= DateTime.UtcNow);
            Assert.True(override_.UpdatedAt <= DateTime.UtcNow);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void PropertySeasonMonthsOverride_Constructor_WithInvalidPropertyId_ThrowsArgumentOutOfRangeException(int invalidPropertyId)
        {
            // Arrange
            var season = Season.PreRut;
            var months = new[] { 10 };

            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
                new PropertySeasonMonthsOverride(invalidPropertyId, season, months));
            Assert.Contains("Property ID must be greater than 0", exception.Message);
        }

        [Fact]
        public void PropertySeasonMonthsOverride_Constructor_WithNullMonths_ThrowsArgumentNullException()
        {
            // Arrange
            var propertyId = 1;
            var season = Season.PreRut;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new PropertySeasonMonthsOverride(propertyId, season, null!));
        }

        [Fact]
        public void PropertySeasonMonthsOverride_Constructor_WithEmptyMonths_ThrowsArgumentException()
        {
            // Arrange
            var propertyId = 1;
            var season = Season.PreRut;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new PropertySeasonMonthsOverride(propertyId, season, new int[0]));
            Assert.Contains("At least one month must be specified", exception.Message);
        }

        [Theory]
        [InlineData(new int[] { 0 })]
        [InlineData(new int[] { 13 })]
        [InlineData(new int[] { 1, 13 })]
        [InlineData(new int[] { 0, 5, 10 })]
        public void PropertySeasonMonthsOverride_Constructor_WithInvalidMonths_ThrowsArgumentOutOfRangeException(int[] invalidMonths)
        {
            // Arrange
            var propertyId = 1;
            var season = Season.PreRut;

            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
                new PropertySeasonMonthsOverride(propertyId, season, invalidMonths));
            Assert.Contains("is invalid. Must be between 1 and 12", exception.Message);
        }

        [Fact]
        public void PropertySeasonMonthsOverride_GetMonths_ReturnsCorrectMonths()
        {
            // Arrange
            var months = new[] { 1, 6, 12 };
            var override_ = new PropertySeasonMonthsOverride(1, Season.YearRound, months);

            // Act
            var retrievedMonths = override_.GetMonths();

            // Assert
            Assert.Equal(months, retrievedMonths);
        }

        [Fact]
        public void PropertySeasonMonthsOverride_SetMonths_WithValidMonths_UpdatesMonthsAndTimestamp()
        {
            // Arrange
            var originalMonths = new[] { 1, 2 };
            var newMonths = new[] { 3, 4, 5 };
            var override_ = new PropertySeasonMonthsOverride(1, Season.EarlySeason, originalMonths);
            var originalUpdateTime = override_.UpdatedAt;

            // Act
            System.Threading.Thread.Sleep(10); // Ensure timestamp difference
            override_.SetMonths(newMonths);

            // Assert
            Assert.Equal(newMonths, override_.GetMonths());
            Assert.True(override_.UpdatedAt > originalUpdateTime);
        }

        [Fact]
        public void PropertySeasonMonthsOverride_SetMonths_WithNullMonths_ClearsMonths()
        {
            // Arrange
            var originalMonths = new[] { 1, 2 };
            var override_ = new PropertySeasonMonthsOverride(1, Season.EarlySeason, originalMonths);

            // Act
            override_.SetMonths(null);

            // Assert
            Assert.Null(override_.GetMonths());
            Assert.True(string.IsNullOrEmpty(override_.MonthsJson));
        }

        [Fact]
        public void PropertySeasonMonthsOverride_SetMonths_WithEmptyArray_ClearsMonths()
        {
            // Arrange
            var originalMonths = new[] { 1, 2 };
            var override_ = new PropertySeasonMonthsOverride(1, Season.EarlySeason, originalMonths);

            // Act
            override_.SetMonths(new int[0]);

            // Assert
            Assert.Null(override_.GetMonths());
            Assert.True(string.IsNullOrEmpty(override_.MonthsJson));
        }

        [Theory]
        [InlineData(new int[] { 0 })]
        [InlineData(new int[] { 13 })]
        [InlineData(new int[] { 1, 0, 5 })]
        public void PropertySeasonMonthsOverride_SetMonths_WithInvalidMonths_ThrowsArgumentOutOfRangeException(int[] invalidMonths)
        {
            // Arrange
            var override_ = new PropertySeasonMonthsOverride(1, Season.PreRut, new[] { 10 });

            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => override_.SetMonths(invalidMonths));
            Assert.Contains("is invalid. Must be between 1 and 12", exception.Message);
        }

        [Fact]
        public void PropertySeasonMonthsOverride_GetMonths_WithCorruptedJson_ReturnsNull()
        {
            // Arrange
            var override_ = new PropertySeasonMonthsOverride(1, Season.PreRut, new[] { 10 });
            
            // Use reflection to set corrupted JSON (simulating database corruption)
            var field = typeof(PropertySeasonMonthsOverride).GetField("<MonthsJson>k__BackingField", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(override_, "invalid json");

            // Act
            var months = override_.GetMonths();

            // Assert
            Assert.Null(months);
        }

        [Fact]
        public void PropertySeasonMonthsOverride_MonthsJson_SerializesCorrectly()
        {
            // Arrange
            var months = new[] { 1, 5, 9, 12 };
            var override_ = new PropertySeasonMonthsOverride(1, Season.YearRound, months);

            // Act
            var json = override_.MonthsJson;
            var deserializedMonths = JsonSerializer.Deserialize<int[]>(json);

            // Assert
            Assert.Equal(months, deserializedMonths);
        }

        [Theory]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 })]
        [InlineData(new int[] { 6, 7, 8 })]
        public void PropertySeasonMonthsOverride_RoundTripSerialization_MaintainsData(int[] months)
        {
            // Arrange & Act
            var override_ = new PropertySeasonMonthsOverride(1, Season.YearRound, months);
            var retrievedMonths = override_.GetMonths();

            // Assert
            Assert.Equal(months, retrievedMonths);
        }
    }
}