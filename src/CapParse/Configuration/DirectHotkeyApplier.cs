namespace CapParse.Configuration;

/// <summary>
/// The operational state of one direct hotkey slot: whether the slot is
/// registered with the OS and, when it is, the combination it carries.
/// Distinct from the configured combination in the settings: a configured
/// slot can be unregistered when its combination is taken by another
/// application.
/// </summary>
public sealed record DirectSlotState(bool Registered, HotkeySetting? Combination)
{
    /// <summary>The state of a slot that has no registration.</summary>
    public static DirectSlotState Off { get; } = new(false, null);
}

/// <summary>
/// The operational state of the direct-mode hotkey slots (what is
/// registered with the OS right now), indexed by the slot ids of
/// <see cref="HotkeySlots"/>. A query for a slot that was never reported
/// reads as "unregistered", so a partial state can never be mistaken for
/// a registration.
/// </summary>
public sealed class DirectHotkeyState
{
    private readonly Dictionary<int, DirectSlotState> _slots = new();

    public DirectHotkeyState()
    {
    }

    public DirectHotkeyState(IReadOnlyDictionary<int, DirectSlotState> slots)
    {
        foreach (var (slot, state) in slots)
        {
            this[slot] = state;
        }
    }

    public DirectSlotState this[int slot]
    {
        get => _slots.GetValueOrDefault(slot) ?? DirectSlotState.Off;
        set => _slots[slot] = value;
    }

    // Value equality over the direct-mode slots: a state is what the
    // registrations are, so two states are equal when every direct slot
    // has the same registration. Records compare by value, so the
    // comparison is per slot.
    public bool Equals(DirectHotkeyState? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        foreach (var slot in HotkeySlots.Direct)
        {
            if (!this[slot].Equals(other[slot]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as DirectHotkeyState);

    public override int GetHashCode()
        => HashCode.Combine(
            this[HotkeySlots.DirectCopyImage],
            this[HotkeySlots.DirectCopyText],
            this[HotkeySlots.DirectSaveImage]);

    public static bool operator ==(DirectHotkeyState? left, DirectHotkeyState? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(DirectHotkeyState? left, DirectHotkeyState? right)
        => !(left == right);
}

/// <summary>
/// The result of an attempt to bring the direct-mode registrations to a
/// desired state: whether it succeeded, a message listing the combinations
/// that could not be registered, and the state the slots are in
/// afterwards. On failure the state is intentionally *not* rolled back
/// here: the caller restores the previous state with
/// <see cref="Restore"/>, so the whole transaction (which may also involve
/// the capture hotkey) is rolled back as a unit.
/// </summary>
public sealed record DirectApplyResult(bool Succeeded, string? FailureMessage, DirectHotkeyState After);

/// <summary>
/// The pure application of the direct-mode hotkey registrations: given the
/// desired configuration (the enabled flag plus the configured
/// combinations) and the current operational state, it decides the
/// register/unregister operations in a fixed slot order and reports the
/// resulting state. All OS interaction goes through the injected
/// <c>register</c> and <c>unregister</c> functions, so the transactional
/// behavior — disabled mode registers nothing, enabled mode registers only
/// the configured combinations, and a failed registration leaves the
/// previous state restorable — is testable without Windows.
/// </summary>
public static class DirectHotkeyApplier
{
    /// <summary>
    /// Brings the direct-mode slots to the state described by
    /// <paramref name="desired"/>: when direct mode is disabled every
    /// registered slot is unregistered (the configured combinations are
    /// preserved by the caller in the settings); when it is enabled, only
    /// the slots with a configured combination are registered. A slot that
    /// is already registered with exactly the desired combination is left
    /// untouched: re-registering it could only lose it (for example when
    /// another application took the combination in the meantime).
    /// </summary>
    public static DirectApplyResult Apply(
        AppSettings desired,
        DirectHotkeyState before,
        Func<int, HotkeySetting, bool> register,
        Action<int> unregister)
    {
        ArgumentNullException.ThrowIfNull(desired);
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(register);
        ArgumentNullException.ThrowIfNull(unregister);

        var after = new DirectHotkeyState();
        var failures = new List<string>();

        foreach (var slot in HotkeySlots.Direct)
        {
            var previous = before[slot];
            var wanted = desired.DirectModeEnabled ? desired.DirectSlot(slot) : null;

            if (wanted is null)
            {
                if (previous.Registered)
                {
                    unregister(slot);
                }

                after[slot] = DirectSlotState.Off;
                continue;
            }

            if (previous.Registered && previous.Combination is { } previousCombination
                && previousCombination.Equals(wanted))
            {
                after[slot] = previous;
                continue;
            }

            if (register(slot, wanted))
            {
                after[slot] = new DirectSlotState(true, wanted);
            }
            else
            {
                failures.Add(wanted.DisplayString);
                // The register attempt left the slot as it found it (the
                // previous combination, or unregistered); the caller rolls
                // the whole transaction back from `before`, so the exact
                // residual state of a failed slot is not load-bearing.
                after[slot] = previous.Registered ? previous : DirectSlotState.Off;
            }
        }

        if (failures.Count > 0)
        {
            return new DirectApplyResult(false, string.Join(", ", failures), after);
        }

        return new DirectApplyResult(true, null, after);
    }

    /// <summary>
    /// The release phase of the direct-mode half of a settings
    /// transaction: unregisters, up front, every slot the destination
    /// state does not want (the direct mode disabled, or the slot
    /// unset). The application runs this before the capture side, so the
    /// combination a direct slot is about to give up is free when the
    /// traditional capture hotkey — which may carry the very same
    /// combination, the modes being mutually exclusive — is registered:
    /// <c>RegisterHotKey</c> refuses a combination that any live
    /// registration of the process already holds, whatever its id, so
    /// registering the capture hotkey before the direct slot is released
    /// can only fail against the process's own registration. Unregistering
    /// cannot fail; when the transaction fails, the whole previous state
    /// is still restored with <see cref="Restore"/>.
    /// </summary>
    public static void Release(
        AppSettings desired,
        DirectHotkeyState before,
        Action<int> unregister)
    {
        ArgumentNullException.ThrowIfNull(desired);
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(unregister);

        foreach (var slot in HotkeySlots.Direct)
        {
            var wanted = desired.DirectModeEnabled ? desired.DirectSlot(slot) : null;
            if (wanted is null && before[slot].Registered)
            {
                unregister(slot);
            }
        }
    }

    /// <summary>
    /// Forces the direct-mode slots back to <paramref name="target"/>:
    /// every slot ends exactly in the state the target describes
    /// (registered with the target's combination, or unregistered). This
    /// is the transaction rollback: after a failed application, the caller
    /// restores the state the slots had before the attempt, so a failed
    /// change never leaves a partial registration behind.
    /// </summary>
    public static void Restore(
        DirectHotkeyState target,
        Func<int, HotkeySetting, bool> register,
        Action<int> unregister)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(register);
        ArgumentNullException.ThrowIfNull(unregister);

        foreach (var slot in HotkeySlots.Direct)
        {
            var state = target[slot];
            if (state.Registered && state.Combination is { } combination)
            {
                register(slot, combination);
            }
            else
            {
                unregister(slot);
            }
        }
    }
}
