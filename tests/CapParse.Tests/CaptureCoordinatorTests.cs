using CapParse.Capture;

namespace CapParse.Tests;

// M4-D: the coordinator has no logic of its own, so the meaningful
// automated check is a real end-to-end call. It requires at least one
// monitor (any configuration works; specific multi-monitor geometry is
// covered by VirtualDesktopTests). The bitmap is disposed by the
// coordinator itself, so a successful call also proves the lifetime is
// safely completed.
public class CaptureCoordinatorTests
{
    [Fact]
    public void StartCapture_RealDesktop_CompletesWithoutThrowing()
    {
        var coordinator = new CaptureCoordinator();

        coordinator.StartCapture();
    }
}
