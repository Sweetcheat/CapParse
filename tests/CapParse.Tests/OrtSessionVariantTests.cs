using CapParse.OCR;
using Microsoft.ML.OnnxRuntime;
using RapidOcrNet;

namespace CapParse.Tests;

/// <summary>
/// Deterministic tests for the ONNX Runtime session-variant selection
/// (docs/ocr-integration.md §11.4/§11.6). The production default — the
/// variant chosen when <c>CAPPARSE_ORT_SESSION</c> is missing, empty, or
/// unrecognized — runs with the CPU memory arena disabled and the memory
/// pattern enabled, the exact same effective options as the explicit
/// <c>noarena</c> variant (the user-approved production default promoted
/// from the A/B experiment). The parsing and the variant-to-options
/// mapping are pure functions: no environment variable is read or set
/// here and no model is loaded, so the tests are free of global state.
/// </summary>
public class OrtSessionVariantTests
{
    // ---- variant resolution (pure, environment-independent) ----

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
    public void ResolveSessionVariant_RecognizedValue_SelectsMatchingVariant(
        string raw, string expectedName)
    {
        // Compared by name so the test method signature never exposes the
        // internal enum type (a public xUnit test cannot take an internal
        // type parameter).
        Assert.Equal(expectedName, RapidOcrEngine.ResolveSessionVariant(raw).ToString());
    }

    // ---- variant -> effective session options (pure mapping) ----

    [Theory]
    [InlineData("Default", false, true)]
    [InlineData("NoArena", false, true)]
    [InlineData("Compact", false, false)]
    [InlineData("NoPattern", true, false)]
    public void MemoryOptionsFor_Variant_PinsDocumentedOptions(
        string variantName, bool expectedArena, bool expectedPattern)
    {
        var variant = Enum.Parse<RapidOcrEngine.OrtSessionVariant>(variantName);

        var (arena, pattern) = RapidOcrEngine.MemoryOptionsFor(variant);

        Assert.Equal(expectedArena, arena);
        Assert.Equal(expectedPattern, pattern);
    }

    [Fact]
    public void MemoryOptionsFor_Default_IsEquivalentToNoArena()
    {
        // The promotion: a run with no CAPPARSE_ORT_SESSION value must be
        // the exact same configuration as an explicit noarena run, so the
        // production default can never diverge from the variant it was
        // promoted from.
        Assert.Equal(
            RapidOcrEngine.MemoryOptionsFor(RapidOcrEngine.OrtSessionVariant.Default),
            RapidOcrEngine.MemoryOptionsFor(RapidOcrEngine.OrtSessionVariant.NoArena));
    }

    // ---- the options object the sessions are created with ----

    [Theory]
    [InlineData("Default", false, true)]
    [InlineData("NoArena", false, true)]
    [InlineData("Compact", false, false)]
    [InlineData("NoPattern", true, false)]
    public void ApplyVariantOptions_SessionOptions_CarriesTheVariantConfiguration(
        string variantName, bool expectedArena, bool expectedPattern)
    {
        // The constructor applies the variant to a fresh
        // GetDefaultSessionOptions() before InitModels; replicating that
        // step on a real SessionOptions (no model, no session) proves the
        // options the sessions are created with match the variant.
        var variant = Enum.Parse<RapidOcrEngine.OrtSessionVariant>(variantName);
        var options = RapidOcr.GetDefaultSessionOptions();

        try
        {
            RapidOcrEngine.ApplyVariantOptions(variant, options);

            Assert.Equal(expectedArena, options.EnableCpuMemArena);
            Assert.Equal(expectedPattern, options.EnableMemoryPattern);
        }
        finally
        {
            options.Dispose();
        }
    }
}
