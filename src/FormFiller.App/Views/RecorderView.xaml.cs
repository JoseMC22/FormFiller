using System.Windows.Controls;
using FormFiller.App.ViewModels;

namespace FormFiller.App.Views;

public partial class RecorderView : UserControl
{
    public RecorderView()
    {
        InitializeComponent();

        var viewModel = new RecorderViewModel();
        DataContext = viewModel;
        viewModel.RefreshWindowsCommand.Execute(null);
    }
}
