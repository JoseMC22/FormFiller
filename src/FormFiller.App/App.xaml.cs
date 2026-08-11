using System.Windows;
using FormFiller.Core.Licensing;

namespace FormFiller.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string? trialBanner = null;
        if (!TrialService.IsTrialGateDisabled)
        {
            var trial = new TrialService();
            var status = trial.Current;

            if (status.State is TrialState.Expired or TrialState.Tampered)
            {
                var message = status.State == TrialState.Tampered
                    ? "The trial license state could not be verified because the system clock appears to have been moved backwards. Restore the correct date or purchase a license to keep using FormFiller."
                    : "Your 15-day trial has ended. Purchase a license to keep using FormFiller.";
                MessageBox.Show(
                    message,
                    "Trial Expired",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Shutdown();
                return;
            }

            if (status.State == TrialState.TrialActive && status.RemainingDays is int remaining)
            {
                trialBanner = remaining == 1
                    ? "Trial: 1 day remaining"
                    : $"Trial: {remaining} days remaining";
            }
        }

        new MainWindow(trialBanner).Show();
    }
}
