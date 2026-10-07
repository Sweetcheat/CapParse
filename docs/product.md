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

1. A configurable global keyboard shortcut.
2. Double-clicking the left mouse button on the CapParse system tray icon.

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

After processing, the user is presented with a simple action selector.

V0.1 should provide:

- Generate Text;
- Copy Image;
- Save Image.

When the user chooses **Generate Text**, CapParse displays the OCR result in a dedicated result view.

The result view should allow the user to:

- inspect the extracted text;
- copy the text to the system clipboard;
- close the result.

When the user chooses **Copy Image**, the captured region is placed on the system clipboard as an image so it can be pasted into other applications.

When the user chooses **Save Image**, the captured region can be saved as an image file.

---

## 6. V0.1 User Flow

### Capture

```text
User
 ↓
Global Hotkey
     OR
Tray Double Click
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
Action Selector
```

### Text Flow

```text
Action Selector
 ↓
Generate Text
 ↓
OCR Result
 ↓
Text Result View
 ↓
Copy / Close
```

### Image Flow

```text
Action Selector
 ↓
Copy Image
 ↓
Image available on Clipboard
```

or:

```text
Action Selector
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

The initial tray menu should contain:

- Capture;
- Settings;
- Exit.

Double-clicking the left mouse button on the tray icon should immediately start the capture workflow.

The tray should not expose unnecessary functionality in V0.1.

---

## 8. Settings

V0.1 should provide a minimal settings interface.

The first configurable setting is the global capture hotkey.

The default hotkey may initially be:

`Ctrl + Shift + X`

The user must be able to change the shortcut through the Settings interface.

Additional configuration should only be introduced when required by a future feature.

---

## 9. Future Direction

The architecture should allow CapParse to evolve from a simple OCR utility into a general visual information extraction and action tool.

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