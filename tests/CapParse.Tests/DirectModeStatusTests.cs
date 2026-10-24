using CapParse.Configuration;
using CapParse.Platform;

namespace CapParse.Tests;

// The small state model behind the direct-mode status the settings
// window shows: the per-slot registration warning (visible only for a
// real, current registration failure — never for a not-yet-saved
// combination, and cleared as soon as the slot registers) and the
// dynamic status message (zero, some, or all slots configured, with or
// without registration failures). Pure logic — no window is involved.
public class DirectModeStatusTests
{
    private static HotkeySetting CtrlShiftX()
        => new(
            (uint)GlobalHotkey.Modifier.Control | (uint)GlobalHotkey.Modifier.Shift,
            GlobalHotkey.KeyX);

    private static HotkeySetting CtrlA()
        => new((uint)GlobalHotkey.Modifier.Control, 0x41);

    private static HotkeySetting CtrlB()
        => new((uint)GlobalHotkey.Modifier.Control, 0x42);

    private static Dictionary<int, HotkeySetting?> Slots(
        HotkeySetting? copyImage = null,
        HotkeySetting? copyText = null,
        HotkeySetting? saveImage = null)
        => new()
        {
            [HotkeySlots.DirectCopyImage] = copyImage,
            [HotkeySlots.DirectCopyText] = copyText,
            [HotkeySlots.DirectSaveImage] = saveImage
        };

    private static bool NoneRegistered(int slot) => false;

    // ------------------------------------------------------------------
    // The per-slot registration warning
    // ------------------------------------------------------------------

    [Fact]
    public void SlotWarning_ModeOff_IsNeverVisible()
    {
        // A saved combination that cannot register while the mode is on
        // must not warn while the mode is off: the slot registers
        // nothing, so nothing failed.
        Assert.False(DirectModeStatus.SlotWarningVisible(false, CtrlShiftX(), false));
    }

    [Fact]
    public void SlotWarning_NoSavedCombination_IsNeverVisible()
    {
        // An unset (or not-yet-saved) slot cannot have failed to
        // register: the warning must not appear while the user is still
        // choosing the combination.
        Assert.False(DirectModeStatus.SlotWarningVisible(true, null, false));
    }

    [Fact]
    public void SlotWarning_SavedCombination_NotRegistered_IsVisible()
    {
        // The shape of a real registration failure: the mode is on, the
        // settings carry a combination, the OS does not have it
        // registered (typically an external application owns it).
        Assert.True(DirectModeStatus.SlotWarningVisible(true, CtrlShiftX(), false));
    }

    [Fact]
    public void SlotWarning_SuccessfulRegistration_ClearsThePreviousWarning()
    {
        // The stale-warning bug: once the combination registers (the user
        // freed the conflicting combination, or re-saved a working one),
        // the warning must be gone — the state says registered, and the
        // UI can only report what the state says.
        Assert.False(DirectModeStatus.SlotWarningVisible(true, CtrlShiftX(), true));
    }

    // ------------------------------------------------------------------
    // The dynamic status message
    // ------------------------------------------------------------------

    [Fact]
    public void Status_Message_ModeOff_IsHidden()
    {
        Assert.Null(DirectModeStatus.Message(false, Slots(CtrlA()), NoneRegistered));
    }

    [Fact]
    public void Status_Message_NoSlotConfigured_PointsToConfiguringAShortcut()
    {
        var message = DirectModeStatus.Message(true, Slots(), NoneRegistered);

        Assert.NotNull(message);
        Assert.Contains("Nenhum atalho configurado", message);
    }

    [Fact]
    public void Status_Message_SomeSlotsConfigured_NamesTheActionsWithoutAShortcut()
    {
        // Partial configuration is a valid state: the message names the
        // actions that still have no shortcut, without presenting the
        // empty slots as an error.
        var message = DirectModeStatus.Message(true, Slots(copyImage: CtrlA()), NoneRegistered);

        Assert.NotNull(message);
        Assert.Contains("Ações sem atalho", message);
        Assert.Contains("OCR + copiar texto", message);
        Assert.Contains("salvar imagem", message);
        Assert.DoesNotContain("copiar imagem", message.Split("Ações sem atalho:")[1]);
    }

    [Fact]
    public void Status_Message_AllConfiguredAndRegistered_ReportsComplete()
    {
        var message = DirectModeStatus.Message(
            true,
            Slots(CtrlA(), CtrlB(), CtrlShiftX()),
            _ => true);

        Assert.Equal("Configuração completa: os três atalhos estão ativos.", message);
    }

    [Fact]
    public void Status_Message_AllConfigured_SomeRegistrationFailed_PointsAtTheSlotWarnings()
    {
        // The registrations of two slots are live and one is not: the
        // status says so and points at the per-slot warnings, which carry
        // the individual failures (and clear when a registration is
        // fixed).
        var message = DirectModeStatus.Message(
            true,
            Slots(CtrlA(), CtrlB(), CtrlShiftX()),
            slot => slot != HotkeySlots.DirectSaveImage);

        Assert.NotNull(message);
        Assert.Contains("salvar imagem", message);
        Assert.Contains("aviso", message);
    }

    [Fact]
    public void SlotName_NamesTheThreeActions()
    {
        Assert.Equal("copiar imagem", DirectModeStatus.SlotName(HotkeySlots.DirectCopyImage));
        Assert.Equal("OCR + copiar texto", DirectModeStatus.SlotName(HotkeySlots.DirectCopyText));
        Assert.Equal("salvar imagem", DirectModeStatus.SlotName(HotkeySlots.DirectSaveImage));
    }
}
