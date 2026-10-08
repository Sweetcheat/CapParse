namespace CapParse.Capture;

/// <summary>
/// Central entry point of the capture flow. The tray menu, the tray
/// double-click and the M3 global hotkey must all call this method.
/// </summary>
public sealed class CaptureCoordinator
{
    public CaptureResult StartCapture()
    {
        // M4-E: the result owns the captured bitmap. The caller owns the
        // result and must dispose it when the capture flow is done.
        return DesktopCapture.CaptureVirtualDesktop();
    }
}
