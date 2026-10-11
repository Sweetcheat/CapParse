using CapParse.OCR;

namespace CapParse.Tests;

// M9: the session's OCR history is pure, in-memory state. It keeps at most
// 20 entries, most recent first, records a run only when it recognized
// non-empty text, never records the same run twice, and stays independent
// of the current result / run state. No thread, no UI, no disk.
public class OcrHistoryTests
{
    private static DateTime At(int minutes)
        => new DateTime(2024, 1, 1, 12, 0, 0).AddMinutes(minutes);

    [Fact]
    public void NewHistory_IsEmpty()
    {
        var history = new OcrHistory();

        Assert.True(history.IsEmpty);
        Assert.Empty(history.Entries);
    }

    [Fact]
    public void Add_ValidText_RecordsAnEntry()
    {
        var history = new OcrHistory();

        var added = history.Add(runId: 1, "linha um\nlinha dois", At(0));

        Assert.True(added);
        Assert.False(history.IsEmpty);
        var entry = Assert.Single(history.Entries);
        Assert.Equal("linha um\nlinha dois", entry.Text);
        Assert.Equal(1, entry.RunId);
    }

    [Fact]
    public void Add_OrdersEntriesFromMostRecentToOldest()
    {
        var history = new OcrHistory();
        history.Add(1, "primeiro", At(0));
        history.Add(2, "segundo", At(10));
        history.Add(3, "terceiro", At(20));

        var texts = history.Entries.Select(e => e.Text).ToArray();

        Assert.Equal(new[] { "terceiro", "segundo", "primeiro" }, texts);
    }

    [Fact]
    public void Add_BeyondLimit_KeepsOnlyTheMostRecentTwenty()
    {
        var history = new OcrHistory();
        for (var i = 0; i < 25; i++)
        {
            Assert.True(history.Add(i, $"texto {i}", At(i)));
        }

        Assert.Equal(OcrHistory.MaxEntries, history.Entries.Count);

        // The 20 most recent (texto 24 down to texto 5) remain.
        Assert.Equal("texto 24", history.Entries[0].Text);
        Assert.Equal("texto 5", history.Entries[^1].Text);
        Assert.DoesNotContain(history.Entries, e => e.Text == "texto 4");
    }

    [Fact]
    public void Add_DistinctCapturesWithIdenticalText_AreAllKept()
    {
        var history = new OcrHistory();
        history.Add(1, "mesmo texto", At(0));
        history.Add(2, "mesmo texto", At(10));
        history.Add(3, "mesmo texto", At(20));

        Assert.Equal(3, history.Entries.Count);
        Assert.All(history.Entries, e => Assert.Equal("mesmo texto", e.Text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n  \r\n")]
    public void Add_EmptyOrWhitespaceText_IsNotRecorded(string text)
    {
        var history = new OcrHistory();

        var added = history.Add(1, text, At(0));

        Assert.False(added);
        Assert.True(history.IsEmpty);
    }

    [Fact]
    public void Add_SameRunTwice_RecordsTheRunOnce()
    {
        var history = new OcrHistory();

        Assert.True(history.Add(7, "resultado", At(0)));
        Assert.False(history.Add(7, "resultado", At(5)));

        Assert.Single(history.Entries);
    }

    [Fact]
    public void Add_DoesNotMutateEarlierEntries()
    {
        var history = new OcrHistory();
        history.Add(1, "primeiro", At(0));
        var first = history.Entries[0];

        history.Add(2, "segundo", At(10));

        // The earlier entry is immutable and unchanged.
        Assert.Equal("primeiro", first.Text);
        Assert.Equal(1, first.RunId);
        Assert.Equal(At(0), first.CapturedAt);
    }

    [Fact]
    public void HistoryStaysIndependentOfTheRunState()
    {
        var history = new OcrHistory();
        var runState = new OcrRunState();

        // Recording a history entry does not move the run state, and driving
        // the run state does not touch the history: the two are separate.
        history.Add(1, "resultado", At(0));
        Assert.Equal(OcrPhase.Idle, runState.Phase);
        Assert.Empty(runState.Text);

        runState.CompleteRun(runState.BeginRun(),
            new OcrResult("outro", Array.Empty<OcrBlock>(), TimeSpan.Zero));
        Assert.Single(history.Entries);
        Assert.Equal("resultado", history.Entries[0].Text);
    }

    [Fact]
    public void SupersededRun_IsNotRecorded_WhenOnlyCompletedRunsAreAdded()
    {
        // Mirrors the app's gate: an entry is added only for a run that
        // validly completes the run state, so a superseded (stale) run can
        // never publish a result to the history.
        var history = new OcrHistory();
        var runState = new OcrRunState();

        var staleRun = runState.BeginRun();
        var currentRun = runState.BeginRun();

        if (runState.CompleteRun(staleRun, new OcrResult("obsoleto", Array.Empty<OcrBlock>(), TimeSpan.Zero)))
        {
            history.Add(staleRun, "obsoleto", At(0));
        }

        if (runState.CompleteRun(currentRun, new OcrResult("atual", Array.Empty<OcrBlock>(), TimeSpan.Zero)))
        {
            history.Add(currentRun, "atual", At(10));
        }

        var entry = Assert.Single(history.Entries);
        Assert.Equal("atual", entry.Text);
        Assert.Equal(currentRun, entry.RunId);
    }
}
