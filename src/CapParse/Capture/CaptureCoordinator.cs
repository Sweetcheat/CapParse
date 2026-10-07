using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace CapParse.Capture;

/// <summary>
/// Central entry point of the capture flow. The tray menu, the tray
/// double-click and the M3 global hotkey must all call this method.
/// </summary>
public sealed class CaptureCoordinator
{
    public void StartCapture()
    {
        // M2: entry point only. The real freeze-frame capture flow is
        // implemented in M3/M4 and will replace this body.
        MessageBox.Show("Capture is not implemented yet.", "CapParse",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
