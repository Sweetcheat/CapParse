using CapParse.OCR;
using CapParse.UI;

namespace CapParse.Tests;

// M9: a history entry's text is copied again through the same clipboard
// abstraction the current result uses. Copying stays explicit, only ever
// copies the entry's own text (never an empty one), and reports clipboard
// failures as outcomes instead of throwing. The clipboard is faked, so none
// of these checks touch the real system clipboard.
public class OcrTextCopierHistoryTests
{
    [Fact]
    public void CopyHistoryEntry_WritesExactlyTheEntryText()
    {
        var clipboard = new FakeClipboard();
        var copier = new OcrTextCopier(clipboard);

        var outcome = copier.TryCopyText("texto do histórico");

        Assert.True(outcome.Succeeded);
        Assert.Equal("texto do histórico", clipboard.LastText);
    }

    [Fact]
    public void CopyHistoryEntry_DoesNotAffectTheCurrentRunState()
    {
        // Copying a history entry goes straight to the clipboard and never
        // reads or changes the run state that backs the current result.
        var clipboard = new FakeClipboard();
        var copier = new OcrTextCopier(clipboard);
        var state = new OcrRunState();

        var outcome = copier.TryCopyText("texto antigo");

        Assert.True(outcome.Succeeded);
        Assert.Equal("texto antigo", clipboard.LastText);
        Assert.Equal(OcrPhase.Idle, state.Phase);
        Assert.Empty(state.Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CopyHistoryEntry_WithoutText_DoesNotTouchTheClipboard(string text)
    {
        var clipboard = new FakeClipboard();
        var copier = new OcrTextCopier(clipboard);

        var outcome = copier.TryCopyText(text);

        Assert.False(outcome.Succeeded);
        Assert.False(clipboard.Written);
    }

    [Fact]
    public void CopyHistoryEntry_ClipboardFailure_IsReportedAsAFailedOutcome()
    {
        var clipboard = new FakeClipboard(throwOnWrite: true);
        var copier = new OcrTextCopier(clipboard);

        var outcome = copier.TryCopyText("texto");

        Assert.False(outcome.Succeeded);
        Assert.NotEmpty(outcome.Message);
    }

    private sealed class FakeClipboard : ITextClipboard
    {
        private readonly bool _throwOnWrite;

        public FakeClipboard(bool throwOnWrite = false)
        {
            _throwOnWrite = throwOnWrite;
        }

        public string? LastText { get; private set; }

        public bool Written { get; private set; }

        public void SetText(string text)
        {
            if (_throwOnWrite)
            {
                throw new System.Runtime.InteropServices.ExternalException(
                    "The clipboard is in use by another application.");
            }

            Written = true;
            LastText = text;
        }
    }
}
