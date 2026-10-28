namespace CapParse.Configuration;

/// <summary>
/// The small state model behind the direct-mode status the settings
/// window shows: the pending slot configuration (what the user is about
/// to save) and, for each slot, whether its registration is live in the
/// OS right now. The status message and the per-slot warning are pure
/// functions of that state, so both are testable without a window — and
/// they can only ever report what the state says: a slot that registered
/// successfully never keeps showing a previous failure.
/// </summary>
public static class DirectModeStatus
{
    /// <summary>The human-readable name of a direct slot's action.</summary>
    public static string SlotName(int slot) => slot switch
    {
        HotkeySlots.DirectCopyImage => "copiar imagem",
        HotkeySlots.DirectCopyText => "OCR + copiar texto",
        HotkeySlots.DirectSaveImage => "salvar imagem",
        _ => "direto"
    };

    /// <summary>
    /// The tray icon's tooltip: it is the passive, always-available
    /// companion of the transient confirmations (the toast disappears, the
    /// tooltip stays). The priority is fixed so the state is readable at a
    /// glance: while the direct mode is in force with no direct shortcut
    /// configured, the nudge points the user at configuring the shortcuts
    /// (it must never be buried under a stale result line, and it must
    /// never suggest that shortcuts are missing once any slot is
    /// configured); otherwise, when a capture result has been confirmed,
    /// the last one is named — the latest confirmation always replaces
    /// the previous line, so a failure can never leave an older success
    /// standing; with no result yet, the plain application name is shown.
    /// The longest combination is well under the shell's 127-character
    /// tooltip limit, so it is never truncated.
    /// </summary>
    public static string TrayTooltip(
        bool modeEnabled,
        bool anyDirectSlotConfigured,
        string? lastConfirmation)
    {
        if (modeEnabled && !anyDirectSlotConfigured)
        {
            return "CapParse — Modo Direto ativo; configure os atalhos";
        }

        return lastConfirmation is not null
            ? $"CapParse — última: {lastConfirmation}"
            : "CapParse";
    }

    /// <summary>
    /// The per-slot registration warning: visible only when the mode is
    /// enabled, the slot carries a combination in the settings the
    /// application is running with, and the OS does not have that slot
    /// registered — the shape of a real registration failure (typically
    /// an external application owns the combination). A not-yet-saved
    /// combination (no saved combination) can have failed nothing, and a
    /// registered slot clears the warning.
    /// </summary>
    public static bool SlotWarningVisible(
        bool modeEnabled,
        HotkeySetting? savedCombination,
        bool registered)
        => modeEnabled && savedCombination is not null && !registered;

    /// <summary>
    /// The direct-mode status message for the settings window, or null
    /// when the mode is off (the status line stays hidden): no slot
    /// configured points the user to configure at least one shortcut;
    /// some configured names the actions that still have no shortcut
    /// (partial configuration is a valid state, never an error); all
    /// configured and registered reports the configuration complete;
    /// all configured but some registration failed points at the
    /// per-slot warnings.
    /// </summary>
    public static string? Message(
        bool modeEnabled,
        IReadOnlyDictionary<int, HotkeySetting?> pendingSlots,
        Func<int, bool> isRegistered)
    {
        ArgumentNullException.ThrowIfNull(pendingSlots);
        ArgumentNullException.ThrowIfNull(isRegistered);

        if (!modeEnabled)
        {
            return null;
        }

        var missing = HotkeySlots.Direct
            .Where(slot => pendingSlots.TryGetValue(slot, out var combination) && combination is null)
            .ToList();

        if (missing.Count == HotkeySlots.Direct.Length)
        {
            return "Nenhum atalho configurado ainda: escolha o atalho de pelo menos uma ação para usar o Modo Direto.";
        }

        if (missing.Count > 0)
        {
            return "Ações sem atalho: "
                + string.Join(", ", missing.Select(SlotName))
                + ". Os demais atalhos funcionam normalmente.";
        }

        var unregistered = HotkeySlots.Direct
            .Where(slot => !isRegistered(slot))
            .ToList();

        if (unregistered.Count == 0)
        {
            return "Configuração completa: os três atalhos estão ativos.";
        }

        return "Todos os slots estão configurados, mas "
            + string.Join(", ", unregistered.Select(SlotName))
            + " não pôde(m) ser registrado(s) agora; veja o aviso de cada slot.";
    }
}
