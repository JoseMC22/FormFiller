namespace FormFiller.Core.Models;

public class FormField
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? AutomationId { get; set; }

    public string? ControlType { get; set; }

    public FieldType FieldType { get; set; } = FieldType.Text;

    public bool IsEditable { get; set; }

    public bool IsInvokable { get; set; }

    public int? PositionX { get; set; }

    public int? PositionY { get; set; }

    public int TemplateId { get; set; }

    public int SortOrder { get; set; }
}
