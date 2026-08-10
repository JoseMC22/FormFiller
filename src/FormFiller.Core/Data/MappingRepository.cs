using FormFiller.Core.Models;
using Microsoft.Data.Sqlite;

namespace FormFiller.Core.Data;

public class MappingRepository
{
    private readonly string _connectionString;

    public MappingRepository(string? dbPath = null)
    {
        var path = Path.GetFullPath(dbPath ?? AppDb.DbPath);
        AppDb.Initialize();
        if (!string.Equals(path, Path.GetFullPath(AppDb.DbPath), StringComparison.OrdinalIgnoreCase))
        {
            AppDb.Initialize(path);
        }

        _connectionString = $"Data Source={path}";
    }

    public void SaveMappings(int templateId, IReadOnlyList<FieldMapping> mappings)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM mappings WHERE template_id = $templateId";
            delete.Parameters.AddWithValue("$templateId", templateId);
            delete.ExecuteNonQuery();
        }

        foreach (var mapping in mappings)
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO mappings (template_id, field_name, excel_column, sort_order)
                VALUES ($templateId, $fieldName, $excelColumn, $sortOrder)
                """;
            insert.Parameters.AddWithValue("$templateId", templateId);
            insert.Parameters.AddWithValue("$fieldName", mapping.FieldName);
            insert.Parameters.AddWithValue("$excelColumn", (object?)mapping.ExcelColumn ?? DBNull.Value);
            insert.Parameters.AddWithValue("$sortOrder", mapping.SortOrder);
            insert.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public IReadOnlyList<FieldMapping> GetMappings(int templateId)
    {
        var mappings = new List<FieldMapping>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT template_id, field_name, excel_column, sort_order
            FROM mappings
            WHERE template_id = $templateId
            ORDER BY sort_order
            """;
        command.Parameters.AddWithValue("$templateId", templateId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            mappings.Add(new FieldMapping
            {
                TemplateId = reader.GetInt32(reader.GetOrdinal("template_id")),
                FieldName = reader.GetString(reader.GetOrdinal("field_name")),
                ExcelColumn = reader.IsDBNull(reader.GetOrdinal("excel_column"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("excel_column")),
                SortOrder = reader.GetInt32(reader.GetOrdinal("sort_order"))
            });
        }

        return mappings;
    }

    public void DeleteMappings(int templateId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM mappings WHERE template_id = $templateId";
        command.Parameters.AddWithValue("$templateId", templateId);
        command.ExecuteNonQuery();
    }
}
