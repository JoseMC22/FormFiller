namespace FormFiller.Core.Automation;

public sealed record ProcessWindowInfo(int ProcessId, string ProcessName, string WindowTitle, IntPtr MainWindowHandle);
