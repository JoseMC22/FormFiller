using FormFiller.Core.Data;
using FormFiller.Core.Models;

namespace FormFiller.Core.Tests;

public sealed class MappingRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly TemplateRepository _templateRepository;
    private readonly MappingRepository _mappingRepository;

    public MappingRepositoryTests()
    {
        var directory = Path.Combine(Path.GetTempPath(), "FormFillerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        _dbPath = Path.Combine(directory, "formfiller.db");
        _templateRepository = new TemplateRepository(_dbPath);
        _mappingRepository = new MappingRepository(_dbPath);
    }

    [Fact]
    public void SaveAndGetMappings_RoundTripsOrderAndValues()
    {
        var templateId = SaveTemplateWithFields(
            ("Codigo", FieldType.Text),
            ("Nombre", FieldType.Text),
            ("Guardar", FieldType.Button));

        _mappingRepository.SaveMappings(templateId, new List<FieldMapping>
        {
            new() { TemplateId = templateId, FieldName = "Codigo", ExcelColumn = "A", SortOrder = 0 },
            new() { TemplateId = templateId, FieldName = "Nombre", ExcelColumn = "B", SortOrder = 1 }
        });

        var mappings = _mappingRepository.GetMappings(templateId);

        Assert.Equal(2, mappings.Count);
        Assert.Equal(new[] { "Codigo", "Nombre" }, mappings.Select(m => m.FieldName).ToArray());
        Assert.Equal(new[] { 0, 1 }, mappings.Select(m => m.SortOrder).ToArray());
        Assert.Equal("A", mappings[0].ExcelColumn);
        Assert.Equal("B", mappings[1].ExcelColumn);
        Assert.All(mappings, m => Assert.Equal(templateId, m.TemplateId));
    }

    [Fact]
    public void SaveMappings_ReplacesPreviousMappings()
    {
        var templateId = SaveTemplateWithFields(
            ("One", FieldType.Text),
            ("Two", FieldType.ComboBox));

        _mappingRepository.SaveMappings(templateId, new List<FieldMapping>
        {
            new() { TemplateId = templateId, FieldName = "One", ExcelColumn = "A", SortOrder = 0 }
        });

        _mappingRepository.SaveMappings(templateId, new List<FieldMapping>
        {
            new() { TemplateId = templateId, FieldName = "One", ExcelColumn = "C", SortOrder = 1 },
            new() { TemplateId = templateId, FieldName = "Two", ExcelColumn = "D", SortOrder = 0 }
        });

        var mappings = _mappingRepository.GetMappings(templateId);

        Assert.Equal(2, mappings.Count);
        Assert.Equal(new[] { "Two", "One" }, mappings.Select(m => m.FieldName).ToArray());
        Assert.Equal(new[] { 0, 1 }, mappings.Select(m => m.SortOrder).ToArray());
        Assert.Equal("D", mappings[0].ExcelColumn);
        Assert.Equal("C", mappings[1].ExcelColumn);
    }

    [Fact]
    public void DeleteMappings_RemovesAllMappings()
    {
        var templateId = SaveTemplateWithFields(("Codigo", FieldType.Text));

        _mappingRepository.SaveMappings(templateId, new List<FieldMapping>
        {
            new() { TemplateId = templateId, FieldName = "Codigo", ExcelColumn = "A", SortOrder = 0 }
        });

        _mappingRepository.DeleteMappings(templateId);

        Assert.Empty(_mappingRepository.GetMappings(templateId));
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

    private int SaveTemplateWithFields(params (string Name, FieldType Type)[] fields)
    {
        var template = new FormTemplate { Name = "Test template" };
        for (var i = 0; i < fields.Length; i++)
        {
            template.Fields.Add(new FormField
            {
                Name = fields[i].Name,
                FieldType = fields[i].Type,
                IsEditable = fields[i].Type is FieldType.Text or FieldType.ComboBox,
                SortOrder = i
            });
        }

        return _templateRepository.SaveTemplate(template);
    }
}
