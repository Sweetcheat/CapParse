using CapParse.OCR;
using SkiaSharp;
using WordBox = RapidOcrNet.WordBox;

namespace CapParse.Tests;

// Deterministic tests for the line-text reconstruction that
// RapidOcrEngine applies to each recognized line. The model's own line
// text is the base (its spaces are preserved exactly); a space is added
// at a word boundary only when the model reported none there and the
// horizontal gap between the word boxes exceeds 0.5 of the shorter
// word's height. All inputs are synthetic; no model is loaded.
public class WordTextReconstructionTests
{
    private static WordBox Word(string text, int x, int y, int width, int height) => new()
    {
        Text = text,
        BoxPoints = new[]
        {
            new SKPointI(x, y),
            new SKPointI(x + width, y),
            new SKPointI(x + width, y + height),
            new SKPointI(x, y + height)
        },
        Score = 1f
    };

    private static RapidOcrNet.TextBlock Line(string text, params WordBox[] words) => new()
    {
        Text = text,
        Chars = Array.Empty<string>(),
        BoxPoints = Array.Empty<SKPointI>(),
        CharScores = Array.Empty<float>(),
        WordResults = words
    };

    [Fact]
    public void ModelSpacesAtWordBoundaries_ArePreserved()
    {
        var block = Line("Hello world", Word("Hello", 0, 0, 100, 20), Word("world", 150, 0, 100, 20));

        Assert.Equal("Hello world", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void MissingModelSpace_DecisiveGap_InsertsSpace()
    {
        // The model dropped the space; the 50 px gap is far above the
        // 0.5 * 20 = 10 px threshold, so it is added back.
        var block = Line("Helloworld", Word("Hello", 0, 0, 100, 20), Word("world", 150, 0, 100, 20));

        Assert.Equal("Hello world", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void MissingModelSpace_TilingGap_DoesNotInsertSpace()
    {
        // The engine tiles word boxes across the line: gaps of a few pixels
        // at space positions are tiling noise, not a missing space.
        var block = Line("Helloworld", Word("Hello", 0, 0, 100, 20), Word("world", 103, 0, 100, 20));

        Assert.Equal("Helloworld", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void OutOfOrderWords_AreSortedByX()
    {
        var block = Line("Helloworld", Word("world", 150, 0, 100, 20), Word("Hello", 0, 0, 100, 20));

        Assert.Equal("Hello world", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void VerticalOffset_Alone_DoesNotCreateASpace()
    {
        // The second word sits well below the first (slanted line). Only
        // the horizontal gap (2 px, below the 10 px threshold) is
        // compared; the vertical distance never by itself creates a space.
        var block = Line("AB", Word("A", 0, 0, 40, 20), Word("B", 42, 30, 40, 20));

        Assert.Equal("AB", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void Punctuation_HuggingNeighbors_IsPreserved()
    {
        // The model text has no spaces and the gaps are small: nothing is
        // inserted around the punctuation.
        var block = Line("foo(bar)",
            Word("foo", 0, 0, 30, 20),
            Word("(", 32, 0, 8, 20),
            Word("bar", 40, 0, 30, 20),
            Word(")", 70, 0, 8, 20));

        Assert.Equal("foo(bar)", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void ModelDoubleSpaces_ArePreserved()
    {
        // UI menus use double spaces between items; the model's exact
        // spacing passes through untouched.
        var block = Line("Arquivo  Editar", Word("Arquivo", 0, 0, 60, 20), Word("Editar", 80, 0, 50, 20));

        Assert.Equal("Arquivo  Editar", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void InternalModelSpaceInsideAWord_IsPreserved()
    {
        // One box covering two model tokens: the model's space inside the
        // span passes through, no geometry is involved.
        var block = Line("foo bar", Word("foo bar", 0, 0, 80, 20));

        Assert.Equal("foo bar", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void WordWithMissingBox_FallsBackToLineText()
    {
        var broken = new WordBox { Text = "mundo", BoxPoints = null, Score = 0f };
        var block = Line("Olá mundo", Word("Olá", 0, 0, 40, 20), broken);

        Assert.Equal("Olá mundo", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void WordWithZeroSizedBox_FallsBackToLineText()
    {
        var zero = new WordBox
        {
            Text = "mundo",
            Score = 0f,
            BoxPoints = new[] { new SKPointI(40, 0), new SKPointI(40, 0), new SKPointI(40, 20), new SKPointI(40, 20) }
        };
        var block = Line("Olá mundo", Word("Olá", 0, 0, 40, 20), zero);

        Assert.Equal("Olá mundo", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void WordWithEmptyText_FallsBackToLineText()
    {
        var empty = new WordBox
        {
            Text = string.Empty,
            Score = 0f,
            BoxPoints = new[] { new SKPointI(40, 0), new SKPointI(80, 0), new SKPointI(80, 20), new SKPointI(40, 20) }
        };
        var block = Line("Olá mundo", Word("Olá", 0, 0, 40, 20), empty);

        Assert.Equal("Olá mundo", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void WordTextsNotCoveringLineText_FallsBackToLineText()
    {
        // The boxes do not align with the model text: no space decision is
        // made and the line text passes through unchanged.
        var block = Line("Hello world", Word("Hxllo", 0, 0, 100, 20), Word("world", 150, 0, 100, 20));

        Assert.Equal("Hello world", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void NullWordResults_KeepsLineText()
    {
        var block = new RapidOcrNet.TextBlock { Text = "Olá mundo", Chars = Array.Empty<string>(), BoxPoints = null, CharScores = null, WordResults = null };

        Assert.Equal("Olá mundo", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void EmptyWordResults_KeepsLineText()
    {
        var block = Line("Olá mundo");

        Assert.Equal("Olá mundo", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void NonEmptyLineText_NeverBecomesEmpty()
    {
        var block = new RapidOcrNet.TextBlock { Text = "abc", Chars = Array.Empty<string>(), BoxPoints = null, CharScores = null };

        Assert.Equal("abc", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void SingleWord_ReturnsTheModelText()
    {
        var block = Line("Settings", Word("Settings", 0, 0, 200, 40));

        Assert.Equal("Settings", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void EdgeSpaces_AreDropped()
    {
        // Leading and trailing spaces of the line are engine artifacts,
        // not content.
        var block = Line(" Olá mundo  ", Word("Olá", 0, 0, 40, 20), Word("mundo", 80, 0, 60, 20));

        Assert.Equal("Olá mundo", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void AccentedWords_ArePreserved()
    {
        var block = Line("Ação coração",
            Word("Ação", 0, 0, 60, 20),
            Word("coração", 120, 0, 90, 20));

        Assert.Equal("Ação coração", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void SamePixelGap_ShortWords_InsertSpace()
    {
        // 12 px gap against a 0.5 * 20 = 10 px threshold.
        var block = Line("ab", Word("a", 0, 0, 10, 20), Word("b", 22, 0, 10, 20));

        Assert.Equal("a b", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void SamePixelGap_TallWords_DoNotInsertSpace()
    {
        // The same 12 px gap against a 0.5 * 100 = 50 px threshold: at a
        // larger font the gap is normal letter spacing, not a hole.
        var block = Line("ab", Word("a", 0, 0, 10, 100), Word("b", 22, 0, 10, 100));

        Assert.Equal("ab", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void OverlappingBoxes_DoNotInsertASpace()
    {
        // Negative gap (the boxes overlap) must never insert a space.
        var block = Line("ab", Word("a", 0, 0, 30, 20), Word("b", 20, 0, 30, 20));

        Assert.Equal("ab", RapidOcrEngine.ReconstructLineText(block));
    }

    [Fact]
    public void Reconstruction_IsDeterministic()
    {
        var block = Line("Helloworld",
            Word("world", 150, 0, 100, 20),
            Word("Hello", 0, 0, 100, 20));

        var first = RapidOcrEngine.ReconstructLineText(block);
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(first, RapidOcrEngine.ReconstructLineText(block));
        }
    }

    [Fact]
    public void MapResult_RebuildsBlockTextFromWordBoxes()
    {
        var package = new RapidOcrNet.OcrResult
        {
            StrRes = "Helloworld",
            TextBlocks = new[]
            {
                Line("Helloworld", Word("Hello", 0, 0, 100, 20), Word("world", 150, 0, 100, 20)),
                Line("Olá mundo  ") // no word boxes: model text, edges trimmed... 
            }
        };

        // The second block has no word boxes, so its text passes through
        // unchanged (including its trailing spaces): the fallback never
        // alters what the engine produced.
        var result = RapidOcrEngine.MapResult(package, TimeSpan.Zero);

        Assert.Equal(2, result.Blocks.Count);
        Assert.Equal("Hello world", result.Blocks[0].Text);
        Assert.Equal("Olá mundo  ", result.Blocks[1].Text);
        Assert.Equal("Hello world\nOlá mundo  ", result.Text);
    }

    [Fact]
    public void MapResult_MixedFallback_KeepsValidLinesIntact()
    {
        var broken = new WordBox { Text = "mundo", BoxPoints = null, Score = 0f };
        var package = new RapidOcrNet.OcrResult
        {
            StrRes = string.Empty,
            TextBlocks = new[]
            {
                Line("olá mundo", Word("olá", 0, 0, 40, 20), broken),
                Line("goodbyegoodbye", Word("goodbye", 0, 40, 120, 20), Word("goodbye", 300, 40, 120, 20))
            }
        };

        var result = RapidOcrEngine.MapResult(package, TimeSpan.Zero);

        Assert.Equal("olá mundo", result.Blocks[0].Text);
        Assert.Equal("goodbye goodbye", result.Blocks[1].Text);
        Assert.Equal("olá mundo\ngoodbye goodbye", result.Text);
    }
}
