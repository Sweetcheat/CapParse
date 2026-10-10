namespace CapParse.OCR;

/// <summary>
/// Coordinates OCR recognition for the application: it creates the engine
/// lazily on the first recognition request, reuses it for all later
/// requests, and releases it in <see cref="Dispose"/>.
///
/// The engine's model loading takes hundreds of milliseconds, so the
/// creation runs on a thread-pool thread, never on the caller's (UI)
/// thread. Concurrent first requests create the engine exactly once, and
/// an initialization failure leaves the coordinator usable: the next
/// request retries the creation.
///
/// Recognitions are serialized: RapidOcrNet does not document concurrent
/// inference on one instance, and the app never needs two in flight at
/// once (a new capture cancels the in-flight recognition).
/// </summary>
public sealed class OcrCoordinator : IDisposable
{
    private readonly Func<IOcrEngine> _engineFactory;
    private readonly object _engineGate = new();
    private readonly SemaphoreSlim _recognitionGate = new(1, 1);
    private IOcrEngine? _engine;
    private volatile bool _disposed;

    /// <summary>
    /// Creates a coordinator that builds a <see cref="RapidOcrEngine"/> on
    /// first use.
    /// </summary>
    public OcrCoordinator()
        : this(() => new RapidOcrEngine())
    {
    }

    /// <summary>
    /// Creates a coordinator with an explicit engine factory. The production
    /// app uses the parameterless constructor; tests substitute a fake
    /// engine through this one.
    /// </summary>
    public OcrCoordinator(Func<IOcrEngine> engineFactory)
    {
        _engineFactory = engineFactory ?? throw new ArgumentNullException(nameof(engineFactory));
    }

    /// <summary>
    /// Recognizes the text in <paramref name="image"/> with the
    /// application's engine, creating it on first use. An image without
    /// text yields a valid, empty <see cref="OcrResult"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">The coordinator was disposed.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    /// <exception cref="Exception">Engine initialization or recognition failed.</exception>
    public async Task<OcrResult> RecognizeAsync(
        ImageData image,
        CancellationToken cancellationToken)
    {
        if (image is null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var engine = await EnsureEngineAsync().ConfigureAwait(false);

        await _recognitionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await engine
                .RecognizeAsync(image, OcrOptions.Default, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _recognitionGate.Release();
        }
    }

    /// <summary>
    /// The engine, creating it on a background thread if this is the first
    /// request. Concurrent callers share a single creation: the lock makes
    /// exactly one factory call happen, and a failure is not cached, so a
    /// later request can retry.
    /// </summary>
    private Task<IOcrEngine> EnsureEngineAsync()
    {
        var engine = _engine;
        if (engine is not null)
        {
            return Task.FromResult(engine);
        }

        return Task.Run(() =>
        {
            lock (_engineGate)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(OcrCoordinator));
                }

                engine = _engine;
                if (engine is not null)
                {
                    return engine;
                }

                var created = _engineFactory();

                // The coordinator may have been disposed while the factory
                // was running: release the engine in that case so nothing
                // is left partially alive.
                if (_disposed)
                {
                    (created as IDisposable)?.Dispose();
                    throw new ObjectDisposedException(nameof(OcrCoordinator));
                }

                _engine = created;
                return created;
            }
        });
    }

    public void Dispose()
    {
        lock (_engineGate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // IOcrEngine does not mandate disposal; the production engine
            // implements IDisposable and is released through it.
            (_engine as IDisposable)?.Dispose();
            _engine = null;
        }
    }
}
