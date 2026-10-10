using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using CapParse.OCR;
using Xunit.Abstractions;

namespace CapParse.Tests;

/// <summary>
/// M7-C quality evaluation: runs the real PP-OCRv6 Small engine over the
/// reference collection and measures quality (CER), recognition time and
/// process memory. It is deliberately NOT part of the regular suite run:
/// the full pass takes tens of seconds, so it is opt-in.
///
/// Run it with (from the repository root):
///
///   powershell:  $env:CAPPARSE_OCR_EVAL="1"; dotnet test --filter "FullyQualifiedName~OcrQualityEvaluationTests"
///
/// The report is written to
/// <c>tests/CapParse.Tests/TestData/Ocr/reports/evaluation-&lt;Config&gt;.md</c>
/// and to the test output. Numbers are a measurement of this machine in
/// this configuration, not a universal quality statement.
/// </summary>
public class OcrQualityEvaluationTests
{
    private sealed record CaseRun(
        OcrCaseSpec Spec,
        string? Text,
        int BlockCount,
        double WallMilliseconds,
        string? Error);

    private readonly ITestOutputHelper _output;

    public OcrQualityEvaluationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Evaluate_RealEngine_OnReferenceCollection()
    {
        // Opt-in: the regular suite keeps running fast without the model.
        if (Environment.GetEnvironmentVariable("CAPPARSE_OCR_EVAL") != "1")
        {
            return;
        }

        var manifest = OcrCollectionManifest.Load(TestDataDirectory);
        var process = Process.GetCurrentProcess();

        process.Refresh();
        long memoryBeforeInit = process.WorkingSet64;

        var initStopwatch = Stopwatch.StartNew();
        using var engine = new RapidOcrEngine();
        initStopwatch.Stop();
        TimeSpan initTime = initStopwatch.Elapsed;

        process.Refresh();
        long memoryAfterInit = process.WorkingSet64;

        // Warm-up pass: primes the ONNX sessions. Reported separately and
        // excluded from the recognition statistics.
        var warmup = new List<CaseRun>();
        foreach (var spec in manifest.Cases)
        {
            warmup.Add(await RecognizeTimedAsync(engine, spec));
        }

        var measured = new List<CaseRun>();
        var memorySamples = new List<long>();
        foreach (var spec in manifest.Cases)
        {
            process.Refresh();
            memorySamples.Add(process.WorkingSet64);

            measured.Add(await RecognizeTimedAsync(engine, spec));
        }

        engine.Dispose();
        process.Refresh();
        long memoryAfterDispose = process.WorkingSet64;

        var report = BuildReport(
            manifest,
            warmup,
            measured,
            memorySamples,
            memoryBeforeInit,
            memoryAfterInit,
            memoryAfterDispose,
            initTime);

        WriteReport(report, _output);

        var failures = measured.Where(r => r.Error is not null).Select(r => $"{r.Spec.Id}: {r.Error}");
        Assert.True(
            !failures.Any(),
            "The engine failed on at least one case:\n" + string.Join("\n", failures));
    }

    private static string TestDataDirectory =>
        Path.Combine(AppContext.BaseDirectory, "TestData", "Ocr");

    private static async Task<CaseRun> RecognizeTimedAsync(RapidOcrEngine engine, OcrCaseSpec spec)
    {
        try
        {
            var path = Path.Combine(TestDataDirectory, spec.File);
            var bytes = File.ReadAllBytes(path);
            using var stream = new MemoryStream(bytes);
            using var bitmap = new Bitmap(stream);

            var stopwatch = Stopwatch.StartNew();
            var result = await engine
                .RecognizeAsync(new ImageData(bitmap), OcrOptions.Default, CancellationToken.None)
                .ConfigureAwait(false);

            return new CaseRun(spec, result.Text, result.Blocks.Count, stopwatch.Elapsed.TotalMilliseconds, null);
        }
        catch (Exception ex)
        {
            return new CaseRun(spec, null, 0, 0, ex.Message);
        }
    }

    private static string BuildReport(
        OcrCollectionManifest manifest,
        List<CaseRun> warmup,
        List<CaseRun> measured,
        List<long> memorySamples,
        long memoryBeforeInit,
        long memoryAfterInit,
        long memoryAfterDispose,
        TimeSpan initTime)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# M7-C — Avaliação de qualidade do OCR (relatório gerado)");
        builder.AppendLine();
        builder.AppendLine($"Gerado em: {DateTime.Now:yyyy-MM-dd HH:mm:ss} por `OcrQualityEvaluationTests`.");
        builder.AppendLine();
        builder.AppendLine("## Ambiente");
        builder.AppendLine();
        builder.AppendLine("| Item | Valor |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine($"| Configuração | {DetectConfiguration()} |");
        builder.AppendLine($"| SO | {RuntimeInformation.OSDescription} |");
        builder.AppendLine($"| Arquitetura do processo | {RuntimeInformation.ProcessArchitecture} |");
        builder.AppendLine($"| Runtime | {RuntimeInformation.FrameworkDescription} |");
        builder.AppendLine("| Mecanismo | RapidOcrNet 4.2.0, preset `PPOCRv6Small` + `RapidOcrOptions.PPOCRv6`, CPU |");
        builder.AppendLine("| Modelo | PP-OCRv6 Small (det/rec small + classificador v5), 100% local |");
        builder.AppendLine();
        builder.AppendLine("## Inicialização");
        builder.AppendLine();
        builder.AppendLine($"- Tempo de inicialização do engine: {initTime.TotalMilliseconds:0.0} ms");
        builder.AppendLine($"- Memória do processo antes da inicialização: {Mebibytes(memoryBeforeInit)} MB");
        builder.AppendLine($"- Memória do processo após a inicialização: {Mebibytes(memoryAfterInit)} MB (Δ {Mebibytes(memoryAfterInit - memoryBeforeInit)} MB)");
        builder.AppendLine();
        builder.AppendLine("## Aquecimento (excluído das estatísticas)");
        builder.AppendLine();
        builder.AppendLine($"- Passada completa da coleção antes das medições: {warmup.Sum(r => r.WallMilliseconds):0.0} ms no total.");
        builder.AppendLine();
        builder.AppendLine("## Resultados por imagem");
        builder.AppendLine();
        builder.AppendLine("CER bruto = distância de Levenshtein(referência, hipótese) / comprimento da referência, com acentos e caso preservados; apenas espaços nas bordas são removidos. CER frouxo aplica ainda o colapso de sequências internas de espaços/quebras de linha em um único espaço.");
        builder.AppendLine();
        builder.AppendLine("| Caso | CER bruto | CER frouxo | Exato (bruto) | Exato (frouxo) | Tempo (ms) | Erro |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");

        var textCases = new List<(
            OcrCaseSpec Spec,
            CaseRun Run,
            string Ref,
            string Hyp,
            string RelRef,
            string RelHyp,
            (int Distance, int Insertions, int Deletions, int Substitutions) Ops,
            (int Distance, int Insertions, int Deletions, int Substitutions) RelOps)>();

        foreach (var run in measured)
        {
            string cerRaw = "—";
            string cerRelaxed = "—";
            string exactRaw = "—";
            string exactRelaxed = "—";

            if (run.Error is null)
            {
                if (run.Spec.IsNoTextCase)
                {
                    cerRaw = "n/d";
                    cerRelaxed = "n/d";
                }
                else
                {
                    var reference = CharacterErrorRate.Trimmed(run.Spec.Reference);
                    var hypothesis = CharacterErrorRate.Trimmed(run.Text);
                    var ops = CharacterErrorRate.EditOperations(reference, hypothesis);
                    var relReference = CharacterErrorRate.WhitespaceCollapsed(reference);
                    var relHypothesis = CharacterErrorRate.WhitespaceCollapsed(hypothesis);
                    var relOps = CharacterErrorRate.EditOperations(relReference, relHypothesis);

                    textCases.Add((run.Spec, run, reference, hypothesis, relReference, relHypothesis, ops, relOps));

                    cerRaw = (ops.Distance / (double)Math.Max(1, reference.Length)).ToString("0.000");
                    cerRelaxed = (relOps.Distance / (double)Math.Max(1, relReference.Length)).ToString("0.000");
                    exactRaw = reference == hypothesis ? "sim" : "não";
                    exactRelaxed = relReference == relHypothesis ? "sim" : "não";
                }
            }
            else
            {
                exactRaw = "erro";
            }

            builder.AppendLine(
                $"| {run.Spec.Id} | {cerRaw} | {cerRelaxed} | {exactRaw} | {exactRelaxed} | {run.WallMilliseconds:0.0} | {run.Error ?? ""} |");
        }

        builder.AppendLine();
        builder.AppendLine("### Detalhes por caso (referência × reconhecimento)");
        builder.AppendLine();
        foreach (var run in measured)
        {
            builder.AppendLine($"#### {run.Spec.Id}");
            builder.AppendLine();
            builder.AppendLine($"- Referência: `{OneLine(run.Spec.Reference)}`");
            builder.AppendLine($"- Reconhecido: `{OneLine(run.Text)}`");
            builder.AppendLine($"- Blocos: {run.BlockCount}; tempo: {run.WallMilliseconds:0.0} ms");
            if (run.Error is not null)
            {
                builder.AppendLine($"- Erro: {run.Error}");
            }

            builder.AppendLine();
        }

        builder.AppendLine("## Casos sem texto");
        builder.AppendLine();
        builder.AppendLine("Para referência vazia o CER não é definido (não há divisão por zero): o critério é se o motor retornou texto/blocos vazios.");
        builder.AppendLine();
        builder.AppendLine("| Caso | Retornou texto vazio? | Blocos | Texto retornado |");
        builder.AppendLine("| --- | --- | --- | --- |");
        foreach (var run in measured.Where(r => r.Spec.IsNoTextCase))
        {
            var empty = run.Error is null && string.IsNullOrEmpty(run.Text);
            builder.AppendLine($"| {run.Spec.Id} | {(empty ? "sim" : "não")} | {run.BlockCount} | `{OneLine(run.Text)}` |");
        }

        builder.AppendLine();
        builder.AppendLine("## Agregados (casos com texto)");
        builder.AppendLine();
        int totalReferenceChars = textCases.Sum(t => t.Ref.Length);
        int totalDistance = textCases.Sum(t => t.Ops.Distance);
        int totalInsertions = textCases.Sum(t => t.Ops.Insertions);
        int totalDeletions = textCases.Sum(t => t.Ops.Deletions);
        int totalSubstitutions = textCases.Sum(t => t.Ops.Substitutions);
        int exactRawCount = textCases.Count(t => t.Ref == t.Hyp);
        int exactRelaxedCount = textCases.Count(t => t.RelRef == t.RelHyp);
        int relTotalDistance = textCases.Sum(t => t.RelOps.Distance);
        int relTotalReferenceChars = textCases.Sum(t => t.RelRef.Length);
        var times = measured.Where(r => r.Error is null).Select(r => r.WallMilliseconds).OrderBy(t => t).ToList();

        builder.AppendLine("| Métrica | Valor |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine($"| Casos com texto avaliados | {textCases.Count} |");
        builder.AppendLine($"| Caracteres de referência (total) | {totalReferenceChars} |");
        builder.AppendLine($"| Distância de edição total | {totalDistance} |");
        builder.AppendLine($"| CER agregado (soma das distâncias / soma das referências) | {(totalDistance / (double)Math.Max(1, totalReferenceChars)):0.000} |");
        builder.AppendLine($"| CER agregado (visão frouxa) | {(relTotalDistance / (double)Math.Max(1, relTotalReferenceChars)):0.000} |");
        builder.AppendLine($"| Inserções / exclusões / substituições (total) | {totalInsertions} / {totalDeletions} / {totalSubstitutions} |");
        builder.AppendLine($"| Transcrição exata (bruta) | {exactRawCount}/{textCases.Count} |");
        builder.AppendLine($"| Transcrição exata (visão frouxa) | {exactRelaxedCount}/{textCases.Count} |");
        if (times.Count > 0)
        {
            double median = Median(times);
            builder.AppendLine($"| Tempo de reconhecimento — média | {times.Average():0.0} ms |");
            builder.AppendLine($"| Tempo de reconhecimento — mediana | {median:0.0} ms |");
            builder.AppendLine($"| Tempo de reconhecimento — mín/máx | {times.Min():0.0} / {times.Max():0.0} ms |");
        }

        builder.AppendLine();
        builder.AppendLine("## Memória");
        builder.AppendLine();
        builder.AppendLine("`Process.WorkingSet64`: amostra da memória residente do processo inteiro (test host + engine + modelos), não uma medição exclusiva do OCR.");
        builder.AppendLine();
        builder.AppendLine("| Ponto | MB |");
        builder.AppendLine("| --- | --- |");
        builder.AppendLine($"| Antes da inicialização | {Mebibytes(memoryBeforeInit)} |");
        builder.AppendLine($"| Após a inicialização | {Mebibytes(memoryAfterInit)} |");
        builder.AppendLine($"| Mínimo durante a coleção | {Mebibytes(memorySamples.Min())} |");
        builder.AppendLine($"| Máximo durante a coleção | {Mebibytes(memorySamples.Max())} |");
        builder.AppendLine($"| Após o Dispose do engine | {Mebibytes(memoryAfterDispose)} |");

        builder.AppendLine();
        builder.AppendLine("## Aquecimento — tempos por caso (para referência)");
        builder.AppendLine();
        builder.AppendLine("| Caso | Tempo (ms) |");
        builder.AppendLine("| --- | --- |");
        foreach (var run in warmup)
        {
            builder.AppendLine($"| {run.Spec.Id} | {run.WallMilliseconds:0.0} |");
        }

        return builder.ToString();
    }

    private static void WriteReport(string report, ITestOutputHelper output)
    {
        output.WriteLine(report);

        // The report lives inside the test structure so the experiment is
        // reproducible from the repository. One file per configuration so
        // the Debug and Release records do not overwrite each other; the
        // header states the machine and configuration explicitly.
        var reportsDirectory = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "Ocr", "reports"));
        Directory.CreateDirectory(reportsDirectory);
        File.WriteAllText(Path.Combine(reportsDirectory, $"evaluation-{DetectConfiguration()}.md"), report);
    }

    private static string DetectConfiguration()
    {
        var baseDirectory = AppContext.BaseDirectory;
        if (baseDirectory.Contains("Release", StringComparison.OrdinalIgnoreCase))
        {
            return "Release";
        }

        if (baseDirectory.Contains("Debug", StringComparison.OrdinalIgnoreCase))
        {
            return "Debug";
        }

        return "desconhecida (" + baseDirectory + ")";
    }

    private static string Mebibytes(long bytes) => (bytes / (1024.0 * 1024.0)).ToString("0.0");

    private static double Median(List<double> sortedValues)
    {
        int count = sortedValues.Count;
        return count % 2 == 1
            ? sortedValues[count / 2]
            : (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2.0;
    }

    private static string OneLine(string? value) =>
        value is null ? "(falha)" : value.Replace("\r", " ").Replace("\n", " ⏎ ");
}
