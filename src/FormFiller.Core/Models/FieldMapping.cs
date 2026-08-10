namespace FormFiller.Core.Models;

public sealed class FieldMapping
{
    public int TemplateId { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public string? ExcelColumn { get; set; }

    public int SortOrder { get; set; }
}
