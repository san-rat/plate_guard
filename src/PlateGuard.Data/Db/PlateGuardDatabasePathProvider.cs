namespace PlateGuard.Data.Db;

public static class PlateGuardDatabasePathProvider
{
    public const string DatabaseFileName = "plateguard.db";
    public const string ApplicationFolderName = "PlateGuard";

    public static string GetDatabasePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, ApplicationFolderName, DatabaseFileName);
    }
}
