# CapParse — Product Definition

## 1. Overview

CapParse is a local-first desktop application for Windows that allows users to capture any region of their screen and turn its visual content into usable information.

The core experience is:

**Capture → Understand → Act**

The application should feel as simple and immediate as a lightweight screenshot utility such as Lightshot, while progressively adding OCR, structured extraction, AI-assisted understanding, and export actions.

CapParse should hide technical complexity from the user. Users should not need to understand OCR engines, APIs, models, terminals, environment variables, or command-line tools to use the application.

---

## 2. Problem

Information displayed on a computer screen is often difficult to reuse.

Users frequently need to:

- copy text from applications that do not allow normal selection;
- extract information from images;
- extract text from screenshots;
- capture tables;
- copy information from PDFs or documents;
- reuse information displayed inside applications, websites, terminals, messaging apps, or games.

Existing solutions often require multiple tools, manual work, cloud uploads, complicated configuration, or command-line interfaces.

CapParse aims to reduce this workflow to a simple interaction:

1. Capture the relevant area.
2. Let CapParse understand its contents.
3. Choose what to do with the result.

---

## 3. Target Users

The initial target audience is Windows desktop users who frequently work with visual information.

Examples include:

- developers;
- students;
- office and administrative workers;
- researchers;
- technical users;
- people working with documents and spreadsheets;
- users who frequently copy information from websites or applications;
- users who want local/private OCR without uploading screenshots.

CapParse should remain approachable to non-technical users while still being useful to technical users.

---

## 4. Core Product Philosophy

CapParse follows these principles:

### Local-first

The core functionality should work locally on the user's computer.

### No account required

The application should not require an account to use its basic functionality.

### No mandatory cloud services

The V0.1 must not depend on a remote backend or cloud service.

### AI is optional

CapParse should be useful without AI.

AI functionality will be added later as an optional capability and should not be required for the core capture and OCR workflow.

### Simple by default

The user should not be exposed to unnecessary technical configuration.

### Minimal clicks and interruptions

Every capability should be reachable with as few clicks and interruptions as possible. The common path (capture a region, get text or an image) should not require navigating through application screens.

### Simple for regular users

The experience must be usable by non-technical users without documentation. Technical depth must stay hidden behind a simple surface.

### Fast interaction

The application should behave like a desktop utility rather than a traditional application that requires navigating through multiple screens.

### Extensible architecture

The initial implementation should provide a clean foundation for future capabilities without implementing unnecessary abstractions or features prematurely.

---

## 5. V0.1 Scope

The first version should focus on the fundamental workflow:

**Capture → OCR → Choose Action**

### Capture

The user can start a capture in two ways:

1. The global keyboard shortcut (`Ctrl + Shift + X` by default; configurable in Settings, M13).
2. Choosing **Capturar** in the system tray context menu.

Double-clicking the tray icon restores the main window; it does not start a capture.

In the **direct mode** (V0.3), captures are triggered by one of three user-configured global shortcuts (copy image, OCR + copy text, save image), and the configured action runs right after the selection — with no result window interaction — reporting the outcome through a discreet tray notice. The modes are mutually exclusive: in the direct mode the traditional shortcut is not registered (its saved combination is preserved in the settings and becomes free to be assigned to a direct slot) and the tray's **Capturar** item is disabled; switching back to the traditional mode registers the saved combination again. The mode is chosen in the first-run presentation and can be changed any time in Settings.

When capture begins:

- the visible desktop is frozen;
- the screen is slightly dimmed;
- the mouse cursor changes to a selection cursor;
- the user can drag a rectangular selection;
- the selected region is visually highlighted;
- pressing `Esc` cancels the operation.

The capture must support multiple monitors and mixed DPI configurations.

### Processing

After the user completes a selection:

1. The selected image is captured in memory.
2. CapParse displays a processing/loading state.
3. The OCR engine processes the image.
4. The application displays the available actions.

The loading state exists both for user feedback and to prevent the application from appearing frozen during processing.

### OCR

V0.1 uses local OCR based on:

- RapidOcrNet;
- PP-OCRv6 Small;
- ONNX Runtime.

The OCR engine should operate locally and should not require network access.

The OCR architecture should expose an application-level `IOcrEngine` interface so that the rest of the application does not depend directly on RapidOcrNet.

The OCR result should retain useful information such as:

- recognized text;
- text blocks;
- confidence values;
- bounding boxes where available;
- processing information where useful.

### Actions

V0.1 provides three actions with the captured content:

- Generate Text;
- Copy Image;
- Save Image.

The shipped interaction (no separate selector dialog): after processing, the main window presents the result — a status line, the recognized text in a read-only panel, and the explicit actions **Copiar texto**, **Copiar imagem** and **Salvar imagem** — plus the session's history of recognized results. Each action is an explicit user action; the application never copies to the clipboard on its own.

**Status:** all three actions are implemented: Generate Text (result view + text copy, M9/M10), Copy Image (M11) and Save Image (M12, the captured region is saved as an image file through the standard Windows save dialog).

**Status (V0.3, direct mode):** the same three actions are also available as immediate post-selection actions, triggered by the configurable direct shortcuts (see Capture above). They reuse the application's existing copy/save services and, for text, the same local OCR pipeline — there is no second OCR engine and no new interaction surface: the outcome is reported through a discreet tray notice. The direct "save image" action follows the save policy configured in Settings (ask for a location on every action, or write to a configured folder with a generated unique file name that never overwrites an existing file).

The result view lets the user:

- inspect the extracted text;
- copy the text to the system clipboard;
- copy the captured image to the clipboard;
- save the captured image to a file chosen by the user;
- consult and re-copy previous results from the session history;
- close the result (the window hides to the tray).

---

## 6. V0.1 User Flow

### Capture

```text
User
 ↓
Global Hotkey
     OR
Tray Menu (Capturar)
 ↓
Freeze Desktop
 ↓
Dim Screen
 ↓
Selection Cursor
 ↓
User Selects Region
```

### Cancel

```text
Capture Mode
 ↓
ESC
 ↓
Cancel
 ↓
Return to Desktop
```

### OCR Flow

```text
Selected Region
 ↓
Capture Image
 ↓
Processing / Loading
 ↓
Local OCR
 ↓
Result view (main window)
```

### Text Flow

```text
Result view
 ↓
Recognized text
 ↓
Copiar texto
 ↓
Text on clipboard
```

### Image Flow

```text
Result view
 ↓
Copiar imagem
 ↓
Image available on Clipboard
```

or (implemented, M12):

```text
Result view
 ↓
Save Image
 ↓
Save dialog
 ↓
Image file
```

---

## 7. System Tray

CapParse runs primarily as a desktop utility and should remain available from the Windows system tray.

The initial tray menu contains:

- Capturar;
- Configurações;
- Sair.

Double-clicking the left mouse button on the tray icon restores the main window (it does not start a capture). Capture is started from the global hotkey or the tray menu.

The tray should not expose unnecessary functionality in V0.1.

---

## 8. Settings

V0.1 should provide a minimal settings interface.

The first configurable setting is the global capture hotkey.

The default hotkey is:

`Ctrl + Shift + X`

The user must be able to change the shortcut through the Settings interface.

Additional configuration should only be introduced when required by a future feature.

**Status (M13): implemented; extended in V0.3 (direct mode).** The Settings window (tray menu → "Configurações", or the gear button on the main window — one instance at a time) is **transactional**: every control edits only a pending copy of the settings; **Salvar** commits the whole pending change as a unit (validated, applied, persisted) and **Cancelar** — like closing the window — discards it, keeping the previous settings and registrations. It configures:

- the capture hotkey (record a new combination with validation and cancel, reset to the default). The value is persisted locally in `%LOCALAPPDATA%\CapParse\settings.json` (the file holds only the key combinations, flags and preferences — no personal data). When the saved shortcut cannot be registered because another application owns it, the application activates the default `Ctrl + Shift + X` and informs the user; the settings file only ever stores a combination the application can actually register;
- the **direct mode** (V0.3): enable/disable, the three independent direct shortcut slots (copy image, OCR + copy text, save image — the combinations are not predefined: the user records any valid combination, or clears a slot), and the save policy of the direct "save image" action (manual dialog on every action, or automatic folder — which requires a valid folder);
- the theme preference (System/Light/Dark), applied when the settings are saved.

---

## 9. Future Direction

The architecture should allow CapParse to evolve from a simple OCR utility into a general visual information extraction and action tool.

Two initiatives are already identified (not available, not implemented — architecture and feasibility still to be investigated):

- **HDR Capture & Tone Mapping** — option to preserve HDR when technically supported; HDR→SDR conversion when preservation is disabled; tone mapping, content peak and SDR paper white as future options. The current capture pipeline (GDI `BitBlt`) does not preserve HDR data; any solution must be validated in an isolated prototype before entering the production pipeline.
- **Spreadsheet Conversion** — extract tables from captures and export them to spreadsheet formats. Output formats, header handling and column structure still need to be defined; preferences would live in a dedicated settings section.

Potential future capabilities include:

- structured data extraction;
- table extraction;
- JSON;
- CSV;
- Markdown;
- XLSX;
- PDF;
- translation;
- code extraction;
- QR/barcode recognition;
- AI vision;
- custom prompts;
- AI-assisted actions;
- OpenAI integration;
- Anthropic integration;
- Google Gemini integration;
- OpenAI-compatible endpoints;
- local AI models;
- history;
- reusable extraction presets.

These features are **not part of V0.1**.

They must not be implemented merely because the architecture could support them.

---

## 10. Platform Direction

CapParse is Windows-first.

The initial implementation should use Windows-native capabilities where they provide the best desktop experience.

However, application architecture should avoid unnecessarily coupling the core OCR, result models, and action concepts to Windows-specific APIs.

Future platforms may include:

- Linux;
- macOS;
- mobile platforms.

Cross-platform support is a future goal and is not part of V0.1.

---

## 11. Design References

CapParse may use existing applications as references for interaction patterns and usability.

Relevant references include:

- Lightshot — simplicity and screenshot selection workflow;
- MewuAI — screenshot/OCR/AI workflow and desktop utility behavior;
- OCR Buddy — local OCR workflow, OCR fidelity, confidence and structured recognition concepts;
- ShareX — mature screenshot workflow and capture behavior;
- Windows 11 — native desktop visual language.

These applications are references for design and engineering decisions only.

CapParse must develop its own visual identity and must not copy proprietary branding, assets, or UI designs.

---

## 12. V0.1 Non-Goals

The following are explicitly outside the V0.1 scope:

- AI providers;
- API keys;
- cloud services;
- user accounts;
- backend infrastructure;
- online synchronization;
- PDF generation;
- Excel generation;
- CSV export;
- JSON export;
- Markdown export;
- translation;
- table-to-spreadsheet conversion;
- video recording;
- scrolling screenshots;
- HDR capture / tone mapping;
- screen annotation;
- image editing;
- screenshot history;
- database storage;
- plugins;
- mobile support;
- Linux support;
- macOS support.

These features may be considered later but should not be implemented as part of V0.1.

---

## 13. Product Principle

CapParse should progressively evolve without losing its fundamental simplicity.

The intended long-term experience is:

```text
Capture
   ↓
Understand
   ↓
Choose
   ↓
Act
```

The application should make technically complex capabilities feel simple to the user.

The user should never need to become a programmer just to extract useful information from something visible on their screen.

---

## 14. Current Implementation Status (summary)

This summary helps orient readers; `docs/development.md` (section 50) is the single source of truth for milestone status, releases and validation evidence.

- **Implemented and committed on `main`** (published line at `e225eb6`, a documentation commit after the `v0.1.0-beta.3` release of 2026-10-15, whose tag points at `8540f8c`; the local `main` has since advanced two commits ahead of the remote, without push): application shell; tray integration; global hotkey (`Ctrl + Shift + X` by default, configurable in Settings); multi-monitor freeze-frame capture; selection overlay; region cropping; local OCR engine (RapidOcrNet + PP-OCRv6 Small); OCR integrated into the capture flow with a processing state; result view in the main window; text copy (M9/M10); **Copy Image (M11, `3bed7be`)**; **Save Image (M12, `1edd9f1`)**; **Settings (M13, `5180b52` + `e3560cc`)** — the capture hotkey is configurable in a dedicated window and persisted in `%LOCALAPPDATA%\CapParse\settings.json`, with conflict recovery to the default shortcut; **UX polish (M14, `a7d2c22` + `c4e76fd`)** — the capture overlay session is built asynchronously, a short first-run presentation, a processing indicator in the result view, and a capture-error dialog with a retry action; session history; single-instance guard; Brazilian Portuguese interface; **V0.2 visual modernization** (light/dark theme system, modernized `MainWindow`/`SettingsWindow`/`FirstRunWindow`, capture-overlay selection highlight, mascot icon — `5a8446e`, `bdfde50`, `1df2a45`, `9256ec7`); OCR space reconstruction and lower OCR memory footprint (P1 + A/B evaluation + `noarena` promotion — `9faccef`, `e307ac8`, `051493d`). The `v0.1.0-beta.1` tag (`86e3d17`) predates M11–M14: that published beta package does not include them.
- **Implemented on `main`, not yet published (V0.3 — direct mode):** the direct mode itself — immediate post-selection actions (copy image, OCR + copy text, save image) through three configurable global shortcuts, with the application's own transient confirmation (toast) as the single presentation channel and the tray tooltip as the later query, and no result window interaction; the transactional Settings window (direct shortcut slots, the manual/automatic save policy and its folder); the mode choice in the first-run presentation (traditional / direct / defer); the corrections that make the two modes strictly isolated (the traditional hotkey and the tray "Capturar" item are inactive and guarded while the direct mode is in force, shared combinations between the modes, registration ordering on mode switches) and the single Win32 text-clipboard channel that replaced the WPF/OLE text writer — the Etapas 1–5, the corrective stages §64–§69, the tray refinement §70 and the clipboard corrections §71.3–§76 of `docs/development.md`. This work is committed on the local `main` (`07fa7ea` + `632c671`, two commits ahead of `origin/main`, not pushed) and is **not included in any published package** (the latest published package is `v0.1.0-beta.3`).
- **Completed (V0.1):** V0.1 validation (M15, closed in `178c940` and published as the `v0.1.0-beta.2` release on 2026-10-13) — full product validation (manual validation reported by the user, recorded in `docs/development.md` §50), documentation consistency, a reproducible publication procedure (`build/publish-win-x64.ps1`) and the published `v0.1.0-beta.2` package. English OCR was validated in M16-2 by automated smoke tests (synthetic images, real model; user manual validation remains optional and was not performed). The OCR error path was validated manually by the user in M16-3 (models folder renamed in the extracted beta.2 package: a friendly message was shown, the app stayed stable, and capture worked normally again after the folder was restored and the app restarted; the exact message text was not recorded). The capture-error dialog remains a documented residual risk (M16-4): inspected in code, with no confirmed reproducible trigger to exercise it.
- **Future (not available):** HDR Capture & Tone Mapping; Spreadsheet Conversion; the list in section 9 (including capture history with image recovery). Post-selection actions are no longer future work: they exist as the direct mode (committed on the local `main`, above).

Nothing in this section should be read as a feature being available if it is not listed under "Implemented".
