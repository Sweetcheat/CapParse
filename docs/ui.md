# CapParse — UI/UX Specification

## 1. Design Goal

CapParse should feel like a lightweight Windows utility rather than a traditional desktop application.

The primary interaction should be fast, direct, and unobtrusive.

The user should be able to capture and process something on screen without navigating through a complex application interface.

The intended interaction model is:

**Trigger → Select → Process → Choose → Act**

The interface should prioritize:

- simplicity;
- clarity;
- responsiveness;
- minimal visual noise;
- predictable behavior;
- native Windows usability;
- accessibility;
- keyboard and mouse interaction.

---

## 2. Visual References

CapParse may take inspiration from the interaction patterns of:

### Lightshot

Use as a reference for:

- immediate capture;
- screen selection;
- visual feedback;
- minimal interruption;
- simple interaction.

### MewuAI

Use as a reference for:

- capture workflow;
- OCR result presentation;
- action-oriented workflow;
- desktop utility behavior.

### OCR Buddy

Use as a reference for:

- OCR result presentation;
- faithful text extraction;
- confidence information;
- keeping OCR output understandable.

### Windows 11

Use as a reference for:

- typography;
- spacing;
- controls;
- dialogs;
- system integration;
- overall visual language.

These are references only. CapParse must have its own visual identity.

---

# 3. General Visual Principles

The interface should be:

- clean;
- modern;
- compact;
- unobtrusive;
- easy to understand;
- consistent with Windows 11.

Avoid:

- excessive gradients;
- excessive animations;
- unnecessary panels;
- excessive configuration;
- visual clutter;
- large decorative elements;
- unnecessary icons;
- feature-heavy toolbars.

The application should feel like a focused utility, not a full productivity suite.

---

# 4. System Tray

CapParse should normally run in the Windows system tray rather than keeping a large main window open.

The tray icon is the primary persistent entry point.

## Left Double Click

Double-clicking the left mouse button on the tray icon restores the main window: a hidden window is shown again, a minimized one returns to the normal state, and the window is activated. It does not start a capture.

Capture is started from the global hotkey or the tray menu, which must call the same capture entry point.

Conceptually:

```text
Global Hotkey ──────────> guarded traditional capture entry point
Tray Menu "Capturar" ───> guarded traditional capture entry point
Tray Double Click ──────> RestoreMainWindow()
```

There must not be two separate capture implementations. The shared entry point validates the mode that is effectively in force before starting a capture: while the direct mode is in effect (committed, or pending in the open settings window) it refuses the traditional capture, whatever the trigger.

## Right Click

Right-clicking the tray icon opens the context menu.

Initial menu (pt-BR):

```text
Capturar
────────────
Configurações
────────────
Sair
```

### Capturar

Starts the same capture workflow as the global hotkey. While the **direct mode** is in effect the item is visually disabled and its entry point refuses to start a traditional capture (the direct mode's shortcuts are the only capture triggers then); in the traditional mode the item is enabled and works as before. The item's state follows the mode that is effectively in force — it updates when the settings are saved and when the open settings window's pending mode changes or is discarded, without a restart.

### Configurações

Opens the Settings window (implemented in M13): a small window with the capture hotkey and, since the direct mode (V0.3), the direct-mode toggle, the three direct shortcut slots and the save policy — see section 14. The main window's title row offers the same access through a discreet gear button; both routes call the same entry point and share the one-instance behavior (an open window is activated, never duplicated).

### Sair

Terminates the application cleanly.

### Tooltip

The tray icon's tooltip is the passive, always-available companion of the transient confirmations (the toast disappears, the tooltip stays). Its content has a fixed priority (implemented, V0.3):

- while the direct mode is in force with no direct shortcut configured, it nudges the user to configure the shortcuts ("CapParse — Modo Direto ativo; configure os atalhos") — the nudge is never buried under a stale result line, and it is gone as soon as any slot is configured;
- after a confirmed capture result, it names the latest one ("CapParse — última: {resultado}") — the latest confirmation always replaces the previous line, so a failure can never leave an older success standing;
- with no result yet, it is the plain application name ("CapParse").

The longest real combination stays under the shell's 127-character tooltip limit, so it is never truncated.

---

# 5. Capture Overlay

The capture overlay is the most important UI interaction in V0.1.

When capture starts:

1. The current desktop is frozen.
2. The captured desktop image is displayed as the background.
3. The background is slightly dimmed.
4. The cursor changes to a selection cursor.
5. The user can select a rectangular region.

The user must feel as though the desktop has been temporarily frozen.

The underlying applications must not visually continue changing during selection.

---

# 6. Multi-Monitor Behavior

The capture overlay must work correctly across multiple monitors.

Each monitor may have a different:

- resolution;
- DPI scaling;
- orientation;
- position.

The implementation must support mixed-DPI configurations.

The overlay should cover the entire virtual desktop.

The user must be able to begin a selection on one monitor and complete it on another monitor when the monitor arrangement allows it.

The selected rectangle must be converted into correct physical pixel coordinates before image cropping.

---

# 7. Selection Behavior

When the user presses the left mouse button:

- record the initial selection point;
- begin drawing the selection rectangle.

While dragging:

- update the rectangle continuously;
- visually distinguish the selected area from the rest of the screen;
- keep the surrounding area dimmed;
- show the current selection bounds when useful.

When the mouse button is released:

- finalize the selection;
- crop the corresponding image;
- exit capture mode;
- begin processing.

A zero-area or invalid selection should not produce an OCR operation.

---

# 8. Cancel Behavior

Pressing `Esc` during capture cancels the operation.

The application should:

- remove the overlay;
- discard the temporary capture;
- restore the normal desktop;
- return to the tray state.

Cancellation must not trigger OCR.

Cancellation must not create files.

---

# 9. Processing State

After the selection is completed, CapParse enters a processing state.

The UI must clearly communicate that processing is occurring.

Example:

```text
Processing...

Capturing image
      ↓
Preparing image
      ↓
Recognizing text
```

The exact progress stages may be simplified if the underlying OCR engine cannot provide meaningful progress information.

The application must never appear frozen during OCR.

A loading indicator should be displayed while processing.

**Status (M14): implemented.** The shipped indicator is an indeterminate
progress bar in the main window, visible only while a recognition is in
flight (the `Processing` phase), above the status line.

If processing fails, the application must display a clear error state and provide an option to retry or close.

---

# 10. Result view (main window)

After processing, the main window presents the result of the current run. There is no separate selector dialog: the user sees the outcome and chooses an explicit action.

The shipped layout (two panes):

- **Left pane (current run):** a status line (idle, processing, text found, no text, failed), the recognized text in a read-only panel, and the actions **Copiar texto**, **Copiar imagem** and **Salvar imagem** with inline success feedback.
- **Right pane (session history):** the recognized results of the session, most recent first; selecting an entry shows its full text and allows copying it again. Consulting history never changes the current result or the run state.

Rules:

- each action is an explicit user action; the application never copies to the clipboard on its own;
- **Copiar texto** is enabled only when the last run produced text; an empty (no-text) result is never copied;
- **Copiar imagem** and **Salvar imagem** are enabled whenever a crop exists (including captures without recognized text) and disabled after a cancelled capture;
- a new capture invalidates the previous result and image.

The exact visual layout may be refined (M14) to achieve a compact and polished result, without changing the behavior above.

---

# 11. Generate Text

The OCR result is displayed in the main window's result pane (section 10).

The result pane contains:

- extracted text in a read-only, selectable text area;
- a **Copiar texto** button.

The text is selectable so the user can copy it manually, but it is not editable: the pane shows what the engine recognized, verbatim.

Copying the text places it into the Windows system clipboard.

The application should not automatically overwrite the clipboard merely because OCR completed.

---

# 12. Copy Image (implemented, M11)

When the user clicks **Copiar imagem**:

1. The captured region (the exact crop of the selection) is placed into the Windows clipboard as an in-memory image (DIB) — no temporary file is written.
2. The image is tagged with the effective DPI of the monitor the selection came from, so DPI-aware applications paste it at its on-screen size.
3. The action completes without requiring the user to save a file; the user can immediately paste the image into another application.
4. The inline feedback shows **Imagem copiada!** on success.

If the clipboard is unavailable (for example locked by another application), a friendly error message is shown and the application keeps running.

Examples of paste targets:

- Discord;
- WhatsApp;
- Paint;
- Word;
- browser applications;
- image editors;
- email clients.

---

# 13. Save Image (implemented, M12)

When the user clicks **Salvar imagem**:

1. The standard Windows save dialog opens, pre-filled with a unique suggested
   name (`CapParse-<data>-<hora>.png`) and, when it exists, the user's
   Pictures folder as the default location.
2. The user chooses the destination and the file name (or cancels).
3. The captured region is saved as a PNG file, tagged with the effective DPI
   of the monitor the selection came from (the same tag the clipboard copy
   uses). The file name is normalized to a `.png` extension, since the
   content is always PNG.
4. The inline feedback shows **Imagem salva!** on success.

Cancelling the dialog is a normal operation: no file is written and no
message is shown. If the file cannot be written (invalid path, access
denied, file in use), a friendly error message is shown and the application
keeps running.

The application never saves screenshots implicitly: no file is created
without an explicit user action (a button, or a direct-mode hotkey, see
section 22). In the direct mode with the automatic save policy, the
user-triggered "save image" action writes to the user-configured folder
without a dialog, with a generated unique file name that never overwrites
an existing file.

---

# 14. Settings

**Status (M13): implemented.** The Settings window started as a single setting — the capture hotkey — and, with the direct mode (V0.3, stage 4, section 22), it also configures the direct hotkey slots, the save policy and the automatic save folder.

The window (title "Configurações", opened from the tray menu or from the gear button in the main window's title row — both routes call the same entry point, so one instance is kept at a time: a request while the window is already open activates it, and the window is not recreated nor its state re-read) is **transactional**: every control edits only a pending copy of the settings; nothing is applied or persisted while the window is open. **Salvar** commits the whole pending change as a unit — validated (hotkey conflicts between the capture and the direct slots, the automatic policy's folder), applied (the capture hotkey through the recovery chain, the direct-mode registrations, the theme) and persisted to `%LOCALAPPDATA%\CapParse\settings.json`. **Cancelar** — like closing the window — discards the pending change and keeps the settings and the registrations that were active before. A rejected change (a conflict, an invalid folder, a combination that could not be registered) restores the previous operational state and persists nothing, with a friendly message explaining why.

The panes:

1. **Atalho de captura:** the pending capture hotkey, with a note when the saved combination is unavailable (the active one is the default) or when no global hotkey could be registered at all (capture stays available through the tray menu), and a note when the pending value differs from the saved one. Actions: **Alterar atalho** (recording), **Restaurar padrão**.
2. **Recording** (capture and direct slots): a dashed target area receives the keyboard focus and the user presses the combination. `Esc` cancels the recording (the previous pending value is kept). Invalid combinations (no Ctrl/Alt/Windows, or a key outside letters/digits/F1–F24) are rejected with an inline message and recording continues. A recorded combination is stored as a pending change, like every other edit.
3. **Modo Direto:** the toggle enables/disables the direct mode (section 22). A dynamic status line tells the user the state of the pending configuration: with no slot configured it points to configuring at least one shortcut; with some configured it names the actions that still have no shortcut (a partial configuration is a valid state, never an error); with the three slots configured and registered it reports the configuration complete; with a registration failure it points at the per-slot warnings. The line is hidden while the mode is off. The three independent slots — **Copiar imagem**, **OCR + copiar texto**, **Salvar imagem** — each show their configured combination (or "Não configurado") with **Alterar** / **Limpar**. A slot shows a registration note only when the running application actually has the mode enabled, the slot configured in the saved settings, and the OS without that slot's registration — the note reflects the effective state, never the not-yet-saved pending edit, and it is cleared as soon as the combination registers (a new working combination removes the previous warning). Disabling the mode unregisters the direct hotkeys but preserves the stored combinations. **Política de salvamento** (Manual/Automático) and, with the automatic policy, the **pasta de salvamento automático** (chosen through the standard folder dialog; a missing folder blocks saving the settings).
4. **Tema:** the pending theme preference (Sistema/Claro/Escuro); the selection is applied when the settings are saved.

The traditional capture hotkey follows the mode: in the traditional mode it is registered with the configured combination; in the direct mode it is **not registered** — the saved combination is preserved in the settings (unregistering never deletes the preference) and becomes free to be assigned to one of the direct slots. Switching back to the traditional mode registers the saved combination again and unregisters the direct hotkeys; a launch that is already configured for the direct mode registers only the direct slots.

Applying a change (the "Salvar" button):

- the combinations are validated (at least one of Ctrl/Alt/Windows, plus a letter, digit or F key F1–F24; no two of the four slots may carry the same combination — in the direct mode the saved traditional combination is not part of the check, because the traditional hotkey is not registered then; the direct slots always check against each other; the automatic policy requires an existing folder);
- the capture hotkey follows the mode: direct mode unregisters it, traditional mode registers it through the recovery chain (the previous active combination, then the default, when the requested one is taken by another application);
- the direct slots are registered only for their configured combinations (none when the mode is disabled);
- only when everything is active is the settings file written; any failure restores the previous state and persists nothing (in a direct-mode transaction the previous state has no capture registration, so the recovery chain can never re-register the traditional hotkey while the mode is on), and a settings-file write failure is reported without rolling the (already active) change back.

The key press interpretation (key, system key, modifier state → decision) is isolated in `Configuration/HotkeyInput` and unit-tested; the window only applies the decision to its states.

Future settings (not part of the current UI, documented for direction only):

- spreadsheet preferences (section 24);
- advanced HDR options in their own section (section 24).

---

# 15. Keyboard Interaction

Important keyboard interactions:

| Key | Behavior |
|---|---|
| Configured capture hotkey | Start capture |
| Esc | Cancel current capture/operation |
| Enter | Confirm where appropriate |
| Ctrl+C | Standard text copy behavior |
| Tab | Navigate controls |
| Shift+Tab | Navigate controls backwards |

The application should remain usable with keyboard navigation wherever practical.

---

# 16. Error States

Errors should be understandable to normal users.

Avoid exposing raw exceptions, stack traces, or technical implementation details in the primary UI.

Bad:

```text
System.AccessViolationException at CaptureService.cs:142
```

Better:

```text
Unable to capture this area.

Try selecting the region again.

[ Try Again ] [ Close ]
```

Technical information may be logged for debugging without being shown directly to normal users.

The shipped interface is in Brazilian Portuguese (the examples above are conceptual; the actual messages are in pt-BR, e.g. "Não foi possível capturar a tela.\n\nTente novamente.").

---

# 17. Feedback

Actions should provide immediate feedback.

Examples:

```text
Copied text
```

```text
Copied image
```

```text
Image saved
```

Feedback should be subtle and should not interrupt the workflow unnecessarily.

A small toast or inline status indicator is preferred over modal dialogs for successful operations.

Implemented (V0.3): the application's own transient toast — a small themed card in the bottom-right corner of the primary monitor's work area (next to the tray icon, clear of the region the user just captured), never stealing focus, never in the taskbar, never receiving input, fading in/out and hiding itself after a short interval (~3 s). It is the **single presentation** of a capture operation's result in both modes: a newer result replaces the current one in place (one window, reused — an older message can never survive a newer result); a cancelled operation shows nothing; a capture failure is already communicated by its dialog, which records the result for the tooltip without a second visual. The tray tooltip is the later query (section 4); the legacy tray balloon is kept only for the first-run mode notice (an independent event, not a capture result).

---

# 18. Loading and Responsiveness

The UI must remain responsive during capture and OCR processing.

Long-running operations must not block the UI thread.

The application should use asynchronous operations where appropriate.

The processing state should clearly communicate that work is occurring.

The UI must never appear permanently frozen because an OCR operation is running.

---

# 19. Accessibility

The application should use:

- readable text;
- sufficient contrast;
- keyboard navigation;
- standard Windows controls where appropriate;
- meaningful accessible names for interactive controls.

Do not rely exclusively on color to communicate state.

---

# 20. Future Action System

The result view is intentionally designed to grow.

V0.1:

```text
Generate Text   (implemented)
Copy Image      (implemented)
Save Image      (implemented)
```

Future versions may add:

```text
Copy Text
JSON
CSV
Markdown
XLSX
PDF
Translate
Extract Table
Extract Code
Ask AI
```

The UI should allow these capabilities to be added without redesigning the entire application.

However, future actions must not be implemented in V0.1.

---

# 21. Visual Quality Standard

The final interface should feel like a polished desktop utility.

Prioritize:

1. responsiveness;
2. clarity;
3. simplicity;
4. consistency;
5. native Windows behavior;
6. visual polish.

Do not add visual effects merely for decoration.

Every UI element should have a functional reason to exist.

---

# 22. Post-Selection Shortcuts (direct mode — implemented, V0.3 stage 4)

After the user completes a region selection, the traditional flow runs OCR automatically (`Ctrl + Shift + X` captures a region and executes OCR) and presents the result view. On top of it, the **direct mode** provides configurable global shortcuts that act on the just-selected region immediately, with no result window interaction — the action runs right after the selection and reports the outcome through the application's own transient toast (the single presentation channel; the tray tooltip keeps the last result as a later query):

- a configurable shortcut to **copy the image** to the clipboard;
- a configurable shortcut to **run OCR and copy the text**;
- a configurable shortcut to **save the image** (the save policy of the settings decides: ask for a location on every action, or write to the configured folder with a unique file name).

The combinations are **not predefined**: the slots start unset, and the user records any combination in Settings (section 14) — the ergonomic validation and the conflict check against the capture hotkey are the user's, with the application validating and reporting conflicts. No direct shortcut is registered by default, and an installation that upgrades into the direct mode never activates it automatically: the mode starts disabled, and enabling it registers only the configured combinations.

---

# 23. First-Run Presentation

**Status (M14): implemented; mode choice added in V0.3 (stage 5).** On the first run only, a short window ("Bem-vindo ao CapParse") is shown: what the application does, how to start a capture (the active hotkey and the tray menu) and the choice of how the user wants to use the application:

- **Modo Tradicional** — select a region, see the result and choose to copy or save manually. The recommended option for people starting out; it keeps the known CapParse behavior.
- **Modo Direto** — select a region and the configured action runs right away, with no result window interaction. The shortcuts (copy image, OCR + copy text, save image) are defined by the user in the settings; the presentation never creates a combination on the user's behalf.
- **Agora não** — defer the decision and continue with the traditional mode; the choice can be made later in the settings.

The presentation is skippable: closing the window without choosing (the close button) keeps the traditional mode, so a dismissed presentation never changes a setting, never creates a hotkey and never blocks the normal use of the application. Choosing the direct mode does not close the window on the spot: the choice card is replaced in place by a short confirmation card (the activation is acknowledged in the window itself before it closes), and the choice is recorded immediately — so closing the presentation in that state applies exactly the mode that was chosen.

The mode is applied through the existing settings transaction: choosing the direct mode flips the persisted `DirectModeEnabled` flag (with no direct hotkey configured, it registers nothing), and the user is then pointed at the settings to configure the direct shortcuts. The "first run" state is persisted in the settings file (`FirstRunPresented`); a missing or legacy file reads as first run, so the presentation is shown exactly once per installation and never again — an existing installation that already has settings never sees it repeatedly.

On the first run only, CapParse may show a short presentation:

- what the application does;
- how to start a capture (hotkey and tray);
- the choice between the traditional mode and the direct mode (or deferring it).

It must be:

- short;
- non-blocking and skippable (not mandatory);
- shown only on the first run; it must not appear again;
- dismissible without any configuration step; choosing the direct mode creates no shortcut by itself.

No first-run experience may be created beyond this.

---

# 24. Future Settings (direction only — not implemented)

The Settings window (M13) shipped with a single setting: the capture hotkey. Since the direct mode (V0.3, stage 4) it also configures the post-selection shortcuts (section 22) and the automatic image save directory (the destination folder of the direct "save image" action with the automatic policy).

The remaining direction for later growth:

- spreadsheet preferences (dedicated section, when spreadsheet conversion exists);
- advanced HDR options in a dedicated section (only when the HDR initiative becomes available).

None of the remaining settings exist today. Do not build UI for them before the corresponding feature is implemented.
