# CapParse — Architecture

## 1. Objetivo

Este documento define a arquitetura técnica do CapParse V0.1.

A arquitetura deve ser:

- simples;
- pequena;
- testável;
- adequada para Windows;
- preparada para evolução futura;
- sem abstrações desnecessárias;
- sem dependências ou componentes que não sejam necessários para o escopo atual.

O objetivo não é criar uma arquitetura genérica para todas as plataformas ou todos os recursos futuros.

O objetivo é criar uma base sólida para o V0.1 sem dificultar sua evolução posterior.

---

# 2. Stack

## Aplicação

- C#
- .NET 8
- WPF
- Windows 10 19041+
- Windows 11 como plataforma principal de referência

## Captura

- WPF para interface e overlays
- Win32 interop quando necessário
- GDI `BitBlt` para captura inicial
- PerMonitorV2 para DPI
- múltiplas janelas de overlay, uma por monitor

## OCR

- RapidOcrNet
- PP-OCRv6 Small
- ONNX Runtime
- execução local na CPU no V0.1

## Testes

- xUnit

## Persistência

Não existe persistência de dados no V0.1.

Configurações simples podem ser armazenadas localmente conforme necessário.

---

# 3. Estrutura da solução

A solução inicialmente deve conter somente dois projetos:

```text
CapParse.sln

src/
└── CapParse/

tests/
└── CapParse.Tests/
```

Não criar projetos adicionais sem uma necessidade concreta.

Não criar uma arquitetura com múltiplas bibliotecas, serviços ou assemblies apenas para separar responsabilidades conceitualmente.

Se uma responsabilidade puder permanecer dentro do projeto principal sem prejudicar testes ou manutenção, ela deve permanecer no projeto principal.

---

# 4. Organização interna

Dentro do projeto principal, organizar o código por responsabilidade:

```text
CapParse/
├── Capture/
├── OCR/
├── Actions/
├── UI/
├── Platform/
└── Configuration/
```

Essa organização é lógica e não significa que cada pasta precise possuir dezenas de classes.

Criar somente os arquivos necessários para o milestone atual.

---

# 5. Fluxo principal

O fluxo do V0.1 é:

```text
Global Hotkey  ────┐
                   ├──► StartCapture()
Tray Menu (Capturar) ─┘
      │
      ▼
Freeze Desktop
      │
      ▼
Capture Monitor Images
      │
      ▼
Show Overlay
      │
      ▼
User Selects Region
      │
      ├── ESC ──► Cancel
      │
      ▼
Crop Selected Region
      │
      ▼
Processing / Loading
      │
      ▼
OCR Engine
      │
      ▼
OcrResult
      │
      ▼
Result view (main window)
      │
      ├── Generate Text (recognized text + Copiar texto)
      │       ▼
      │   Text on clipboard
      │
      ├── Copy Image
      │       ▼
      │   Clipboard (in-memory DIB, DPI-tagged)
      │
      └── Save Image
              ▼
          Save Dialog
              ▼
          PNG file (DPI-tagged)
```

O fluxo deve ser explícito.

Evitar eventos encadeados e lógica espalhada entre diversas janelas.

---

# 6. Capture

## 6.1 Responsabilidade

O sistema de captura é responsável por:

- identificar os monitores;
- capturar o desktop;
- lidar com coordenadas do desktop virtual;
- criar os overlays;
- permitir a seleção retangular;
- converter a seleção em coordenadas físicas;
- produzir uma imagem final da região selecionada.

O sistema de captura não deve conhecer detalhes do OCR.

Ele apenas produz uma imagem.

---

# 7. Freeze-frame

O CapParse V0.1 deve utilizar captura congelada.

Não utilizar um overlay transparente que permita visualizar o desktop em tempo real.

O fluxo é:

1. capturar os monitores;
2. armazenar as imagens em memória;
3. exibir as imagens capturadas;
4. aplicar o escurecimento;
5. permitir a seleção sobre a imagem congelada.

Isso garante:

- seleção estável;
- comportamento previsível;
- ausência de mudanças no conteúdo durante a seleção;
- independência do conteúdo atual da tela.

---

# 8. Multi-monitor

O sistema deve suportar:

- múltiplos monitores;
- resoluções diferentes;
- DPI diferente;
- orientação diferente;
- monitores posicionados em qualquer lado;
- coordenadas negativas no desktop virtual.

O desktop virtual deve ser tratado como um espaço de coordenadas único.

Exemplo conceitual:

```text
        Monitor 1
     ┌──────────────┐
     │              │
     │              │
─────┴──────────────┴───────────────
     Monitor 2
     ┌─────────────────────────────┐
     │                             │
     │                             │
     └─────────────────────────────┘
```

Não assumir que o monitor principal está em `(0,0)`.

Não assumir que todos os monitores possuem a mesma escala.

---

# 9. Overlay

O padrão inicial deve ser:

**uma janela WPF de overlay por monitor.**

Cada overlay:

- cobre completamente o monitor;
- exibe a imagem congelada correspondente;
- possui o efeito de dim;
- recebe interação do mouse;
- participa da seleção;
- pode ser fechado sem deixar janelas residuais.

Os overlays devem ser coordenados por um componente central de captura.

A lógica de seleção não deve ser duplicada de forma independente em cada janela.

---

# 10. Seleção

O usuário deve conseguir:

1. pressionar o botão esquerdo;
2. arrastar;
3. visualizar o retângulo de seleção;
4. soltar o botão;
5. confirmar a região.

A seleção deve ser convertida para coordenadas físicas da imagem capturada.

Uma seleção inválida deve ser ignorada.

Exemplos:

- largura zero;
- altura zero;
- região fora da imagem;
- coordenadas inválidas.

Pressionar `ESC` cancela a captura.

---

# 11. Imagem capturada

O resultado da captura deve ser mantido em memória.

O sistema não deve salvar automaticamente a captura no disco.

A imagem capturada deve poder ser utilizada por:

- OCR;
- Copy Image;
- Save Image;
- futuras ações.

O formato interno pode ser escolhido de acordo com o componente utilizado, mas a representação deve permitir conversão confiável para os consumidores.

Não criar um sistema genérico de gerenciamento de imagens no V0.1.

---

# 12. OCR

## 12.1 Abstração

O OCR deve possuir uma abstração mínima:

```csharp
public interface IOcrEngine
{
    Task<OcrResult> RecognizeAsync(
        ImageData image,
        OcrOptions options,
        CancellationToken cancellationToken);
}
```

A implementação inicial será:

```text
RapidOcrEngine
```

Essa abstração existe por um motivo concreto:

**permitir substituir o mecanismo de OCR no futuro sem modificar o fluxo de captura e ações.**

Não criar abstrações adicionais para cada etapa interna do OCR.

---

# 13. OcrResult

O OCR não deve retornar somente uma string.

O resultado deve possuir estrutura suficiente para futuras funcionalidades.

Conceitualmente:

```csharp
public sealed class OcrResult
{
    public string Text { get; init; }

    public IReadOnlyList<OcrBlock> Blocks { get; init; }

    public TimeSpan ProcessingTime { get; init; }
}
```

Um bloco pode conter informações como:

```csharp
public sealed class OcrBlock
{
    public string Text { get; init; }

    public float Confidence { get; init; }

    public Rectangle Bounds { get; init; }
}
```

Os detalhes exatos podem ser adaptados à API do RapidOcrNet.

Não adicionar propriedades que não sejam úteis para o V0.1.

---

# 14. OCR engine

A implementação `RapidOcrEngine` deve:

- carregar o modelo necessário;
- executar OCR localmente;
- aceitar uma imagem;
- retornar `OcrResult`;
- respeitar `CancellationToken` quando tecnicamente possível;
- não possuir lógica de UI;
- não acessar clipboard;
- não abrir janelas;
- não salvar arquivos.

O OCR deve ser independente da interface gráfica.

---

# 15. Inicialização do OCR

Decisão implementada: **lazy loading via `OcrCoordinator`**.

```text
Application Start
      │
      ▼
Application Ready (nenhum modelo carregado)
      │
      ▼
First Recognition
      │
      ▼
OcrCoordinator creates the engine (thread-pool thread)
      │
      ▼
Recognize (engine reused for all later runs)
```

- o engine (`RapidOcrEngine`) é criado uma única vez, na primeira requisição de reconhecimento, em uma thread da pool (nunca na UI thread — o carregamento dos modelos leva centenas de ms);
- inicialização falha deixa o coordenador utilizável: a próxima requisição tenta criar novamente;
- reconhecimentos são serializados (`SemaphoreSlim`), pois a biblioteca não documenta inferência concorrente em uma instância;
- o `App` descarta o coordenador (e o engine, se criado) no encerramento.

Não criar um complexo sistema de gerenciamento de modelos.

---

# 16. OCR e UI

O OCR nunca deve bloquear a UI thread.

Operações potencialmente demoradas devem ser executadas de maneira assíncrona.

Durante o processamento, a interface deve apresentar um estado de loading.

Exemplo:

```text
Capturing...
Preparing image...
Recognizing text...
```

As mensagens podem ser simplificadas caso uma etapa seja muito rápida para justificar apresentação individual.

---

# 17. Actions

As ações representam o que o usuário pode fazer com o resultado da captura.

V0.1:

```text
Generate Text   (implementado: resultado na janela principal + cópia de texto)
Copy Image      (implementado: M11)
Save Image      (implementado: M12)
```

Cada ação deve receber somente os dados necessários para executá-la.

Não criar um framework genérico de plugins de ações no V0.1.

Uma abstração simples pode ser introduzida quando houver mais de uma implementação realmente beneficiada por ela.

---

# 18. Generate Text

O fluxo (implementado): o resultado é apresentado no painel de resultado da janela principal (a janela principal é a view do `OcrRunState` do `App`), e `OcrTextCopier` copia o texto sob demanda.

O painel deve:

- exibir o texto extraído;
- permitir seleção (somente leitura — o texto não é editado);
- permitir copiar o texto (ação explícita);
- nunca copiar automaticamente.

O CapParse não deve substituir automaticamente o clipboard do usuário apenas por executar OCR.

---

# 19. Copy Image

Implementação (M11): `CropImageCopier` converte o bitmap do crop em um `BitmapSource` Bgra32 congelado (pixels copiados verbatim; 32bpp ARGB mapeia 1:1) marcado com o DPI efetivo do monitor da seleção (`MonitorDiscovery.GetEffectiveDpiForSelection`, fallback 96), e `WpfImageClipboard` o coloca no clipboard do Windows como DIB em memória (a chamada deve ocorrer na UI thread/STA).

Regras mantidas:

- não salvar a imagem temporariamente em disco apenas para copiá-la;
- o clipboard recebe sua própria cópia dos pixels; o crop continua pertencendo ao `App` (nunca é descartado pelo copier);
- falhas de clipboard são relatadas como resultado com mensagem amigável, nunca lançadas;
- feedback visual de sucesso na janela principal.

---

# 20. Save Image

Implementação (M12): `CropImageSaver` converte o bitmap do crop em `BitmapSource` Bgra32 congelada reaproveitando `CropImageCopier.ToBitmapSource` (mesmos pixels e tag de DPI da cópia de imagem), e a grava como PNG (`PngBitmapEncoder`, sem arquivo temporário). `WpfCropImageSaveDialog` (seam `ICropImageSaveDialog`) mostra o diálogo padrão de salvar do Windows (`Microsoft.Win32.SaveFileDialog`), com nome sugerido único (`CapParse-<data>-<hora>.png`), a pasta Imagens do usuário como diretório padrão (quando existe) e filtro apenas PNG; o nome escolhido é normalizado para extensão `.png` (o conteúdo é sempre PNG).

Regras mantidas:

- salvamento sempre explícito: a aplicação nunca salva automaticamente;
- cancelamento do diálogo é um resultado normal (nenhum arquivo é gravado e nenhuma mensagem é exibida);
- o crop continua pertencendo ao `App` (nunca é descartado pelo saver);
- falhas de gravação (caminho inválido, acesso negado, arquivo em uso) são relatadas como resultado com mensagem amigável, nunca lançadas;
- feedback visual de sucesso na janela principal ("Imagem salva!").

---

# 21. Clipboard

O acesso ao clipboard fica isolado em pequenas seams testáveis, já implementadas:

```text
UI/
├── ITextClipboard        (WpfTextClipboard)
├── IImageClipboard       (WpfImageClipboard)
├── OcrTextCopier         (copia o texto reconhecido; nunca copia vazio)
└── CropImageCopier       (copia a imagem do crop, com DPI)
```

Essa camada lida com:

- texto;
- imagem.

As seams permitem que os testes substituam o clipboard real por fakes.

Não criar um sistema genérico de integração com todos os formatos possíveis.

---

# 22. Global Hotkey

O hotkey global deve funcionar mesmo quando o CapParse não estiver em primeiro plano.

O hotkey padrão será:

```text
Ctrl + Shift + X
```

O sistema deve permitir alterar o atalho nas configurações.

A implementação pode utilizar Win32 interop.

A lógica de hotkey não deve conhecer a lógica de captura.

Ela apenas deve disparar:

```text
StartCapture()
```

---

# 23. Tray

O CapParse permanece disponível na área de notificação do Windows.

Menu (pt-BR):

```text
Capturar
Configurações
Sair
```

Comportamento (implementado):

### Double Left Click

Restaura a janela principal (`RestoreMainWindow()`): mostra uma janela oculta, restaura uma minimizada e ativa a janela. Nunca inicia captura.

### Right Click

Exibe o menu.

### Capturar

Inicia:

```text
StartCapture()
```

### Configurações

Placeholder até o M13.

### Sair

Encerra a aplicação e libera recursos.

O hotkey e o menu do tray devem compartilhar exatamente o mesmo fluxo de captura.

Não implementar dois sistemas diferentes para iniciar captura.

---

# 24. StartCapture

`StartCapture()` representa a entrada principal do fluxo de captura.

Conceitualmente:

```text
StartCapture()
    │
    ├── Validate state
    ├── Capture monitors
    ├── Create overlays
    ├── Wait for selection
    ├── Crop image
    ├── Close overlays
    └── Start processing
```

Essa operação deve coordenar o processo.

Ela não deve conter toda a implementação detalhada de cada etapa.

---

# 25. Estado da aplicação

O V0.1 impede estados inválidos simples. Implementação atual:

- **Captura:** a flag `_captureInFlight` no `App` bloqueia novos gatilhos enquanto uma tentativa está em andamento (incluindo a construção da sessão, cujo construtor bombeia a UI thread); falhas nunca deixam a aplicação bloqueada.
- **OCR:** `OcrRunState` é o estado explícito das execuções — `Idle`, `Processing`, `Succeeded`, `NoText`, `Failed` — com ids de execução: apenas a execução mais recente pode completar, falhar ou cancelar o estado, então um reconhecimento obsoleto jamais sobrescreve um mais novo nem deixa a interface presa em `Processing`.
- **Histórico:** `OcrHistory` guarda, em memória e por sessão, os resultados reconhecidos (mais recente primeiro, com limite de entradas); apenas execuções que completam validamente gravam entrada.

Não criar uma máquina de estados complexa.

Um enum e controles equivalentes são suficientes.

---

# 26. Errors

Erros devem ser tratados em nível de usuário.

O usuário não deve receber:

```text
System.NullReferenceException
Win32Exception 0x...
```

como única mensagem.

Exemplo:

```text
Could not capture the screen.

Try again.
```

ou:

```text
OCR failed.

Try again or close.
```

Detalhes técnicos podem ser registrados para diagnóstico.

O V0.1 não precisa de um sistema complexo de logging.

---

# 27. UI Thread

A UI deve permanecer responsiva.

Não executar diretamente na UI thread:

- OCR;
- operações pesadas de imagem;
- inicialização pesada;
- processamento prolongado.

A criação e manipulação de controles WPF deve permanecer na UI thread quando necessário.

Não usar `Task.Run()` indiscriminadamente.

Cada operação assíncrona deve ter uma razão concreta.

---

# 28. Cancellation

Operações canceláveis devem aceitar `CancellationToken` quando isso fizer sentido.

O principal caso do V0.1 é permitir que operações futuras de processamento sejam canceladas de forma limpa.

Não criar um sistema global de cancelamento.

O cancelamento da seleção pelo usuário através de `ESC` deve ser tratado diretamente pelo fluxo de captura.

---

# 29. Configuration

As configurações do V0.1 devem ser mínimas.

Inicialmente:

```text
Capture Hotkey
```

A configuração deve:

- carregar ao iniciar;
- utilizar valores padrão quando não existir configuração;
- salvar alterações;
- validar o hotkey.

Não criar dezenas de opções de configuração.

---

# 30. DPI

O aplicativo deve utilizar comportamento compatível com:

```text
PerMonitorV2
```

A captura deve trabalhar corretamente com DPI diferente entre monitores.

Não assumir:

```text
96 DPI
100%
```

como condição universal.

O sistema deve distinguir corretamente:

- coordenadas lógicas WPF;
- coordenadas físicas de pixels;
- coordenadas do desktop virtual.

Essa conversão é uma parte crítica da implementação de captura.

---

# 31. Win32 interop

Win32 interop é permitido quando necessário para:

- captura;
- hotkeys;
- DPI;
- informações de monitores;
- clipboard;
- integração com tray;
- outras operações específicas do Windows.

Interop deve ser isolado sempre que possível.

Não espalhar chamadas Win32 diretamente por toda a aplicação.

---

# 32. Testes

O projeto de testes deve validar principalmente lógica que possa ser testada sem uma sessão gráfica real.

Exemplos:

- validação de seleção;
- conversão de coordenadas;
- configuração de hotkey;
- transformação de resultados OCR;
- comportamento de ações que possa ser isolado;
- regras de estado.

Testes de integração com:

- múltiplos monitores;
- DPI real;
- clipboard real;
- WPF;
- Win32;

podem ser adicionados posteriormente.

Não criar uma infraestrutura complexa de testes de UI no V0.1.

---

# 33. Dependências

Cada dependência deve possuir uma justificativa concreta.

V0.1 deve evitar:

- frameworks de DI;
- MediatR;
- AutoMapper;
- Prism;
- ReactiveUI;
- bibliotecas de MVVM desnecessárias;
- containers de serviços;
- sistemas de plugin;
- bancos de dados;
- frameworks de logging pesados;
- bibliotecas de captura adicionais sem necessidade.

Se o .NET ou o WPF resolver o problema adequadamente, utilizar a solução nativa.

---

# 34. MVVM

O projeto pode utilizar uma separação simples entre:

- lógica;
- estado;
- apresentação.

Não é necessário introduzir um framework MVVM.

Se um ViewModel simples melhorar claramente a testabilidade ou organização de uma tela, ele pode ser criado.

Não criar ViewModels para cada pequena janela ou controle apenas por seguir uma convenção arquitetural.

---

# 35. Dependency Injection

Não utilizar um container de Dependency Injection no V0.1.

As dependências podem ser construídas explicitamente.

Exemplo:

```csharp
var ocrEngine = new RapidOcrEngine(...);
var captureService = new CaptureService(...);
```

Interfaces devem existir somente quando houver uma razão concreta, principalmente:

- substituição de implementação;
- testes;
- isolamento de plataforma.

---

# 36. Logging

O V0.1 deve possuir diagnóstico suficiente para descobrir falhas sem poluir a experiência do usuário.

Utilizar uma solução simples.

Não criar:

- sistema distribuído de logs;
- telemetria;
- analytics;
- envio automático de erros;
- serviços externos.

CapParse é local-first.

---

# 37. Privacidade

A imagem capturada deve permanecer local.

O V0.1:

- não envia screenshots para servidores;
- não envia OCR para servidores;
- não exige conta;
- não exige API key;
- não depende de conexão com a internet para OCR.

A ausência de rede deve ser uma condição normal de funcionamento.

---

# 38. Memória

O sistema trabalha com screenshots potencialmente grandes.

Evitar duplicações desnecessárias das imagens em memória.

Sempre que possível:

```text
Screen Capture
      │
      ▼
Image
      │
      ├── Overlay
      │
      └── Selected Crop
```

Após o término da captura, recursos que não são mais necessários devem ser liberados.

Não implementar cache complexo no V0.1.

---

# 39. Arquitetura conceitual

A arquitetura pode ser entendida como:

```text
┌──────────────────────────────────────────────┐
│                     UI                       │
│ Tray / Overlay / Result / Settings          │
└──────────────────────┬───────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────┐
│                  Workflow                    │
│ CaptureCoordinator / Application State       │
└───────────────┬───────────────────┬──────────┘
                │                   │
                ▼                   ▼
┌──────────────────────┐   ┌──────────────────┐
│       Capture        │   │       OCR        │
│ Monitor / Selection  │   │ IOcrEngine       │
└──────────────────────┘   └──────────────────┘
                │                   │
                └─────────┬─────────┘
                          ▼
                 ┌──────────────────┐
                 │     Actions      │
                 │ Copy / Save /    │
                 │ Generate Text    │
                 └──────────────────┘

                 Platform
             ┌───────────────────┐
             │ Win32 / Clipboard │
             │ Hotkey / DPI      │
             │ Monitor           │
             └───────────────────┘
```

A arquitetura não deve crescer além disso sem necessidade.

---

# 40. O que NÃO implementar no V0.1

Explicitamente fora da arquitetura atual:

- IA generativa;
- visão multimodal;
- OpenAI;
- Ollama;
- servidores locais;
- API keys;
- tradução;
- tabelas;
- Excel;
- CSV;
- JSON;
- Markdown;
- PDF;
- código;
- fórmulas;
- QR code;
- barcode;
- gravação de vídeo;
- edição de imagem;
- anotações;
- histórico;
- sincronização;
- banco de dados;
- login;
- cloud;
- plugins;
- sistema de extensões;
- Linux;
- macOS;
- Android;
- iOS.

Esses recursos podem existir posteriormente.

A arquitetura deve evitar bloqueá-los, mas não deve implementá-los antecipadamente.

---

# 41. Regra de evolução

Antes de adicionar uma nova abstração, perguntar:

1. Existe mais de uma implementação?
2. Existe uma necessidade real de teste?
3. Existe uma fronteira de plataforma?
4. A abstração reduz complexidade ou apenas a move?
5. Ela é necessária para o milestone atual?

Se a resposta for não, não criar a abstração.

---

# 42. Regra para o agente de desenvolvimento

O agente de desenvolvimento deve:

- seguir este documento;
- seguir `product.md`;
- seguir `ui.md`;
- não inventar requisitos;
- não adicionar funcionalidades fora do milestone;
- não adicionar dependências sem justificativa;
- não criar abstrações prematuras;
- explicar mudanças arquiteturais relevantes antes de implementá-las;
- manter o projeto compilável;
- adicionar testes quando houver lógica testável;
- executar os testes antes de concluir um milestone;
- não modificar documentos de produto para justificar uma implementação que fugiu do escopo.

Quando existir conflito entre uma implementação desejada e este documento, parar e solicitar decisão antes de expandir o escopo.

---

# 43. Critério arquitetural principal

A arquitetura do CapParse V0.1 deve ser julgada por uma pergunta simples:

> "Isso torna o próximo passo mais fácil sem tornar o código atual desnecessariamente complexo?"

Se não, não deve ser adicionado.

O objetivo do V0.1 é estabelecer uma fundação pequena, funcional e confiável.

Não construir o CapParse inteiro antecipadamente.

---

# 44. Estado da implementação (verificado)

Seção de continuidade: o que realmente existe no código versus o que este documento apenas descreve como alvo. Atualizada na consolidação documental pós-M11 (2026-10; detalhes, evidências e histórico em `development.md`, seção 50).

## 44.1 Componentes existentes

```text
src/CapParse/
├── App.xaml.cs            tray (WinForms NotifyIcon), hotkey, StartCapture,
│                          OcrCoordinator/OcrRunState/OcrHistory, SingleInstanceGuard,
│                          Restauração da janela principal, mensagens pt-BR
├── MainWindow.xaml(.cs)   janela principal = view do resultado: status, texto
│                          reconhecido, Copiar texto / Copiar imagem / Salvar imagem,
│                          histórico
├── Capture/
│   ├── ScreenCapture.cs       GDI BitBlt do desktop virtual
│   ├── DesktopCapture.cs      captura completa do desktop virtual
│   ├── CaptureResult.cs       imagem capturada em memória (disponível p/ crop)
│   ├── CaptureCropper.cs      crop da seleção em coordenadas físicas
│   ├── SelectionGeometry.cs   validação/conversão da seleção
│   └── CaptureCoordinator.cs  StartCapture() — entrada única do fluxo
├── UI/
│   ├── FrozenOverlaySession.cs  sessão de overlay congelado (1 janela/monitor)
│   ├── MonitorOverlayWindow.cs  janela de overlay por monitor
│   ├── MonitorGeometry.cs       geometria dos monitores em coords. físicas
│   ├── ITextClipboard.cs        seam de clipboard de texto (WpfTextClipboard)
│   ├── OcrTextCopier.cs         copia o texto reconhecido (nunca vazio)
│   ├── IImageClipboard.cs       seam de clipboard de imagem (WpfImageClipboard)
│   ├── CropImageCopier.cs       crop → BitmapSource Bgra32 congelado c/ DPI → clipboard
│   ├── ICropImageSaveDialog.cs  seam do diálogo de salvar (WpfCropImageSaveDialog)
│   └── CropImageSaver.cs        crop → PNG c/ DPI no arquivo escolhido (diálogo padrão)
├── Platform/
│   ├── GlobalHotkey.cs        RegisterHotkey (Ctrl+Shift+X por padrão)
│   ├── MonitorDiscovery.cs    EnumDisplayMonitors + DPI + GetEffectiveDpiForSelection
│   └── SingleInstanceGuard.cs mutex de instância única (CapParse.SingleInstance.v1)
└── OCR/
    ├── IOcrEngine.cs          contrato Image → OcrResult
    ├── ImageData.cs           wrapper do Bitmap do consumidor (não descarta)
    ├── OcrOptions.cs          sem campos no V0.1
    ├── OcrResult.cs / OcrBlock.cs
    ├── RapidOcrEngine.cs      RapidOcrNet 4.2.0 + PP-OCRv6 Small, local
    ├── OcrCoordinator.cs      criação lazy do engine (thread pool), serialização,
    │                          dispose no encerramento
    ├── OcrRunState.cs         estado explícito das execuções (Idle/Processing/
    │                          Succeeded/NoText/Failed) com ids de execução
    └── OcrHistory.cs          histórico em memória da sessão (imutável, limitado)
```

Modelos: `models/v6/` no repositório (3 arquivos, SHA256 verificados) + classificador v5 do pacote RapidOcrNet; `build/OcrModels.targets` copia para a saída de build/publish.

## 44.2 Documentado mas AINDA NÃO existente

- janela de Settings e atalhos configuráveis (M13; o menu do tray mostra um placeholder);
- apresentação curta de primeira execução (M14, `ui.md` §23);
- atalhos pós-seleção para copiar/salvar imagem (M14, `ui.md` §22 — combinações não definitivas);
- persistência de configurações;
- refinamento visual geral (M14).

Esses componentes pertencem aos milestones M13–M15 (ver `development.md`). Não tratá-los como existentes.

---

# 45. Iniciativas futuras a investigar

Possibilidades futuras cuja arquitetura e viabilidade ainda precisam ser investigadas. Nenhuma delas possui design definido, dependência escolhida ou código implementado.

## 45.1 HDR Capture & Tone Mapping

- opção futura para preservar HDR quando tecnicamente suportado;
- conversão HDR → SDR quando a preservação estiver desativada;
- investigação do código-fonte do ShareX-HDR como referência;
- análise da API de captura (a captura atual usa GDI `BitBlt`, que não preserva dados HDR — não presumir recuperação de dados perdidos) e dos formatos de pixel disponíveis;
- opções futuras de tone mapping, content peak e SDR paper white;
- avaliação de processamento por CPU, GPU ou modo automático;
- critérios de qualidade visual, compatibilidade e desempenho.

A solução só entrará no pipeline de produção após validação em protótipo isolado.

## 45.2 Spreadsheet Conversion

- extração de tabelas de capturas;
- conversão e exportação para formatos de planilha;
- preferências futuras em uma seção específica de configurações;
- formatos de saída, tratamento de cabeçalhos e estrutura de colunas ainda precisam ser definidos.

Nenhuma dependência ou código deve ser adicionado antes de um milestone dedicado.
