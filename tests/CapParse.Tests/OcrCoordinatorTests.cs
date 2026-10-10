using System.Drawing;
using CapParse.OCR;

namespace CapParse.Tests;

// M8-A: the coordinator manages the engine's lifecycle (lazy creation,
// reuse, single concurrent initialization, retry after a failed
// initialization), serializes recognitions and propagates cancellation
// and failures distinctly. All checks run against a fake engine, so none
// of them load the real models.
public class OcrCoordinatorTests
{
    [Fact]
    public async Task Recognize_PassesTheCallerImageToTheEngine()
    {
        var bitmap = new Bitmap(8, 8);
        var engineResult = Result("engine saw the image");
        var engine = new FakeOcrEngine((_, _) => Task.FromResult(engineResult));
        using var coordinator = new OcrCoordinator(() => engine);

        try
        {
            var result = await coordinator.RecognizeAsync(new ImageData(bitmap), CancellationToken.None);

            Assert.Same(bitmap, engine.LastImage?.Bitmap);
            Assert.Same(engineResult, result);
        }
        finally
        {
            bitmap.Dispose();
        }
    }

    [Fact]
    public async Task Recognize_ReturnsTheEngineResultWithoutLosingInformation()
    {
        var bitmap = new Bitmap(4, 4);
        var block = new OcrBlock("linha", 0.9f, new Rectangle(1, 2, 3, 4));
        var expected = new OcrResult("linha", new[] { block }, TimeSpan.FromMilliseconds(42));
        using var coordinator = new OcrCoordinator(() => new FakeOcrEngine((_, _) => Task.FromResult(expected)));

        try
        {
            var result = await coordinator.RecognizeAsync(new ImageData(bitmap), CancellationToken.None);

            Assert.Same(expected, result);
            Assert.Equal("linha", result.Text);
            Assert.Single(result.Blocks);
            Assert.Equal(0.9f, result.Blocks[0].Confidence);
            Assert.Equal(new Rectangle(1, 2, 3, 4), result.Blocks[0].Bounds);
        }
        finally
        {
            bitmap.Dispose();
        }
    }

    [Fact]
    public async Task Recognize_ImageWithoutText_IsAValidResult()
    {
        var bitmap = new Bitmap(4, 4);
        var empty = new OcrResult(string.Empty, Array.Empty<OcrBlock>(), TimeSpan.Zero);
        using var coordinator = new OcrCoordinator(() => new FakeOcrEngine((_, _) => Task.FromResult(empty)));

        try
        {
            var result = await coordinator.RecognizeAsync(new ImageData(bitmap), CancellationToken.None);

            // An empty result is a success, not an exception.
            Assert.Same(empty, result);
            Assert.Empty(result.Text);
            Assert.Empty(result.Blocks);
        }
        finally
        {
            bitmap.Dispose();
        }
    }

    [Fact]
    public async Task Engine_IsCreatedOnlyOnFirstRecognition()
    {
        var creations = 0;
        using var coordinator = new OcrCoordinator(() =>
        {
            creations++;
            return new FakeOcrEngine((_, _) => Task.FromResult(Result("x")));
        });

        Assert.Equal(0, creations);

        await coordinator.RecognizeAsync(NewImage(), CancellationToken.None);

        Assert.Equal(1, creations);
    }

    [Fact]
    public async Task Engine_IsReusedAcrossSuccessiveRecognitions()
    {
        var creations = 0;
        using var coordinator = new OcrCoordinator(() =>
        {
            creations++;
            return new FakeOcrEngine((_, _) => Task.FromResult(Result("x")));
        });

        await coordinator.RecognizeAsync(NewImage(), CancellationToken.None);
        await coordinator.RecognizeAsync(NewImage(), CancellationToken.None);
        await coordinator.RecognizeAsync(NewImage(), CancellationToken.None);

        Assert.Equal(1, creations);
    }

    [Fact]
    public async Task ConcurrentFirstRecognizations_CreateTheEngineExactlyOnce()
    {
        var creations = 0;
        using var coordinator = new OcrCoordinator(() =>
        {
            creations++;
            // Widen the window in which both callers can be waiting on the
            // same in-flight creation. The factory runs on a thread-pool
            // thread inside the coordinator, so a plain sleep is safe.
            Thread.Sleep(250);
            return new FakeOcrEngine((image, _) => Task.FromResult(Result(image.Bitmap.Width.ToString())));
        });

        var first = coordinator.RecognizeAsync(NewImage(11), CancellationToken.None);
        var second = coordinator.RecognizeAsync(NewImage(22), CancellationToken.None);

        await Task.WhenAll(first, second);

        Assert.Equal(1, creations);
    }

    [Fact]
    public async Task InitializationFailure_LeavesTheCoordinatorUsable()
    {
        var attempts = 0;
        IOcrEngine CreateEngine()
        {
            attempts++;
            if (attempts == 1)
            {
                throw new InvalidOperationException("model files missing");
            }

            return new FakeOcrEngine((_, _) => Task.FromResult(Result("recovered")));
        }

        using var coordinator = new OcrCoordinator(CreateEngine);

        // The first request fails with the initialization failure.
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.RecognizeAsync(NewImage(), CancellationToken.None));
        Assert.Equal("model files missing", failure.Message);

        // The failed creation is not cached: the next request retries and
        // succeeds.
        var result = await coordinator.RecognizeAsync(NewImage(), CancellationToken.None);
        Assert.Equal("recovered", result.Text);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task PreCancelledToken_FailsWithOperationCanceled()
    {
        using var coordinator = new OcrCoordinator(() =>
            new FakeOcrEngine((_, _) => Task.FromResult(Result("x"))));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => coordinator.RecognizeAsync(NewImage(), cancellation.Token));
    }

    [Fact]
    public async Task Cancellation_DuringRecognition_IsDistinguishedFromAFailure()
    {
        using var coordinator = new OcrCoordinator(() =>
            new FakeOcrEngine((_, token) => Task.Run(async () =>
            {
                // The recognition outlives the cancellation request, so
                // the engine reports the cancellation itself.
                await Task.Delay(100);
                token.ThrowIfCancellationRequested();
                return Result("x");
            })));

        using var cancellation = new CancellationTokenSource();
        var recognition = coordinator.RecognizeAsync(NewImage(), cancellation.Token);

        await Task.Delay(20);
        cancellation.Cancel();

        // The engine reports the cancellation as OperationCanceledException;
        // the coordinator must not turn it into a generic failure.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => recognition);
    }

    [Fact]
    public async Task EngineFailure_PropagatesAsAFailure()
    {
        using var coordinator = new OcrCoordinator(() =>
            new FakeOcrEngine((_, _) => Task.FromException<OcrResult>(
                new InvalidOperationException("inference failed"))));

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.RecognizeAsync(NewImage(), CancellationToken.None));
        Assert.Equal("inference failed", failure.Message);
    }

    [Fact]
    public async Task Dispose_ReleasesTheEngine()
    {
        var engine = new FakeOcrEngine((_, _) => Task.FromResult(Result("x")));
        var coordinator = new OcrCoordinator(() => engine);

        await coordinator.RecognizeAsync(NewImage(), CancellationToken.None);
        Assert.False(engine.Disposed);

        coordinator.Dispose();

        Assert.True(engine.Disposed);
    }

    [Fact]
    public void Dispose_WithoutAnyRecognition_CreatesNoEngine()
    {
        var creations = 0;
        var coordinator = new OcrCoordinator(() =>
        {
            creations++;
            return new FakeOcrEngine((_, _) => Task.FromResult(Result("x")));
        });

        coordinator.Dispose();

        Assert.Equal(0, creations);
    }

    [Fact]
    public async Task Recognize_AfterDispose_Fails()
    {
        var coordinator = new OcrCoordinator(() =>
            new FakeOcrEngine((_, _) => Task.FromResult(Result("x"))));
        coordinator.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => coordinator.RecognizeAsync(NewImage(), CancellationToken.None));
    }

    [Fact]
    public async Task ConcurrentRecognitions_DeliverTheirOwnResults()
    {
        using var coordinator = new OcrCoordinator(() =>
            new FakeOcrEngine((image, _) =>
            {
                // Delay so both recognitions are in flight at the same time;
                // the engine is invoked on a thread-pool thread, so a plain
                // sleep is safe.
                Thread.Sleep(100);
                return Task.FromResult(Result(image.Bitmap.Width.ToString()));
            }));

        var firstBitmap = new Bitmap(111, 111);
        var secondBitmap = new Bitmap(222, 222);
        try
        {
            var first = coordinator.RecognizeAsync(new ImageData(firstBitmap), CancellationToken.None);
            var second = coordinator.RecognizeAsync(new ImageData(secondBitmap), CancellationToken.None);

            var results = await Task.WhenAll(first, second);

            // Serialized recognitions must not hand one caller the other's
            // result: each result describes the image it was asked for.
            Assert.Contains("111", results.Select(r => r.Text));
            Assert.Contains("222", results.Select(r => r.Text));
            Assert.NotEqual(results[0].Text, results[1].Text);
        }
        finally
        {
            firstBitmap.Dispose();
            secondBitmap.Dispose();
        }
    }

    private static ImageData NewImage(int size = 4)
        => new(new Bitmap(size, size));

    private static OcrResult Result(string text)
        => new(text, Array.Empty<OcrBlock>(), TimeSpan.Zero);

    /// <summary>
    /// A controllable <see cref="IOcrEngine"/> for deterministic lifecycle
    /// tests: it records the last image it received, never disposes it,
    /// and produces whatever its behavior delegate returns or throws.
    /// </summary>
    private sealed class FakeOcrEngine : IOcrEngine, IDisposable
    {
        private readonly Func<ImageData, CancellationToken, Task<OcrResult>> _behavior;
        private bool _disposed;

        public FakeOcrEngine(Func<ImageData, CancellationToken, Task<OcrResult>> behavior)
        {
            _behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
        }

        public ImageData? LastImage { get; private set; }

        public bool Disposed => _disposed;

        public Task<OcrResult> RecognizeAsync(
            ImageData image,
            OcrOptions options,
            CancellationToken cancellationToken)
        {
            if (image is null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            ObjectDisposedException.ThrowIf(_disposed, this);
            LastImage = image;
            return _behavior(image, cancellationToken);
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
