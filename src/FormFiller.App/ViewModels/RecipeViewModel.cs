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
            StatusMessage = "Select a template before creating a recipe.";
            return;
        }

        SelectedRecipe = new Recipe { TemplateId = SelectedTemplate.Id };
        StatusMessage = "New recipe created. Type a name, add steps, then save.";
    }

    [RelayCommand]
    private void DeleteRecipe()
    {
        if (SelectedRecipe is null || SelectedRecipe.Id <= 0)
        {
            StatusMessage = "Select a saved recipe to delete.";
            return;
        }

        var name = SelectedRecipe.Name;
        _recipeRepository.DeleteRecipe(SelectedRecipe.Id);
        StatusMessage = $"Recipe '{name}' deleted.";
        LoadRecipes();
    }

    [RelayCommand]
    private void SaveRecipe()
    {
        if (SelectedTemplate is null)
        {
            StatusMessage = "Select a template before saving a recipe.";
            return;
        }

        if (SelectedRecipe is null)
        {
            StatusMessage = "Create a new recipe before saving.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedRecipe.Name))
        {
            StatusMessage = "Type a name for the recipe before saving.";
            return;
        }

        if (Steps.Count == 0)
        {
            StatusMessage = "Add at least one step before saving.";
            return;
        }

        SelectedRecipe.TemplateId = SelectedTemplate.Id;
        SelectedRecipe.Steps = Steps.ToList();
        _recipeRepository.SaveRecipe(SelectedRecipe);

        var savedId = SelectedRecipe.Id;
        var savedName = SelectedRecipe.Name;
        StatusMessage = $"Recipe '{savedName}' saved.";
        LoadRecipes();
        SelectedRecipe = Recipes.FirstOrDefault(r => r.Id == savedId);
    }

    [RelayCommand]
    private void AddStep(RecipeStepType stepType)
    {
        if (SelectedRecipe is null)
        {
            StatusMessage = "Select or create a recipe before adding steps.";
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
            StatusMessage = "Select a step to remove.";
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
