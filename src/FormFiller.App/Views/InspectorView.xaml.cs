using System.Windows.Controls;
using FormFiller.App.ViewModels;

namespace FormFiller.App.Views;

public partial class InspectorView : UserControl
{
    public InspectorView()
    {
        InitializeComponent();

        var viewModel = new InspectorViewModel();
        DataContext = viewModel;
        viewModel.RefreshWindowsCommand.Execute(null);
        viewModel.LoadTemplatesCommand.Execute(null);
    }
}
