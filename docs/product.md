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

**Status (M13): implemented.** The Settings window (tray menu → "Configurações") shows the active capture hotkey and lets the user record a new combination (with validation and cancel), reset it to the default, or close the window. The value is persisted locally in `%LOCALAPPDATA%\CapParse\settings.json` (the file holds only the key combination — no personal data). When the saved shortcut cannot be registered because another application owns it, the application activates the default `Ctrl + Shift + X` and informs the user; the settings file only ever stores a combination the application can actually register.

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

- **Implemented and published** (mainline, in the `v0.1.0-beta.1` line): application shell; tray integration; global hotkey (`Ctrl + Shift + X` by default); multi-monitor freeze-frame capture; selection overlay; region cropping; local OCR engine (RapidOcrNet + PP-OCRv6 Small); OCR integrated into the capture flow with a processing state; result view in the main window; text copy; session history; single-instance guard; Brazilian Portuguese interface. **M11 Copy Image** is implemented and published on `main` (commit `3bed7be`) but postdates the `v0.1.0-beta.1` tag. **M12 Save Image** is implemented and published on `main` (commit `1edd9f1`) and also postdates the tag.
- **Implemented (local on `main`, postdates the tag):** Settings (M13) — the capture hotkey is configurable in a dedicated Settings window (tray menu) and persisted in `%LOCALAPPDATA%\CapParse\settings.json`, with conflict recovery to the default shortcut.
- **Planned (V0.1):** UX Polish (M14); V0.1 validation (M15).
- **Future (not available):** HDR Capture & Tone Mapping; Spreadsheet Conversion; the list in section 9.

Nothing in this section should be read as a feature being available if it is not listed under "Implemented".
