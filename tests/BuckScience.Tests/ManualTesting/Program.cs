using BuckScience.Tests.ManualTesting;

Console.WriteLine("Manual Hybrid Season Testing");
Console.WriteLine("=============================");

try
{
    await ManualHybridSeasonTesting.RunAllScenariosAsync();
    Console.WriteLine("\n✅ All manual tests completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Manual test failed: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}