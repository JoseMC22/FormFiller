using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FormFiller.Core.Automation;
using FormFiller.Core.Data;
using FormFiller.Core.Models;

namespace FormFiller.App.ViewModels;

public partial class InspectorViewModel : ViewModelBase
{
    private readonly TemplateRepository _repository = new();

    public ObservableCollection<ProcessWindowInfo> Windows { get; } = new();

    [ObservableProperty]
    private ProcessWindowInfo? _selectedWindow;

    public ObservableCollection<ControlNode> ControlTree { get; } = new();

    public ObservableCollection<FormField> DetectedFields { get; } = new();

    [ObservableProperty]
    private string _templateName = string.Empty;

    public ObservableCollection<FormTemplate> SavedTemplates { get; } = new();

    [ObservableProperty]
    private FormTemplate? _selectedSavedTemplate;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public Array FieldTypeValues => Enum.GetValues(typeof(FieldType));

    [RelayCommand]
    private void RefreshWindows()
    {
        Windows.Clear();
        foreach (var window in UiInspector.GetOpenWindows())
        {
            Windows.Add(window);
        }

        StatusMessage = $"{Windows.Count} ventana(s) encontrada(s).";
    }

    [RelayCommand]
    private void CaptureWindow()
    {
        if (SelectedWindow is null)
        {
            StatusMessage = "Seleccione primero una ventana para capturar.";
            return;
        }

        ControlTree.Clear();
        foreach (var node in UiInspector.GetControlTree(SelectedWindow.MainWindowHandle))
        {
            if (!string.IsNullOrWhiteSpace(node.Name) || !string.IsNullOrWhiteSpace(node.AutomationId))
            {
                ControlTree.Add(node);
            }
        }

        var template = UiInspector.CaptureWindow(SelectedWindow.MainWindowHandle, TemplateName);
        DetectedFields.Clear();
        foreach (var field in template.Fields)
        {
            DetectedFields.Add(field);
        }

        if (string.IsNullOrWhiteSpace(TemplateName))
        {
            TemplateName = SelectedWindow.WindowTitle;
        }

        StatusMessage = $"{DetectedFields.Count} campo(s) detectado(s).";
    }

    [RelayCommand]
    private void SaveTemplate()
    {
        if (string.IsNullOrWhiteSpace(TemplateName))
        {
            StatusMessage = "El nombre de la plantilla es obligatorio.";
            return;
        }

        if (DetectedFields.Count == 0)
        {
            StatusMessage = "Capture primero una ventana para detectar campos.";
            return;
        }

        var template = new FormTemplate
        {
            Name = TemplateName,
            ProcessName = SelectedWindow?.ProcessName,
            WindowTitle = SelectedWindow?.WindowTitle,
            Fields = DetectedFields.ToList()
        };

        var id = _repository.SaveTemplate(template);
        LoadTemplates();
        StatusMessage = $"Plantilla guardada (Id {id}).";
    }

    [RelayCommand]
    private void LoadTemplates()
    {
        SavedTemplates.Clear();
        foreach (var template in _repository.GetTemplates())
        {
            SavedTemplates.Add(template);
        }
    }

    [RelayCommand]
    private void DeleteTemplate()
    {
        if (SelectedSavedTemplate is null)
        {
            StatusMessage = "Seleccione una plantilla guardada para eliminar.";
            return;
        }

        _repository.DeleteTemplate(SelectedSavedTemplate.Id);
        LoadTemplates();
        SelectedSavedTemplate = null;
        StatusMessage = "Plantilla eliminada.";
    }
}
