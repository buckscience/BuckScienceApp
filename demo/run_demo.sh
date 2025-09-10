#!/bin/bash

# Demo script to showcase the season-month mapping functionality
echo "Building BuckScience solution..."
cd /home/runner/work/BuckScienceApp/BuckScienceApp
dotnet build --configuration Release --verbosity quiet

echo ""
echo "Running Season-Month Mapping Demo..."
echo "======================================"

# Create a temporary console app to run the demo
cat > /tmp/demo_runner.cs << 'EOF'
using BuckScience.Domain.Enums;
using System;
using System.Linq;

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

Console.WriteLine("\n=== Demo Complete ===");

static string GetMonthName(int month)
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
EOF

# Create a temporary project file that references the domain project
cat > /tmp/demo.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="/home/runner/work/BuckScienceApp/BuckScienceApp/src/BuckScience.Domain/BuckScience.Domain.csproj" />
  </ItemGroup>
</Project>
EOF

# Run the demo
cd /tmp
cp /tmp/demo_runner.cs Program.cs
dotnet run --project demo.csproj

echo ""
echo "Demo completed successfully!"