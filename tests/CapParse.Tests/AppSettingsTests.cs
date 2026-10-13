using System.IO;
using CapParse.Configuration;

namespace CapParse.Tests;

// The settings file lives in a temporary directory per test class run, so
// the tests never touch the user's real settings file.
public class AppSettingsTests : IDisposable
{
    private readonly string _directory =
        Directory.CreateTempSubdirectory("capparse-settings-").FullName;

    private string SettingsPath => System.IO.Path.Combine(_directory, "settings.json");

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch
        {
            // Best effort: a leftover temp directory is harmless.
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var settings = AppSettings.Load(SettingsPath);

        Assert.Equal(HotkeySetting.Default, settings.CaptureHotkey);
        Assert.False(settings.FirstRunPresented);
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var settings = new AppSettings
        {
            CaptureHotkey = new HotkeySetting(
                (uint)CapParse.Platform.GlobalHotkey.Modifier.Control, 0x41) // Ctrl + A
        };

        Assert.True(settings.TrySave(SettingsPath));

        var loaded = AppSettings.Load(SettingsPath);
        Assert.Equal(settings.CaptureHotkey, loaded.CaptureHotkey);
        Assert.Equal("Ctrl + A", loaded.CaptureHotkey.DisplayString);
    }

    [Fact]
    public void Load_InvalidJson_ReturnsDefaults()
    {
        File.WriteAllText(SettingsPath, "{ this is not json ");

        var settings = AppSettings.Load(SettingsPath);

        Assert.Equal(HotkeySetting.Default, settings.CaptureHotkey);
    }

    [Theory]
    [InlineData(@"{ ""Version"": 1, ""CaptureHotkey"": { ""Modifiers"": 0, ""Key"": 88 } }")]     // no required modifier
    [InlineData(@"{ ""Version"": 1, ""CaptureHotkey"": { ""Modifiers"": 4, ""Key"": 88 } }")]    // Shift only
    [InlineData(@"{ ""Version"": 1, ""CaptureHotkey"": { ""Modifiers"": 2, ""Key"": 18 } }")]    // unsupported key (Esc)
    [InlineData(@"{ ""Version"": 1, ""CaptureHotkey"": { ""Modifiers"": 2, ""Key"": 0 } }")]     // no key
    [InlineData(@"{ ""Version"": 1, ""CaptureHotkey"": { ""Modifiers"": 16, ""Key"": 88 } }")]   // unknown modifier
    [InlineData(@"{ ""Version"": 1 }")]                                                         // hotkey missing
    [InlineData(@"{ ""Version"": 2, ""CaptureHotkey"": { ""Modifiers"": 6, ""Key"": 88 } }")]    // unsupported version
    public void Load_InvalidFile_ReturnsDefaults(string content)
    {
        File.WriteAllText(SettingsPath, content);

        var settings = AppSettings.Load(SettingsPath);

        Assert.Equal(HotkeySetting.Default, settings.CaptureHotkey);
    }

    [Fact]
    public void Save_ThenLoad_FirstRunPresented_RoundTrips()
    {
        var settings = new AppSettings
        {
            CaptureHotkey = HotkeySetting.Default,
            FirstRunPresented = true
        };

        Assert.True(settings.TrySave(SettingsPath));

        var loaded = AppSettings.Load(SettingsPath);
        Assert.True(loaded.FirstRunPresented);
    }

    [Fact]
    public void Load_LegacyFileWithoutTheFlag_FirstRunNotPresented()
    {
        // A file written before the flag existed must read as "first run
        // not presented yet", so the presentation shows for upgrading
        // users exactly once.
        File.WriteAllText(SettingsPath,
            "{ \"Version\": 1, \"CaptureHotkey\": { \"Modifiers\": 6, \"Key\": 88 } }");

        var settings = AppSettings.Load(SettingsPath);

        Assert.False(settings.FirstRunPresented);
        Assert.Equal("Ctrl + Shift + X", settings.CaptureHotkey.DisplayString);
    }

    [Fact]
    public void Load_InvalidFile_FirstRunNotPresented()
    {
        File.WriteAllText(SettingsPath, "{ this is not json ");

        Assert.False(AppSettings.Load(SettingsPath).FirstRunPresented);
    }

    [Fact]
    public void Load_DoesNotModifyTheFile()
    {
        const string content = "{ corrupted ";
        File.WriteAllText(SettingsPath, content);

        AppSettings.Load(SettingsPath);

        Assert.Equal(content, File.ReadAllText(SettingsPath));
    }

    [Fact]
    public void Save_OverwritesThePreviousFile()
    {
        var first = new AppSettings
        {
            CaptureHotkey = new HotkeySetting(
                (uint)CapParse.Platform.GlobalHotkey.Modifier.Control, 0x41)
        };
        Assert.True(first.TrySave(SettingsPath));

        var second = new AppSettings
        {
            CaptureHotkey = HotkeySetting.Default
        };
        Assert.True(second.TrySave(SettingsPath));

        var loaded = AppSettings.Load(SettingsPath);
        Assert.Equal(HotkeySetting.Default, loaded.CaptureHotkey);
    }

    [Fact]
    public void Save_CreatesTheDirectoryWhenMissing()
    {
        var nested = System.IO.Path.Combine(_directory, "new", "sub", "settings.json");
        var settings = new AppSettings { CaptureHotkey = HotkeySetting.Default };

        Assert.True(settings.TrySave(nested));
        Assert.Equal(HotkeySetting.Default, AppSettings.Load(nested).CaptureHotkey);
    }

    [Fact]
    public void TrySave_UnwritablePath_ReturnsFalse()
    {
        // A directory component that is a regular file makes every
        // create/write attempt fail, without needing special permissions.
        var blocker = System.IO.Path.Combine(_directory, "blocker");
        File.WriteAllText(blocker, "blocker");
        var unwritable = System.IO.Path.Combine(blocker, "settings.json");

        var settings = new AppSettings { CaptureHotkey = HotkeySetting.Default };

        Assert.False(settings.TrySave(unwritable));
    }

    [Fact]
    public void DefaultSettingsPath_IsUnderTheUserLocalAppData()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        Assert.StartsWith(localAppData, AppSettings.DefaultSettingsPath);
        Assert.EndsWith(System.IO.Path.Combine("CapParse", "settings.json"), AppSettings.DefaultSettingsPath);
    }

    [Fact]
    public void ThePersistedFile_ContainsNoPersonalData()
    {
        // The settings file must never grow personal data: it holds the
        // version, the hotkey combination and the first-run flag, and
        // nothing else.
        var settings = new AppSettings
        {
            CaptureHotkey = HotkeySetting.Default,
            FirstRunPresented = true
        };
        Assert.True(settings.TrySave(SettingsPath));

        var content = File.ReadAllText(SettingsPath);
        Assert.Contains("\"Version\"", content);
        Assert.Contains("\"CaptureHotkey\"", content);
        Assert.Contains("\"Modifiers\"", content);
        Assert.Contains("\"Key\"", content);
        Assert.Contains("\"FirstRunPresented\"", content);
    }
}
