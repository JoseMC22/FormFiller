namespace FormFiller.Core.Models;

public class FormTemplate
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ProcessName { get; set; }

    public string? WindowTitle { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<FormField> Fields { get; set; } = new();
}
