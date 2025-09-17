using BuckScience.Web.Helpers;

namespace BuckScience.Tests
{
    public class DirectionHelperTests
    {
        [Theory]
        [InlineData(DirectionHelper.CompassDirection.N, 0f)]
        [InlineData(DirectionHelper.CompassDirection.NE, 45f)]
        [InlineData(DirectionHelper.CompassDirection.E, 90f)]
        [InlineData(DirectionHelper.CompassDirection.SE, 135f)]
        [InlineData(DirectionHelper.CompassDirection.S, 180f)]
        [InlineData(DirectionHelper.CompassDirection.SW, 225f)]
        [InlineData(DirectionHelper.CompassDirection.W, 270f)]
        [InlineData(DirectionHelper.CompassDirection.NW, 315f)]
        public void ToFloat_ShouldReturnCorrectDegrees(DirectionHelper.CompassDirection direction, float expectedDegrees)
        {
            // Act
            var result = DirectionHelper.ToFloat(direction);

            // Assert
            Assert.Equal(expectedDegrees, result);
        }

        [Theory]
        [InlineData(0f, DirectionHelper.CompassDirection.N)]
        [InlineData(45f, DirectionHelper.CompassDirection.NE)]
        [InlineData(90f, DirectionHelper.CompassDirection.E)]
        [InlineData(135f, DirectionHelper.CompassDirection.SE)]
        [InlineData(180f, DirectionHelper.CompassDirection.S)]
        [InlineData(225f, DirectionHelper.CompassDirection.SW)]
        [InlineData(270f, DirectionHelper.CompassDirection.W)]
        [InlineData(315f, DirectionHelper.CompassDirection.NW)]
        public void FromFloat_ShouldReturnCorrectDirection(float degrees, DirectionHelper.CompassDirection expectedDirection)
        {
            // Act
            var result = DirectionHelper.FromFloat(degrees);

            // Assert
            Assert.Equal(expectedDirection, result);
        }

        [Theory]
        [InlineData(22f, DirectionHelper.CompassDirection.N)]   // Closer to 0 than 45
        [InlineData(23f, DirectionHelper.CompassDirection.NE)]  // Closer to 45 than 0
        [InlineData(67f, DirectionHelper.CompassDirection.NE)]  // Closer to 45 than 90
        [InlineData(68f, DirectionHelper.CompassDirection.E)]   // Closer to 90 than 45
        [InlineData(360f, DirectionHelper.CompassDirection.N)]  // 360° = 0°
        [InlineData(365f, DirectionHelper.CompassDirection.N)]  // Normalized to 5°, closest to 0°
        [InlineData(-15f, DirectionHelper.CompassDirection.NW)] // Normalized to 345°, closest to 315° (NW)
        public void FromFloat_ShouldFindClosestDirection(float degrees, DirectionHelper.CompassDirection expectedDirection)
        {
            // Act
            var result = DirectionHelper.FromFloat(degrees);

            // Assert
            Assert.Equal(expectedDirection, result);
        }

        [Fact]
        public void GetDisplayName_ShouldReturnCorrectDisplayNames()
        {
            // Act & Assert
            Assert.Equal("North", DirectionHelper.GetDisplayName(DirectionHelper.CompassDirection.N));
            Assert.Equal("Northeast", DirectionHelper.GetDisplayName(DirectionHelper.CompassDirection.NE));
            Assert.Equal("East", DirectionHelper.GetDisplayName(DirectionHelper.CompassDirection.E));
            Assert.Equal("Southeast", DirectionHelper.GetDisplayName(DirectionHelper.CompassDirection.SE));
            Assert.Equal("South", DirectionHelper.GetDisplayName(DirectionHelper.CompassDirection.S));
            Assert.Equal("Southwest", DirectionHelper.GetDisplayName(DirectionHelper.CompassDirection.SW));
            Assert.Equal("West", DirectionHelper.GetDisplayName(DirectionHelper.CompassDirection.W));
            Assert.Equal("Northwest", DirectionHelper.GetDisplayName(DirectionHelper.CompassDirection.NW));
        }

        [Fact]
        public void GetAllDirections_ShouldReturnAllDirectionsWithCorrectData()
        {
            // Act
            var result = DirectionHelper.GetAllDirections().ToList();

            // Assert
            Assert.Equal(8, result.Count);
            
            var north = result.FirstOrDefault(d => d.Direction == DirectionHelper.CompassDirection.N);
            Assert.Equal("North", north.DisplayName);
            Assert.Equal(0f, north.Degrees);

            var south = result.FirstOrDefault(d => d.Direction == DirectionHelper.CompassDirection.S);
            Assert.Equal("South", south.DisplayName);
            Assert.Equal(180f, south.Degrees);
        }

        [Fact]
        public void DirectionMapping_ShouldBeConsistent()
        {
            // Test that converting direction to float and back gives the same result
            foreach (var direction in Enum.GetValues<DirectionHelper.CompassDirection>())
            {
                // Act
                var degrees = DirectionHelper.ToFloat(direction);
                var backToDirection = DirectionHelper.FromFloat(degrees);

                // Assert
                Assert.Equal(direction, backToDirection);
            }
        }
    }
}