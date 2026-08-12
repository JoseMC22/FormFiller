using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FormFiller.Core.Data;
using FormFiller.Core.Models;

namespace FormFiller.App.ViewModels;

public partial class RecipeViewModel : ViewModelBase
{
    private readonly TemplateRepository _templateRepository;
    private readonly RecipeRepository _recipeRepository;

    public RecipeViewModel()
        : this(new TemplateRepository(), new RecipeRepository())
    {
    }

    public RecipeViewModel(TemplateRepository templateRepository, RecipeRepository recipeRepository)
    {
        _templateRepository = templateRepository;
        _recipeRepository = recipeRepository;
    }

    public ObservableCollection<FormTemplate> Templates { get; } = new();

    [ObservableProperty]
    private FormTemplate? _selectedTemplate;

    public ObservableCollection<Recipe> Recipes { get; } = new();

    [ObservableProperty]
    private Recipe? _selectedRecipe;

    public ObservableCollection<RecipeStep> Steps { get; } = new();

    [ObservableProperty]
    private RecipeStep? _selectedStep;

    public IReadOnlyList<RecipeStepType> StepTypes { get; } = Enum.GetValues<RecipeStepType>();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    partial void OnSelectedTemplateChanged(FormTemplate? value) => LoadRecipes();

    partial void OnSelectedRecipeChanged(Recipe? value)
    {
        Steps.Clear();
        SelectedStep = null;
        if (value is null || value.Id <= 0)
        {
            return;
        }

        var loaded = _recipeRepository.GetRecipe(value.Id);
        if (loaded is null)
        {
            return;
        }

        foreach (var step in loaded.Steps)
        {
            Steps.Add(step);
        }
    }

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
    private void NewRecipe()
    {
        if (SelectedTemplate is null)
        {
            StatusMessage = "Seleccione una plantilla antes de crear una receta.";
            return;
        }

        SelectedRecipe = new Recipe { TemplateId = SelectedTemplate.Id };
        StatusMessage = "Receta creada. Escriba un nombre, agregue pasos y luego guarde.";
    }

    [RelayCommand]
    private void DeleteRecipe()
    {
        if (SelectedRecipe is null || SelectedRecipe.Id <= 0)
        {
            StatusMessage = "Seleccione una receta guardada para eliminar.";
            return;
        }

        var name = SelectedRecipe.Name;
        _recipeRepository.DeleteRecipe(SelectedRecipe.Id);
        StatusMessage = $"Receta '{name}' eliminada.";
        LoadRecipes();
    }

    [RelayCommand]
    private void SaveRecipe()
    {
        if (SelectedTemplate is null)
        {
            StatusMessage = "Seleccione una plantilla antes de guardar una receta.";
            return;
        }

        if (SelectedRecipe is null)
        {
            StatusMessage = "Cree una nueva receta antes de guardar.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedRecipe.Name))
        {
            StatusMessage = "Escriba un nombre para la receta antes de guardar.";
            return;
        }

        if (Steps.Count == 0)
        {
            StatusMessage = "Agregue al menos un paso antes de guardar.";
            return;
        }

        SelectedRecipe.TemplateId = SelectedTemplate.Id;
        SelectedRecipe.Steps = Steps.ToList();
        _recipeRepository.SaveRecipe(SelectedRecipe);

        var savedId = SelectedRecipe.Id;
        var savedName = SelectedRecipe.Name;
        StatusMessage = $"Receta '{savedName}' guardada.";
        LoadRecipes();
        SelectedRecipe = Recipes.FirstOrDefault(r => r.Id == savedId);
    }

    [RelayCommand]
    private void AddStep(RecipeStepType stepType)
    {
        if (SelectedRecipe is null)
        {
            StatusMessage = "Seleccione o cree una receta antes de agregar pasos.";
            return;
        }

        var step = new RecipeStep
        {
            StepType = stepType,
            SortOrder = Steps.Count == 0 ? 0 : Steps.Max(s => s.SortOrder) + 1
        };
        Steps.Add(step);
        SelectedStep = step;
    }

    [RelayCommand]
    private void RemoveStep()
    {
        if (SelectedStep is null)
        {
            StatusMessage = "Seleccione un paso para quitar.";
            return;
        }

        Steps.Remove(SelectedStep);
        SelectedStep = null;
    }

    [RelayCommand]
    private void MoveUpStep()
    {
        var step = SelectedStep;
        if (step is null)
        {
            return;
        }

        var index = Steps.IndexOf(step);
        if (index <= 0)
        {
            return;
        }

        SwapSortOrder(Steps[index - 1], step);
        Steps.Move(index, index - 1);
        SelectedStep = step;
    }

    [RelayCommand]
    private void MoveDownStep()
    {
        var step = SelectedStep;
        if (step is null)
        {
            return;
        }

        var index = Steps.IndexOf(step);
        if (index < 0 || index >= Steps.Count - 1)
        {
            return;
        }

        SwapSortOrder(step, Steps[index + 1]);
        Steps.Move(index, index + 1);
        SelectedStep = step;
    }

    private static void SwapSortOrder(RecipeStep first, RecipeStep second)
    {
        (second.SortOrder, first.SortOrder) = (first.SortOrder, second.SortOrder);
    }
}
