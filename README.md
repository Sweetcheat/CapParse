# CapParse

Utilitário desktop para Windows, local-first: capture uma região da tela e transforme o conteúdo visual em texto utilizável. O OCR roda 100% localmente, sem conta e sem nuvem.

> **Estado atual:** o projeto está em desenvolvimento ativo. O pacote publicado é o **beta** `v0.1.0-beta.1` (anterior às funções de copiar/salvar imagem, configurações e refinamentos finais); a candidata `v0.1.0-beta.2`, com essas funções, está em preparação (M15) e **ainda não foi publicada** — veja as [limitações conhecidas](#limitações-conhecidas).

[Português](#português) | [English](#english)

---

<a id="português"></a>
## Português

### O que é o CapParse

O CapParse segue o conceito **Capture → Understand → Act**:

1. **Capture** — pressione `Ctrl + Shift + X` (ou use o menu da bandeja) e arraste para selecionar uma região da tela. A tela é "congelada" no momento da captura, com suporte a múltiplos monitores e DPI diferente por monitor.
2. **Understand** — o texto da região é reconhecido por OCR totalmente local (modelo PP-OCRv6 Small via ONNX Runtime). Nenhuma imagem ou texto sai do seu computador; não há telemetria, conta ou dependência de internet.
3. **Act** — o resultado é exibido na janela principal, onde você pode copiar o texto, copiar a imagem capturada ou salvá-la em arquivo.

### O que já funciona (implementado)

- Captura de tela congelada (freeze-frame) de todo o desktop virtual, com seleção por arraste;
- Suporte a múltiplos monitores, posições negativas e DPI diferente por monitor (PerMonitorV2);
- Cancelamento da seleção com `Esc`;
- OCR local de português e inglês (texto simples); o processamento não bloqueia a interface;
- Janela de resultados com texto selecionável e as ações explícitas **Copiar texto**, **Copiar imagem** e **Salvar imagem** (o aplicativo nunca copia para a área de transferência por conta própria);
- Cópia da imagem capturada para a área de transferência (colada em Paint, editores e outros aplicativos que aceitam imagem);
- Salvamento da imagem capturada em arquivo (diálogo padrão do Windows, PNG);
- Histórico da sessão em memória (resultados de capturas anteriores, na janela principal);
- Tela de configurações (menu da bandeja): alterar o atalho de captura, com persistência entre reinícios;
- Ícone na bandeja do sistema: **Capturar**, **Configurações**, **Sair**; duplo clique restaura a janela principal;
- Atalho global configurável (`Ctrl + Shift + X` por padrão); se o atalho salvo já estiver em uso por outro aplicativo, o CapParse ativa o padrão e avisa;
- Apresentação curta de primeira execução (exibida uma única vez);
- Indicador de processamento e diálogo de erro de captura com "tentar novamente";
- Instância única: um segundo lançamento não abre outra instância;
- Interface em português brasileiro.

### O que ainda não está disponível (planejado)

- Histórico com as imagens capturadas (o histórico atual mantém apenas o texto reconhecido — melhoria registrada no backlog);
- Atalhos pós-seleção para as ações (as combinações ainda não são definitivas);
- Validação final da versão 0.1 (M15 em andamento).

### Requisitos do sistema

- Windows 10 ou 11, 64 bits (x64);
- O pacote beta é **self-contained**: não é necessário instalar o .NET, o Visual Studio ou usar terminal;
- OCR roda na CPU; o primeiro reconhecimento de cada sessão demora mais (carregamento do modelo), os seguintes são mais rápidos.

> A compatibilidade foi verificada no computador de desenvolvimento. O beta está sendo distribuído justamente para validar outros computadores — se algo não funcionar no seu, isso é um **bug a reportar**, não necessariamente incompatibilidade.

### Como executar o beta

1. Baixe o arquivo `.zip` do [release beta](https://github.com/Sweetcheat/CapParse/releases) e extraia em uma pasta (por exemplo, `CapParse`);
2. Se o Windows avisar (SmartScreen), clique em **Mais informações** → **Executar assim mesmo** — o pacote não é assinado digitalmente;
3. Execute `CapParse.exe`;
4. O aplicativo fica minimizado na bandeja do sistema. Pressione `Ctrl + Shift + X` para capturar, arraste para selecionar a região e solte o botão do mouse;
5. O texto reconhecido aparece na janela principal — use **Copiar texto**, **Copiar imagem** ou **Salvar imagem** (as capturas anteriores ficam no histórico da janela);
6. Para sair, use **Sair** no menu da bandeja (fechar a janela apenas a esconde).

A pasta `models/` ao lado do executável contém os modelos de OCR (pasta `v6` + o classificador de orientação `v5`) — **não a remova**; sem ela o reconhecimento não funciona.

### Compilando a partir do código-fonte

Requer o [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (ou mais novo) no Windows:

```powershell
git clone https://github.com/Sweetcheat/CapParse.git
cd CapParse
dotnet build
dotnet test
```

A pasta `models/v6` do repositório contém os modelos de OCR usados pelo aplicativo (o classificador de orientação v5 vem do pacote RapidOcrNet).

Para gerar o pacote Windows x64 (self-contained: um único executável + pasta `models/`), use o script versionado:

```powershell
powershell -ExecutionPolicy Bypass -File build/publish-win-x64.ps1
```

O script executa restore, build, testes (incluindo os smoke tests de OCR com o modelo real), publish e empacotamento, e registra o tamanho e o SHA-256 do ZIP.

### Solução de problemas

- **O atalho não funciona:** outro aplicativo pode estar usando a combinação configurada. O CapParse avisa ao iniciar e ativa o atalho padrão; a captura continua disponível pelo menu da bandeja (**Capturar**). O atalho pode ser alterado em **Configurações**.
- **A primeira captura "trava" por alguns segundos:** é o carregamento do modelo de OCR (apenas no primeiro reconhecimento da sessão).
- **Nada acontece ao capturar:** verifique se o aplicativo está na bandeja (ícone CapParse) e se não há outra instância aberta.
- **SmartScreen/antivírus:** o pacote beta não é assinado; alguns antivírus podem sinalizar executáveis desconhecidos. O código-fonte está disponível para inspeção.
- **O OCR não reconhece o texto:** imagens com muito ruído, resolução muito baixa ou fontes decorativas reduzem a precisão. O modelo atual é otimizado para texto simples em português e inglês.

### Limitações conhecidas

- Windows x64 apenas (o pacote beta não inclui outras plataformas);
- O histórico de resultados existe apenas em memória (é perdido ao sair do aplicativo) e mantém somente o texto reconhecido, não a imagem (melhoria no backlog);
- O pacote `v0.1.0-beta.1` não inclui as funções adicionadas após a tag (copiar/salvar imagem, configurações, primeira execução) — elas entram na candidata `v0.1.0-beta.2`;
- Durante o período de testes, o comportamento em computadores não validados pode revelar bugs — reporte-os.

### Privacidade

O CapParse é local-first: capturas, texto reconhecido e histórico nunca saem do seu computador. Não há telemetria, análise, upload automático ou chamadas de rede.

### Reportando bugs e enviando feedback

Durante o período de testes, abra uma [issue no GitHub](https://github.com/Sweetcheat/CapParse/issues) incluindo:

- versão do Windows e arquitetura (64 bits);
- o que você fez, o que esperava e o que aconteceu;
- se possível, uma captura de tela do comportamento.

### Licença

MIT — veja [LICENSE](LICENSE).

---

<a id="english"></a>
## English

### What is CapParse

CapParse is a local-first Windows desktop utility built around **Capture → Understand → Act**:

1. **Capture** — press `Ctrl + Shift + X` (or use the tray menu) and drag to select a screen region. The desktop is "frozen" at the moment of capture, with multi-monitor support and per-monitor DPI.
2. **Understand** — the region's text is recognized by a fully local OCR engine (PP-OCRv6 Small via ONNX Runtime). No image or text ever leaves your computer; there is no telemetry, no account and no internet dependency.
3. **Act** — the result is shown in the main window, where you can copy the text, copy the captured image, or save it to a file.

### What already works (implemented)

- Freeze-frame capture of the whole virtual desktop with drag selection;
- Multi-monitor support, negative positions and per-monitor DPI (PerMonitorV2);
- Cancel the selection with `Esc`;
- Local OCR for Portuguese and English (plain text); processing never blocks the interface;
- Results window with selectable text and the explicit actions **Copiar texto** (copy text), **Copiar imagem** (copy image) and **Salvar imagem** (save image) — the app never copies to the clipboard on its own;
- Copying the captured image to the clipboard (pastes into Paint, editors and other apps that accept images);
- Saving the captured image to a file (standard Windows dialog, PNG);
- In-memory session history (previous capture results, shown in the main window);
- A settings screen (tray menu): change the capture hotkey, persisted across restarts;
- System tray icon: **Capturar**, **Configurações**, **Sair** (menu labels are in Portuguese); double-click restores the main window;
- Configurable global hotkey (`Ctrl + Shift + X` by default); when the saved shortcut is already in use by another app, CapParse activates the default and tells you;
- A short first-run presentation (shown once);
- A processing indicator and a capture-error dialog with a retry action;
- Single instance: launching a second copy does not start another instance;
- Brazilian Portuguese interface.

### What is not available yet (planned)

- History with the captured images (the current history keeps only the recognized text — a backlog improvement);
- Post-selection shortcuts for the actions (the combinations are not final yet);
- The final 0.1 validation (M15 in progress).

### System requirements

- Windows 10 or 11, 64-bit (x64);
- The beta package is **self-contained**: no .NET, Visual Studio or terminal required;
- OCR runs on the CPU; the first recognition of a session is slower (model loading), later ones are faster.

> Compatibility was verified on the development machine. The beta is being distributed precisely to validate other computers — if something does not work on yours, that is a **bug to report**, not necessarily an incompatibility.

### Running the beta

1. Download the `.zip` from the [beta release](https://github.com/Sweetcheat/CapParse/releases) and extract it to a folder (e.g. `CapParse`);
2. If Windows warns you (SmartScreen), click **More info** → **Run anyway** — the package is not digitally signed;
3. Run `CapParse.exe`;
4. The app lives in the system tray. Press `Ctrl + Shift + X` to capture, drag to select the region and release the mouse button;
5. The recognized text appears in the main window — use **Copiar texto** (copy text), **Copiar imagem** (copy image) or **Salvar imagem** (save image); previous captures are kept in the window's history;
6. To quit, use **Sair** (Exit) in the tray menu (closing the window only hides it).

The `models/` folder next to the executable contains the OCR models (the `v6` folder plus the `v5` orientation classifier) — **do not delete it**; without it, recognition will not work.

### Building from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or newer) on Windows:

```powershell
git clone https://github.com/Sweetcheat/CapParse.git
cd CapParse
dotnet build
dotnet test
```

The repository's `models/v6` folder contains the OCR models used by the application (the v5 orientation classifier ships inside the RapidOcrNet package).

To produce the Windows x64 package (self-contained: a single executable + the `models/` folder), run the versioned script:

```powershell
powershell -ExecutionPolicy Bypass -File build/publish-win-x64.ps1
```

The script performs restore, build, tests (including the OCR smoke tests with the real model), publish and packaging, and records the ZIP's size and SHA-256.

### Troubleshooting

- **The hotkey does nothing:** another application may be using the configured combination. CapParse warns at startup and activates the default shortcut; capture remains available from the tray menu (**Capturar**). The shortcut can be changed in **Configurações** (Settings).
- **The first capture "freezes" for a few seconds:** that is the OCR model loading (only on the first recognition of a session).
- **Nothing happens when capturing:** make sure the app is in the tray (CapParse icon) and that no other instance is running.
- **SmartScreen/antivirus:** the beta package is unsigned; some antivirus products may flag unknown executables. The source code is available for inspection.
- **OCR does not recognize the text:** noisy images, very low resolution or decorative fonts reduce accuracy. The current model is optimized for plain Portuguese and English text.

### Known limitations

- Windows x64 only (the beta package does not include other platforms);
- The results history is in-memory only (lost when the app exits) and keeps only the recognized text, not the image (a backlog improvement);
- The `v0.1.0-beta.1` package does not include the features added after the tag (image copy/save, settings, first-run presentation) — they are in the `v0.1.0-beta.2` candidate;
- During the testing period, behavior on unvalidated computers may reveal bugs — please report them.

### Privacy

CapParse is local-first: captures, recognized text and history never leave your computer. There is no telemetry, analytics, automatic upload or network calls.

### Reporting bugs and sending feedback

During the testing period, open an [issue on GitHub](https://github.com/Sweetcheat/CapParse/issues) including:

- Windows version and architecture (64-bit);
- what you did, what you expected and what happened;
- if possible, a screenshot of the behavior.

### License

MIT — see [LICENSE](LICENSE).
