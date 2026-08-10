using FormFiller.Core.Data;
using FormFiller.Core.Models;

namespace FormFiller.Core.Tests;

public sealed class TemplateRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly TemplateRepository _repository;

    public TemplateRepositoryTests()
    {
        var directory = Path.Combine(Path.GetTempPath(), "FormFillerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        _dbPath = Path.Combine(directory, "formfiller.db");
        _repository = new TemplateRepository(_dbPath);
    }

    [Fact]
    public void SaveAndGetTemplate_StoresFieldsInOrder()
    {
        var template = new FormTemplate
        {
            Name = "Carga de datos",
            ProcessName = "MuestraApp",
            WindowTitle = "MuestraApp - Carga de Datos"
        };
        template.Fields.Add(CreateField("Codigo", "txtCodigo", "Edit", FieldType.Text, 125, 28, 0));
        template.Fields.Add(CreateField("Nombre", "txtNombre", "Edit", FieldType.Text, 125, 63, 1));
        template.Fields.Add(CreateField("Guardar", "btnGuardar", "Button", FieldType.Button, 160, 215, 2));

        var savedId = _repository.SaveTemplate(template);

        Assert.True(savedId > 0);
        Assert.Equal(savedId, template.Id);

        var loaded = _repository.GetTemplate(savedId);
        Assert.NotNull(loaded);
        Assert.Equal("Carga de datos", loaded.Name);
        Assert.Equal("MuestraApp", loaded.ProcessName);
        Assert.Equal("MuestraApp - Carga de Datos", loaded.WindowTitle);
        Assert.Equal(3, loaded.Fields.Count);

        Assert.Equal(new[] { "Codigo", "Nombre", "Guardar" }, loaded.Fields.Select(f => f.Name).ToArray());
        Assert.Equal(new[] { 0, 1, 2 }, loaded.Fields.Select(f => f.SortOrder).ToArray());
        Assert.Equal(FieldType.Text, loaded.Fields[0].FieldType);
        Assert.Equal(FieldType.Button, loaded.Fields[2].FieldType);
        Assert.Equal("txtCodigo", loaded.Fields[0].AutomationId);
        Assert.True(loaded.Fields[0].IsEditable);
        Assert.True(loaded.Fields[2].IsInvokable);
        Assert.Equal(125, loaded.Fields[0].PositionX);
        Assert.Equal(28, loaded.Fields[0].PositionY);
    }

    [Fact]
    public void GetTemplates_ReturnsListWithoutFieldsOrderedByName()
    {
        _repository.SaveTemplate(new FormTemplate
        {
            Name = "Zulu",
            Fields = { CreateField("A", "a", "Edit", FieldType.Text, 1, 1, 0) }
        });
        _repository.SaveTemplate(new FormTemplate
        {
            Name = "Alpha",
            Fields = { CreateField("B", "b", "ComboBox", FieldType.ComboBox, 2, 2, 0) }
        });

        var templates = _repository.GetTemplates();

        Assert.Equal(new[] { "Alpha", "Zulu" }, templates.Select(t => t.Name).ToArray());
        Assert.All(templates, t => Assert.Empty(t.Fields));
    }

    [Fact]
    public void UpdateTemplate_ReplacesFieldsAndPersistsChanges()
    {
        var template = new FormTemplate
        {
            Name = "Original",
            Fields = { CreateField("One", "txtOne", "Edit", FieldType.Text, 10, 10, 0) }
        };
        var id = _repository.SaveTemplate(template);

        template.Name = "Renamed";
        template.Fields.Clear();
        template.Fields.Add(CreateField("Two", "cboTwo", "ComboBox", FieldType.ComboBox, 20, 20, 0));
        template.Fields.Add(CreateField("Three", "chkThree", "CheckBox", FieldType.CheckBox, 30, 30, 1));
        var updatedId = _repository.SaveTemplate(template);

        Assert.Equal(id, updatedId);

        var loaded = _repository.GetTemplate(id);
        Assert.NotNull(loaded);
        Assert.Equal("Renamed", loaded.Name);
        Assert.Equal(2, loaded.Fields.Count);
        Assert.Equal(new[] { "Two", "Three" }, loaded.Fields.Select(f => f.Name).ToArray());
        Assert.Equal(FieldType.ComboBox, loaded.Fields[0].FieldType);
        Assert.Equal(FieldType.CheckBox, loaded.Fields[1].FieldType);
    }

    [Fact]
    public void DeleteTemplate_RemovesItFromStore()
    {
        var id = _repository.SaveTemplate(new FormTemplate
        {
            Name = "To delete",
            Fields = { CreateField("Field", "txtField", "Edit", FieldType.Text, 5, 5, 0) }
        });

        _repository.DeleteTemplate(id);

        Assert.Null(_repository.GetTemplate(id));
        Assert.Empty(_repository.GetTemplates());
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
