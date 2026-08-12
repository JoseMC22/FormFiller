using System.Globalization;
using System.Windows.Data;
using FormFiller.Core.Models;

namespace FormFiller.App.Converters;

/// <summary>
/// Converts <see cref="FieldType"/> and <see cref="RecipeStepType"/> values to
/// their neutral Spanish display labels. The enum values themselves are never
/// renamed (they persist as ints in the SQLite stores); this converter only
/// translates them for the UI.
/// </summary>
public sealed class EnumToSpanishConverter : IValueConverter
{
    private static readonly IReadOnlyDictionary<FieldType, string> FieldTypeLabels =
        new Dictionary<FieldType, string>
        {
            [FieldType.Text] = "Texto",
            [FieldType.ComboBox] = "Lista desplegable",
            [FieldType.CheckBox] = "Casilla de verificación",
            [FieldType.RadioButton] = "Botón de opción",
            [FieldType.DatePicker] = "Selector de fecha",
            [FieldType.Button] = "Botón",
            [FieldType.Other] = "Otro"
        };

    private static readonly IReadOnlyDictionary<RecipeStepType, string> RecipeStepTypeLabels =
        new Dictionary<RecipeStepType, string>
        {
            [RecipeStepType.SetField] = "Asignar campo",
            [RecipeStepType.ClickButton] = "Clic en botón",
            [RecipeStepType.Wait] = "Espera",
            [RecipeStepType.WaitForWindow] = "Esperar ventana",
            [RecipeStepType.ClickIfWindowVisible] = "Clic si la ventana está visible"
        };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        switch (value)
        {
            case FieldType fieldType when FieldTypeLabels.TryGetValue(fieldType, out var fieldLabel):
                return fieldLabel;
            case RecipeStepType stepType when RecipeStepTypeLabels.TryGetValue(stepType, out var stepLabel):
                return stepLabel;
            default:
                return value?.ToString() ?? string.Empty;
        }
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("EnumToSpanishConverter is one-way only.");
    }
}
