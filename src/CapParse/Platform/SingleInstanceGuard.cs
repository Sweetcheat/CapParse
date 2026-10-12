namespace CapParse.Platform;

/// <summary>
/// Guarantees that a single instance of the application runs for the current
/// user and Windows session, using a named local mutex. The name carries no
/// "Global\" prefix, so the scope is the user session, not the whole machine.
///
/// The mutex is acquired on the thread that calls <see cref="TryAcquire"/>
/// (the WPF startup/UI thread in the app) and must be released by that same
/// thread through <see cref="Release"/>/<see cref="Dispose"/>. .NET ties
/// mutex ownership to the acquiring thread, so <see cref="Release"/> is a safe
/// no-op if it is reached from a different thread, and the OS reclaims the
/// mutex automatically when the process exits, which also covers crashes and
/// hard kills.
///
/// Ownership is fail-closed: if the handle cannot be created or the ownership
/// cannot be confirmed, <see cref="TryAcquire"/> returns false so the caller
/// refuses to start a second (or any) functional instance.
/// </summary>
public sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex _mutex;
    private bool _owns;
    private bool _disposed;

    /// <summary>
    /// Creates the guard for the named mutex without acquiring it. Call
    /// <see cref="TryAcquire"/> to take ownership.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    public SingleInstanceGuard(string name)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        // initiallyOwned is false so a mutex that another instance already
        // holds does not block startup; TryAcquire decides ownership.
        _mutex = new Mutex(initiallyOwned: false, name);
    }

    /// <summary>
    /// True when this guard owns the mutex, i.e. a single instance is
    /// guaranteed.
    /// </summary>
    public bool IsOwned => _owns;

    /// <summary>
    /// Tries to acquire the mutex without blocking. Returns true when this
    /// process is now the single owner. A previously-abandoned mutex (its
    /// owning process died without releasing it) is acquired here and counts
    /// as owned.
    /// </summary>
    public bool TryAcquire()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            _owns = _mutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            // The previous owner terminated without releasing the mutex; .NET
            // still hands ownership to this thread, so we now own it.
            _owns = true;
        }
        catch
        {
            // Ownership could not be confirmed: fail closed so a second
            // instance is never started.
            _owns = false;
        }

        return _owns;
    }

    /// <summary>
    /// Releases ownership if this guard (on the owning thread) holds it. Safe
    /// to call multiple times and safe from a non-owning thread (a no-op).
    /// </summary>
    public void Release()
    {
        if (_disposed || !_owns)
        {
            return;
        }

        try
        {
            _mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // Not the owning thread: there is nothing to release; the OS
            // reclaims the mutex when the process exits.
        }

        _owns = false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Release();
        _mutex.Dispose();
    }
}
