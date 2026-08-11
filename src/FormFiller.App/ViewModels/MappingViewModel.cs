using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FormFiller.Core.Data;
using FormFiller.Core.Excel;
using FormFiller.Core.Models;
using Microsoft.Win32;

namespace FormFiller.App.ViewModels;

public sealed partial class MappingRow : ObservableObject
{
    public MappingRow(FormField field)
    {
        Field = field;
        FieldName = field.Name;
        FieldType = field.FieldType;
    }

    public FormField Field { get; }

    public string FieldName { get; }

    public FieldType FieldType { get; }

    [ObservableProperty]
    private string? _selectedColumn;
}

public partial class MappingViewModel : ViewModelBase
{
    private static readonly FieldType[] FillableTypes =
    {
        FieldType.Text,
        FieldType.ComboBox,
        FieldType.CheckBox,
        FieldType.RadioButton,
        FieldType.DatePicker
    };

    private readonly TemplateRepository _templateRepository;
    private readonly MappingRepository _mappingRepository;

    public MappingViewModel()
        : this(new TemplateRepository(), new MappingRepository())
    {
    }

    public MappingViewModel(TemplateRepository templateRepository, MappingRepository mappingRepository)
    {
        _templateRepository = templateRepository;
        _mappingRepository = mappingRepository;
    }

    public ObservableCollection<FormTemplate> Templates { get; } = new();

    [ObservableProperty]
    private FormTemplate? _selectedTemplate;

    [ObservableProperty]
    private string? _excelFilePath;

    public ObservableCollection<string> Sheets { get; } = new();

    [ObservableProperty]
    private string? _selectedSheet;

    public ObservableCollection<string> Columns { get; } = new();

    public ObservableCollection<MappingRow> MappingRows { get; } = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    partial void OnSelectedTemplateChanged(FormTemplate? value) => LoadMappings();

    partial void OnSelectedSheetChanged(string? value) => LoadSheet();

    [RelayCommand]
    private void LoadTemplates()
    {
        Templates.Clear();
        foreach (var template in _templateRepository.GetTemplates())
        {
            Templates.Add(template);
        }

        SelectedTemplate = Templates.FirstOrDefault();
    }

    [RelayCommand]
    private void OpenExcel()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open Excel file",
            Filter = "Excel files (*.xlsx)|*.xlsx"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        ExcelFilePath = dialog.FileName;
        Sheets.Clear();
        foreach (var sheet in ExcelReader.GetSheetNames(ExcelFilePath))
        {
            Sheets.Add(sheet);
        }

        SelectedSheet = Sheets.FirstOrDefault();
        StatusMessage = $"Excel file loaded: {Path.GetFileName(ExcelFilePath)}";
    }

    [RelayCommand]
    private void LoadSheet()
    {
        Columns.Clear();
        if (string.IsNullOrWhiteSpace(SelectedSheet) || string.IsNullOrWhiteSpace(ExcelFilePath))
        {
            return;
        }

        foreach (var column in ExcelReader.GetColumns(ExcelFilePath, SelectedSheet))
        {
            Columns.Add(column);
        }

        StatusMessage = $"{Columns.Count} column(s) in sheet '{SelectedSheet}'.";
    }

    [RelayCommand]
    private void LoadMappings()
    {
        MappingRows.Clear();
        if (SelectedTemplate is null)
        {
            return;
        }

        var template = _templateRepository.GetTemplate(SelectedTemplate.Id);
        if (template is null)
        {
            return;
        }

        var saved = _mappingRepository.GetMappings(SelectedTemplate.Id)
            .ToDictionary(m => m.FieldName);

        foreach (var field in template.Fields
                     .Where(f => FillableTypes.Contains(f.FieldType))
                     .OrderBy(f => f.SortOrder))
        {
            var row = new MappingRow(field);
            if (saved.TryGetValue(field.Name, out var mapping))
            {
                row.SelectedColumn = mapping.ExcelColumn;
            }

            MappingRows.Add(row);
        }

        StatusMessage = $"{MappingRows.Count} fillable field(s) for template '{SelectedTemplate.Name}'.";
    }

    [RelayCommand]
    private void SaveMappings()
    {
        if (SelectedTemplate is null)
        {
            StatusMessage = "Select a template before saving mappings.";
            return;
        }

        var mappings = MappingRows
            .Where(r => !string.IsNullOrWhiteSpace(r.SelectedColumn))
            .Select((r, index) => new FieldMapping
            {
                TemplateId = SelectedTemplate.Id,
                FieldName = r.FieldName,
                ExcelColumn = r.SelectedColumn,
                SortOrder = index
            })
            .ToList();

        if (mappings.Count == 0)
        {
            StatusMessage = "Map at least one field to an Excel column before saving.";
            return;
        }

        _mappingRepository.SaveMappings(SelectedTemplate.Id, mappings);
        StatusMessage = $"Mapping saved for template {SelectedTemplate.Name} ({mappings.Count} fields).";
    }
}
