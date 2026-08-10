using System.Windows.Controls;
using FormFiller.App.ViewModels;

namespace FormFiller.App.Views;

public partial class RunnerView : UserControl
{
    public RunnerView()
    {
        InitializeComponent();

        var viewModel = new RunnerViewModel();
        DataContext = viewModel;
        viewModel.LoadTemplatesCommand.Execute(null);
        viewModel.RefreshWindowsCommand.Execute(null);
    }
}
