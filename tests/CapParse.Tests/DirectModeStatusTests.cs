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

    // ------------------------------------------------------------------
    // The tray icon's tooltip
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(true, false, null)]   // direct mode in force, no shortcut: the nudge
    [InlineData(true, true, null)]    // direct mode in force, shortcut(s) configured: no nudge
    [InlineData(false, false, null)]  // traditional mode: plain name
    [InlineData(false, true, null)]   // traditional mode (slots preserved): plain name
    public void TrayTooltip_WithoutALastResult_ReflectsTheDirectMode(
        bool modeEnabled,
        bool anySlotConfigured,
        string? lastConfirmation)
    {
        var expected = modeEnabled && !anySlotConfigured
            ? "CapParse — Modo Direto ativo; configure os atalhos"
            : "CapParse";

        Assert.Equal(expected, DirectModeStatus.TrayTooltip(modeEnabled, anySlotConfigured, lastConfirmation));
    }

    [Fact]
    public void TrayTooltip_NamesTheLastConfirmedResult()
    {
        // The toast disappears after a few seconds; the tooltip is the
        // later query, so it names the last result that was confirmed.
        Assert.Equal(
            "CapParse — última: Imagem salva.",
            DirectModeStatus.TrayTooltip(false, false, "Imagem salva."));
    }

    [Fact]
    public void TrayTooltip_TheNudgeWinsOverALastResult()
    {
        // Configuring the direct shortcuts is the state the user must act
        // on: a stale result line must never bury the nudge.
        Assert.Equal(
            "CapParse — Modo Direto ativo; configure os atalhos",
            DirectModeStatus.TrayTooltip(true, false, "Imagem salva."));
    }

    [Fact]
    public void TrayTooltip_HoldsExactlyTheLatestResult()
    {
        // The tooltip carries one line: the latest confirmation. After a
        // failure, the previous success is gone — a stale success can
        // never stand after a failure.
        var afterSuccess = DirectModeStatus.TrayTooltip(false, false, "Imagem copiada.");
        var afterFailure = DirectModeStatus.TrayTooltip(false, false, "Falha na captura");

        Assert.Contains("Imagem copiada.", afterSuccess);
        Assert.Contains("Falha na captura", afterFailure);
        Assert.DoesNotContain("Imagem copiada.", afterFailure);
    }

    [Fact]
    public void TrayTooltip_Nudge_StaysWithinTheShellLimit()
    {
        // The shell truncates tooltips at 127 characters: the nudge must
        // never be cut off mid-sentence.
        Assert.True(DirectModeStatus.TrayTooltip(true, false, null).Length <= 127);
    }

    [Theory]
    [InlineData("Imagem copiada.")]
    [InlineData("Nenhum texto foi reconhecido nesta região.")]
    [InlineData("Falha na captura")]
    [InlineData("Captura pronta")]
    public void TrayTooltip_WithALastResult_StaysWithinTheShellLimit(string lastResult)
    {
        // The longest real message kept under the limit: the line is
        // never truncated mid-sentence.
        Assert.True(DirectModeStatus.TrayTooltip(false, false, lastResult).Length <= 127);
    }
}
