using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FormFiller.Core.Automation;
using FormFiller.Core.Data;
using FormFiller.Core.Excel;
using FormFiller.Core.Models;
using Microsoft.Win32;

namespace FormFiller.App.ViewModels;

public partial class RunnerViewModel : ViewModelBase
{
    private const int PreviewRowLimit = 50;

    private readonly TemplateRepository _templateRepository = new();
    private readonly MappingRepository _mappingRepository = new();
    private readonly RecipeRepository _recipeRepository = new();
    private readonly List<IReadOnlyList<string>> _allRows = new();

    private CancellationTokenSource? _cancellation;

    public ObservableCollection<FormTemplate> Templates { get; } = new();

    [ObservableProperty]
    private FormTemplate? _selectedTemplate;

    public ObservableCollection<ProcessWindowInfo> Windows { get; } = new();

    [ObservableProperty]
    private ProcessWindowInfo? _selectedWindow;

    public ObservableCollection<string> SubmitButtons { get; } = new();

    [ObservableProperty]
    private string? _selectedSubmitButton;

    public ObservableCollection<Recipe> Recipes { get; } = new();

    [ObservableProperty]
    private Recipe? _selectedRecipe;

    [ObservableProperty]
    private string? _excelFilePath;

    public ObservableCollection<string> Sheets { get; } = new();

    [ObservableProperty]
    private string? _selectedSheet;

    public ObservableCollection<string> Columns { get; } = new();

    public ObservableCollection<IReadOnlyList<string>> PreviewRows { get; } = new();

    public ObservableCollection<RunRowResult> Results { get; } = new();

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private int _currentRow;

    [ObservableProperty]
    private int _progressCurrent;

    [ObservableProperty]
    private int _progressTotal = 1;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private int _startRow = 1;

    [ObservableProperty]
    private int _endRow;

    partial void OnSelectedTemplateChanged(FormTemplate? value)
    {
        RebuildSubmitButtons();
        LoadRecipes();
    }

    partial void OnSelectedSheetChanged(string? value) => LoadSheet();

    partial void OnIsRunningChanged(bool value) => RunCommand.NotifyCanExecuteChanged();

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
    private void LoadRecipes()
    {
        Recipes.Clear();
        SelectedRecipe = null;
        if (SelectedTemplate is null)
        {
            return;
        }

        foreach (var recipe in _recipeRepository.GetRecipes(SelectedTemplate.Id))
        {
            Recipes.Add(recipe);
        }
    }

    [RelayCommand]
    private void RefreshWindows()
    {
        Windows.Clear();
        foreach (var window in UiInspector.GetOpenWindows())
        {
            Windows.Add(window);
        }

        StatusMessage = $"{Windows.Count} window(s) found.";
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
        PreviewRows.Clear();
        _allRows.Clear();

        if (string.IsNullOrWhiteSpace(SelectedSheet) || string.IsNullOrWhiteSpace(ExcelFilePath))
        {
            return;
        }

        foreach (var column in ExcelReader.GetColumns(ExcelFilePath, SelectedSheet))
        {
            Columns.Add(column);
        }

        var rows = ExcelReader.GetPreviewRows(ExcelFilePath, SelectedSheet, int.MaxValue);
        _allRows.AddRange(rows);

        foreach (var row in rows.Take(PreviewRowLimit))
        {
            PreviewRows.Add(row);
        }

        StatusMessage = $"{_allRows.Count} row(s), {Columns.Count} column(s) in sheet '{SelectedSheet}'.";
    }

    private void RebuildSubmitButtons()
    {
        SubmitButtons.Clear();
        if (SelectedTemplate is null)
        {
            return;
        }

        var template = _templateRepository.GetTemplate(SelectedTemplate.Id);
        if (template is null)
        {
            return;
        }

        foreach (var field in template.Fields
                     .Where(f => f.FieldType == FieldType.Button)
                     .Where(f => !string.IsNullOrWhiteSpace(f.Name) || !string.IsNullOrWhiteSpace(f.AutomationId)))
        {
            SubmitButtons.Add(string.IsNullOrWhiteSpace(field.Name) ? field.AutomationId! : field.Name);
        }

        SelectedSubmitButton = SubmitButtons.FirstOrDefault();
    }

    private bool CanRun() =>
        !IsRunning &&
        SelectedTemplate is not null &&
        SelectedWindow is not null &&
        !string.IsNullOrWhiteSpace(ExcelFilePath) &&
        Columns.Count > 0 &&
        _allRows.Count > 0;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        var template = _templateRepository.GetTemplate(SelectedTemplate!.Id);
        var mappings = _mappingRepository.GetMappings(SelectedTemplate.Id);
        if (template is null || mappings.Count == 0)
        {
            StatusMessage = "No field mappings found for the selected template.";
            return;
        }

        var startRowIndex = Math.Max(0, StartRow - 1);
        var endRowIndex = EndRow <= 0 ? _allRows.Count - 1 : Math.Min(_allRows.Count - 1, EndRow - 1);
        var totalRows = Math.Max(0, endRowIndex - startRowIndex + 1);
        if (totalRows == 0)
        {
            StatusMessage = "No rows to process in the selected range.";
            return;
        }

        var selectedRecipe = SelectedRecipe is null
            ? null
            : _recipeRepository.GetRecipe(SelectedRecipe.Id);
        var options = new RunOptions(
            SelectedSubmitButton,
            StartRowIndex: startRowIndex,
            EndRowIndex: endRowIndex,
            Recipe: selectedRecipe);

        var windowHandle = SelectedWindow!.MainWindowHandle;
        var columns = Columns.ToList();
        var dispatcher = Application.Current?.Dispatcher;
        var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;

        Results.Clear();
        ProgressTotal = totalRows;
        ProgressCurrent = 0;
        CurrentRow = 0;
        IsRunning = true;

        var okCount = 0;
        var failedCount = 0;
        var canceled = false;
        Exception? runError = null;

        try
        {
            await Task.Run(() =>
            {
                Runner.RunAll(
                    windowHandle,
                    template,
                    mappings,
                    columns,
                    _allRows,
                    options,
                    onRowDone: result =>
                    {
                        void Append()
                        {
                            Results.Add(result);
                            ProgressCurrent = Results.Count;
                            CurrentRow = result.RowNumber;
                            if (result.Success)
                            {
                                okCount++;
                            }
                            else
                            {
                                failedCount++;
                            }
                        }

                        if (dispatcher is not null)
                        {
                            dispatcher.Invoke(Append);
                        }
                        else
                        {
                            Append();
                        }
                    },
                    ct: cancellation.Token);
            }, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }
        catch (Exception ex)
        {
            runError = ex;
        }
        finally
        {
            _cancellation = null;
            cancellation.Dispose();
            IsRunning = false;

            if (canceled)
            {
                StatusMessage = $"Run canceled after {ProgressCurrent} row(s).";
            }
            else if (runError is not null)
            {
                StatusMessage = $"Run failed: {runError.Message}";
            }
            else
            {
                StatusMessage = $"Completed: {okCount} OK, {failedCount} failed.";
            }
        }
    }

    [RelayCommand]
    private void Stop() => _cancellation?.Cancel();
}
