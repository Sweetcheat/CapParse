using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using CapParse.OCR;
using Xunit.Abstractions;

namespace CapParse.Tests;

/// <summary>
/// One real engine for the whole smoke test class: model initialization is
/// the expensive part and the engine is safe to share across sequential
/// recognitions.
/// </summary>
public sealed class OcrEngineFixture : IDisposable
{
    public RapidOcrEngine Engine { get; }

    public TimeSpan InitializationTime { get; }

    public OcrEngineFixture()
    {
        var stopwatch = Stopwatch.StartNew();
        Engine = new RapidOcrEngine();
        stopwatch.Stop();
        InitializationTime = stopwatch.Elapsed;
    }

    public void Dispose() => Engine.Dispose();
}

// Real-mechanism smoke tests: the PP-OCRv6 Small models are copied next to
// the test binary by build/OcrModels.targets and loaded through the same
// path resolution the application uses. These tests prove the models are
// integrated and the engine executes; per-image quality (CER) is measured
// in M7-C on a reference collection.
public class OcrSmokeTests : IClassFixture<OcrEngineFixture>
{
    private readonly OcrEngineFixture _fixture;
    private readonly ITestOutputHelper _output;

    public OcrSmokeTests(OcrEngineFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    private static Bitmap CreateTextImage(string text, float fontSize)
    {
        // Clean black-on-white rendering of a common UI font: the fairest
        // synthetic input for a screen-content OCR engine.
        var image = new Bitmap(1200, 220, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(image);
        graphics.Clear(Color.White);
        using var font = new Font("Segoe UI", fontSize, FontStyle.Regular, GraphicsUnit.Point);
        using var brush = new SolidBrush(Color.Black);
        graphics.DrawString(text, font, brush, 40, 60);
        return image;
    }

    [Fact]
    public async Task Recognize_PortugueseSyntheticImage_ReturnsRecognizedText()
    {
        using var bitmap = CreateTextImage("Olá, ação, coração, informação e português.", 32);

        var result = await _fixture.Engine
            .RecognizeAsync(new ImageData(bitmap), OcrOptions.Default, CancellationToken.None);

        _output.WriteLine($"Recognized text: \"{result.Text}\"");
        _output.WriteLine($"Blocks: {result.Blocks.Count}");
        foreach (var block in result.Blocks)
        {
            _output.WriteLine($"  [{block.Text}] confidence={block.Confidence:0.00} bounds={block.Bounds}");
        }
        _output.WriteLine($"Recognition time: {result.ProcessingTime.TotalMilliseconds:0.0} ms");
        _output.WriteLine($"Engine initialization time: {_fixture.InitializationTime.TotalMilliseconds:0.0} ms");

        Assert.True(result.ProcessingTime > TimeSpan.Zero);
        Assert.False(string.IsNullOrWhiteSpace(result.Text));
        Assert.Contains("Olá", result.Text);
    }

    [Fact]
    public async Task Recognize_EnglishSingleWordSyntheticImage_ReturnsRecognizedText()
    {
        using var bitmap = CreateTextImage("Settings", 32);

        var result = await _fixture.Engine
            .RecognizeAsync(new ImageData(bitmap), OcrOptions.Default, CancellationToken.None);

        _output.WriteLine($"Recognized text: \"{result.Text}\"");
        _output.WriteLine($"Recognition time: {result.ProcessingTime.TotalMilliseconds:0.0} ms");

        Assert.True(result.ProcessingTime > TimeSpan.Zero);
        Assert.False(string.IsNullOrWhiteSpace(result.Text));
        Assert.Contains("Settings", result.Text);
    }

    [Fact]
    public async Task Recognize_EnglishPhraseSyntheticImage_ReturnsRecognizedWords()
    {
        using var bitmap = CreateTextImage("Capture image", 32);

        var result = await _fixture.Engine
            .RecognizeAsync(new ImageData(bitmap), OcrOptions.Default, CancellationToken.None);

        _output.WriteLine($"Recognized text: \"{result.Text}\"");
        _output.WriteLine($"Recognition time: {result.ProcessingTime.TotalMilliseconds:0.0} ms");

        // Each word is asserted on its own: the M7-C evaluation recorded that
        // the PP-OCRv6 rec model frequently drops the space between tokens
        // (docs/ocr-integration.md §10.5), so the exact phrase is not a fair
        // criterion — both words must be recognized, with or without the space.
        Assert.True(result.ProcessingTime > TimeSpan.Zero);
        Assert.False(string.IsNullOrWhiteSpace(result.Text));
        Assert.Contains("Capture", result.Text);
        Assert.Contains("image", result.Text);
    }

    [Fact]
    public async Task Recognize_BlankImage_ReturnsValidEmptyResult()
    {
        using var bitmap = new Bitmap(400, 200, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
        }

        var result = await _fixture.Engine
            .RecognizeAsync(new ImageData(bitmap), OcrOptions.Default, CancellationToken.None);

        // No text is a valid outcome, not an error.
        Assert.Equal(string.Empty, result.Text);
        Assert.Empty(result.Blocks);
    }

    [Fact]
    public async Task Recognize_PreCancelledToken_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        using var bitmap = new Bitmap(4, 4, PixelFormat.Format32bppArgb);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _fixture.Engine.RecognizeAsync(new ImageData(bitmap), OcrOptions.Default, cts.Token));
    }

    [Fact]
    public async Task Dispose_ThenRecognize_ThrowsObjectDisposed()
    {
        var engine = new RapidOcrEngine();
        engine.Dispose();

        using var bitmap = new Bitmap(4, 4, PixelFormat.Format32bppArgb);

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            engine.RecognizeAsync(new ImageData(bitmap), OcrOptions.Default, CancellationToken.None));
    }
}
