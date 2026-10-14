using System.Drawing;
using CapParse.OCR;
using PackageOcrBlock = RapidOcrNet.TextBlock;
using PackageOcrResult = RapidOcrNet.OcrResult;
using SkiaSharp;

namespace CapParse.Tests;

// Mapping from the RapidOcrNet result into the public OcrResult/OcrBlock
// types, with fully synthetic data. No model is loaded by these tests.
public class OcrResultMappingTests
{
    // TextBlock is a record with a required Chars member, so the synthetic
    // blocks always carry one (the mapping never reads it).
    private static PackageOcrBlock PackageBlock(string? text, SKPointI[]? boxPoints, float[]? charScores)
        => new()
        {
            Text = text,
            Chars = Array.Empty<string>(),
            BoxPoints = boxPoints,
            CharScores = charScores
        };

    [Fact]
    public void MapResult_SingleBlock_MapsTextBoundsAndConfidence()
    {
        var package = new PackageOcrResult
        {
            StrRes = "Olá",
            TextBlocks = new[]
            {
                PackageBlock(
                    "Olá",
                    new[] { new SKPointI(10, 20), new SKPointI(109, 20), new SKPointI(109, 55), new SKPointI(10, 55) },
                    new[] { 0.9f, 0.8f, 0.7f })
            }
        };

        var result = RapidOcrEngine.MapResult(package, TimeSpan.FromMilliseconds(42));

        Assert.Equal("Olá", result.Text);
        Assert.Equal(42, result.ProcessingTime.TotalMilliseconds);

        var block = Assert.Single(result.Blocks);
        Assert.Equal("Olá", block.Text);
        // Inclusive corners (10..109, 20..55) -> 100x36.
        Assert.Equal(new Rectangle(10, 20, 100, 36), block.Bounds);
        Assert.Equal(0.8f, block.Confidence, 3);
    }

    [Fact]
    public void MapResult_MultipleBlocks_TextIsTheBlockTextsJoined()
    {
        // Without word boxes each line falls back to its own text, so the
        // full text is the block texts joined with newlines: Text and
        // Blocks always agree, whatever the package's StrRes carried.
        var package = new PackageOcrResult
        {
            StrRes = "linha um\nlinha dois",
            TextBlocks = new[]
            {
                PackageBlock("linhaum", null, null),
                PackageBlock("linhadois", null, null)
            }
        };

        var result = RapidOcrEngine.MapResult(package, TimeSpan.Zero);

        Assert.Equal(2, result.Blocks.Count);
        Assert.Equal("linhaum", result.Blocks[0].Text);
        Assert.Equal("linhadois", result.Blocks[1].Text);
        Assert.Equal("linhaum\nlinhadois", result.Text);
    }

    [Fact]
    public void MapResult_RotatedBox_DerivesAxisAlignedRectangle()
    {
        var package = new PackageOcrResult
        {
            StrRes = "x",
            TextBlocks = new[]
            {
                // A quadrilateral that is not aligned with the axes.
                PackageBlock("x", new[] { new SKPointI(10, 20), new SKPointI(40, 15), new SKPointI(42, 45), new SKPointI(12, 50) }, null)
            }
        };

        var result = RapidOcrEngine.MapResult(package, TimeSpan.Zero);

        Assert.Equal(new Rectangle(10, 15, 33, 36), Assert.Single(result.Blocks).Bounds);
    }

    [Fact]
    public void MapResult_MissingCharScores_ConfidenceIsZero()
    {
        var result = RapidOcrEngine.MapResult(new PackageOcrResult
        {
            StrRes = "x",
            TextBlocks = new[] { PackageBlock("x", null, null) }
        }, TimeSpan.Zero);

        Assert.Equal(0f, Assert.Single(result.Blocks).Confidence);
    }

    [Fact]
    public void MapResult_EmptyCharScores_ConfidenceIsZero()
    {
        var result = RapidOcrEngine.MapResult(new PackageOcrResult
        {
            StrRes = "x",
            TextBlocks = new[] { PackageBlock("x", null, Array.Empty<float>()) }
        }, TimeSpan.Zero);

        Assert.Equal(0f, Assert.Single(result.Blocks).Confidence);
    }

    [Fact]
    public void MapResult_MissingBoxPoints_BoundsIsEmpty()
    {
        var result = RapidOcrEngine.MapResult(new PackageOcrResult
        {
            StrRes = "x",
            TextBlocks = new[] { PackageBlock("x", null, null) }
        }, TimeSpan.Zero);

        Assert.Equal(Rectangle.Empty, Assert.Single(result.Blocks).Bounds);
    }

    [Fact]
    public void MapResult_NoBlocks_ReturnsValidEmptyResult()
    {
        var result = RapidOcrEngine.MapResult(new PackageOcrResult
        {
            StrRes = string.Empty,
            TextBlocks = Array.Empty<PackageOcrBlock>()
        }, TimeSpan.FromMilliseconds(7));

        Assert.Equal(string.Empty, result.Text);
        Assert.Empty(result.Blocks);
        Assert.Equal(7, result.ProcessingTime.TotalMilliseconds);
    }

    [Fact]
    public void MapResult_NullTextFields_DoesNotProduceNulls()
    {
        var result = RapidOcrEngine.MapResult(new PackageOcrResult
        {
            StrRes = null,
            TextBlocks = new[] { PackageBlock(null, null, null) }
        }, TimeSpan.Zero);

        Assert.Equal(string.Empty, result.Text);
        Assert.Equal(string.Empty, Assert.Single(result.Blocks).Text);
    }
}
