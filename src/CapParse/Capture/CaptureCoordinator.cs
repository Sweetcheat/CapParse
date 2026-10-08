namespace CapParse.Capture;

/// <summary>
/// Central entry point of the capture flow. The tray menu, the tray
/// double-click and the M3 global hotkey must all call this method.
/// </summary>
public sealed class CaptureCoordinator
{
    public void StartCapture()
    {
        // M4-D: the coordinator drives the M4-C virtual-desktop capture
        // pipeline. The bitmap is integration proof only and is disposed
        // immediately; the in-memory capture-result ownership is designed
        // in M4-E.
        using var bitmap = DesktopCapture.CaptureVirtualDesktop();
    }
}
