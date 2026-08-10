using System.Windows.Controls;
using FormFiller.App.ViewModels;

namespace FormFiller.App.Views;

public partial class MappingView : UserControl
{
    public MappingView()
    {
        InitializeComponent();

        var viewModel = new MappingViewModel();
        DataContext = viewModel;
        viewModel.LoadTemplatesCommand.Execute(null);
    }
}
