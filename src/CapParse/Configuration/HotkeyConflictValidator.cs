namespace CapParse.Configuration;

/// <summary>
/// Internal-conflict validation for the hotkey slots: two slots of the
/// application must never carry the same combination, so one shortcut can
/// never point at two actions. The check is a pure function over the
/// slot-to-setting map and needs no Windows API: null (unset) slots are
/// ignored, because an unset slot registers nothing and cannot conflict.
/// The application runs this before registering, so a conflicting
/// combination is rejected before any registration is attempted.
/// </summary>
public static class HotkeyConflictValidator
{
    /// <summary>
    /// The combinations (each reported once) that more than one slot
    /// carries. Empty when there is no internal conflict.
    /// </summary>
    public static List<HotkeySetting> FindConflicts(IReadOnlyDictionary<int, HotkeySetting?> slots)
    {
        if (slots is null)
        {
            throw new ArgumentNullException(nameof(slots));
        }

        var counts = new Dictionary<HotkeySetting, int>();
        foreach (var setting in slots.Values)
        {
            if (setting is null)
            {
                continue;
            }

            counts[setting] = counts.GetValueOrDefault(setting) + 1;
        }

        var conflicts = new List<HotkeySetting>();
        foreach (var (setting, count) in counts)
        {
            if (count > 1)
            {
                conflicts.Add(setting);
            }
        }

        return conflicts;
    }
}
