using FormFiller.Core.Models;
using Microsoft.Data.Sqlite;

namespace FormFiller.Core.Data;

public class TemplateRepository
{
    private readonly string _connectionString;

    public TemplateRepository(string? dbPath = null)
    {
        var path = Path.GetFullPath(dbPath ?? AppDb.DbPath);
        AppDb.Initialize();
        if (!string.Equals(path, Path.GetFullPath(AppDb.DbPath), StringComparison.OrdinalIgnoreCase))
        {
            AppDb.Initialize(path);
        }

        _connectionString = $"Data Source={path}";
    }

    public int SaveTemplate(FormTemplate template)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        long templateId;
        if (template.Id > 0 && TemplateExists(connection, transaction, template.Id))
        {
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = """
                UPDATE templates
                SET name = $name, process_name = $processName, window_title = $windowTitle
                WHERE id = $id
                """;
            update.Parameters.AddWithValue("$name", template.Name);
            update.Parameters.AddWithValue("$processName", (object?)template.ProcessName ?? DBNull.Value);
            update.Parameters.AddWithValue("$windowTitle", (object?)template.WindowTitle ?? DBNull.Value);
            update.Parameters.AddWithValue("$id", template.Id);
            update.ExecuteNonQuery();

            templateId = template.Id;

            using var deleteFields = connection.CreateCommand();
            deleteFields.Transaction = transaction;
            deleteFields.CommandText = "DELETE FROM fields WHERE template_id = $templateId";
            deleteFields.Parameters.AddWithValue("$templateId", template.Id);
            deleteFields.ExecuteNonQuery();
        }
        else
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO templates (name, process_name, window_title, created_at)
                VALUES ($name, $processName, $windowTitle, $createdAt)
                """;
            insert.Parameters.AddWithValue("$name", template.Name);
            insert.Parameters.AddWithValue("$processName", (object?)template.ProcessName ?? DBNull.Value);
            insert.Parameters.AddWithValue("$windowTitle", (object?)template.WindowTitle ?? DBNull.Value);
            insert.Parameters.AddWithValue("$createdAt", template.CreatedAt.ToString("o"));
            insert.ExecuteNonQuery();

            using var lastId = connection.CreateCommand();
            lastId.Transaction = transaction;
            lastId.CommandText = "SELECT last_insert_rowid()";
            templateId = (long)(lastId.ExecuteScalar() ?? 0L);
        }

        foreach (var field in template.Fields)
        {
            using var insertField = connection.CreateCommand();
            insertField.Transaction = transaction;
            insertField.CommandText = """
                INSERT INTO fields (template_id, name, automation_id, control_type, field_type,
                                    is_editable, is_invokable, position_x, position_y, sort_order)
                VALUES ($templateId, $name, $automationId, $controlType, $fieldType,
                        $isEditable, $isInvokable, $positionX, $positionY, $sortOrder)
                """;
            insertField.Parameters.AddWithValue("$templateId", templateId);
            insertField.Parameters.AddWithValue("$name", field.Name);
            insertField.Parameters.AddWithValue("$automationId", (object?)field.AutomationId ?? DBNull.Value);
            insertField.Parameters.AddWithValue("$controlType", (object?)field.ControlType ?? DBNull.Value);
            insertField.Parameters.AddWithValue("$fieldType", (int)field.FieldType);
            insertField.Parameters.AddWithValue("$isEditable", field.IsEditable ? 1 : 0);
            insertField.Parameters.AddWithValue("$isInvokable", field.IsInvokable ? 1 : 0);
            insertField.Parameters.AddWithValue("$positionX", (object?)field.PositionX ?? DBNull.Value);
            insertField.Parameters.AddWithValue("$positionY", (object?)field.PositionY ?? DBNull.Value);
            insertField.Parameters.AddWithValue("$sortOrder", field.SortOrder);
            insertField.ExecuteNonQuery();
        }

        transaction.Commit();
        template.Id = (int)templateId;
        return (int)templateId;
    }

    public IReadOnlyList<FormTemplate> GetTemplates()
    {
        var templates = new List<FormTemplate>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, name, process_name, window_title, created_at FROM templates ORDER BY name";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            templates.Add(MapTemplate(reader));
        }

        return templates;
    }

    public FormTemplate? GetTemplate(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT t.id AS template_id, t.name AS template_name, t.process_name, t.window_title, t.created_at,
                   f.id AS field_id, f.name AS field_name, f.automation_id, f.control_type, f.field_type,
                   f.is_editable, f.is_invokable, f.position_x, f.position_y, f.sort_order
            FROM templates t
            LEFT JOIN fields f ON f.template_id = t.id
            WHERE t.id = $id
            ORDER BY f.sort_order
            """;
        command.Parameters.AddWithValue("$id", id);

        FormTemplate? template = null;
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                template ??= new FormTemplate
                {
                    Id = reader.GetInt32(reader.GetOrdinal("template_id")),
                    Name = reader.GetString(reader.GetOrdinal("template_name")),
                    ProcessName = reader.IsDBNull(reader.GetOrdinal("process_name"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("process_name")),
                    WindowTitle = reader.IsDBNull(reader.GetOrdinal("window_title"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("window_title")),
                    CreatedAt = DateTime.Parse(
                        reader.GetString(reader.GetOrdinal("created_at")),
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.RoundtripKind)
                };

                var fieldIdOrdinal = reader.GetOrdinal("field_id");
                if (reader.IsDBNull(fieldIdOrdinal))
                {
                    continue;
                }

                template.Fields.Add(new FormField
                {
                    Id = reader.GetInt32(fieldIdOrdinal),
                    Name = reader.GetString(reader.GetOrdinal("field_name")),
                    AutomationId = reader.IsDBNull(reader.GetOrdinal("automation_id"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("automation_id")),
                    ControlType = reader.IsDBNull(reader.GetOrdinal("control_type"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("control_type")),
                    FieldType = (FieldType)reader.GetInt32(reader.GetOrdinal("field_type")),
                    IsEditable = reader.GetInt32(reader.GetOrdinal("is_editable")) != 0,
                    IsInvokable = reader.GetInt32(reader.GetOrdinal("is_invokable")) != 0,
                    PositionX = reader.IsDBNull(reader.GetOrdinal("position_x"))
                        ? null
                        : reader.GetInt32(reader.GetOrdinal("position_x")),
                    PositionY = reader.IsDBNull(reader.GetOrdinal("position_y"))
                        ? null
                        : reader.GetInt32(reader.GetOrdinal("position_y")),
                    SortOrder = reader.GetInt32(reader.GetOrdinal("sort_order")),
                    TemplateId = id
                });
            }
        }

        return template;
    }

    public void DeleteTemplate(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        using (var deleteFields = connection.CreateCommand())
        {
            deleteFields.Transaction = transaction;
            deleteFields.CommandText = "DELETE FROM fields WHERE template_id = $id";
            deleteFields.Parameters.AddWithValue("$id", id);
            deleteFields.ExecuteNonQuery();
        }

        using (var deleteTemplate = connection.CreateCommand())
        {
            deleteTemplate.Transaction = transaction;
            deleteTemplate.CommandText = "DELETE FROM templates WHERE id = $id";
            deleteTemplate.Parameters.AddWithValue("$id", id);
            deleteTemplate.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private static bool TemplateExists(SqliteConnection connection, SqliteTransaction transaction, int id)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(1) FROM templates WHERE id = $id";
        command.Parameters.AddWithValue("$id", id);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static FormTemplate MapTemplate(SqliteDataReader reader)
    {
        return new FormTemplate
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            ProcessName = reader.IsDBNull(reader.GetOrdinal("process_name"))
                ? null
                : reader.GetString(reader.GetOrdinal("process_name")),
            WindowTitle = reader.IsDBNull(reader.GetOrdinal("window_title"))
                ? null
                : reader.GetString(reader.GetOrdinal("window_title")),
            CreatedAt = DateTime.Parse(
                reader.GetString(reader.GetOrdinal("created_at")),
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind)
        };
    }
}
