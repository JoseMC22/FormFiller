using System.Windows.Controls;
using FormFiller.App.ViewModels;

namespace FormFiller.App.Views;

public partial class RecipeView : UserControl
{
    public RecipeView()
    {
        InitializeComponent();

        var viewModel = new RecipeViewModel();
        DataContext = viewModel;
        viewModel.LoadTemplatesCommand.Execute(null);
    }
}
