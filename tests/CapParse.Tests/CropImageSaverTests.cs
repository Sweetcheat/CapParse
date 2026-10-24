using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CapParse.UI;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace CapParse.Tests;

// M12: the save action is explicit and never writes a file unless the user
// confirms a path (cancellation is a normal outcome, not an error). The
// written PNG keeps the crop's pixels and DPI tag verbatim, the name is
// normalized to the PNG extension, the source bitmap is left alive, and
// write failures are reported as outcomes instead of throwing. The save
// dialog is faked, so none of these checks touch a real dialog or the
// desktop.
public class CropImageSaverTests
{
    [Fact]
    public void TrySave_NullCrop_FailsWithoutShowingTheDialog()
    {
        var dialog = new FakeSaveDialog(result: null);
        var saver = new CropImageSaver(dialog);

        var outcome = saver.TrySave(null, 96);

        Assert.Equal(SaveImageStatus.Failed, outcome.Status);
        Assert.NotEmpty(outcome.Message);
        Assert.Equal(0, dialog.ShowCount);
    }

    [Fact]
    public void TrySave_DisposedCrop_FailsWithoutShowingTheDialog()
    {
        var crop = MakeCrop(2, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        crop.Dispose();
        var dialog = new FakeSaveDialog(result: null);
        var saver = new CropImageSaver(dialog);

        var outcome = saver.TrySave(crop, 96);

        Assert.Equal(SaveImageStatus.Failed, outcome.Status);
        Assert.NotEmpty(outcome.Message);
        Assert.Equal(0, dialog.ShowCount);
    }

    [Fact]
    public void TrySave_DialogCanceled_ReturnsCanceledAndWritesNothing()
    {
        using var dir = new TempDirectory();
        using var crop = MakeCrop(2, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var dialog = new FakeSaveDialog(result: null);
        var saver = new CropImageSaver(dialog);

        var outcome = saver.TrySave(crop, 96);

        Assert.Equal(SaveImageStatus.Canceled, outcome.Status);
        Assert.Empty(outcome.Message);
        Assert.Empty(Directory.GetFiles(dir.Path));
    }

    [Fact]
    public void TrySave_ConfirmedPath_SavesPngWithTheSamePixels()
    {
        // 4x2 with known BGRA pixels: the written PNG must carry the same
        // dimensions, pixel format and pixels as the crop.
        var expected = new byte[]
        {
            0, 0, 0, 255,
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255,
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16,
        };
        using var dir = new TempDirectory();
        using var crop = MakeCrop(4, 2, expected);
        var path = Path.Combine(dir.Path, "crop.png");
        var dialog = new FakeSaveDialog(result: path);
        var saver = new CropImageSaver(dialog);

        var outcome = saver.TrySave(crop, 144);

        Assert.Equal(SaveImageStatus.Saved, outcome.Status);
        Assert.True(File.Exists(path));

        var frame = DecodePng(path);
        Assert.Equal(4, frame.PixelWidth);
        Assert.Equal(2, frame.PixelHeight);
        Assert.Equal(PixelFormats.Bgra32, frame.Format);

        var actual = new byte[4 * 2 * 4];
        frame.CopyPixels(actual, 4 * 4, 0);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TrySave_ConfirmedPath_KeepsTheDpiTag()
    {
        // The saved file must carry the same DPI tag the clipboard copy
        // uses. The PNG pHYs chunk stores pixels per meter, so the decoded
        // value round-trips with sub-DPI precision.
        using var dir = new TempDirectory();
        using var crop = MakeCrop(2, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var path = Path.Combine(dir.Path, "crop.png");
        var saver = new CropImageSaver(new FakeSaveDialog(result: path));

        var outcome = saver.TrySave(crop, 144);

        Assert.Equal(SaveImageStatus.Saved, outcome.Status);

        var frame = DecodePng(path);
        Assert.InRange(frame.DpiX, 143, 145);
        Assert.InRange(frame.DpiY, 143, 145);
    }

    [Fact]
    public void TrySave_InvalidDpi_FallsBackTo96()
    {
        using var dir = new TempDirectory();
        using var crop = MakeCrop(2, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var path = Path.Combine(dir.Path, "crop.png");
        var saver = new CropImageSaver(new FakeSaveDialog(result: path));

        var outcome = saver.TrySave(crop, 0);

        Assert.Equal(SaveImageStatus.Saved, outcome.Status);

        var frame = DecodePng(path);
        Assert.InRange(frame.DpiX, 95, 97);
        Assert.InRange(frame.DpiY, 95, 97);
    }

    [Fact]
    public void TrySave_MissingExtension_WritesAPngFile()
    {
        // The dialog returns what the user confirmed; the saver must still
        // write a PNG with a .png name.
        using var dir = new TempDirectory();
        using var crop = MakeCrop(2, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var saver = new CropImageSaver(new FakeSaveDialog(result: Path.Combine(dir.Path, "crop")));

        var outcome = saver.TrySave(crop, 96);

        Assert.Equal(SaveImageStatus.Saved, outcome.Status);
        Assert.True(File.Exists(Path.Combine(dir.Path, "crop.png")));
        Assert.False(File.Exists(Path.Combine(dir.Path, "crop")));
    }

    [Fact]
    public void TrySave_DifferentExtension_IsReplacedByPng()
    {
        // The content is always PNG, so a foreign extension is replaced
        // instead of producing a file whose name contradicts its content.
        using var dir = new TempDirectory();
        using var crop = MakeCrop(2, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var saver = new CropImageSaver(new FakeSaveDialog(result: Path.Combine(dir.Path, "crop.jpg")));

        var outcome = saver.TrySave(crop, 96);

        Assert.Equal(SaveImageStatus.Saved, outcome.Status);
        Assert.True(File.Exists(Path.Combine(dir.Path, "crop.png")));
        Assert.False(File.Exists(Path.Combine(dir.Path, "crop.jpg")));
    }

    [Fact]
    public void TrySave_UncreatablePath_IsReportedAsAFailedOutcome()
    {
        // The chosen path is inside a directory that does not exist: the
        // write fails and the saver reports it instead of throwing.
        using var dir = new TempDirectory();
        using var crop = MakeCrop(2, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var saver = new CropImageSaver(
            new FakeSaveDialog(result: Path.Combine(dir.Path, "no-such-directory", "crop.png")));

        var outcome = saver.TrySave(crop, 96);

        Assert.Equal(SaveImageStatus.Failed, outcome.Status);
        Assert.NotEmpty(outcome.Message);
    }

    [Fact]
    public void TrySave_DialogFailure_IsReportedAsAFailedOutcome()
    {
        using var crop = MakeCrop(2, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var saver = new CropImageSaver(new FakeSaveDialog(throwOnShow: true));

        var outcome = saver.TrySave(crop, 96);

        Assert.Equal(SaveImageStatus.Failed, outcome.Status);
        Assert.NotEmpty(outcome.Message);
    }

    [Fact]
    public void TrySave_LeavesTheSourceAlive()
    {
        using var dir = new TempDirectory();
        var crop = MakeCrop(2, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var saver = new CropImageSaver(new FakeSaveDialog(result: Path.Combine(dir.Path, "crop.png")));

        var outcome = saver.TrySave(crop, 96);

        Assert.Equal(SaveImageStatus.Saved, outcome.Status);

        // The conversion and the encoding must not dispose the source: the
        // application keeps owning the crop until the next capture.
        Assert.Equal(2, crop.Width);
        Assert.Equal(1, crop.Height);
        crop.Dispose();
    }

    [Fact]
    public void TrySave_PassesASuggestedPngNameToTheDialog()
    {
        using var dir = new TempDirectory();
        using var crop = MakeCrop(2, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var dialog = new FakeSaveDialog(result: Path.Combine(dir.Path, "crop.png"));
        var saver = new CropImageSaver(dialog);

        saver.TrySave(crop, 96);

        Assert.NotNull(dialog.LastSuggestedFileName);
        Assert.StartsWith("CapParse-", dialog.LastSuggestedFileName);
        Assert.EndsWith(".png", dialog.LastSuggestedFileName);
    }

    [Fact]
    public void SuggestedFileName_FollowsTheCapParseTimestampPattern()
    {
        var name = CropImageSaver.SuggestedFileName(new DateTime(2026, 10, 12, 13, 45, 30));

        Assert.Equal("CapParse-20261012-134530.png", name);
    }

    [Theory]
    [InlineData("foto", "foto.png")]
    [InlineData("C:\\imgs\\foto", "C:\\imgs\\foto.png")]
    [InlineData("foto.PNG", "foto.PNG")]
    [InlineData("foto.png", "foto.png")]
    [InlineData("foto.jpg", "foto.png")]
    [InlineData("C:\\imgs\\foto.jpeg", "C:\\imgs\\foto.png")]
    public void NormalizePngPath_KeepsTheNameAndForcesThePngExtension(string input, string expected)
    {
        Assert.Equal(expected, CropImageSaver.NormalizePngPath(input));
    }

    // --------------------------------------------------------------------
    // Automatic save policy (direct mode): TrySaveToFolder
    // --------------------------------------------------------------------

    [Fact]
    public void TrySaveToFolder_WritesAPngIntoTheFolder()
    {
        using var dir = new TempDirectory();
        using var crop = MakeCrop(2, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var saver = new CropImageSaver(new FakeSaveDialog());

        var outcome = saver.TrySaveToFolder(crop, 96, dir.Path);

        Assert.Equal(SaveImageStatus.Saved, outcome.Status);

        var file = Assert.Single(Directory.GetFiles(dir.Path));
        Assert.StartsWith("CapParse-", Path.GetFileName(file));
        Assert.EndsWith(".png", file);

        var frame = DecodePng(file);
        Assert.Equal(2, frame.PixelWidth);
        Assert.Equal(1, frame.PixelHeight);
    }

    [Fact]
    public void TrySaveToFolder_DoesNotConsultTheDialog()
    {
        // The automatic policy writes to the folder directly: the save
        // dialog (the manual policy) must not be shown.
        using var dir = new TempDirectory();
        using var crop = MakeCrop(2, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var dialog = new FakeSaveDialog();
        var saver = new CropImageSaver(dialog);

        var outcome = saver.TrySaveToFolder(crop, 96, dir.Path);

        Assert.Equal(SaveImageStatus.Saved, outcome.Status);
        Assert.Equal(0, dialog.ShowCount);
    }

    [Fact]
    public void TrySaveToFolder_NeverOverwritesAnExistingFile()
    {
        // Two saves with the same generated name (the same second) must
        // produce two distinct files: the second gets a numbered name and
        // the first file is left byte-for-byte identical.
        using var dir = new TempDirectory();
        using var crop = MakeCrop(2, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var saver = new CropImageSaver(new FakeSaveDialog());
        var now = new DateTime(2026, 10, 12, 13, 45, 30);

        Assert.Equal(SaveImageStatus.Saved, saver.TrySaveToFolder(crop, 96, dir.Path, now).Status);

        var baseName = CropImageSaver.SuggestedFileName(now);
        var firstPath = Path.Combine(dir.Path, baseName);
        Assert.True(File.Exists(firstPath));
        var firstBytes = File.ReadAllBytes(firstPath);

        Assert.Equal(SaveImageStatus.Saved, saver.TrySaveToFolder(crop, 96, dir.Path, now).Status);

        Assert.Equal(firstBytes, File.ReadAllBytes(firstPath));
        var files = Directory.GetFiles(dir.Path);
        Assert.Equal(2, files.Length);
        Assert.Contains(files, f => Path.GetFileName(f) == baseName.Replace(".png", " (2).png"));
    }

    [Fact]
    public void TrySaveToFolder_MissingFolder_FailsWithoutThrowing()
    {
        using var dir = new TempDirectory();
        using var crop = MakeCrop(2, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var saver = new CropImageSaver(new FakeSaveDialog());

        var outcome = saver.TrySaveToFolder(crop, 96, Path.Combine(dir.Path, "no-such-folder"));

        Assert.Equal(SaveImageStatus.Failed, outcome.Status);
        Assert.NotEmpty(outcome.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TrySaveToFolder_EmptyFolder_Fails(string folder)
    {
        using var crop = MakeCrop(2, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var saver = new CropImageSaver(new FakeSaveDialog());

        var outcome = saver.TrySaveToFolder(crop, 96, folder);

        Assert.Equal(SaveImageStatus.Failed, outcome.Status);
        Assert.NotEmpty(outcome.Message);
    }

    [Fact]
    public void TrySaveToFolder_NullCrop_Fails()
    {
        using var dir = new TempDirectory();
        var saver = new CropImageSaver(new FakeSaveDialog());

        var outcome = saver.TrySaveToFolder(null, 96, dir.Path);

        Assert.Equal(SaveImageStatus.Failed, outcome.Status);
        Assert.Empty(Directory.GetFiles(dir.Path));
    }

    [Fact]
    public void UniqueFileName_KeepsTheNameWhenTheFileDoesNotExist()
    {
        using var dir = new TempDirectory();

        Assert.Equal(Path.Combine(dir.Path, "a.png"), CropImageSaver.UniqueFileName(dir.Path, "a.png"));
    }

    [Fact]
    public void UniqueFileName_NumbersAnExistingFile()
    {
        using var dir = new TempDirectory();
        File.WriteAllText(Path.Combine(dir.Path, "a.png"), "existing");

        Assert.EndsWith("a (2).png", CropImageSaver.UniqueFileName(dir.Path, "a.png"));
    }

    [Fact]
    public void UniqueFileName_NumbersSequentiallyPastExistingSuffixedFiles()
    {
        using var dir = new TempDirectory();
        File.WriteAllText(Path.Combine(dir.Path, "a.png"), "1");
        File.WriteAllText(Path.Combine(dir.Path, "a (2).png"), "2");

        Assert.EndsWith("a (3).png", CropImageSaver.UniqueFileName(dir.Path, "a.png"));
    }

    private static BitmapFrame DecodePng(string path)
    {
        using var stream = File.OpenRead(path);
        var decoder = new PngBitmapDecoder(
            stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        return BitmapFrame.Create(decoder.Frames[0]);
    }

    private static Bitmap MakeCrop(int width, int height, byte[] bgra)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        var bits = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadWrite,
            PixelFormat.Format32bppArgb);
        try
        {
            Marshal.Copy(bgra, 0, bits.Scan0, bgra.Length);
        }
        finally
        {
            bitmap.UnlockBits(bits);
        }

        return bitmap;
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; }

        public TempDirectory()
        {
            Path = Directory.CreateTempSubdirectory("CapParseSaveTest").FullName;
        }

        public void Dispose()
        {
            // Best effort: a left-over file (for example held open by an
            // antivirus scan) must not fail the test run.
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private sealed class FakeSaveDialog : ICropImageSaveDialog
    {
        private readonly string? _result;
        private readonly bool _throwOnShow;

        public FakeSaveDialog(string? result = null, bool throwOnShow = false)
        {
            _result = result;
            _throwOnShow = throwOnShow;
        }

        public string? LastSuggestedFileName { get; private set; }

        public int ShowCount { get; private set; }

        public string? ShowDialog(string suggestedFileName)
        {
            ShowCount++;
            LastSuggestedFileName = suggestedFileName;

            if (_throwOnShow)
            {
                throw new InvalidOperationException("The dialog could not be shown.");
            }

            return _result;
        }
    }
}
