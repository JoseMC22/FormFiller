using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FormFiller.Core.Automation;
using FormFiller.Core.Data;
using FormFiller.Core.Models;

namespace FormFiller.App.ViewModels;

public partial class RecorderViewModel : ViewModelBase
{
    private readonly TemplateRepository _templateRepository = new();
    private readonly RecipeRepository _recipeRepository = new();
    private RecipeRecorder? _recorder;

    public RecorderViewModel()
    {
        RecordedSteps.CollectionChanged += (_, _) => SaveAsRecipeCommand.NotifyCanExecuteChanged();
        LoadTemplates();
    }

    public ObservableCollection<ProcessWindowInfo> Windows { get; } = new();

    [ObservableProperty]
    private ProcessWindowInfo? _selectedWindow;

    public ObservableCollection<FormTemplate> Templates { get; } = new();

    [ObservableProperty]
    private FormTemplate? _selectedTemplate;

    public ObservableCollection<RecipeStep> RecordedSteps { get; } = new();

    [ObservableProperty]
    private string _recipeName = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isRecording;

    partial void OnIsRecordingChanged(bool value)
    {
        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        SaveAsRecipeCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedWindowChanged(ProcessWindowInfo? value) => StartCommand.NotifyCanExecuteChanged();

    partial void OnRecipeNameChanged(string value) => SaveAsRecipeCommand.NotifyCanExecuteChanged();

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

    private void LoadTemplates()
    {
        Templates.Clear();
        foreach (var template in _templateRepository.GetTemplates())
        {
            Templates.Add(template);
        }

        SelectedTemplate = Templates.FirstOrDefault();
    }

    private bool CanStart() => !IsRecording && SelectedWindow is not null;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        var recorder = new RecipeRecorder();
        recorder.StepRecorded += OnStepRecorded;
        try
        {
            recorder.StartRecording(SelectedWindow!.MainWindowHandle);
        }
        catch (Exception ex)
        {
            recorder.StepRecorded -= OnStepRecorded;
            recorder.Dispose();
            StatusMessage = $"No se pudo iniciar la grabación: {ex.Message}";
            return;
        }

        _recorder = recorder;
        RecordedSteps.Clear();
        IsRecording = true;
        StatusMessage = $"Grabando en '{SelectedWindow.WindowTitle}'. Interactúe con la ventana de destino ahora.";
    }

    private bool CanStop() => IsRecording;

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop()
    {
        var recorder = _recorder;
        _recorder = null;
        if (recorder is null)
        {
            return;
        }

        try
        {
            recorder.StepRecorded -= OnStepRecorded;
            recorder.StopRecording();
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo detener la grabación: {ex.Message}";
        }
        finally
        {
            recorder.Dispose();
            IsRecording = false;
        }

        StatusMessage = $"Grabación detenida. Se capturaron {RecordedSteps.Count} paso(s).";
    }

    private bool CanSaveAsRecipe() =>
        !IsRecording &&
        RecordedSteps.Count > 0 &&
        SelectedTemplate is not null &&
        !string.IsNullOrWhiteSpace(RecipeName);

    [RelayCommand(CanExecute = nameof(CanSaveAsRecipe))]
    private void SaveAsRecipe()
    {
        if (SelectedTemplate is null)
        {
            StatusMessage = "Seleccione una plantilla antes de guardar una receta.";
            return;
        }

        if (RecordedSteps.Count == 0)
        {
            StatusMessage = "Grabe al menos un paso antes de guardar.";
            return;
        }

        if (string.IsNullOrWhiteSpace(RecipeName))
        {
            StatusMessage = "Escriba un nombre para la receta antes de guardar.";
            return;
        }

        var recipe = new Recipe
        {
            Name = RecipeName.Trim(),
            TemplateId = SelectedTemplate.Id,
            Steps = RecordedSteps
                .Select((step, index) => new RecipeStep
                {
                    StepType = step.StepType,
                    Target = step.Target,
                    Value = step.Value,
                    SortOrder = index
                })
                .ToList()
        };

        var id = _recipeRepository.SaveRecipe(recipe);
        StatusMessage = $"Receta '{recipe.Name}' guardada (Id {id}). Está disponible en la pestaña Ejecución.";
    }

    private void OnStepRecorded(RecipeStep step)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(() => RecordedSteps.Add(step));
        }
        else
        {
            RecordedSteps.Add(step);
        }
    }
}
