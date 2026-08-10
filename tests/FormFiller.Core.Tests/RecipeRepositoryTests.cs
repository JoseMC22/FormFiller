using FormFiller.Core.Data;
using FormFiller.Core.Models;

namespace FormFiller.Core.Tests;

public sealed class RecipeRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly RecipeRepository _repository;
    private readonly int _templateId;

    public RecipeRepositoryTests()
    {
        var directory = Path.Combine(Path.GetTempPath(), "FormFillerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        _dbPath = Path.Combine(directory, "formfiller.db");
        _repository = new RecipeRepository(_dbPath);
        _templateId = new TemplateRepository(_dbPath).SaveTemplate(new FormTemplate
        {
            Name = "Recipe fixture template",
            Fields = { CreateField("Codigo", "txtCodigo", "Edit", FieldType.Text, 125, 28, 0) }
        });
    }

    [Fact]
    public void SaveAndGetRecipe_ReturnsStepsInSortOrder()
    {
        var recipe = new Recipe
        {
            TemplateId = _templateId,
            Name = "Guardar con espera",
            Steps =
            {
                new RecipeStep { StepType = RecipeStepType.Wait, Value = "200", SortOrder = 1 },
                new RecipeStep { StepType = RecipeStepType.SetField, Target = "Codigo", Value = "Codigo", SortOrder = 0 },
                new RecipeStep { StepType = RecipeStepType.ClickButton, Target = "btnGuardar", SortOrder = 2 }
            }
        };

        var savedId = _repository.SaveRecipe(recipe);

        Assert.True(savedId > 0);
        Assert.Equal(savedId, recipe.Id);

        var loaded = _repository.GetRecipe(savedId);
        Assert.NotNull(loaded);
        Assert.Equal("Guardar con espera", loaded.Name);
        Assert.Equal(_templateId, loaded.TemplateId);
        Assert.Equal(3, loaded.Steps.Count);
        Assert.Equal(
            new[] { RecipeStepType.SetField, RecipeStepType.Wait, RecipeStepType.ClickButton },
            loaded.Steps.Select(s => s.StepType).ToArray());
        Assert.Equal(new[] { 0, 1, 2 }, loaded.Steps.Select(s => s.SortOrder).ToArray());
        Assert.Equal("Codigo", loaded.Steps[0].Target);
        Assert.Equal("btnGuardar", loaded.Steps[2].Target);
    }

    [Fact]
    public void GetRecipes_ReturnsListWithoutStepsOrderedByName()
    {
        _repository.SaveRecipe(new Recipe { TemplateId = _templateId, Name = "Zulu" });
        _repository.SaveRecipe(new Recipe { TemplateId = _templateId, Name = "Alpha" });

        var recipes = _repository.GetRecipes(_templateId);

        Assert.Equal(new[] { "Alpha", "Zulu" }, recipes.Select(r => r.Name).ToArray());
        Assert.All(recipes, r => Assert.Empty(r.Steps));
    }

    [Fact]
    public void SaveRecipe_UpdatePersistsChanges()
    {
        var recipe = new Recipe
        {
            TemplateId = _templateId,
            Name = "Original",
            Steps = { new RecipeStep { StepType = RecipeStepType.Wait, Value = "100", SortOrder = 0 } }
        };
        var id = _repository.SaveRecipe(recipe);

        recipe.Name = "Renamed";
        recipe.Steps.Clear();
        recipe.Steps.Add(new RecipeStep { StepType = RecipeStepType.ClickButton, Target = "btnGuardar", SortOrder = 0 });
        recipe.Steps.Add(new RecipeStep { StepType = RecipeStepType.WaitForWindow, Target = "Aviso", Value = "3000", SortOrder = 1 });
        var updatedId = _repository.SaveRecipe(recipe);

        Assert.Equal(id, updatedId);

        var loaded = _repository.GetRecipe(id);
        Assert.NotNull(loaded);
        Assert.Equal("Renamed", loaded.Name);
        Assert.Equal(2, loaded.Steps.Count);
        Assert.Equal(
            new[] { RecipeStepType.ClickButton, RecipeStepType.WaitForWindow },
            loaded.Steps.Select(s => s.StepType).ToArray());
        Assert.Equal("btnGuardar", loaded.Steps[0].Target);
        Assert.Equal("Aviso", loaded.Steps[1].Target);
        Assert.Equal("3000", loaded.Steps[1].Value);
    }

    [Fact]
    public void DeleteRecipe_RemovesItFromStore()
    {
        var id = _repository.SaveRecipe(new Recipe
        {
            TemplateId = _templateId,
            Name = "To delete",
            Steps = { new RecipeStep { StepType = RecipeStepType.Wait, Value = "500", SortOrder = 0 } }
        });

        _repository.DeleteRecipe(id);

        Assert.Null(_repository.GetRecipe(id));
        Assert.Empty(_repository.GetRecipes(_templateId));
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_dbPath);
        if (directory != null && Directory.Exists(directory))
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch
            {
                // Best-effort cleanup of temp files.
            }
        }
    }

    private static FormField CreateField(string name, string automationId, string controlType, FieldType fieldType, int x, int y, int sortOrder)
    {
        return new FormField
        {
            Name = name,
            AutomationId = automationId,
            ControlType = controlType,
            FieldType = fieldType,
            IsEditable = fieldType is FieldType.Text or FieldType.ComboBox,
            IsInvokable = fieldType == FieldType.Button,
            PositionX = x,
            PositionY = y,
            SortOrder = sortOrder
        };
    }
}
