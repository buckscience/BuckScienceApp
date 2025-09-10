using BuckScience.Domain.Enums;
using System;
using Xunit;

namespace BuckScience.Tests.Domain
{
    public class SeasonExtensionsTests
    {
        [Fact]
        public void GetDefaultMonths_EarlySeason_ReturnsCorrectMonths()
        {
            // Arrange
            var season = Season.EarlySeason;

            // Act
            var months = season.GetDefaultMonths();

            // Assert
            Assert.Equal(new[] { 9, 10 }, months);
        }

        [Fact]
        public void GetDefaultMonths_PreRut_ReturnsCorrectMonths()
        {
            // Arrange
            var season = Season.PreRut;

            // Act
            var months = season.GetDefaultMonths();

            // Assert
            Assert.Equal(new[] { 10 }, months);
        }

        [Fact]
        public void GetDefaultMonths_Rut_ReturnsCorrectMonths()
        {
            // Arrange
            var season = Season.Rut;

            // Act
            var months = season.GetDefaultMonths();

            // Assert
            Assert.Equal(new[] { 11 }, months);
        }

        [Fact]
        public void GetDefaultMonths_PostRut_ReturnsCorrectMonths()
        {
            // Arrange
            var season = Season.PostRut;

            // Act
            var months = season.GetDefaultMonths();

            // Assert
            Assert.Equal(new[] { 12 }, months);
        }

        [Fact]
        public void GetDefaultMonths_LateSeason_ReturnsCorrectMonths()
        {
            // Arrange
            var season = Season.LateSeason;

            // Act
            var months = season.GetDefaultMonths();

            // Assert
            Assert.Equal(new[] { 12, 1 }, months);
        }

        [Fact]
        public void GetDefaultMonths_YearRound_ReturnsAllMonths()
        {
            // Arrange
            var season = Season.YearRound;

            // Act
            var months = season.GetDefaultMonths();

            // Assert
            Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, months);
        }

        [Fact]
        public void GetDefaultMonths_ReturnsArrayCopy_PreventsMutation()
        {
            // Arrange
            var season = Season.PreRut;

            // Act
            var months1 = season.GetDefaultMonths();
            var months2 = season.GetDefaultMonths();
            months1[0] = 999; // Modify first array

            // Assert
            Assert.NotSame(months1, months2); // Different array instances
            Assert.Equal(new[] { 10 }, months2); // Second array unchanged
        }

        [Fact]
        public void GetDefaultMonths_AllSeasons_HaveValidAttributes()
        {
            // Arrange & Act & Assert
            foreach (Season season in Enum.GetValues<Season>())
            {
                var months = season.GetDefaultMonths();
                Assert.NotNull(months);
                Assert.True(months.Length > 0, $"Season {season} should have at least one month");
                
                foreach (var month in months)
                {
                    Assert.InRange(month, 1, 12);
                }
            }
        }

        [Fact]
        public void GetMonthsForProperty_WithNullProperty_ReturnsDefaultMonths()
        {
            // Arrange
            var season = Season.PreRut;

            // Act
            var months = season.GetMonthsForProperty(null!);

            // Assert
            Assert.Equal(season.GetDefaultMonths(), months);
        }

        [Fact]
        public void GetMonthsForProperty_WithProperty_ReturnsDefaultMonths()
        {
            // Arrange
            var season = Season.Rut;
            var property = new BuckScience.Domain.Entities.Property(
                "Test Property", 
                new NetTopologySuite.Geometries.Point(0, 0) { SRID = 4326 }, 
                null, 
                "UTC", 
                6, 
                20);

            // Act
            var months = season.GetMonthsForProperty(property);

            // Assert
            Assert.Equal(season.GetDefaultMonths(), months);
        }
    }
}