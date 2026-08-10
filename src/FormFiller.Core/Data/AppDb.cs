using Microsoft.Data.Sqlite;

namespace FormFiller.Core.Data;

public static class AppDb
{
    public static string DbPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FormFiller",
            "formfiller.db");

    public static string ConnectionString => $"Data Source={DbPath}";

    public static void Initialize()
    {
        Initialize(DbPath);
    }

    public static void Initialize(string dbPath)
    {
        var fullPath = Path.GetFullPath(dbPath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = new SqliteConnection($"Data Source={fullPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS templates (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                process_name TEXT NULL,
                window_title TEXT NULL,
                created_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS fields (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                template_id INTEGER NOT NULL REFERENCES templates(id) ON DELETE CASCADE,
                name TEXT NOT NULL,
                automation_id TEXT NULL,
                control_type TEXT NULL,
                field_type INTEGER NOT NULL,
                is_editable INTEGER NOT NULL,
                is_invokable INTEGER NOT NULL,
                position_x INTEGER NULL,
                position_y INTEGER NULL,
                sort_order INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS mappings (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                template_id INTEGER NOT NULL REFERENCES templates(id) ON DELETE CASCADE,
                field_name TEXT NOT NULL,
                excel_column TEXT NULL,
                sort_order INTEGER NOT NULL,
                UNIQUE(template_id, field_name)
            );

            CREATE TABLE IF NOT EXISTS recipes (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                template_id INTEGER NOT NULL REFERENCES templates(id) ON DELETE CASCADE,
                name TEXT NOT NULL,
                created_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS recipe_steps (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                recipe_id INTEGER NOT NULL REFERENCES recipes(id) ON DELETE CASCADE,
                step_type INTEGER NOT NULL,
                target TEXT NULL,
                value TEXT NULL,
                sort_order INTEGER NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }
}
