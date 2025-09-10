using BuckScience.Domain.Enums;
using System;
using Xunit;

namespace BuckScience.Tests.Domain
{
    public class MonthsAttributeTests
    {
        [Fact]
        public void MonthsAttribute_Constructor_WithValidMonths_SetsMonthsCorrectly()
        {
            // Arrange & Act
            var attribute = new MonthsAttribute(1, 2, 3);

            // Assert
            Assert.Equal(new[] { 1, 2, 3 }, attribute.Months);
        }

        [Fact]
        public void MonthsAttribute_Constructor_WithSingleMonth_SetsMonthCorrectly()
        {
            // Arrange & Act
            var attribute = new MonthsAttribute(6);

            // Assert
            Assert.Equal(new[] { 6 }, attribute.Months);
        }

        [Fact]
        public void MonthsAttribute_Constructor_WithAllMonths_SetsAllMonthsCorrectly()
        {
            // Arrange & Act
            var attribute = new MonthsAttribute(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);

            // Assert
            Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, attribute.Months);
        }

        [Fact]
        public void MonthsAttribute_Constructor_WithNullMonths_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MonthsAttribute(null!));
        }

        [Fact]
        public void MonthsAttribute_Constructor_WithEmptyMonths_ThrowsArgumentException()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new MonthsAttribute());
            Assert.Contains("At least one month must be specified", exception.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        [InlineData(-1)]
        [InlineData(25)]
        public void MonthsAttribute_Constructor_WithInvalidMonth_ThrowsArgumentOutOfRangeException(int invalidMonth)
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new MonthsAttribute(invalidMonth));
            Assert.Contains($"Month value {invalidMonth} is invalid", exception.Message);
        }

        [Fact]
        public void MonthsAttribute_Constructor_WithMixedValidAndInvalidMonths_ThrowsArgumentOutOfRangeException()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new MonthsAttribute(1, 2, 13, 4));
            Assert.Contains("Month value 13 is invalid", exception.Message);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(12)]
        public void MonthsAttribute_Constructor_WithBoundaryValidMonths_SetsMonthsCorrectly(int month)
        {
            // Arrange & Act
            var attribute = new MonthsAttribute(month);

            // Assert
            Assert.Equal(new[] { month }, attribute.Months);
        }
    }
}