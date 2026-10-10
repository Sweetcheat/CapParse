using System.Text;

namespace CapParse.Tests;

/// <summary>
/// Character Error Rate (CER) for the M7-C OCR quality evaluation.
///
/// CER = Levenshtein distance(reference, hypothesis) / reference length.
///
/// Normalization policy (applied by the caller, kept explicit here):
/// - accents, case and every non-whitespace character are compared as-is;
///   nothing is normalized away, so spelling errors are never hidden;
/// - leading/trailing whitespace is trimmed (OCR line boxes do not report
///   outer padding as characters);
/// - <see cref="WhitespaceCollapsed"/> is an optional secondary view that
///   collapses inner whitespace runs (spaces, tabs, newlines) to single
///   spaces, separating content errors from line-structure errors.
///
/// An empty reference has no defined CER (division by zero is not invented):
/// <see cref="TryComputeRate"/> reports it as undefined and no-text cases
/// are assessed separately by whether the engine returned empty text.
/// </summary>
public static class CharacterErrorRate
{
    /// <summary>
    /// The Levenshtein distance between the two strings plus how many
    /// insertions, deletions and substitutions a deterministic
    /// backtracking of the edit path uses. Operations are counted as the
    /// transformation of <paramref name="reference"/> into
    /// <paramref name="hypothesis"/>: a deletion removes a reference
    /// character, an insertion adds a hypothesis character, a substitution
    /// swaps one for the other. When several optimal paths exist the
    /// counts follow one of them (substitutions are preferred over an
    /// insertion+deletion pair), so the split is a deterministic artifact;
    /// the distance itself is exact.
    /// </summary>
    public static (int Distance, int Insertions, int Deletions, int Substitutions) EditOperations(
        string reference, string hypothesis)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(hypothesis);

        int n = reference.Length;
        int m = hypothesis.Length;
        var cost = new int[n + 1, m + 1];
        int i;
        int j;

        for (i = 0; i <= n; i++)
        {
            cost[i, 0] = i;
        }

        for (j = 0; j <= m; j++)
        {
            cost[0, j] = j;
        }

        for (i = 1; i <= n; i++)
        {
            for (j = 1; j <= m; j++)
            {
                int substitutionCost = reference[i - 1] == hypothesis[j - 1] ? 0 : 1;
                int best = cost[i - 1, j] + 1;
                int fromInsertion = cost[i, j - 1] + 1;
                if (fromInsertion < best)
                {
                    best = fromInsertion;
                }

                int fromSubstitution = cost[i - 1, j - 1] + substitutionCost;
                if (fromSubstitution < best)
                {
                    best = fromSubstitution;
                }

                cost[i, j] = best;
            }
        }

        int insertions = 0;
        int deletions = 0;
        int substitutions = 0;
        i = n;
        j = m;

        while (i > 0 || j > 0)
        {
            if (i > 0 && j > 0 && reference[i - 1] == hypothesis[j - 1] && cost[i, j] == cost[i - 1, j - 1])
            {
                i--;
                j--;
            }
            else if (i > 0 && j > 0 && cost[i, j] == cost[i - 1, j - 1] + 1)
            {
                substitutions++;
                i--;
                j--;
            }
            else if (j > 0 && cost[i, j] == cost[i, j - 1] + 1)
            {
                insertions++;
                j--;
            }
            else
            {
                deletions++;
                i--;
            }
        }

        return (cost[n, m], insertions, deletions, substitutions);
    }

    /// <summary>
    /// The CER of <paramref name="hypothesis"/> against
    /// <paramref name="reference"/>. Returns false (undefined) when the
    /// reference is empty: a no-text case is not given a CER.
    /// </summary>
    public static bool TryComputeRate(string reference, string hypothesis, out double rate)
    {
        if (string.IsNullOrEmpty(reference))
        {
            rate = 0.0;
            return false;
        }

        rate = (double)EditOperations(reference, hypothesis).Distance / reference.Length;
        return true;
    }

    /// <summary>
    /// Trims leading and trailing whitespace, the only trimming applied
    /// before CER. Inner content is preserved exactly.
    /// </summary>
    public static string Trimmed(string? value) => value?.Trim() ?? string.Empty;

    /// <summary>
    /// The secondary, relaxed form: trims the edges and collapses every
    /// inner whitespace run (spaces, tabs, newlines) to a single space, so
    /// line-structure differences do not count against the content.
    /// </summary>
    public static string WhitespaceCollapsed(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;

        foreach (char c in value.Trim())
        {
            if (char.IsWhiteSpace(c))
            {
                pendingSpace = true;
            }
            else
            {
                if (pendingSpace && builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(c);
                pendingSpace = false;
            }
        }

        return builder.ToString();
    }
}
