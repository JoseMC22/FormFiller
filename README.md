# FormFiller — Getting Started (Local Test)

This guide explains how to run the **test form** (MuestraApp) and **FormFiller** on a development machine, and how to test the app end to end with the sample Excel file.

---

## 1. Start the test form (MuestraApp)

MuestraApp is a WinForms fixture that simulates a StarSoft-style business form. It is the target app you automate with FormFiller.

### Option A — Pre-built executable (recommended)

```powershell
& "C:\Users\mcjos\Documents\Proyectos\Automatizacion_2\fixtures\MuestraApp\bin\Release\net8.0-windows\MuestraApp.exe"
```

### Option B — Build and run from source

```powershell
cd C:\Users\mcjos\Documents\Proyectos\Automatizacion_2
dotnet build fixtures/MuestraApp/MuestraApp.csproj -c Release
dotnet run --project fixtures/MuestraApp/MuestraApp.csproj -c Release
```

> The fixture window appears with 15 controls: Codigo, Nombre, Direccion, Telefono, Ciudad, Email, DNI, Password, CUIT, Country (combo), Active (checkbox), Person/Company (radios), Date (picker), Observations (multi-line), plus Guardar / View Details / Close Details buttons.

---

## 2. Start FormFiller

### Installed version (Start Menu)

1. Open the **Start Menu** and search for **FormFiller**.
2. Launch the app.
3. The status bar shows the trial countdown (e.g. `Trial: 14 days remaining`).

### Developer mode (skip the trial)

The 15-day trial gate is active by default. To run without the trial during development:

```powershell
$env:FORMFILLER_SKIP_TRIAL = "1"
& "C:\Users\mcjos\Documents\Proyectos\Automatizacion_2\src\FormFiller.App\bin\Debug\net8.0-windows\FormFiller.App.exe"
```

Or run the installed version with the environment variable set:

```powershell
$env:FORMFILLER_SKIP_TRIAL = "1"
& "$env:LOCALAPPDATA\Programs\FormFiller\FormFiller.App.exe"
```

> Tip: set the variable in the same terminal session, otherwise the gate runs again.

---

## 3. End-to-end test with the sample Excel

A ready-made Excel file is included at:

```
samples/formfiller-test-data.xlsx
```

It contains 13 columns matching the MuestraApp field names (Codigo, Nombre, Direccion, Telefono, Ciudad, Email, DNI, Password, CUIT, Country, Active, Date, Observations) and 5 example rows.

### Suggested flow

1. **Start MuestraApp** (section 1).
2. **Start FormFiller** (section 2).
3. **Inspector tab** — select the `MuestraApp` process from the list, click **Capture**, and save the template.
4. **Mapping tab** — load `samples/formfiller-test-data.xlsx`, choose the sheet, and map each Excel column to the corresponding form field.
5. **Runner tab** — select the mapped Excel file, pick the target window (MuestraApp), and click **Run**. Watch each row fill the form and press Guardar.
6. **Recorder tab (REC)** — click **Start**, interact with MuestraApp by hand (type, select country, click Guardar), then **Stop** and save as a recipe. Load it from the Runner to replay.

---

## 4. Build the installer (optional)

```powershell
cd C:\Users\mcjos\Documents\Proyectos\Automatizacion_2
scripts\publish-release.ps1        # publishes Release + signs (dev cert)
scripts\build-installer.ps1        # compiles installer/FormFiller.iss
```

Output: `artifacts\FormFillerSetup-<version>.exe`

> For customer distribution, pass the commercial certificate:
> `scripts\publish-release.ps1 -CertificateThumbprint <SHA1>`
