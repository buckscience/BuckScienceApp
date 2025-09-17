using BuckScience.Domain.Enums;
using System;

namespace BuckScience.Demo
{
    /// <summary>
    /// Demo script showcasing the hybrid season-month mapping functionality.
    /// Run this to see how default mappings work via attributes.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== BuckScience Hybrid Season-Month Mapping Demo ===\n");

            // Demonstrate default mappings for all seasons
            Console.WriteLine("1. Default Season-to-Month Mappings:");
            Console.WriteLine("=====================================");

            foreach (Season season in Enum.GetValues<Season>())
            {
                var months = season.GetDefaultMonths();
                var monthNames = months.Select(m => GetMonthName(m)).ToArray();
                Console.WriteLine($"{season,-12}: {string.Join(", ", monthNames)} ({string.Join(", ", months)})");
            }

            // Demonstrate individual season lookups
            Console.WriteLine("\n2. Individual Season Lookups:");
            Console.WriteLine("==============================");

            var preRutMonths = Season.PreRut.GetDefaultMonths();
            Console.WriteLine($"Pre-Rut Season: {string.Join(", ", preRutMonths.Select(GetMonthName))}");

            var rutMonths = Season.Rut.GetDefaultMonths();
            Console.WriteLine($"Rut Season: {string.Join(", ", rutMonths.Select(GetMonthName))}");

            var yearRoundMonths = Season.YearRound.GetDefaultMonths();
            Console.WriteLine($"Year-Round: {yearRoundMonths.Length} months (All months)");

            // Demonstrate array immutability
            Console.WriteLine("\n3. Array Immutability Test:");
            Console.WriteLine("============================");

            var originalMonths = Season.LateSeason.GetDefaultMonths();
            Console.WriteLine($"Original Late Season months: {string.Join(", ", originalMonths)}");

            var copiedMonths = Season.LateSeason.GetDefaultMonths();
            copiedMonths[0] = 99; // Try to modify the copy

            var freshCopy = Season.LateSeason.GetDefaultMonths();
            Console.WriteLine($"Fresh copy after modification: {string.Join(", ", freshCopy)}");
            Console.WriteLine("✓ Default mappings are protected from modification");

            // Show the typical usage pattern
            Console.WriteLine("\n4. Typical Usage Pattern:");
            Console.WriteLine("==========================");
            Console.WriteLine("// Get default months for a season");
            Console.WriteLine("var months = Season.PreRut.GetDefaultMonths();");
            Console.WriteLine("");
            Console.WriteLine("// For property-specific overrides, use the service:");
            Console.WriteLine("var service = new SeasonMonthMappingService(dbContext);");
            Console.WriteLine("var months = await service.GetMonthsForPropertyAsync(Season.PreRut, property);");

            Console.WriteLine("\n=== Demo Complete ===");
        }

        private static string GetMonthName(int month)
        {
            return month switch
            {
                1 => "January",
                2 => "February", 
                3 => "March",
                4 => "April",
                5 => "May",
                6 => "June",
                7 => "July",
                8 => "August",
                9 => "September",
                10 => "October",
                11 => "November",
                12 => "December",
                _ => $"Month{month}"
            };
        }
    }
}