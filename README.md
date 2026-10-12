# CapParse

Utilitário desktop para Windows, local-first: capture uma região da tela e transforme o conteúdo visual em texto utilizável. O OCR roda 100% localmente, sem conta e sem nuvem.

> **Estado atual:** o projeto está em desenvolvimento ativo. O pacote distribuído é um **beta** (`v0.1.0-beta.1`) para testes em computadores externos — veja as [limitações](#limitações-conhecidas).

[Português](#português) | [English](#english)

---

<a id="português"></a>
## Português

### O que é o CapParse

O CapParse segue o conceito **Capture → Understand → Act**:

1. **Capture** — pressione `Ctrl + Shift + X` (ou use o menu da bandeja) e arraste para selecionar uma região da tela. A tela é "congelada" no momento da captura, com suporte a múltiplos monitores e DPI diferente por monitor.
2. **Understand** — o texto da região é reconhecido por OCR totalmente local (modelo PP-OCRv6 Small via ONNX Runtime). Nenhuma imagem ou texto sai do seu computador; não há telemetria, conta ou dependência de internet.
3. **Act** — o resultado é exibido na janela principal, onde você pode selecionar e copiar o texto. Ações adicionais (copiar a imagem, salvar a imagem) estão no plano de desenvolvimento.

### O que já funciona (implementado)

- Captura de tela congelada (freeze-frame) de todo o desktop virtual, com seleção por arraste;
- Suporte a múltiplos monitores, posições negativas e DPI diferente por monitor (PerMonitorV2);
- Cancelamento da seleção com `Esc`;
- OCR local de português e inglês (texto simples); o processamento não bloqueia a interface;
- Janela de resultados com texto selecionável e botão **Copiar** (o texto nunca é copiado automaticamente);
- Histórico da sessão em memória (resultados de capturas anteriores, na janela principal);
- Ícone na bandeja do sistema: **Capturar**, **Sair**; duplo clique restaura a janela principal;
- Atalho global `Ctrl + Shift + X` para iniciar a captura;
- Instância única: um segundo lançamento não abre outra instância;
- Interface em português brasileiro.

### O que ainda não está disponível (planejado)

- Copiar a imagem capturada para a área de transferência;
- Salvar a imagem capturada em arquivo;
- Tela de configurações (o item da bandeja ainda exibe um aviso);
- Tratamento de erros expandido e validação final da versão 0.1.

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
5. O texto reconhecido aparece na janela principal — use **Copiar** para enviá-lo à área de transferência;
6. Para sair, use **Sair** no menu da bandeja (fechar a janela apenas a esconde).

A pasta `models/` ao lado do executável contém os modelos de OCR — **não a remova**; sem ela o reconhecimento não funciona.

### Compilando a partir do código-fonte

Requer o [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (ou mais novo) no Windows:

```powershell
git clone https://github.com/Sweetcheat/CapParse.git
cd CapParse
dotnet build
dotnet test
```

A pasta `models/v6` do repositório contém os modelos de OCR usados pelo aplicativo.

### Solução de problemas

- **O atalho `Ctrl + Shift + X` não funciona:** outro aplicativo pode estar usando esse atalho. O CapParse avisa ao iniciar; a captura continua disponível pelo menu da bandeja (**Capturar**).
- **A primeira captura "trava" por alguns segundos:** é o carregamento do modelo de OCR (apenas no primeiro reconhecimento da sessão).
- **Nada acontece ao capturar:** verifique se o aplicativo está na bandeja (ícone CapParse) e se não há outra instância aberta.
- **SmartScreen/antivírus:** o pacote beta não é assinado; alguns antivírus podem sinalizar executáveis desconhecidos. O código-fonte está disponível para inspeção.
- **O OCR não reconhece o texto:** imagens com muito ruído, resolução muito baixa ou fontes decorativas reduzem a precisão. O modelo atual é otimizado para texto simples em português e inglês.

### Limitações conhecidas

- Windows x64 apenas (o pacote beta não inclui outras plataformas);
- O histórico de resultados existe apenas em memória e é perdido ao sair do aplicativo;
- Não há ainda copiar/salvar a imagem, nem tela de configurações;
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
3. **Act** — the result is shown in the main window, where you can select and copy the text. Additional actions (copy image, save image) are on the development roadmap.

### What already works (implemented)

- Freeze-frame capture of the whole virtual desktop with drag selection;
- Multi-monitor support, negative positions and per-monitor DPI (PerMonitorV2);
- Cancel the selection with `Esc`;
- Local OCR for Portuguese and English (plain text); processing never blocks the interface;
- Results window with selectable text and a **Copy** button (text is never copied automatically);
- In-memory session history (previous capture results, shown in the main window);
- System tray icon: **Capturar**, **Sair** (menu labels are in Portuguese); double-click restores the main window;
- Global `Ctrl + Shift + X` hotkey to start a capture;
- Single instance: launching a second copy does not start another instance;
- Brazilian Portuguese interface.

### What is not available yet (planned)

- Copying the captured image to the clipboard;
- Saving the captured image to a file;
- A settings screen (the tray item still shows a notice);
- Expanded error handling and the final 0.1 validation.

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
5. The recognized text appears in the main window — use **Copy** to send it to the clipboard;
6. To quit, use **Sair** (Exit) in the tray menu (closing the window only hides it).

The `models/` folder next to the executable contains the OCR models — **do not delete it**; without it, recognition will not work.

### Building from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or newer) on Windows:

```powershell
git clone https://github.com/Sweetcheat/CapParse.git
cd CapParse
dotnet build
dotnet test
```

The repository's `models/v6` folder contains the OCR models used by the application.

### Troubleshooting

- **The `Ctrl + Shift + X` hotkey does nothing:** another application may be using that shortcut. CapParse warns at startup; capture remains available from the tray menu (**Capturar**).
- **The first capture "freezes" for a few seconds:** that is the OCR model loading (only on the first recognition of a session).
- **Nothing happens when capturing:** make sure the app is in the tray (CapParse icon) and that no other instance is running.
- **SmartScreen/antivirus:** the beta package is unsigned; some antivirus products may flag unknown executables. The source code is available for inspection.
- **OCR does not recognize the text:** noisy images, very low resolution or decorative fonts reduce accuracy. The current model is optimized for plain Portuguese and English text.

### Known limitations

- Windows x64 only (the beta package does not include other platforms);
- The results history is in-memory only and is lost when the app exits;
- Image copy/save and a settings screen are not available yet;
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
