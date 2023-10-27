namespace Akka.Pathfinder.Core;

public static class AkkaPathfinder
{
    public static string? GetEnvironmentVariable(string key) => Environment.GetEnvironmentVariable(AddPrefixAndUpperCase(key));


    public static void SetEnvironmentVariable(string key, string? value) => Environment.SetEnvironmentVariable(AddPrefixAndUpperCase(key), value);


    internal static string AddPrefixAndUpperCase(string key) => $"pathfinder_{key}".ToUpper();
}
