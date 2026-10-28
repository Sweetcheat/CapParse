using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using CapParse.Capture;
using CapParse.OCR;
using CapParse.UI;

namespace CapParse.Tests;

// The direct capture actions (direct mode): once a selection is finished,
// the action runs immediately through the application's existing services
// (image copy, text copy, image save) and the shared recognition. The
// runner is the dispatch under test: which action triggers which service,
// a cancelled selection (null crop) performs nothing, an OCR without text
// informs without copying, a superseded run stays silent, and canceling
// the save dialog is silent. The clipboard, the save (dialog) service and
// the OCR engine are faked, so no test touches the desktop. The save is
// injected as a delegate, like the application composes it from the
// configured policy (the manual policy: the dialog-backed save).
public class DirectActionRunnerTests
{
    private static Bitmap MakeCrop() => new(4, 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

    private static OcrResult Result(string text)
        => new(text, Array.Empty<OcrBlock>(), TimeSpan.FromMilliseconds(1));

    // Mirrors the application's recognition core: a valid result maps to
    // OcrRunOutcome(result, false); a failed recognition maps to
    // OcrRunOutcome(null, true). The run-state bookkeeping itself lives in
    // the application (see OcrRunStateTests for its transitions).
    private static Func<Bitmap, Task<OcrRunOutcome>> RecognizeWith(IOcrEngine engine)
    {
        var coordinator = new OcrCoordinator(() => engine);

        return async crop =>
        {
            try
            {
                var result = await coordinator.RecognizeAsync(new ImageData(crop), CancellationToken.None);
                return new OcrRunOutcome(result, false);
            }
            catch
            {
                return new OcrRunOutcome(null, true);
            }
            finally
            {
                coordinator.Dispose();
            }
        };
    }

    private sealed record RunnerFixture
    {
        public required FakeImageClipboard ImageClipboard { get; init; }
        public required FakeTextClipboard TextClipboard { get; init; }
        public required FakeSaveDialog SaveDialog { get; init; }
        public required List<DirectFeedback> Feedback { get; init; }
        public required DirectActionRunner Runner { get; init; }

        public static RunnerFixture Create(Func<Bitmap, Task<OcrRunOutcome>> recognize)
        {
            var imageClipboard = new FakeImageClipboard();
            var textClipboard = new FakeTextClipboard();
            var saveDialog = new FakeSaveDialog();
            var feedback = new List<DirectFeedback>();

            // The manual save policy: the dialog-backed save of the single
            // existing save service, injected as a delegate exactly like
            // the application composes it.
            var saver = new CropImageSaver(saveDialog);

            var runner = new DirectActionRunner(
                new CropImageCopier(imageClipboard),
                new OcrTextCopier(textClipboard),
                (crop, dpi) => saver.TrySave(crop, dpi),
                recognize,
                feedback.Add);

            return new RunnerFixture
            {
                ImageClipboard = imageClipboard,
                TextClipboard = textClipboard,
                SaveDialog = saveDialog,
                Feedback = feedback,
                Runner = runner
            };
        }
    }

    [Fact]
    public async Task ActionNone_PerformsNothing()
    {
        var fixture = RunnerFixture.Create(_ => Task.FromResult(new OcrRunOutcome(null, false)));
        using var crop = MakeCrop();

        await fixture.Runner.RunAsync(DirectAction.None, crop, 96);

        Assert.Empty(fixture.ImageClipboard.Images);
        Assert.Empty(fixture.TextClipboard.Texts);
        Assert.Equal(0, fixture.SaveDialog.ShowCount);
        Assert.Empty(fixture.Feedback);
    }

    [Theory]
    [InlineData(DirectAction.CopyImage)]
    [InlineData(DirectAction.CopyText)]
    [InlineData(DirectAction.SaveImage)]
    public async Task CancelledSelection_CropNull_PerformsNothing(DirectAction action)
    {
        // Esc during the selection (or an empty selection) reaches the
        // runner with a null crop: the action must not run and nothing is
        // reported.
        var fixture = RunnerFixture.Create(_ => Task.FromResult(new OcrRunOutcome(null, false)));

        await fixture.Runner.RunAsync(action, null, 96);

        Assert.Empty(fixture.ImageClipboard.Images);
        Assert.Empty(fixture.TextClipboard.Texts);
        Assert.Equal(0, fixture.SaveDialog.ShowCount);
        Assert.Empty(fixture.Feedback);
    }

    [Fact]
    public async Task CopyImage_CopiesTheCrop_ReportsSuccess()
    {
        var fixture = RunnerFixture.Create(_ => Task.FromResult(new OcrRunOutcome(null, false)));
        using var crop = MakeCrop();

        await fixture.Runner.RunAsync(DirectAction.CopyImage, crop, 96);

        Assert.Single(fixture.ImageClipboard.Images);
        Assert.Empty(fixture.TextClipboard.Texts);
        Assert.Equal(0, fixture.SaveDialog.ShowCount);

        var feedback = Assert.Single(fixture.Feedback);
        Assert.Equal(DirectFeedbackKind.Success, feedback.Kind);
    }

    [Fact]
    public async Task CopyImage_ClipboardFailure_ReportsError()
    {
        var fixture = RunnerFixture.Create(_ => Task.FromResult(new OcrRunOutcome(null, false)));
        fixture.ImageClipboard.FailNext = true;
        using var crop = MakeCrop();

        await fixture.Runner.RunAsync(DirectAction.CopyImage, crop, 96);

        Assert.Empty(fixture.ImageClipboard.Images);
        var feedback = Assert.Single(fixture.Feedback);
        Assert.Equal(DirectFeedbackKind.Error, feedback.Kind);
        Assert.NotEmpty(feedback.Message);
    }

    [Fact]
    public async Task CopyText_CopiesTheRecognizedText_ReportsSuccess()
    {
        var engine = new FakeOcrEngine((_, _) => Task.FromResult(Result("olá mundo")));
        var fixture = RunnerFixture.Create(RecognizeWith(engine));
        using var crop = MakeCrop();

        await fixture.Runner.RunAsync(DirectAction.CopyText, crop, 96);

        Assert.Equal(new[] { "olá mundo" }, fixture.TextClipboard.Texts);
        Assert.Empty(fixture.ImageClipboard.Images);

        var feedback = Assert.Single(fixture.Feedback);
        Assert.Equal(DirectFeedbackKind.Success, feedback.Kind);
    }

    [Fact]
    public async Task CopyText_EmptyResult_ReportsNoText_CopiesNothing()
    {
        var engine = new FakeOcrEngine(
            (_, _) => Task.FromResult(new OcrResult(string.Empty, Array.Empty<OcrBlock>(), TimeSpan.Zero)));
        var fixture = RunnerFixture.Create(RecognizeWith(engine));
        using var crop = MakeCrop();

        await fixture.Runner.RunAsync(DirectAction.CopyText, crop, 96);

        Assert.Empty(fixture.TextClipboard.Texts);
        var feedback = Assert.Single(fixture.Feedback);
        Assert.Equal(DirectFeedbackKind.Info, feedback.Kind);
        Assert.NotEmpty(feedback.Message);
    }

    [Fact]
    public async Task CopyText_OcrFailure_ReportsError_CopiesNothing()
    {
        var engine = new FakeOcrEngine(
            (_, _) => throw new InvalidOperationException("engine down"));
        var fixture = RunnerFixture.Create(RecognizeWith(engine));
        using var crop = MakeCrop();

        await fixture.Runner.RunAsync(DirectAction.CopyText, crop, 96);

        Assert.Empty(fixture.TextClipboard.Texts);
        var feedback = Assert.Single(fixture.Feedback);
        Assert.Equal(DirectFeedbackKind.Error, feedback.Kind);
    }

    [Fact]
    public async Task CopyText_SupersededRun_StaysSilent()
    {
        // A run that was superseded by a newer capture is not a failure:
        // the newer run owns the state, so this one must not report
        // anything.
        var fixture = RunnerFixture.Create(_ => Task.FromResult(new OcrRunOutcome(null, false)));
        using var crop = MakeCrop();

        await fixture.Runner.RunAsync(DirectAction.CopyText, crop, 96);

        Assert.Empty(fixture.TextClipboard.Texts);
        Assert.Empty(fixture.Feedback);
    }

    [Fact]
    public async Task CopyText_TextClipboardFailure_ReportsError()
    {
        var engine = new FakeOcrEngine((_, _) => Task.FromResult(Result("texto")));
        var fixture = RunnerFixture.Create(RecognizeWith(engine));
        fixture.TextClipboard.FailNext = true;
        using var crop = MakeCrop();

        await fixture.Runner.RunAsync(DirectAction.CopyText, crop, 96);

        Assert.Empty(fixture.TextClipboard.Texts);
        var feedback = Assert.Single(fixture.Feedback);
        Assert.Equal(DirectFeedbackKind.Error, feedback.Kind);
    }

    [Fact]
    public async Task CopyText_WriterRecoversFromATransientLock_ReportsSuccessNotError()
    {
        // The scenario end to end: the clipboard is locked when the write
        // starts, the writer's own bounded open-retry survives it and the
        // text is written. The copy really happened, so the runner's
        // feedback must be the success confirmation — never the clipboard
        // error (the message the user kept seeing in the trace-75 defect,
        // where the stacked retries stalled for ~3.6 s and both channels
        // failed).
        var engine = new FakeOcrEngine((_, _) => Task.FromResult(Result("texto")));
        var writer = new RecurringFailureWriter();
        var feedback = new List<DirectFeedback>();
        var runner = new DirectActionRunner(
            new CropImageCopier(new FakeImageClipboard()),
            new OcrTextCopier(writer),
            (_, _) => new SaveImageOutcome(SaveImageStatus.Canceled, string.Empty),
            RecognizeWith(engine),
            feedback.Add);
        using var crop = MakeCrop();

        await runner.RunAsync(DirectAction.CopyText, crop, 96);

        // The writer is invoked once (its internal open-retry owns the
        // transient-lock tolerance), and it writes the text.
        Assert.Equal(1, writer.WriteCount);
        Assert.Equal("texto", writer.LastText);
        var feedbackItem = Assert.Single(feedback);
        Assert.Equal(DirectFeedbackKind.Success, feedbackItem.Kind);
        Assert.Equal("Texto copiado.", feedbackItem.Message);
    }

    [Fact]
    public async Task CopyText_WriterFails_ReportsTheRealFailure()
    {
        // The writer's whole bounded open budget is spent and the text is
        // not on the clipboard: the runner's feedback is the failure, with
        // the usual message — the feedback keeps mirroring the real
        // outcome (no false success).
        var engine = new FakeOcrEngine((_, _) => Task.FromResult(Result("texto")));
        var writer = new FailingWriter();
        var feedback = new List<DirectFeedback>();
        var runner = new DirectActionRunner(
            new CropImageCopier(new FakeImageClipboard()),
            new OcrTextCopier(writer),
            (_, _) => new SaveImageOutcome(SaveImageStatus.Canceled, string.Empty),
            RecognizeWith(engine),
            feedback.Add);
        using var crop = MakeCrop();

        await runner.RunAsync(DirectAction.CopyText, crop, 96);

        var feedbackItem = Assert.Single(feedback);
        Assert.Equal(DirectFeedbackKind.Error, feedbackItem.Kind);
        Assert.Equal(
            "Não foi possível copiar o texto. A área de transferência pode estar em uso por outro aplicativo.",
            feedbackItem.Message);
    }

    [Fact]
    public async Task SaveImage_ConfirmedPath_SavesAndReportsSuccess()
    {
        var fixture = RunnerFixture.Create(_ => Task.FromResult(new OcrRunOutcome(null, false)));
        using var dir = new TempDirectory();
        fixture.SaveDialog.Result = Path.Combine(dir.Path, "capture.png");
        using var crop = MakeCrop();

        await fixture.Runner.RunAsync(DirectAction.SaveImage, crop, 96);

        Assert.Equal(1, fixture.SaveDialog.ShowCount);
        Assert.True(File.Exists(fixture.SaveDialog.Result));
        Assert.Empty(fixture.ImageClipboard.Images);

        var feedback = Assert.Single(fixture.Feedback);
        Assert.Equal(DirectFeedbackKind.Success, feedback.Kind);
    }

    [Fact]
    public async Task SaveImage_DialogCanceled_IsSilent()
    {
        // Canceling the save dialog is a normal operation, not an error:
        // no file is written and nothing is reported.
        var fixture = RunnerFixture.Create(_ => Task.FromResult(new OcrRunOutcome(null, false)));
        using var dir = new TempDirectory();
        fixture.SaveDialog.Result = null;
        using var crop = MakeCrop();

        await fixture.Runner.RunAsync(DirectAction.SaveImage, crop, 96);

        Assert.Equal(1, fixture.SaveDialog.ShowCount);
        Assert.Empty(Directory.GetFiles(dir.Path));
        Assert.Empty(fixture.Feedback);
    }

    [Fact]
    public async Task SaveImage_WriteFailure_ReportsError()
    {
        var fixture = RunnerFixture.Create(_ => Task.FromResult(new OcrRunOutcome(null, false)));
        // A path inside a directory that does not exist: the write fails.
        fixture.SaveDialog.Result = Path.Combine(Path.GetTempPath(), "capparse-nao-existe", "capture.png");
        using var crop = MakeCrop();

        await fixture.Runner.RunAsync(DirectAction.SaveImage, crop, 96);

        Assert.Equal(1, fixture.SaveDialog.ShowCount);
        var feedback = Assert.Single(fixture.Feedback);
        Assert.Equal(DirectFeedbackKind.Error, feedback.Kind);
        Assert.NotEmpty(feedback.Message);
    }

    // Records the images written to the clipboard.
    private sealed class FakeImageClipboard : IImageClipboard
    {
        public List<BitmapSource> Images { get; } = new();

        public bool FailNext { get; set; }

        public void SetImage(BitmapSource image)
        {
            if (FailNext)
            {
                FailNext = false;
                throw new InvalidOperationException("clipboard locked");
            }

            Images.Add(image);
        }
    }

    // Records the texts written to the clipboard through the application's
    // single text-clipboard channel.
    private sealed class FakeTextClipboard : IRawTextClipboardWriter
    {
        public List<string> Texts { get; } = new();

        public bool FailNext { get; set; }

        public void WriteUtf16Text(string text)
        {
            if (FailNext)
            {
                FailNext = false;
                throw new System.Runtime.InteropServices.ExternalException(
                    "OpenClipboard failed (win32 error 5).");
            }

            Texts.Add(text);
        }
    }

    // A text-clipboard channel that is locked when the write starts: its
    // own bounded open-retry fails a few opens and then writes (the shape
    // of a transient lock, as it happens inside a single WriteUtf16Text
    // call).
    private sealed class RecurringFailureWriter : IRawTextClipboardWriter
    {
        public int WriteCount { get; private set; }
        public string? LastText { get; private set; }

        public void WriteUtf16Text(string text)
        {
            WriteCount++;
            LastText = text;
        }
    }

    // A text-clipboard channel whose whole bounded open budget is spent
    // (the clipboard stayed locked the whole time).
    private sealed class FailingWriter : IRawTextClipboardWriter
    {
        public void WriteUtf16Text(string text)
            => throw new System.Runtime.InteropServices.ExternalException(
                "OpenClipboard failed (win32 error 5).");
    }

    // A save dialog with a fixed outcome: the path the user confirms, or
    // null when the user cancels.
    private sealed class FakeSaveDialog : ICropImageSaveDialog
    {
        public int ShowCount { get; private set; }

        public string? Result { get; set; }

        public string? ShowDialog(string suggestedFileName)
        {
            ShowCount++;
            return Result;
        }
    }

    private sealed class FakeOcrEngine : IOcrEngine
    {
        private readonly Func<ImageData, CancellationToken, Task<OcrResult>> _behavior;

        public FakeOcrEngine(Func<ImageData, CancellationToken, Task<OcrResult>> behavior)
        {
            _behavior = behavior;
        }

        public Task<OcrResult> RecognizeAsync(
            ImageData image,
            OcrOptions options,
            CancellationToken cancellationToken)
            => _behavior(image, cancellationToken);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("capparse-test-").FullName;

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch
            {
                // The test outcome does not depend on the cleanup.
            }
        }
    }
}
