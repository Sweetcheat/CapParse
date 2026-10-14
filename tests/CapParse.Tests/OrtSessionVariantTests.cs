using CapParse.OCR;

namespace CapParse.Tests;

/// <summary>
/// Deterministic tests for the experimental ONNX Runtime session-variant
/// selection (the <c>CAPPARSE_ORT_SESSION</c> opt-in, see
/// docs/ocr-integration.md §11.4). The parsing is a pure function of the raw
/// environment-variable value, so it is tested in isolation: no environment
/// variable is read or set here, which keeps the tests free of any global
/// state that could leak between tests. The only value that selects the
/// compact (diagnostic) variant is the exact, case-sensitive string
/// <c>"compact"</c>; everything else — missing, empty, or unrecognized —
/// selects the default (production) variant, so a typo can never silently
/// switch a normal run to the diagnostic configuration.
/// </summary>
public class OrtSessionVariantTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("default")]
    [InlineData("DEFAULT")]
    [InlineData("compact ")]
    [InlineData(" compact")]
    [InlineData("COMPACT")]
    [InlineData("Compact")]
    [InlineData("compact-arena")]
    [InlineData("noarena ")]
    [InlineData("NOpattern")]
    [InlineData("garbage")]
    public void ResolveSessionVariant_UnrecognizedValue_SelectsDefault(string? raw)
    {
        Assert.Equal(RapidOcrEngine.OrtSessionVariant.Default, RapidOcrEngine.ResolveSessionVariant(raw));
    }

    [Theory]
    [InlineData("compact", "Compact")]
    [InlineData("noarena", "NoArena")]
    [InlineData("nopattern", "NoPattern")]
    public void ResolveSessionVariant_RecognizedDiagnosticValue_SelectsMatchingVariant(
        string raw, string expectedName)
    {
        // Compared by name so the test method signature never exposes the
        // internal enum type (a public xUnit test cannot take an internal
        // type parameter).
        Assert.Equal(expectedName, RapidOcrEngine.ResolveSessionVariant(raw).ToString());
    }
}
