using FormFiller.Core.Models;
using Microsoft.Data.Sqlite;

namespace FormFiller.Core.Data;

public class RecipeRepository
{
    private readonly string _connectionString;

    public RecipeRepository(string? dbPath = null)
    {
        var path = Path.GetFullPath(dbPath ?? AppDb.DbPath);
        AppDb.Initialize();
        if (!string.Equals(path, Path.GetFullPath(AppDb.DbPath), StringComparison.OrdinalIgnoreCase))
        {
            AppDb.Initialize(path);
        }

        _connectionString = $"Data Source={path}";
    }

    public int SaveRecipe(Recipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        long recipeId;
        if (recipe.Id > 0 && RecipeExists(connection, transaction, recipe.Id))
        {
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = """
                UPDATE recipes
                SET template_id = $templateId, name = $name
                WHERE id = $id
                """;
            update.Parameters.AddWithValue("$templateId", recipe.TemplateId);
            update.Parameters.AddWithValue("$name", recipe.Name);
            update.Parameters.AddWithValue("$id", recipe.Id);
            update.ExecuteNonQuery();

            recipeId = recipe.Id;

            using var deleteSteps = connection.CreateCommand();
            deleteSteps.Transaction = transaction;
            deleteSteps.CommandText = "DELETE FROM recipe_steps WHERE recipe_id = $recipeId";
            deleteSteps.Parameters.AddWithValue("$recipeId", recipe.Id);
            deleteSteps.ExecuteNonQuery();
        }
        else
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO recipes (template_id, name, created_at)
                VALUES ($templateId, $name, $createdAt)
                """;
            insert.Parameters.AddWithValue("$templateId", recipe.TemplateId);
            insert.Parameters.AddWithValue("$name", recipe.Name);
            insert.Parameters.AddWithValue("$createdAt", recipe.CreatedAt.ToString("o"));
            insert.ExecuteNonQuery();

            using var lastId = connection.CreateCommand();
            lastId.Transaction = transaction;
            lastId.CommandText = "SELECT last_insert_rowid()";
            recipeId = (long)(lastId.ExecuteScalar() ?? 0L);
        }

        foreach (var step in recipe.Steps.OrderBy(s => s.SortOrder))
        {
            using var insertStep = connection.CreateCommand();
            insertStep.Transaction = transaction;
            insertStep.CommandText = """
                INSERT INTO recipe_steps (recipe_id, step_type, target, value, sort_order)
                VALUES ($recipeId, $stepType, $target, $value, $sortOrder)
                """;
            insertStep.Parameters.AddWithValue("$recipeId", recipeId);
            insertStep.Parameters.AddWithValue("$stepType", (int)step.StepType);
            insertStep.Parameters.AddWithValue("$target", (object?)step.Target ?? DBNull.Value);
            insertStep.Parameters.AddWithValue("$value", (object?)step.Value ?? DBNull.Value);
            insertStep.Parameters.AddWithValue("$sortOrder", step.SortOrder);
            insertStep.ExecuteNonQuery();
        }

        transaction.Commit();
        recipe.Id = (int)recipeId;
        return (int)recipeId;
    }

    public IReadOnlyList<Recipe> GetRecipes(int templateId)
    {
        var recipes = new List<Recipe>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, template_id, name, created_at
            FROM recipes
            WHERE template_id = $templateId
            ORDER BY name
            """;
        command.Parameters.AddWithValue("$templateId", templateId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            recipes.Add(MapRecipe(reader));
        }

        return recipes;
    }

    public Recipe? GetRecipe(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        Recipe? recipe = null;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT id, template_id, name, created_at FROM recipes WHERE id = $id";
            command.Parameters.AddWithValue("$id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                recipe = MapRecipe(reader);
            }
        }

        if (recipe is null)
        {
            return null;
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT id, step_type, target, value, sort_order
                FROM recipe_steps
                WHERE recipe_id = $recipeId
                ORDER BY sort_order
                """;
            command.Parameters.AddWithValue("$recipeId", id);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                recipe.Steps.Add(new RecipeStep
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    RecipeId = id,
                    StepType = (RecipeStepType)reader.GetInt32(reader.GetOrdinal("step_type")),
                    Target = reader.IsDBNull(reader.GetOrdinal("target"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("target")),
                    Value = reader.IsDBNull(reader.GetOrdinal("value"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("value")),
                    SortOrder = reader.GetInt32(reader.GetOrdinal("sort_order"))
                });
            }
        }

        return recipe;
    }

    public void DeleteRecipe(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        using (var deleteSteps = connection.CreateCommand())
        {
            deleteSteps.Transaction = transaction;
            deleteSteps.CommandText = "DELETE FROM recipe_steps WHERE recipe_id = $id";
            deleteSteps.Parameters.AddWithValue("$id", id);
            deleteSteps.ExecuteNonQuery();
        }

        using (var deleteRecipe = connection.CreateCommand())
        {
            deleteRecipe.Transaction = transaction;
            deleteRecipe.CommandText = "DELETE FROM recipes WHERE id = $id";
            deleteRecipe.Parameters.AddWithValue("$id", id);
            deleteRecipe.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private static bool RecipeExists(SqliteConnection connection, SqliteTransaction transaction, int id)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(1) FROM recipes WHERE id = $id";
        command.Parameters.AddWithValue("$id", id);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static Recipe MapRecipe(SqliteDataReader reader)
    {
        return new Recipe
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            TemplateId = reader.GetInt32(reader.GetOrdinal("template_id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            CreatedAt = DateTime.Parse(
                reader.GetString(reader.GetOrdinal("created_at")),
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind)
        };
    }
}
