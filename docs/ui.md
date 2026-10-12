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
Global Hotkey ──────────> StartCapture()
Tray Menu "Capturar" ───> StartCapture()
Tray Double Click ──────> RestoreMainWindow()
```

There must not be two separate capture implementations.

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

Starts the same capture workflow as the global hotkey.

### Configurações

Placeholder until M13 (Settings window).

### Sair

Terminates the application cleanly.

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

The application never saves screenshots automatically.

No capture files are created unless the user explicitly chooses to save one.

---

# 14. Settings

The V0.1 Settings window should remain intentionally small.

The primary setting is the capture hotkey.

Conceptual layout:

```text
┌─────────────────────────────────────┐
│ Settings                            │
│                                     │
│ Capture Hotkey                      │
│                                     │
│ [ Ctrl + Shift + X ]  [ Change ]   │
│                                     │
│                                     │
│                          [ Close ]  │
└─────────────────────────────────────┘
```

When changing the hotkey:

- the user should be prompted to press a key combination;
- invalid combinations should be rejected;
- `Esc` should cancel the change;
- the application should detect conflicts where possible.

Additional settings should not be added unless required by a future feature.

Future settings (not part of the current UI, documented for direction only):

- configurable shortcuts for the post-selection image actions (section 22);
- image save directory (section 24);
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

# 22. Post-Selection Shortcuts (proposed — not finalized)

After the user completes a region selection, the intended flow runs OCR automatically (`Ctrl + Shift + X` captures a region and executes OCR). On top of the result view actions (section 10), the product direction includes configurable shortcuts that act on the just-selected region:

- a configurable shortcut to **copy the image** to the clipboard;
- a configurable shortcut to **save the image**.

These shortcuts are part of the intent to minimize clicks and interruptions.

**The proposed key combinations are not final.** They still need ergonomic validation and a conflict check against the capture hotkey, the `Esc` cancel key and common system/application shortcuts. Do not implement or document a specific combination as definitive until that validation is done.

Both shortcuts must be configurable in Settings (section 24) once implemented.

---

# 23. First-Run Presentation

On the first run only, CapParse may show a short presentation:

- what the application does;
- how to start a capture (hotkey and tray);
- what happens after a selection.

It must be:

- short;
- non-blocking and skippable (not mandatory);
- shown only on the first run; it must not appear again;
- dismissible without any configuration step.

No first-run experience may be created beyond this.

---

# 24. Future Settings (direction only — not implemented)

When the Settings window is implemented (M13), its initial content is the capture hotkey. The direction for later growth:

- configurable post-selection shortcuts (section 22);
- image save directory (default location for Save Image);
- spreadsheet preferences (dedicated section, when spreadsheet conversion exists);
- advanced HDR options in a dedicated section (only when the HDR initiative becomes available).

None of these settings exist today. Do not build UI for them before the corresponding feature is implemented.
