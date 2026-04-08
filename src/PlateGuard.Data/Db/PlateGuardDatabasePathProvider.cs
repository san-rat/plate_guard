namespace PlateGuard.Data.Db;

public static class PlateGuardDatabasePathProvider
{
    public const string DatabasePathEnvironmentVariableName = "PLATEGUARD_DB_PATH";
    public const string DatabaseFileName = "plateguard.db";
    public const string ApplicationFolderName = "PlateGuard";

    public static string GetDatabasePath()
    {
        var overridePath = Environment.GetEnvironmentVariable(DatabasePathEnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return overridePath;
        }

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, ApplicationFolderName, DatabaseFileName);
    }
}
