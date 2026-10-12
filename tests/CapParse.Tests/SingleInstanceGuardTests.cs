using CapParse.Platform;

namespace CapParse.Tests;

// M10-B: the single-instance guard is a small, WPF-free wrapper around a
// named local mutex. The first owner wins, a second owner with the same name
// is denied, distinct names are independent, and releasing/disposing frees
// the name for a new owner. Ownership is fail-closed and safe to release
// multiple times. Each test uses a unique name so parallel runs of the suite
// never collide.
public class SingleInstanceGuardTests
{
    private static string UniqueName() => "CapParse.Test." + Guid.NewGuid().ToString("N");

    [Fact]
    public void FirstGuard_AcquiresTheMutex()
    {
        using var guard = new SingleInstanceGuard(UniqueName());

        var acquired = guard.TryAcquire();

        Assert.True(acquired);
        Assert.True(guard.IsOwned);
    }

    [Fact]
    public void SecondGuard_SameName_CannotAcquireWhileFirstOwns()
    {
        var name = UniqueName();
        var owner = new SingleInstanceGuard(name);
        var intruder = new SingleInstanceGuard(name);
        try
        {
            Assert.True(owner.TryAcquire()); // owned by this (test) thread

            // Windows mutexes are reentrant per thread, so the second guard
            // is run on a *different* thread that does not hold the mutex.
            // That mirrors a second process's instance, whose acquisition
            // must be denied while the first one owns it.
            var intruded = RunOnOtherThread(() => intruder.TryAcquire());

            Assert.False(intruded);
            Assert.False(intruder.IsOwned);
            Assert.True(owner.IsOwned);
        }
        finally
        {
            owner.Dispose();
            intruder.Dispose();
        }
    }

    [Fact]
    public void Guards_WithDifferentNames_AcquireIndependently()
    {
        using var first = new SingleInstanceGuard(UniqueName());
        using var second = new SingleInstanceGuard(UniqueName());

        Assert.True(first.TryAcquire());
        Assert.True(second.TryAcquire());
        Assert.True(first.IsOwned);
        Assert.True(second.IsOwned);
    }

    [Fact]
    public void AfterReleaseAndDispose_ANewGuard_AcquiresTheSameName()
    {
        var name = UniqueName();
        var first = new SingleInstanceGuard(name);
        Assert.True(first.TryAcquire());

        // Releasing and disposing frees the name for a new owner.
        first.Dispose();

        using var second = new SingleInstanceGuard(name);
        Assert.True(second.TryAcquire());
    }

    [Fact]
    public void Release_WhenNotOwned_IsASafeNoOp()
    {
        using var guard = new SingleInstanceGuard(UniqueName());

        // Never acquired: releasing must not throw.
        guard.Release();
        Assert.False(guard.IsOwned);
    }

    [Fact]
    public void DoubleDispose_DoesNotThrowOrDoubleRelease()
    {
        var name = UniqueName();
        var guard = new SingleInstanceGuard(name);
        guard.TryAcquire();

        guard.Dispose();
        guard.Dispose(); // Second dispose is a no-op.

        // No exception, and the name is free for a new owner.
        using var next = new SingleInstanceGuard(name);
        Assert.True(next.TryAcquire());
    }

    [Fact]
    public void TryAcquire_AfterDispose_Throws()
    {
        var guard = new SingleInstanceGuard(UniqueName());
        guard.Dispose();

        Assert.Throws<ObjectDisposedException>(() => guard.TryAcquire());
    }

    // The abandoned-mutex path (its owning process died without releasing)
    // is handled in the guard by catching AbandonedMutexException and counting
    // the result as owned. It is intentionally not unit-tested here: reliably
    // reproducing it requires a helper process that creates the mutex and exits
    // without releasing it, which is disproportionate infrastructure for this
    // low-level scenario.

    private static bool RunOnOtherThread(Func<bool> action)
    {
        var result = false;
        var thread = new Thread(() => result = action());
        thread.Start();
        thread.Join();
        return result;
    }
}
