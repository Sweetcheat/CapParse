namespace CapParse.Tests;

/// <summary>
/// Known-case validation of the CER computation: exact matches, insertions,
/// deletions, substitutions, empty references and the normalization
/// helpers. These tests pin the metric so the M7-C report numbers are
/// reproducible from a defined formula, not from an implementation detail.
/// </summary>
public class CharacterErrorRateTests
{
    [Theory]
    [InlineData("abc", "abc", 0)]
    [InlineData("abc", "axc", 1)]
    [InlineData("abc", "abbc", 1)]
    [InlineData("abc", "ac", 1)]
    [InlineData("abc", "", 3)]
    [InlineData("", "abc", 3)]
    [InlineData("", "", 0)]
    [InlineData("saturday", "sunday", 3)]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("Olá, ação.", "Olá, acao.", 2)]
    public void EditOperations_KnownPairs_ReturnsExpectedDistance(
        string reference, string hypothesis, int expectedDistance)
    {
        var (distance, _, _, _) = CharacterErrorRate.EditOperations(reference, hypothesis);

        Assert.Equal(expectedDistance, distance);
    }

    [Fact]
    public void EditOperations_ExactMatch_HasNoOperations()
    {
        var (distance, insertions, deletions, substitutions) =
            CharacterErrorRate.EditOperations("abc", "abc");

        Assert.Equal(0, distance);
        Assert.Equal(0, insertions);
        Assert.Equal(0, deletions);
        Assert.Equal(0, substitutions);
    }

    [Fact]
    public void EditOperations_SingleSubstitution_IsCountedAsSubstitution()
    {
        var (distance, insertions, deletions, substitutions) =
            CharacterErrorRate.EditOperations("abc", "axc");

        Assert.Equal(1, distance);
        Assert.Equal(0, insertions);
        Assert.Equal(0, deletions);
        Assert.Equal(1, substitutions);
    }

    [Fact]
    public void EditOperations_SingleInsertion_IsCountedAsInsertion()
    {
        var (distance, insertions, deletions, substitutions) =
            CharacterErrorRate.EditOperations("abc", "abxc");

        Assert.Equal(1, distance);
        Assert.Equal(1, insertions);
        Assert.Equal(0, deletions);
        Assert.Equal(0, substitutions);
    }

    [Fact]
    public void EditOperations_SingleDeletion_IsCountedAsDeletion()
    {
        var (distance, insertions, deletions, substitutions) =
            CharacterErrorRate.EditOperations("abc", "ac");

        Assert.Equal(1, distance);
        Assert.Equal(0, insertions);
        Assert.Equal(1, deletions);
        Assert.Equal(0, substitutions);
    }

    [Fact]
    public void EditOperations_OperationCountsSumToDistance()
    {
        var (distance, insertions, deletions, substitutions) =
            CharacterErrorRate.EditOperations("Olá, ação, coração e informação.", "Ola, acao, coracao e informacao.");

        Assert.Equal(distance, insertions + deletions + substitutions);
    }

    [Fact]
    public void TryComputeRate_ExactMatch_ReturnsZero()
    {
        Assert.True(CharacterErrorRate.TryComputeRate("abc", "abc", out double rate));
        Assert.Equal(0.0, rate, 10);
    }

    [Fact]
    public void TryComputeRate_KnownPair_ReturnsDistanceOverReferenceLength()
    {
        Assert.True(CharacterErrorRate.TryComputeRate("abcd", "axcd", out double rate));

        Assert.Equal(0.25, rate, 10);
    }

    [Fact]
    public void TryComputeRate_EmptyReference_IsUndefined()
    {
        Assert.False(CharacterErrorRate.TryComputeRate("", "abc", out _));
        Assert.False(CharacterErrorRate.TryComputeRate("", "", out _));
    }

    [Theory]
    [InlineData("  abc  ", "abc")]
    [InlineData("\nabc\n", "abc")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Trimmed_RemovesOnlyEdgeWhitespace(string input, string expected)
    {
        Assert.Equal(expected, CharacterErrorRate.Trimmed(input));
    }

    [Theory]
    [InlineData("a  b\n\n c", "a b c")]
    [InlineData("a\tb", "a b")]
    [InlineData("  abc  ", "abc")]
    [InlineData("abc", "abc")]
    [InlineData("   ", "")]
    public void WhitespaceCollapsed_CollapsesInnerRuns(string input, string expected)
    {
        Assert.Equal(expected, CharacterErrorRate.WhitespaceCollapsed(input));
    }

    [Fact]
    public void WhitespaceCollapsed_PreservesAccentsAndCase()
    {
        Assert.Equal("Olá, ação e coração.", CharacterErrorRate.WhitespaceCollapsed("Olá, ação\n  e  coração."));
    }
}
