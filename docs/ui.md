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

Double-clicking the left mouse button on the tray icon starts the capture workflow immediately.

It must perform the same operation as the global capture hotkey.

Both inputs must call the same capture entry point.

Conceptually:

```text
Global Hotkey ─────┐
                   ├──> StartCapture()
Tray Double Click ─┘
```

There must not be two separate capture implementations.

## Right Click

Right-clicking the tray icon opens the context menu.

Initial menu:

```text
Capture
────────────
Settings
────────────
Exit
```

### Capture

Starts the same capture workflow as the global hotkey.

### Settings

Opens the Settings window.

### Exit

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

# 10. Action Selector

After processing completes, CapParse displays the available actions.

The action selector should be simple and visually prominent.

Initial actions:

```text
Generate Text
Copy Image
Save Image
```

The interface should make clear that the user is choosing what to do with the captured content.

Example conceptual layout:

```text
┌─────────────────────────────────────┐
│                                     │
│       What do you want to do?       │
│                                     │
│   ┌─────────────┐ ┌─────────────┐   │
│   │             │ │             │   │
│   │ Generate    │ │ Copy Image  │   │
│   │ Text        │ │             │   │
│   └─────────────┘ └─────────────┘   │
│                                     │
│           ┌─────────────┐           │
│           │ Save Image  │           │
│           └─────────────┘           │
│                                     │
└─────────────────────────────────────┘
```

The exact visual layout may be adjusted during implementation to achieve a compact and polished result.

---

# 11. Generate Text

When the user selects **Generate Text**, the OCR result is displayed in a text result view.

The result view should contain:

- extracted text;
- a clear text area;
- Copy button;
- Close button.

Conceptually:

```text
┌─────────────────────────────────────┐
│ Extracted Text                      │
│                                     │
│ ┌─────────────────────────────────┐ │
│ │ Recognized text...              │ │
│ │                                 │ │
│ │ More recognized text...         │ │
│ │                                 │ │
│ └─────────────────────────────────┘ │
│                                     │
│ [ Copy ]                    [Close] │
└─────────────────────────────────────┘
```

The text should remain editable/selectable by the user.

Copying the text places it into the Windows system clipboard.

The application should not automatically overwrite the clipboard merely because OCR completed.

---

# 12. Copy Image

When the user selects **Copy Image**:

1. The captured region is placed into the Windows clipboard.
2. The action completes without requiring the user to save a file.
3. The user can immediately paste the image into another application.

Examples:

- Discord;
- WhatsApp;
- Paint;
- Word;
- browser applications;
- image editors;
- email clients.

The user should receive clear visual feedback that the operation succeeded.

---

# 13. Save Image

When the user selects **Save Image**:

1. Open the standard Windows save dialog.
2. Allow the user to choose the destination.
3. Save the captured region as an image.
4. Confirm successful saving.

The application should not save screenshots automatically.

No capture files should be created unless the user explicitly chooses to save one.

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

The action selector is intentionally designed to grow.

V0.1:

```text
Generate Text
Copy Image
Save Image
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