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
                    ? "No se pudo verificar el estado de la licencia de prueba porque el reloj del sistema parece haberse retrocedido. Restaure la fecha correcta o adquiera una licencia para seguir usando FormFiller."
                    : "Su prueba de 15 días ha finalizado. Adquiera una licencia para seguir usando FormFiller.";
                MessageBox.Show(
                    message,
                    "Prueba caducada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Shutdown();
                return;
            }

            if (status.State == TrialState.TrialActive && status.RemainingDays is int remaining)
            {
                trialBanner = remaining == 1
                    ? "Prueba: 1 día restante"
                    : $"Prueba: {remaining} días restantes";
            }
        }

        new MainWindow(trialBanner).Show();
    }
}
