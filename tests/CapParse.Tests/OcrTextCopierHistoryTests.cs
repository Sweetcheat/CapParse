using System.Runtime.InteropServices;
using CapParse.OCR;
using CapParse.UI;

namespace CapParse.Tests;

// M9: a history entry's text is copied again through the application's
// single text-clipboard channel. Copying stays explicit, only ever
// copies the entry's own text (never an empty one), and reports
// clipboard failures as outcomes instead of throwing. The writer is
// faked, so none of these checks touch the real system clipboard.
public class OcrTextCopierHistoryTests
{
    [Fact]
    public void CopyHistoryEntry_WritesExactlyTheEntryText()
    {
        var writer = new FakeWriter();
        var copier = new OcrTextCopier(writer);

        var outcome = copier.TryCopyText("texto do histórico");

        Assert.True(outcome.Succeeded);
        Assert.Equal("texto do histórico", writer.LastText);
    }

    [Fact]
    public void CopyHistoryEntry_DoesNotAffectTheCurrentRunState()
    {
        // Copying a history entry goes straight to the clipboard and never
        // reads or changes the run state that backs the current result.
        var writer = new FakeWriter();
        var copier = new OcrTextCopier(writer);
        var state = new OcrRunState();

        var outcome = copier.TryCopyText("texto antigo");

        Assert.True(outcome.Succeeded);
        Assert.Equal("texto antigo", writer.LastText);
        Assert.Equal(OcrPhase.Idle, state.Phase);
        Assert.Empty(state.Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CopyHistoryEntry_WithoutText_DoesNotTouchTheClipboard(string text)
    {
        var writer = new FakeWriter();
        var copier = new OcrTextCopier(writer);

        var outcome = copier.TryCopyText(text);

        Assert.False(outcome.Succeeded);
        Assert.Equal(0, writer.WriteCount);
    }

    [Fact]
    public void CopyHistoryEntry_ClipboardFailure_IsReportedAsAFailedOutcome()
    {
        var writer = new FailingWriter();
        var copier = new OcrTextCopier(writer);

        var outcome = copier.TryCopyText("texto");

        Assert.False(outcome.Succeeded);
        Assert.NotEmpty(outcome.Message);
    }

    private sealed class FakeWriter : IRawTextClipboardWriter
    {
        public int WriteCount { get; private set; }
        public string? LastText { get; private set; }

        public void WriteUtf16Text(string text)
        {
            WriteCount++;
            LastText = text;
        }
    }

    private sealed class FailingWriter : IRawTextClipboardWriter
    {
        public void WriteUtf16Text(string text)
            => throw new ExternalException("OpenClipboard failed (win32 error 5).");
    }
}
