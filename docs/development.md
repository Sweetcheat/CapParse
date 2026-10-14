# CapParse — Development Guide

## 1. Objetivo

Este documento define como o CapParse deve ser desenvolvido.

Ele complementa:

- `product.md` — o que o produto é;
- `ui.md` — como o produto deve se comportar e parecer;
- `architecture.md` — como o sistema deve ser estruturado.

Este documento define:

- como dividir o desenvolvimento;
- como o agente de desenvolvimento deve trabalhar;
- como controlar o escopo;
- como validar cada etapa;
- quando fazer commits;
- como lidar com decisões arquiteturais;
- como evitar complexidade desnecessária.

Para o estado atual do projeto (milestones, releases, evidências e próximo passo), leia a seção 50 — Estado atual do projeto (continuidade entre sessões).

---

# 2. Princípio central

O CapParse deve ser desenvolvido de forma incremental.

Não tentar implementar toda a aplicação de uma vez.

Cada milestone deve:

1. possuir um objetivo pequeno;
2. possuir escopo explícito;
3. possuir critérios de aceite;
4. produzir uma alteração verificável;
5. compilar;
6. passar pelos testes aplicáveis;
7. terminar em um estado funcional;
8. ser registrado em Git.

Um milestone concluído é um checkpoint.

---

# 3. Agente de desenvolvimento

O agente principal de desenvolvimento será utilizado para implementar o projeto (ex.: OpenCode, Pi).

O agente deve atuar como um desenvolvedor supervisionado.

Ele pode:

- analisar o código;
- propor implementação;
- escrever código;
- criar testes;
- executar build;
- executar testes;
- corrigir problemas relacionados ao milestone;
- preparar commits.

Ele não deve assumir decisões de produto que não foram definidas.

---

# 4. Regra de escopo

A regra mais importante:

> Implementar somente o que pertence ao milestone atual.

Se o agente encontrar algo interessante fora do escopo, ele deve:

1. registrar a observação;
2. não implementar;
3. continuar o milestone atual.

Exemplo:

Durante a implementação do OCR, o agente percebe que seria interessante adicionar tradução automática.

A resposta correta é:

```text
Não implementar.

Registrar como possível melhoria futura.
Continuar o milestone atual.
```

---

# 5. Não inventar requisitos

O agente não deve inventar:

- funcionalidades;
- telas;
- configurações;
- comportamentos;
- APIs;
- integrações;
- dependências;
- abstrações;
- sistemas de persistência.

Se um comportamento não estiver definido e for necessário para continuar, o agente deve parar e solicitar uma decisão.

Quando houver uma decisão razoavelmente óbvia e de baixo impacto, o agente pode escolher a opção mais simples, desde que documente a decisão.

---

# 6. YAGNI

Aplicar YAGNI:

**You Aren't Gonna Need It.**

Não implementar algo apenas porque:

- pode ser útil no futuro;
- talvez seja necessário;
- seria mais "profissional";
- outras aplicações fazem isso;
- facilita uma hipotética expansão;
- parece arquiteturalmente elegante.

Implementar quando existir uma necessidade real.

---

# 7. KISS

Preferir:

```text
solução simples que funciona
```

a:

```text
solução genérica que talvez funcione para muitos casos futuros
```

Código pequeno e claro é preferível a uma abstração sofisticada.

---

# 8. Dependências

Antes de adicionar uma dependência externa, verificar:

1. O .NET já resolve isso?
2. O WPF já resolve isso?
3. O Windows fornece uma API nativa?
4. Uma implementação pequena seria suficiente?
5. A dependência é realmente necessária para o milestone?

Se a resposta for não, não adicionar.

Toda nova dependência deve ter justificativa técnica.

---

# 9. Abstrações

Interfaces e abstrações devem possuir uma razão concreta.

Uma interface é justificável quando:

- existem múltiplas implementações;
- uma implementação precisa ser substituível;
- existe uma fronteira de plataforma;
- melhora significativamente a testabilidade.

Não criar interfaces para todas as classes.

Evitar:

```text
IFoo
Foo
IFooFactory
FooFactory
IFooProvider
FooProvider
```

quando uma classe simples resolver o problema.

---

# 10. Antes de codificar

Antes de modificar o código, o agente deve:

1. ler os documentos relevantes;
2. inspecionar o estado atual do projeto;
3. entender a implementação existente;
4. identificar arquivos afetados;
5. verificar se existe código semelhante;
6. propor a abordagem mais simples.

Para mudanças pequenas, não é necessário produzir uma especificação longa.

Para mudanças arquiteturalmente relevantes, explicar:

- o que será alterado;
- por quê;
- quais arquivos serão afetados;
- quais consequências existem.

---

# 11. Alterações arquiteturais

Uma alteração arquitetural relevante deve ser comunicada antes da implementação.

Exemplos:

- adicionar um novo projeto;
- introduzir uma nova camada;
- substituir uma biblioteca;
- adicionar um framework;
- mudar o modelo de OCR;
- alterar a estratégia de captura;
- adicionar um container de DI;
- criar um sistema de plugins;
- mudar o fluxo principal da aplicação.

Não realizar esse tipo de mudança silenciosamente.

---

# 12. Milestones

O desenvolvimento deve ser dividido em milestones pequenos.

A ordem inicial recomendada é:

```text
M0 — Repository Foundation
M1 — WPF Application Shell
M2 — Tray Integration
M3 — Global Hotkey
M4 — Monitor Discovery and Capture
M5 — Freeze-Frame Selection Overlay
M6 — Image Crop Pipeline
M7 — OCR Engine
M8 — Processing / Loading UX
M9 — Action Selector
M10 — Generate Text
M11 — Copy Image
M12 — Save Image
M13 — Settings
M14 — UX Polish
M15 — V0.1 Validation
```

A ordem pode mudar se uma dependência técnica justificar isso.

O escopo de um milestone não deve crescer durante sua implementação sem decisão explícita.

---

# 13. M0 — Repository Foundation

## Objetivo

Criar a estrutura mínima do projeto.

## Deve conter

```text
CapParse/
├── src/
├── tests/
├── docs/
├── assets/
├── .github/
├── .gitignore
├── LICENSE
├── README.md
└── CapParse.sln
```

Projetos:

```text
src/CapParse
tests/CapParse.Tests
```

## Critérios de aceite

- solução abre corretamente;
- aplicação compila;
- projeto de testes compila;
- testes podem ser executados;
- Git está configurado;
- documentos principais estão presentes.

---

# 14. M1 — WPF Application Shell

## Objetivo

Criar a aplicação WPF mínima.

## Deve funcionar

- aplicação inicia;
- aplicação encerra corretamente;
- janela principal ou shell existe;
- configuração básica de aplicação está correta;
- aplicação não apresenta erros durante inicialização.

A interface final ainda não precisa estar implementada.

## Não implementar

- captura;
- OCR;
- tray;
- hotkey;
- settings completos.

---

# 15. M2 — Tray Integration

## Objetivo

Manter o CapParse disponível na área de notificação.

## Deve funcionar

- ícone no tray;
- clique direito;
- menu:
  - Capture
  - Settings
  - Exit;
- Exit encerra corretamente;
- double-click esquerdo pode disparar um placeholder para `StartCapture()`.

## Critério principal

O CapParse deve poder permanecer em execução sem manter uma janela principal aberta na frente do usuário.

---

# 16. M3 — Global Hotkey

## Objetivo

Adicionar hotkey global.

Padrão:

```text
Ctrl + Shift + X
```

## Deve funcionar

- hotkey funciona fora da aplicação;
- hotkey chama `StartCapture()`;
- hotkey pode ser posteriormente substituído por configuração;
- conflito ou registro inválido deve ser tratado.

Nesta etapa, `StartCapture()` pode executar somente um comportamento temporário.

Não implementar captura real ainda se isso não fizer parte do milestone.

---

# 17. M4 — Monitor Discovery and Capture

## Objetivo

Implementar captura do desktop.

## Deve funcionar

- detectar todos os monitores;
- obter posição;
- obter tamanho;
- considerar DPI;
- capturar cada monitor;
- trabalhar com desktop virtual;
- suportar coordenadas negativas.

## Critério

As imagens capturadas devem corresponder visualmente ao conteúdo real dos monitores.

---

# 18. M5 — Freeze-Frame Selection Overlay

## Objetivo

Criar a experiência principal de seleção.

Fluxo:

```text
StartCapture()
      ↓
Capture
      ↓
Freeze
      ↓
Overlay
      ↓
Select
      ↓
Confirm / ESC
```

## Deve funcionar

- imagem congelada;
- dim;
- cursor de seleção;
- retângulo de seleção;
- múltiplos monitores;
- DPI diferente;
- ESC cancela;
- seleção inválida é rejeitada;
- overlays são fechados corretamente.

---

# 19. M6 — Image Crop Pipeline

## Objetivo

Transformar a seleção em uma imagem final.

## Deve funcionar

```text
Selected Rectangle
        ↓
Physical Coordinates
        ↓
Crop
        ↓
ImageData
```

A imagem deve ser mantida em memória.

Não salvar automaticamente.

## Testes

Testar especialmente:

- coordenadas;
- dimensões;
- seleção pequena;
- seleção grande;
- coordenadas negativas;
- limites da imagem.

---

# 20. M7 — OCR Engine

## Objetivo

Integrar:

- RapidOcrNet;
- PP-OCRv6 Small;
- ONNX Runtime.

Implementar:

```text
IOcrEngine
RapidOcrEngine
OcrResult
OcrBlock
OcrOptions
```

## Deve funcionar

Uma imagem capturada pode ser enviada ao OCR e produzir um `OcrResult`.

## Critério principal

O OCR deve funcionar localmente sem internet.

---

# 21. M8 — Processing / Loading UX

## Objetivo

Integrar OCR ao fluxo visual.

Após a seleção:

```text
Capture
   ↓
Preparing
   ↓
Recognizing
   ↓
Result
```

A UI não deve congelar durante o processamento.

Erros devem ser apresentados ao usuário.

---

# 22. M9 — Action Selector (entregue como a view de resultado na janela principal — ver §50.4)

## Objetivo

Após o OCR, apresentar:

```text
Generate Text
Copy Image
Save Image
```

O usuário deve escolher explicitamente o que deseja fazer.

Não executar automaticamente uma ação que o usuário não solicitou.

---

# 23. M10 — Generate Text

## Objetivo

Exibir o resultado do OCR.

A janela deve:

- mostrar texto;
- permitir seleção;
- permitir edição;
- possuir Copy;
- possuir Close.

Não copiar automaticamente o texto.

---

# 24. M11 — Copy Image

**Status: concluído e publicado** (commit `3bed7be`, em `main` no GitHub; ainda não incluído na release `v0.1.0-beta.1`).

## Objetivo

Copiar a região capturada para o clipboard.

## Critérios

- imagem disponível imediatamente para paste;
- nenhuma gravação temporária desnecessária;
- feedback de sucesso;
- erro tratado de maneira amigável.

Implementação: `CropImageCopier` + `WpfImageClipboard` (DIB em memória, tag de DPI efetivo do monitor da seleção), botão "Copiar imagem" na janela principal, 8 testes automatizados com fake de clipboard.

---

# 25. M12 — Save Image

**Status: concluído e publicado** (commit `1edd9f1` em `main` no GitHub; validação manual do diálogo registrada em `6401b4b`; ainda não incluído na release `v0.1.0-beta.1`).

## Objetivo

Salvar a captura usando o diálogo padrão do Windows.

## Critérios

- usuário escolhe caminho;
- usuário escolhe nome;
- imagem é salva corretamente;
- cancelamento não é tratado como erro;
- erro de gravação é apresentado de maneira amigável.

Implementação: `CropImageSaver` + `WpfCropImageSaveDialog` (diálogo padrão `Microsoft.Win32.SaveFileDialog`, PNG com tag de DPI, nome sugerido único, pasta Imagens como diretório padrão), botão "Salvar imagem" na janela principal, 19 testes automatizados com fake do diálogo.

---

# 26. M13 — Settings

**Status: concluído, com correção** (commits `feat: add application settings` e `fix: repair settings hotkey capture`, locais em `main` sobre `6401b4b`; 326/326 testes em Debug e Release; fluxo de gravação do atalho validado em runtime no Windows via harness que dirige a `SettingsWindow` real; restam como validação manual do usuário o clique físico no menu do tray e teclas físicas com modificadores).

## Objetivo

Criar a tela mínima de configurações.

Inicialmente:

```text
Capture Hotkey
```

## Deve funcionar

- visualizar hotkey atual;
- alterar;
- cancelar alteração;
- salvar;
- validar;
- restaurar padrão se necessário.

Não adicionar outras configurações sem necessidade.

## Implementação (M13)

- `SettingsWindow` (janela própria, pt-BR, aberta pelo menu do tray "Configurações"): mostra o atalho ativo, grava uma nova combinação (tecla pressionada; `Esc` cancela; combinações inválidas são rejeitadas com mensagem), confirma com Salvar/Cancelar, e oferece "Restaurar padrão".
- Validação: letra, número ou F1–F24 + pelo menos um de Ctrl/Alt/Windows.
- A interpretação da entrada de teclado (tecla/tecla de sistema/modificadores → decisão) está isolada em `Configuration/HotkeyInput`, testável sem janela; a janela só aplica a decisão aos estados da interface.
- Persistência: `%LOCALAPPDATA%\CapParse\settings.json` (JSON mínimo, escrita atômica). Arquivo ausente/inválido → padrões; leitura nunca lança e nunca modifica o arquivo; falha de gravação não encerra a aplicação.
- Integração: o atalho é registrado no startup a partir da configuração; ao alterar, o app tenta registrar a nova combinação e, em conflito, recupera na ordem anterior ativo → padrão (`HotkeyRecovery`), persistindo apenas quando a combinação escolhida realmente ficou ativa.
- Testes: 98 novos no M13 (validação, exibição, interpretação da entrada `HotkeyInput`, recuperação, persistência/leitura inválida).

## Correção (M13)

- **Captura de teclas inexistente:** o handler `PreviewKeyDown` da `SettingsWindow` nunca foi ligado ao alvo de gravação (ausente no XAML), então nenhuma tecla era capturada — defeito funcional confirmado pelo usuário. Correção: ligar o handler no XAML; o foco já estava correto (confirmado em runtime: `Keyboard.FocusedElement` = alvo após o clique).
- **Códigos de tecla virtual das F-keys:** a validação original usava valores de scan code (0x3B–0x44/0x5D–0x68) em vez dos VK do Win32 (F1–F24 = 0x70–0x87); F7–F12 não possuem colisão com letras. Corrigido: letras, dígitos e F1–F24 são todas aceitas.

---

# 27. M14 — UX Polish

**Status: implementado localmente (commits M14 sobre `e3560cc`), validação manual concluída pelo usuário (interface, primeira execução, feedbacks de cópia e fluxos funcionais); resta a publicação.**

## Objetivo

Consolidar a experiência e a qualidade do V0.1: interface principal, primeira execução (apresentação curta e não obrigatória), refinamento da experiência e consolidação do tratamento de erros.

Verificar:

- erros de captura;
- erros de OCR;
- erro de clipboard;
- erro de gravação;
- fechamento durante processamento;
- múltiplas capturas simultâneas;
- overlays residuais;
- recursos não liberados;
- UI travando;
- DPI;
- múltiplos monitores.

Corrigir somente problemas relacionados ao V0.1.

Não utilizar esse milestone para adicionar novas funcionalidades.

## Implementação (M14)

- **Construção assíncrona da sessão de overlay** (`FrozenOverlaySession`): os bombeamentos síncronos do Dispatcher do construtor (`Dispatcher.PushFrame` + `DispatcherTimer` de 2 s em `PumpDispatcher`/`PumpUntilFirstRender`/`PlaceOnMonitor`) foram substituídos por passes assíncronos — `CreateAsync` + `PumpDispatcherAsync` (um `InvokeAsync` em prioridade Background + limite de 2 s) e `WaitForFirstRenderAsync` (`ContentRendered` disputando com `Task.Delay` de 2 s). A UI thread nunca é bloqueada durante a construção: entrada, hotkey e encerramento continuam sendo processados entre as etapas. Análise de reentrância: um segundo gatilho de captura atinge a guarda `_captureInFlight`; um `Sair` no meio da construção é detectado pela guarda `HasShutdownStarted` nos passes, que abortam a construção — esta fecha as janelas já criadas e executa o callback de fechamento (a guarda é sempre resetada); um `Esc` durante a construção é adiado e aplicado exatamente uma vez ao final (`_closeRequested` + guarda `_closed`). O callback de fechamento dispara no máximo uma vez.
- **Apresentação de primeira execução** (`ui.md` §23): `FirstRunWindow` — curta, não obrigatória e dispensável (um botão "Começar"; o botão de fechar da janela também a dispensa), exibida somente na primeira execução. O estado é persistido em `settings.json` (`FirstRunPresented`); arquivo ausente ou legado (sem a propriedade) é lido como primeira execução.
- **Indicador de carregamento** (`ui.md` §9): `ProgressBar` indeterminada na janela principal, visível somente na fase `Processing` (ao lado do status "Reconhecendo a região selecionada…").
- **Diálogo de erro de captura com retry** (`ui.md` §16): `OKCancel` — OK reinicia a captura, Cancelar mantém o app ocioso. O WPF não oferece botão "Tentar novamente"; o texto do diálogo orienta ("Pressione OK para tentar novamente").
- **Guards de encerramento** em `RecognizeCropAsync`: não inicia reconhecimento nem mostra diálogo de erro se o Dispatcher já estiver encerrando (fluxo `Sair` durante processamento, incluindo o cleanup final da sessão).
- Testes: suíte 326 → 331 — 2 testes novos dos passes assíncronos (`PumpDispatcherAsync` em dispatcher vivo; `WaitForFirstRenderAsync` limitado a timeout), 3 testes novos da flag de primeira execução (round-trip, arquivo legado, arquivo corrompido) e o teste de vazamento de janelas atualizado para dirigir a construção assíncrona em thread STA.
- Diagnósticos `CAPPARSE_TRACE`/`CAPPARSE_NO_OVERLAY` **mantidos** (o fix do black-flash continua sem validação manual registrada; a remoção exige justificativa documentada e autorização).
- **Ajuste de layout (validação manual do M14):** os feedbacks de cópia da janela principal ("Texto copiado!"/"Imagem copiada!"/"Imagem salva!" e o do histórico) estavam em fluxo horizontal centralizado — ao aparecer, esticavam o `StackPanel` e deslocavam todos os botões. Correção (somente XAML): cada feedback passa a ocupar um slot fixo de 110px (mais largo que a mensagem mais longa) **abaixo do seu botão**, em colunas de `Grid` — a aparência/desaparição das mensagens não altera largura nem posição dos botões. A `FirstRunWindow` foi ampliada de 440×260 para 520×300 (margens 20→28, espaçamento entre parágrafos 10→12, botão "Começar" 130×32) para o conteúdo respirar. Validado geometricamente via UIAutomation (bounding boxes dos controles nas duas janelas) + screenshots; o estado com mensagens visíveis e o fluxo de captura seguem como validação manual do usuário.
- Atalhos pós-seleção (`ui.md` §22) **não implementados**: as combinações propostas não são definitivas (validação ergonômica e de conflitos pendente).

---

# 28. M15 — V0.1 Validation

## Objetivo

Validar o produto completo.

Checklist mínimo — a origem da validação está entre parênteses. "usuário" = validação manual informada pelo usuário na sessão M15 (o agente não a executou); "agente" = evidência de comando nesta sessão (ver §50.5).

### Captura

- [x] hotkey funciona (usuário);
- [x] tray double-click funciona (usuário);
- [x] Capture do tray funciona (usuário);
- [x] ESC cancela (usuário);
- [x] seleção funciona (usuário);
- [x] múltiplos monitores funcionam (usuário — nos monitores disponíveis, com escalas diferentes);
- [x] DPI diferente funciona (usuário — monitores com escalas diferentes).

### OCR

- [x] OCR local funciona (usuário — modo avião);
- [x] português funciona (usuário);
- [x] inglês funciona (agente — M16-2: 2 smoke tests automatizados com o modelo real em imagens sintéticas em inglês, `OcrSmokeTests`; a validação manual do aplicativo pelo usuário permanece opcional e não foi realizada);
- [x] texto simples funciona (usuário);
- [x] OCR não bloqueia a UI (usuário — fluxo completo responsivo durante o reconhecimento; indicador de processamento validado no M14).

### Ações

- [x] Generate Text funciona (usuário);
- [x] Copy Image funciona (usuário — colado no Paint);
- [x] Save Image funciona (usuário).

### Aplicação

- [x] Settings funciona (usuário — atalho alterado, programa reiniciado, persistência confirmada);
- [x] Exit funciona (usuário — incluindo saída durante OCR em andamento);
- [x] erros de OCR são tratados (usuário — M16-3: no pacote beta.2 extraído, pasta `models/` renomeada → o aplicativo abriu e iniciou a captura, exibiu uma mensagem informando que não era possível capturar, sem travar nem encerrar; após fechar o app, restaurar a pasta e reabrir, a captura voltou a funcionar. O texto exato da mensagem não foi registrado; o retry dentro da mesma sessão — próxima captura após a falha, sem reiniciar — é coberto apenas pelo teste automatizado `OcrCoordinatorTests.InitializationFailure_LeavesTheCoordinatorUsable`);
- [ ] erros de captura são tratados (sem gatilho reproduzível confirmado — o caminho foi inspecionado por código na M16-4; risco residual documentado em §50.7);
- [x] não há dependência de internet (usuário — modo avião);
- [x] aplicação pode permanecer no tray (usuário);
- [x] não existem janelas residuais (usuário — ausência de flash preto e de janelas residuais confirmada);
- [x] build Release funciona (agente — §50.5).

### Itens da validação manual informada pelo usuário (sessão M15)

Todos reportados como OK:

- Interface e captura: hotkey + captura completa; ausência de flash preto e de janelas residuais; `Esc` durante a seleção; Capturar/Configurações/Sair no tray; duplo clique no ícone do tray.
- Funcionalidades: copiar a imagem e colá-la no Paint; alterar a hotkey, reiniciar o programa e confirmar a persistência; sair durante um OCR em andamento.
- Confiabilidade: captura, OCR, cópia e salvamento em modo avião; múltiplos monitores com escalas diferentes (se disponíveis); fallback quando a hotkey já está ocupada.

---

# 29. Critérios de aceite

Todo milestone deve possuir critérios objetivos.

Evitar critérios como:

```text
"funciona bem"
```

Preferir:

```text
"pressionar Ctrl + Shift + X inicia a seleção"
```

ou:

```text
"uma seleção de 500×300 pixels produz uma imagem de 500×300 pixels"
```

ou:

```text
"ESC fecha todos os overlays sem deixar janelas abertas"
```

---

# 30. Testes

Antes de concluir um milestone:

1. executar build;
2. executar testes;
3. verificar warnings relevantes;
4. executar manualmente o comportamento quando necessário.

Comandos esperados:

```powershell
dotnet build
dotnet test
```

Para validação final:

```powershell
dotnet build -c Release
dotnet test -c Release
```

O agente deve corrigir problemas introduzidos pelo próprio milestone antes de considerá-lo concluído.

---

# 31. Testes manuais

Funcionalidades dependentes de Windows/WPF devem possuir testes manuais quando testes automatizados não forem suficientes.

Especialmente:

- hotkey;
- tray;
- múltiplos monitores;
- DPI;
- seleção;
- clipboard;
- diálogos;
- UX.

O agente deve informar claramente quando algo exige validação manual.

---

# 32. Git

Git deve ser utilizado como checkpoint permanente.

Cada milestone concluído deve resultar em um commit.

Exemplo:

```text
feat: add WPF application shell
```

```text
feat: add tray integration
```

```text
feat: add global capture hotkey
```

```text
feat: add multi-monitor capture
```

```text
feat: add OCR engine
```

---

# 33. Commits

Commits devem ser:

- pequenos;
- relacionados a uma única mudança;
- descritivos;
- compiláveis sempre que possível.

Evitar:

```text
feat: implement everything
```

Preferir:

```text
feat: add freeze-frame capture overlay
```

ou:

```text
fix: correct DPI coordinate conversion
```

---

# 34. Antes do commit

Antes de criar o commit:

```text
Check scope
      ↓
Build
      ↓
Tests
      ↓
Manual validation
      ↓
Review diff
      ↓
Commit
```

O agente deve verificar o diff antes do commit.

Não incluir:

- arquivos temporários;
- binários desnecessários;
- logs;
- credenciais;
- arquivos locais de configuração;
- artefatos de build.

---

# 35. GitHub

O GitHub representa o checkpoint remoto do projeto.

Após milestones importantes, o estado local deve ser sincronizado com o repositório remoto.

O objetivo é manter uma versão recuperável do projeto.

Releases públicas são marcadas com tags (ex.: `v0.1.0-beta.1`, apontando para `86e3d17`). Commits publicados após a tag (ex.: o M11 em `3bed7be`) fazem parte de `main` mas não daquela release, até uma nova release ser criada.

Não reescrever histórico remoto sem necessidade.

---

# 36. Documentação

Os documentos em `docs/` devem acompanhar decisões estáveis.

Arquivos principais:

```text
docs/
├── product.md
├── ui.md
├── architecture.md
├── development.md
└── ocr-integration.md   (registro histórico da integração do OCR: M6-A/M7-A/M7-B/M7-C)
```

Não atualizar documentos apenas para refletir uma implementação ruim.

Se o código contradiz uma decisão documentada, primeiro determinar se:

1. o código está errado;
2. a documentação está desatualizada;
3. existe uma nova decisão de produto.

---

# 37. Regra de mudança de requisitos

Se durante o desenvolvimento surgir uma nova ideia:

```text
Nova ideia
    │
    ▼
É necessária para o milestone atual?
    │
    ├── Sim → avaliar e implementar
    │
    └── Não → registrar para depois
```

Não transformar cada descoberta em escopo novo.

---

# 38. Backlog futuro

Ideias futuras devem permanecer separadas do código atual.

Exemplos:

- Copy Text;
- JSON;
- CSV;
- Markdown;
- XLSX;
- PDF;
- tradução;
- extração de tabelas;
- código;
- fórmulas;
- AI Vision;
- provedores de IA;
- modelos locais;
- Ollama;
- OpenAI;
- histórico;
- anotações;
- QR/barcode;
- gravação;
- plugins;
- Linux;
- macOS;
- mobile.

A existência dessa lista não significa que esses recursos devem ser implementados agora.

## 38.1 Itens registrados

### Histórico de capturas com recuperação de imagem (registrado na M15)

**Decisão de produto (M15): o histórico da V0.1 permanece somente textual** — ele mantém até 20 resultados OCR em memória, mas não preserva a imagem de cada entrada; copiar ou salvar uma imagem antiga a partir do histórico não é suportado.

Melhoria futura (não implementada):

- Associar cada resultado OCR à imagem da captura correspondente;
- Permitir selecionar uma entrada antiga e copiar ou salvar sua imagem original;
- Manter o limite de 20 entradas;
- Investigar arquivos temporários por sessão como estratégia preferencial, evitando manter 20 bitmaps grandes em RAM;
- Limpar arquivos ao remover entradas do histórico e ao encerrar a aplicação normalmente;
- Considerar encerramentos inesperados, arquivos órfãos, falhas de gravação e ciclo de vida das imagens;
- Não persistir o histórico entre sessões, salvo decisão futura explícita.

---

# 39. Quando parar

O agente deve parar quando:

- os critérios de aceite forem atendidos;
- os testes passarem;
- o build passar;
- não houver problemas conhecidos introduzidos pelo milestone.

Não continuar "melhorando" indefinidamente.

Depois disso:

```text
Implementação
    ↓
Validation
    ↓
Commit
    ↓
Stop
```

---

# 40. Regra contra refactoring prematuro

Não refatorar código que funciona apenas porque uma solução futura poderia ser diferente.

Refatorar quando:

- existe duplicação real;
- existe bug;
- a manutenção está ficando difícil;
- uma nova funcionalidade realmente exige a mudança;
- a estrutura atual impede evolução.

Evitar refatorações especulativas.

---

# 41. Regra contra overengineering

Se uma implementação simples resolve:

```text
usar a implementação simples
```

Não substituir por:

- framework;
- padrão complexo;
- abstração genérica;
- sistema de eventos;
- pipeline;
- mediator;
- container;
- plugin system;

sem necessidade concreta.

---

# 42. Regra de investigação

Quando uma tecnologia apresentar comportamento inesperado, o agente deve investigar antes de criar uma solução alternativa.

Exemplo:

```text
Problema
   ↓
Verificar documentação/API
   ↓
Verificar implementação atual
   ↓
Reproduzir
   ↓
Identificar causa
   ↓
Corrigir
```

Não adicionar workarounds complexos baseados em suposições.

---

# 43. Regra de compatibilidade

A prioridade de compatibilidade do V0.1 é:

1. Windows 11;
2. Windows 10 22H2 ou superior dentro do suporte definido;
3. múltiplos monitores;
4. DPI diferente.

Não sacrificar simplicidade para suportar plataformas que não fazem parte do V0.1.

---

# 44. Regra de UX

Uma implementação tecnicamente correta que produz uma experiência ruim não deve ser considerada concluída.

Verificar sempre:

- resposta rápida;
- feedback durante operações;
- ausência de travamentos;
- mensagens claras;
- comportamento previsível;
- cancelamento;
- ausência de janelas desnecessárias.

---

# 45. Regra de segurança e privacidade

Não introduzir:

- coleta de dados;
- telemetria;
- chamadas externas;
- upload automático;
- API keys;
- credenciais.

Sem decisão explícita de produto.

Screenshots podem conter informações privadas e devem ser tratados como dados locais do usuário.

---

# 46. Regra final para o agente

Antes de implementar qualquer coisa, o agente deve conseguir responder:

```text
O que estou implementando?
Por que isso é necessário agora?
Qual documento define esse comportamento?
Quais arquivos precisam mudar?
Como vou verificar que funciona?
```

Se essas perguntas não tiverem respostas claras, o agente deve parar antes de aumentar o código.

---

# 47. Definition of Done

Um milestone só está concluído quando:

- [ ] escopo foi respeitado;
- [ ] critérios de aceite foram atendidos;
- [ ] código compila;
- [ ] testes passam;
- [ ] validação manual necessária foi realizada;
- [ ] não foram adicionadas funcionalidades fora do escopo;
- [ ] não foram adicionadas dependências desnecessárias;
- [ ] diff foi revisado;
- [ ] documentação continua consistente;
- [ ] commit foi criado;
- [ ] projeto está em um estado recuperável.

**Done significa terminado.**

Não significa "funciona mais ou menos e podemos continuar".

---

# 48. Filosofia

CapParse deve crescer por necessidade.

Não por antecipação.

A prioridade é:

```text
Clareza
   ↓
Correção
   ↓
Simplicidade
   ↓
Experiência do usuário
   ↓
Extensibilidade
```

A extensibilidade é importante, mas não deve dominar o design do V0.1.

O objetivo é construir uma ferramenta pequena que funcione muito bem.

Depois disso, expandir.

---

# 49. Regra absoluta

> Não construir o futuro antes de terminar o presente.

Cada milestone deve deixar o projeto melhor, mais funcional e ainda fácil de entender.

Se uma mudança torna o projeto significativamente mais complexo sem entregar valor imediato ao usuário, ela provavelmente não pertence ao milestone atual.

---

# 50. Estado atual do projeto (continuidade entre sessões)

Esta seção é o ponto de entrada de uma nova sessão. Reescrita na consolidação documental pós-M11 (2026-10), substitui a versão da auditoria de 2026-10-12 (que descrevia o estado de um clone obsoleto); atualizada na sessão M14 (registro do M13 publicado e do trabalho M14 local) e na sessão M15 (preparação da candidata v0.1.0-beta.2: documentação, metadados de versão, procedimento de publicação reproduzível e pacote de validação), na sessão M16 (M16-1 consistência documental pós-publicação da beta.2; M16-2 validação automatizada do OCR em inglês; M16-3 validação manual do erro de OCR pelo usuário; M16-4 registro do erro de captura como risco residual) e nas sessões da V0.2 (fase 1: fundação de tema, §51; fase 2: MainWindow, §52; fase 3: SettingsWindow + seletor de tema, §53; fase 4: FirstRunWindow, §55; fase 5: modernização visual do overlay de captura, §56). O que está marcado como "executado" tem evidência de comando na sessão indicada; o restante vem do histórico Git e da documentação verificada contra o código.

## 50.1 Repositório de referência

- **Repositório principal de trabalho: `C:\Dev\CapParseIntegI`** (branch `main`, remoto `https://github.com/Sweetcheat/CapParse.git`).
- `C:\Dev\CapParse` é um **clone obsoleto** (HEAD `3324900`, 13 commits atrás da linha principal). Ele serviu apenas como fonte de comparação documental na auditoria de 2026-10-12. **Não trabalhar, não commitar e não fazer push a partir dele** (o `origin/main` local dele está desatualizado; um force-push a partir dele apagaria a linha publicada do GitHub).

## 50.2 Estado do repositório (sessão V0.2 — fase 4)

- Branch: `main`; HEAD = `origin/main` = `178c940` (chore: finalize beta.2 release readiness) — o fechamento da M15 está **publicado** no GitHub (commit, tag e release).
- Working tree: **trabalho local não commitado** de M16 (M16-1/M16-2/M16-4: `AGENTS.md`, `README.md`, `docs/*`, `App.xaml.cs`, `AppSettings.cs`, `AppSettingsTests.cs`, `OcrSmokeTests.cs`) + V0.2 fases 1–5 (fases 1: `Themes/`, `ThemePreference.cs` e testes de tema; fase 2: `MainWindow.xaml`; fase 3: `SettingsWindow.xaml`/`.cs` + `App.xaml.cs`; fase 4: `FirstRunWindow.xaml` + `ThemeServiceTests.cs`; fase 5: `UI/MonitorOverlayWindow.cs` + `UI/FrozenOverlaySession.cs` (apenas a constante `DimOpacity` e o construtor do overlay) + dicionários de tema (`OverlayDimBrush`) + `MonitorOverlayWindowTests.cs`) — tudo aguardando aprovação explícita para commit; ver §50.3, §55 e §56.
- Tag `v0.1.0-beta.1` → `86e3d17`: a release beta publicada **não inclui** o M11, o M12, o M13 nem o M14. A tag não foi movida nem alterada.
- Tag `v0.1.0-beta.2` → `178c940`: release publicada no GitHub em 2026-10-13 ("Latest", 3 assets), com o pacote `build/dist/CapParse-v0.1.0-beta.2-win-x64.zip` (dados em §50.5). `build/dist/CapParse-v0.1.0-beta.1-win-x64.zip` é **desatualizado** (anterior a M11–M14) e não deve ser reutilizado como artefato final.
- `.gitignore` ignora `build/dist/` (pacotes de publicação).

## 50.3 Milestones — status

Statuses: **CV** = concluído e verificado (commitado + testes + evidência de validação); **I** = implementado/commitado, validação manual pendente; **P** = planejado; **FI** = investigação futura.

| Milestone | Status | Commit(s) | Observação |
| --- | --- | --- | --- |
| M0 — Repository Foundation | CV | `ab09abf` | — |
| M1 — WPF Application Shell | CV | `c142ecb` | — |
| M2 — Tray Integration | CV | `47a403d` | — |
| M3 — Global Hotkey | CV | `a1306d5` | `Ctrl + Shift + X` |
| M4 — Monitor Discovery and Capture | I (código + testes; validação manual multi-monitor/DPI sem evidência no repositório) | `7325645`, `3657169`, `69653dd`, `8596164` | — |
| M5 — Freeze-Frame Selection Overlay | I (código + testes; validação manual de overlay/DPI sem evidência no repositório) | `7fc544a`, `ea059bf` | fixes posteriores: `2de9f78`, `d13e5a4`, `3fc8ee7`, `a32b480`, `a79724d` |
| M6 — Image Crop Pipeline | CV (código + testes) | `3324900` | — |
| M7 — OCR Engine | CV (commitado; 146/146 testes na época; avaliação M7-C registrada) | `3c0da24` | engine + modelos + coleção de qualidade; registro em `docs/ocr-integration.md` |
| M8 — Processing/Loading UX | CV (commitado) | `e383d90` | `OcrCoordinator` (engine lazy em thread pool), `OcrRunState`, cancelamento de execução obsoleta, estado de processamento na janela |
| M9/M10 — Resultado + Generate Text | CV (commitado; o commit agrupa apresentação e cópia de texto) | `61379b7` | resultado na janela principal + `OcrTextCopier` + `WpfTextClipboard` |
| — Trabalhos adicionais publicados no beta | CV (commitados) | `f2c1036` (pt-BR), `a097cfa` (histórico de sessão), `f67b3a3` (instância única), `d9bdef1` (double-click do tray restaura a janela), `86e3d17` (README bilíngue, tag da beta) | — |
| M11 — Copy Image | CV (commitado e **publicado**; 209/209 testes em Debug e Release) | `3bed7be` | `CropImageCopier` + `WpfImageClipboard` (DIB em memória, DPI efetivo), botão "Copiar imagem", 8 testes com fake; **fora da release `v0.1.0-beta.1`** |
| M12 — Save Image | CV (commitado e **publicado** em `1edd9f1`; validação manual do diálogo registrada em `6401b4b`) | `1edd9f1` | `CropImageSaver` + `WpfCropImageSaveDialog` (diálogo padrão, PNG c/ tag de DPI, nome sugerido único), botão "Salvar imagem", 19 testes com fake do diálogo; **fora da release `v0.1.0-beta.1`** |
| M13 — Settings | CV (commitado e **publicado** em `main` no GitHub: `5180b52` + `e3560cc`; correção da captura de teclas da SettingsWindow — handler `PreviewKeyDown` nunca foi ligado no XAML, e a validação de F-keys foi corrigida para os códigos VK reais 0x70–0x87; 326/326 testes em Debug e Release; fluxo completo validado em runtime no Windows via harness; restam ao usuário o clique físico no menu do tray e teclas físicas com modificadores) | `5180b52`, `e3560cc` | `SettingsWindow` (tray → Configurações), `Configuration/` (`HotkeySetting`, `HotkeyInput`, `HotkeyRecovery`, `AppSettings`), persistência em `%LOCALAPPDATA%\CapParse\settings.json`, 98 testes novos no total; **fora da release `v0.1.0-beta.1`** |
| M14 — UX Polish | CV (commitado e **publicado** em `main`: `a7d2c22` + `c4e76fd`; 331/331 testes em Debug e Release; validação manual concluída pelo usuário — interface, apresentação de primeira execução, feedbacks de cópia e fluxos funcionais) | `a7d2c22`, `c4e76fd` | construção assíncrona do overlay (`CreateAsync`, sem `Dispatcher.PushFrame`), `FirstRunWindow` (flag `FirstRunPresented` em `settings.json`), `ProgressBar` de processamento, diálogo de erro de captura `OKCancel` com retry, guards de shutdown em `RecognizeCropAsync`, ajustes de layout (feedbacks em slots fixos sob os botões; `FirstRunWindow` 520×300); atalhos pós-seleção (`ui.md` §22) fora do escopo (combinações não definitivas); **fora da release `v0.1.0-beta.1`** |
| M15 — V0.1 Validation | CV (commitado em `178c940`, push para `origin/main`, tag `v0.1.0-beta.2` e release GitHub publicadas em 2026-10-13): documentação consistente, backlog (histórico com imagem), procedimento de publicação reproduzível (`build/publish-win-x64.ps1`), metadados de versão `0.1.0-beta.2`, pacote `CapParse-v0.1.0-beta.2-win-x64.zip` publicado na release; validação manual do produto informada pelo usuário (§28) | `178c940` | o OCR em inglês foi validado na M16-2 (testes automatizados com o modelo real) e o caminho de erro de OCR foi validado pelo usuário na M16-3; resta (M16): o caminho de erro de captura (§28, item `[ ]` — risco residual, M16-4) |
| M16 — Beta Feedback & Stabilization | Em andamento: M16-1 consistência documental pós-beta.2, M16-2 validação automatizada do OCR em inglês e M16-4 registro do erro de captura como risco residual (implementados localmente, sem commit); M16-3 validação manual do erro de OCR concluída pelo usuário; pendente: M16-5 (remoção dos diagnósticos `CAPPARSE_TRACE`/`CAPPARSE_NO_OVERLAY`) é candidata e exige aprovação do usuário | — | feedback cotidiano do usuário como fonte principal; bugs triados por impacto × reproduzibilidade |
| V0.2 — Modernização visual (temas + janelas) | Em andamento: fases 1–5 implementadas localmente, sem commit (fases 1–3: §51–§53; fase 4: §55; fase 5: §56); 363/363 testes em Debug e Release | — | fase 1: fundação de tema (tokens, `ThemeService`, persistência); fase 2: `MainWindow`; fase 3: `SettingsWindow` + seletor de tema; fase 4: `FirstRunWindow`; fase 5: overlay de captura (destaque por corte do escurecimento + borda de acento + escurecimento por tema); pendências: validação humana dos temas/estados, commit com aprovação (asset `.ico` da mascote aprovado e integrado: `src/CapParse/Assets/capparse.ico` como ícone do executável, da janela principal e da bandeja) |
| Futuro — HDR Capture & Tone Mapping | FI | — | `product.md` §9; `architecture.md` §45.1 |
| Futuro — Spreadsheet Conversion | FI | — | `product.md` §9; `architecture.md` §45.2 |

Notas sobre o M11:

- validação automatizada: build Debug/Release com 0 erros e **209/209 testes aprovados em ambas as configurações** (sessão de commit, 2026-10);
- um rebuild completo (`--no-incremental`) identificou **3 avisos preexistentes** em arquivos fora do escopo do M11 (`FrozenOverlaySession.cs` CS8625 e `OcrCollectionGeneratorTests.cs` xUnit2020) — não atribuir esses avisos ao novo recurso;
- validação manual do paste em aplicações de destino (Discord, Paint etc.) ainda pendente.

Notas sobre o M12:

- validação automatizada: build Debug/Release com 0 erros e **228/228 testes aprovados em ambas as configurações** (sessão do M12); os testes gravam PNGs reais em disco (diretório temporário) e os decodificam de volta para verificar pixels e DPI;
- um rebuild completo (`--no-incremental`) em ambas as configurações confirma apenas os 3 avisos preexistentes do M11 (o CS8625 de `FrozenOverlaySession.cs` é contado duas vezes — projeto principal e projeto temporário do WPF) — nenhum aviso novo foi introduzido;
- validação manual concluída pelo usuário no Windows: o fluxo completo funcionou corretamente — `Ctrl+Shift+X` congela a tela e inicia a seleção; após a seleção da região, o botão "Salvar imagem" abre o diálogo padrão do Windows; o diálogo sugere um nome `CapParse` com data/hora; o usuário escolhe o destino e salva; a imagem é gravada no arquivo escolhido. (O agente também registrou, na sessão do M12, evidência de startup da aplicação em sessão interativa.)

## 50.4 Comportamentos publicados que difere dos documentos originais

Decisões de produto tomadas na linha principal (registradas aqui para evitar "corretivas" involuntárias em sessões futuras):

1. **Double-click do tray restaura a janela principal** — não inicia captura (a captura é por hotkey ou menu "Capturar"). Diferente do `product.md`/`ui.md` originais.
2. **A janela principal é a view do resultado** — não há um "action selector" separado nem uma janela de resultado dedicada; as ações são explícitas na própria janela (Copiar texto / Copiar imagem / Salvar imagem) + histórico da sessão.
3. **Inicialização do OCR é lazy** via `OcrCoordinator` (thread pool, primeira execução), não antecipada na startup.
4. **Interface em pt-BR** (janela, tray, mensagens de erro).
5. **Instância única** por mutex (`CapParse.SingleInstance.v1`); segunda instância exibe mensagem e encerra.
6. **Histórico de OCR em memória**, por sessão (máximo de entradas, imutável, mais recente primeiro).
7. **Settings (M13)**: o atalho de captura é configurável na janela `SettingsWindow` (menu do tray "Configurações") e persistido em `%LOCALAPPDATA%\CapParse\settings.json`. O arquivo guarda somente a combinação (modificadores + tecla virtual) — nenhum dado pessoal. Quando o atalho salvo não pode ser registrado (outro aplicativo o está usando), o app ativa o padrão `Ctrl + Shift + X` e informa o usuário; o arquivo passa a ser gravado apenas com o atalho efetivamente ativo. Teclas aceitas: letras, números e F1–F24.

## 50.5 Evidências de build e testes

- V0.2 fase 5 (sessão atual, modernização visual do overlay de captura): `dotnet build` + `dotnet test` em **Debug** — 0 erros, **363/363 aprovados** (359 anteriores + 4 novos: `MonitorOverlayWindowTests` — geometria do escurecimento: superfície integral, furo sobre a seleção, seleção de área zero ignorada, furo registrado como dado); em **Release** — 0 erros, **363/363 aprovados**; apenas o aviso preexistente xUnit2020 em `OcrCollectionGeneratorTests.cs` — nenhum aviso novo. `git diff --check` limpo; diff revisado: apenas `UI/MonitorOverlayWindow.cs`, `UI/FrozenOverlaySession.cs` (constante `DimOpacity` + construtor), `Themes/Theme.{Light,Dark}.xaml` (token `OverlayDimBrush`) e `tests/CapParse.Tests/MonitorOverlayWindowTests.cs` (novo) — a lógica de revelação em dois frames, congelamento, coordenadas, seleção, DPI e multi-monitor é **inalterada** (verificado no diff). **Vision validado na sessão** (imagem de teste inspecionada com sucesso; 11/12 imagens usadas). Validação visual do overlay real (executável, tema dark via `System`/`AppsUseLightTheme=0`): o overlay em repouso mostra o desktop congelado com o novo escurecimento dark (40%) — conteúdo mais reconhecível que o anterior (50%); durante a seleção, a área é destacada a brilho pleno (corte no escurecimento) com borda de acento de 2px — em ambos os estados, sem flash preto, sem janelas residuais, app vivo em todos os checkpoints e encerrado limpo. O tema light foi validado por render offscreen (harness) nos dois estados (repouso/seleção), pois a preferência do usuário é `System`→dark e o `settings.json` não pode ser alterado. **Limitações:** o driver sintético de mouse (`SendInput`) não produziu a seleção exata coordenada que foi enviada (o retângulo visível aparece deslocado/estreito; a seleção final do run resultou em cancelamento — janela principal em estado ocioso) — artefato do driver em monitor 133%, não do aplicativo (a seleção real foi validada pelo usuário na M15); a inspeção do flash preto em tempo de execução continua coberta pela lógica de revelação em dois frames inalterada + a ausência de flash nos frames capturados, não por gravação de vídeo; a validação humana (incluindo a preferência light no executável) permanece pendente com o usuário. `settings.json` do usuário intocado (SHA-256 idêntico antes/depois). PNGs de referência: `%TEMP%\overlay-{before,after}-{rest,drag,result}.png` (executável) e `%TEMP%\overlay-{before,after}-{light,dark}-{idle,select}.png` (harness offscreen).
- V0.2 fase 4 (sessão atual, modernização visual da `FirstRunWindow`): `dotnet build` + `dotnet test` em **Debug** — 0 erros, **359/359 aprovados** (358 anteriores + 1 novo: `FirstRunWindow_OnlyUsesTokensThatExistInBothThemes`); em **Release** — 0 erros, **359/359 aprovados**; apenas o aviso preexistente xUnit2020 em `OcrCollectionGeneratorTests.cs` — nenhum aviso novo. Validação visual offscreen (harness descartável em `%TEMP%\CapParseThemeHarness`, estendido nesta fase com o modo `firstrun`/`firstrun-swap`): renders 520×300 da `FirstRunWindow` nos dois temas com a paleta exata dos tokens (light: `FFFFFF`/`F3F4F6`/`2E6BE6`/`464E58`/`1A1E24`/`D6DAE0`; dark: `22262E`/`1A1D23`/`5B93FF`/`B9C0CA`/`F1F3F6`/`39404C`) e sem nenhum controle sem tema; troca ao vivo (uma instância: render light → dicionário trocado para dark → render) produz contagens de pixels idênticas entre os dois temas (mesmo layout, paleta trocada) — re-tematização sem reinício; geometria do botão "Começar" medida no accent: `x=37, y=232, 130×31` nos dois temas (invariante); `settings.json` do usuário não tocado (SHA-256 idêntico antes/depois dos runs do harness). **Limitação:** a inspeção visual humana/modelada **não pôde ser realizada nesta sessão** — o modelo da sessão não aceita imagens (a política de Vision, `AGENTS.md` §39/§54, exige declarar isso explicitamente); os histogramas/medições de pixels acima são evidência objetiva complementar, não substituto da avaliação visual; estados interativos (hover, pressed, anel de foco) existem nos templates, mas não foram exercitados por interação real; o fluxo real de primeira execução no executável (com a flag re-armada) não foi exercitado nesta sessão — permanece a validação manual do usuário.
- M16-3 (validação manual informada pelo usuário, pacote beta.2 extraído): o usuário renomeou a pasta `models/`, abriu o aplicativo e iniciou uma captura — o aplicativo exibiu uma mensagem informando que não era possível capturar, sem travar nem encerrar; depois de fechar o aplicativo, restaurar o nome da pasta e reabri-lo, a captura voltou a funcionar normalmente. O texto exato da mensagem **não foi registrado** e o agente **não executou** esta validação. Observação: a recuperação ocorreu com reinício do aplicativo — o retry dentro da mesma sessão (próxima captura após a falha de inicialização do engine, sem reiniciar) não foi exercitado manualmente e permanece coberto apenas pelo teste automatizado `OcrCoordinatorTests.InitializationFailure_LeavesTheCoordinatorUsable`.
- M16-4 (sessão atual, registro documental — sem mudança de código): o caminho de erro de captura (`App.xaml.cs`: o `catch` da construção da sessão reseta a guarda `_captureInFlight` e exibe o diálogo amigável com retry quando o app não está em encerramento) foi inspecionado por código; **não há gatilho reproduzível confirmado** para exercitá-lo diretamente em desktop real (uma falha de enumeração de monitores/`BitBlt` não pode ser induzida de forma controlada). Risco residual da V0.1: o comportamento do diálogo de erro de captura (mensagem amigável, app não fica bloqueado) depende da guarda inspecionada, sem evidência de teste automatizado nem de validação manual.
- M16-2 (sessão atual, validação automatizada do OCR em inglês): 2 novos smoke tests em `OcrSmokeTests` (modelo real PP-OCRv6 Small, imagens sintéticas Segoe UI 32 pt, preto sobre branco, 1200×220 — mesmo padrão do caso PT): entrada `Settings` → reconhecido `Settings` (773,7 ms); entrada `Capture image` → reconhecido `Capture image` (976,8 ms, espaço preservado); o caso PT continua exato (`Olá, ação, coração, informação e português.`, confidence 0,98). `dotnet build` + `dotnet test` em **Debug** e **Release** — 0 erros, **333/333 aprovados** em ambas (331 anteriores + 2 novos); apenas o aviso preexistente xUnit2020 em `OcrCollectionGeneratorTests.cs`. **Isenção:** esta é validação automatizada com imagens sintéticas — não substitui a validação manual do aplicativo pelo usuário (opcional para o inglês) e não exercita os diálogos de erro (M16-3/M16-4).

- M15 (sessão atual, preparação da beta.2): `dotnet restore` + `dotnet build` + `dotnet test` em **Debug** — 0 erros, **331/331 aprovados**; em **Release** (via `build/publish-win-x64.ps1`) — 0 erros, **331/331 aprovados** (incluindo os 4 smoke tests com o modelo real PP-OCRv6); apenas o aviso preexistente xUnit2020 em `OcrCollectionGeneratorTests.cs` — nenhum aviso novo. Publish `win-x64` self-contained single-file (`PublishSingleFile=true` + `IncludeNativeLibrariesForSelfExtract=true` — o mesmo layout do beta.1, cujo comportamento em campo o usuário validou): o executável embute o runtime .NET e as nativas (onnxruntime, SkiaSharp); FileVersion `0.1.0.0`, ProductVersion `0.1.0-beta.2`. Pacote: `build/dist/CapParse-v0.1.0-beta.2-win-x64.zip` — **104.227.368 bytes (99,4 MB)**, SHA-256 `DB6D4606A6A1CE58071B3E06A1317A0212325D5934AF5D1C0A955EC19128A6EC`; 5 entradas (`CapParse.exe` 181,8 MB + `models/v6/*` + classificador v5), sem PDB, `.lib` ou artefatos de desenvolvimento. **Teste em diretório limpo (agente)**: o ZIP foi extraído para uma pasta temporária nova e o `CapParse.exe` inicializou e permaneceu estável (~15 s) sem depender do ambiente de desenvolvimento, encerrando-se limpo. **Restante ao usuário:** executar o pacote beta.2 em computador externo — a captura completa + OCR a partir do pacote embalado exige seleção interativa (o OCR é lazy e não pode ser gatilho headless); o teste limpo cobriu inicialização, não OCR.
- M15 (validação manual informada pelo usuário): o usuário reportou todos os itens da validação do produto como OK — interface e captura (hotkey + captura completa, ausência de flash preto e janelas residuais, `Esc`, tray Capturar/Configurações/Sair, duplo clique no tray), funcionalidades (copiar a imagem e colá-la no Paint; alterar a hotkey, reiniciar e confirmar a persistência; sair durante OCR em andamento) e confiabilidade (captura, OCR, cópia e salvamento em modo avião; múltiplos monitores com escalas diferentes, se disponíveis; fallback quando a hotkey já está ocupada). O agente **não** executou essas validações; o mapeamento item a item com o checklist do §28 está registrado ali.
- Decisão sobre os modelos v5 (M15): o preset `PPOCRv6Small` do RapidOcrNet 4.2.0 (verificado por reflexão contra o pacote) referencia `models/v6/PP-OCRv6_det_small.onnx`, `models/v6/PP-OCRv6_rec_small.onnx`, `models/v6/ppocrv6_dict.txt` e `models/v5/ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx` (classificador de orientação). Os demais três arquivos v5 (`ch_PP-OCRv5_mobile_det.onnx`, `latin_PP-OCRv5_rec_mobile_infer.onnx`, `ppocrv5_latin_dict.txt`) pertencem ao preset PP-OCRv5 mobile, que o aplicativo nunca carrega — eles chegam à saída de publish apenas pelo target do próprio pacote. Decisão: **manter o classificador v5** (obrigatório em runtime) e **remover os três arquivos v5 não utilizados do pacote distribuído** (passo explícito do `build/publish-win-x64.ps1`), validado pelos 331 testes (incluindo os smoke tests com o modelo real) e pelo teste em diretório limpo. Nenhum arquivo foi removido do repositório.
- M14-ajustes de layout (sessão atual): `dotnet build` e `dotnet test` em Debug e Release — 0 erros, **331/331 aprovados** em ambas as configurações; apenas o aviso preexistente xUnit2020. Validação visual (agente, Windows): app lançado em Release com a primeira execução re-armada — `Bem-vindo ao CapParse` (520×300) e `CapParse` (960×560) inspecionados via UIAutomation (bounding boxes exatos: margens de 36px simétricas e botão "Começar" 130×32 na primeira execução; na janela principal, botões 120×30 com colunas de 16px e slots de feedback de 110×16 permanentes sob cada botão — geometria invariante à exibição das mensagens) e screenshots; processo encerrado e `settings.json` restaurado a `FirstRunPresented: true`.
- M14 (validação manual do usuário, sessão atual): o usuário concluiu a validação manual do M14 — interface (janela principal, feedbacks de cópia/salvamento exibidos), apresentação de primeira execução e os fluxos funcionais da aplicação (captura/OCR/copiar/salvar).
- M14 (sessão atual): `dotnet build` e `dotnet test` em Debug e Release — 0 erros, **331/331 aprovados** em ambas as configurações; apenas o aviso preexistente xUnit2020 em `OcrCollectionGeneratorTests.cs` (o CS8625 preexistente de `FrozenOverlaySession.cs` foi **resolvido** com o parâmetro `failureHook` agora declarável como anulável — nenhum aviso novo). Validação manual parcial (agente, Windows): app lançado em Release — janela principal + apresentação de primeira execução ("Bem-vindo ao CapParse") exibidas, flag `FirstRunPresented` gravada em `settings.json`, processo encerrado. O restante da validação manual foi concluído pelo usuário (linha anterior).
- M13-corr (sessão da correção): `dotnet build` e `dotnet test` em Debug e Release — 0 erros, **326/326 aprovados** em ambas as configurações; apenas os 3 avisos preexistentes do M11 (CS8625 em `FrozenOverlaySession.cs` contado duas vezes + xUnit2020 em `OcrCollectionGeneratorTests.cs`) — nenhum aviso novo. Validação em runtime no Windows: harness temporário (fora do repositório) executou o app em ambiente completo (tray + hotkey global + janela principal), abriu a `SettingsWindow` real via `ShowSettings()` e dirigiu o fluxo por eventos WPF — clique em "Alterar atalho" (foco confirmado no alvo de captura), tecla pressionada → feedback de combinação inválida (prova de que a tecla foi capturada), `Esc` → cancela sem alterar o atalho, segunda entrada em modo gravação, modificador sozinho ignorado. **PASS completo.** O que o harness não cobre: cliques físicos no menu do tray e teclas físicas com modificadores (estado `Keyboard.Modifiers` é de dispositivo); essas duas pontas permanecem como validação manual do usuário.
- M13 (sessão do commit): `dotnet build` e `dotnet test` em Debug e Release — 0 erros, **296/296 aprovados** em ambas as configurações; apenas os 3 avisos preexistentes do M11 — nenhum aviso novo; startup da aplicação validado no Windows (processo estável por ~12 s, encerramento limpo).
- M12 (sessão do commit, 2026-10): `dotnet build` e `dotnet test` em Debug e Release — 0 erros, **228/228 aprovados** em ambas as configurações; rebuild completo (`--no-incremental`) confirma apenas os 3 avisos preexistentes.
- M11 (sessão de commit, 2026-10): `dotnet build` e `dotnet test` em Debug e Release — 0 erros, **209/209 aprovados** em ambas as configurações.
- M7 (registro histórico): 146/146 em Debug e Release na época do commit `3c0da24`; avaliação de qualidade do modelo em `docs/ocr-integration.md` §10 e `tests/CapParse.Tests/TestData/Ocr/reports/`.
- A suíte cresce com os milestones (146 → 209 → 228 → 296 entre M7, M11, M12 e M13): comparar sempre com o total da suíte atual, não com números históricos.

## 50.6 Próximos passos

A M15 está concluída: as alterações foram commitadas em `178c940`, o push para `origin/main` foi concluído, a tag `v0.1.0-beta.2` foi criada e a release foi publicada no GitHub em 2026-10-13 com o ZIP de `build/dist/` (a tag e o pacote da beta.1 permanecem intactos).

M16 (Beta Feedback & Stabilização) — tarefas planejadas na auditoria de abertura da M16 (M16-1 a M16-4 concluídas; M16-1/M16-2/M16-4 com alterações locais e sem commit, M16-3 pelo usuário):
1. M16-1 consistência documental pós-beta.2 (concluído localmente, sem commit);
2. M16-2 validação automatizada do OCR em inglês (concluído localmente, sem commit — 2 smoke tests com o modelo real, 333/333 em Debug e Release, evidência em §50.5);
3. M16-3 validação manual do caminho de erro de OCR (concluído pelo usuário — pasta `models/` renomeada no pacote beta.2 extraído: mensagem amigável, app estável, recuperação após restaurar a pasta e reiniciar; evidência em §50.5);
4. M16-4 registro do caminho de erro de captura como risco residual (concluído localmente, sem commit — §28, §50.7; sem gatilho reproduzível confirmado);
5. M16-5 (candidata, exige aprovação do usuário): remoção dos diagnósticos `CAPPARSE_TRACE`/`CAPPARSE_NO_OVERLAY`.

V0.2 (Modernização visual) — estado após a fase 5 (todas as fases locais, sem commit): fases 1–5 concluídas (fundação de tema §51, `MainWindow` §52, `SettingsWindow` + seletor §53, `FirstRunWindow` §55, overlay de captura §56). Pendências:

1. ~~Asset `.ico` derivado da identidade visual~~ — concluído: mascote aprovada integrada em `src/CapParse/Assets/capparse.ico` (ícone do executável via `ApplicationIcon`, janela principal via recurso WPF e bandeja via `NotifyIcon`, com descarte do `Icon` no `ExitApplication`);
2. Validação humana dos dois temas, dos estados interativos (hover/foco), do seletor, da apresentação de primeira execução e do overlay (repouso/seleção, flash preto, multi-monitor/DPI) no executável (usuário);
3. Commit do trabalho local (M16 + V0.2) com aprovação explícita do usuário.

O feedback cotidiano do usuário é a fonte principal da M16; bugs encontrados são triados por impacto × reproduzibilidade, com correção mínima + teste de regressão quando reproduzível.

Restrição operacional: o OCR a partir do pacote beta.2 embalado só pode ser validado interativamente (usuário, em computador externo) — não há gatilho headless para a captura.

## 50.7 Problemas conhecidos, riscos e limitações

- OCR (PP-OCRv6 Small, medido no M7-C): fonte pequena em fundo escuro degrada severamente; espaços entre palavras/tokens são frequentemente perdidos no texto reconstruído.
- A arena de memória do ONNX Runtime cresce durante execuções (WorkingSet do processo ~2 GB na avaliação M7-C; não é memória exclusiva do OCR).
- Modelos: ~32 MB no pacote distribuído (pasta `models/v6` + classificador v5 de ~1 MB) — aceitável para app local-first. Os três arquivos v5 não utilizados pelo preset foram removidos do pacote na M15 (decisão e evidência em §50.5); o classificador v5 permanece porque o preset `PPOCRv6Small` o carrega em runtime.
- O histórico da sessão é somente textual (decisão de produto da M15 para a V0.1) — a melhoria "Histórico de capturas com recuperação de imagem" está no backlog (§38.1) e não foi implementada.
- Os diagnósticos temporários `CAPPARSE_TRACE`/`CAPPARSE_NO_OVERLAY` permanecem no código: o fix do black-flash (overlay layered + revelação em dois frames) foi validado manualmente pelo usuário na M15 (ausência de flash preto e de janelas residuais reportada como OK); a remoção dos diagnósticos ainda exige justificativa documentada e autorização.
- O diálogo de erro de captura (falha na construção da sessão de captura) é um **risco residual** da V0.1 (M16-4): o caminho foi inspecionado por código (a guarda `_captureInFlight` é resetada no `catch`, então uma falha não deixa o app bloqueado; o diálogo amigável oferece retry), mas **não há gatilho reproduzível confirmado** para exercitá-lo — nem teste automatizado nem validação manual o cobrem.
- A coleção M7-C é sintética (anti-aliasing em cinza, 96 DPI): não representa capturas reais (ClearType, escalas, DPI ≠ 100%).
- O clone obsoleto `C:\Dev\CapParse` continua existindo em disco com 43 arquivos M7 staged e docs da auditoria — risco de trabalho acidental; arquivá-lo quando o trabalho documental estiver consolidado aqui.
- O OCR a partir do pacote beta.2 embalado (single-file) não pôde ser validado headless na M15 (a captura exige seleção interativa): a validação de campo em computador externo fica a cargo do usuário com o novo pacote.

## 50.8 Fora do escopo atual (explícito)

- Lista completa: `product.md` §12 (non-goals do V0.1) e `AGENTS.md` §35 (funcionalidades futuras).
- Em particular: HDR, planilhas, IA, cloud, histórico persistente, outras plataformas, e qualquer milestone não explicitamente solicitado.

## 50.9 Como iniciar uma nova sessão

1. Ler `AGENTS.md` e esta seção.
2. Trabalhar somente em `C:\Dev\CapParseIntegI`; conferir `git status` / `git log` (HEAD deve ser `178c940` ou um descendente; o working tree deve refletir apenas o trabalho da sessão atual).
3. Iniciar o contador de imagens da sessão (política de Vision, `AGENTS.md` §39 / §54) em `0`.
4. Executar `dotnet build` e `dotnet test` (suíte atual: 363 testes).
5. Trabalhar apenas no milestone atual; não criar commit, tag ou push sem aprovação explícita.
6. Atualizar esta seção ao final da sessão (estado, evidências, pendências).

---

# 51. V0.2 — Fase 1: Fundação de tema e recursos compartilhados

Sessão de implementação da fundação visual da V0.2 (tema claro/escuro, preferência de tema, recursos WPF compartilhados). Sem commit nesta sessão; o trabalho anterior da M16 (M16-1/M16-2/M16-4, 4 arquivos) permanece intacto no working tree e não foi modificado.

## 51.1 Escopo executado

- Sistema de temas claro/escuro com tokens semânticos centralizados;
- Preferência de tema (`System`/`Light`/`Dark`) persistida no `settings.json` existente, com compatibilidade de legado e fallback seguro;
- Detecção do tema de aplicativos do Windows (`AppsUseLightTheme`) com fallback seguro;
- Serviço que aplica o tema aos recursos da aplicação e o troca sem reinício;
- Testes automatizados da lógica nova (resolução, fallback, persistência, troca de dicionários);
- Avaliação de biblioteca de UI WPF (abaixo).

Fora do escopo (preservado): nenhum redesign de janelas, nenhuma mudança no overlay/captura, nenhum ícone `.ico`, nenhuma mudança no pipeline de OCR, `CAPPARSE_TRACE`/`CAPPARSE_NO_OVERLAY` intactos, versão/metadados de release intactos.

## 51.2 Decisão de estilização: recursos WPF locais, sem biblioteca externa

Candidatas avaliadas (fontes oficiais/atuais):

- **WPF nativo (`ResourceDictionary`)**: zero dependências, controle total da identidade visual, impacto nulo no pacote distribuído.
- **WPF-UI (lepoco/wpfui)**: mantida ativamente (v4.x no NuGet; usada pelo PowerToys; colaboração declarada com a equipe WPF do .NET). Porém ela **reescreve os controles base** (Page, ToggleButton, List, chrome de janela via `FluentWindow`), impõe o design language Fluent da biblioteca e aumenta o pacote — desproporcional para 3 janelas pequenas, e em conflito com a decisão de produto "Modern Fluent **com identidade própria**".
- **MahApps.Metro**: madura (fundação .NET) mas estilo Metro (não Fluent) e chrome de janela próprio; mesma objeção de desproporção.
- **ModernWpf**: temas estilo UWP/WinUI; mesma categoria de objeção.
- **Tema Fluent nativo do WPF**: existe a partir do **.NET 9** (`ThemeMode`); o projeto é **.NET 8** (tecnologia fixada em `AGENTS.md`), e subir o TFM é uma mudança de arquitetura que exige aprovação explícita — fora do escopo desta fase.

**Decisão: recursos WPF locais** (`ResourceDictionary` em `App` + dicionários de tema). Justificativa: o benefício de uma biblioteca (centenas de controles restilizados) não é proporcional ao tamanho real da UI (3 janelas, poucos controles), a decisão de produto pede identidade própria, o app é local-first e o pacote self-contained já tem ~99 MB — nenhuma dependência nova entra na fundação. Reavaliar somente se a V0.2 crescer a superfície de controles o suficiente para justificar.

## 51.3 Arquitetura implementada

```
ThemePreference (Configuration; persistida)      System | Light | Dark
        │  ThemeResolver (puro, fonte injetada)
        ▼
ResolvedTheme (Themes; estado concreto)          Light | Dark
        │  ThemeService.Apply
        ▼
Application.Resources.MergedDictionaries ← Themes/Theme.Light.xaml | Theme.Dark.xaml
```

Novos arquivos:

- `src/CapParse/Configuration/ThemePreference.cs` — enum da preferência (`System`, `Light`, `Dark`).
- `src/CapParse/Themes/ResolvedTheme.cs` — enum do estado resolvido (`Light`, `Dark`).
- `src/CapParse/Themes/ThemeResolver.cs` — resolução pura; a fonte do tema do sistema é injetada (`Func<ResolvedTheme>`), o que torna os testes determinísticos; uma preferência forçada nunca consulta o sistema.
- `src/CapParse/Themes/WindowsSystemTheme.cs` — lê `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme` (o valor que o Windows grava para "Apps use light theme"; não existe API dedicada para a preferência de apps, e o atributo dark por janela descreve janelas individuais, não a preferência do sistema). Chave ausente, valor inesperado ou exceção → fallback seguro para `Light` (estado de Windows anteriores ao dark mode). A interpretação do valor bruto (`Interpret`) é pura e testada; a leitura do registro em si depende da máquina e fica na validação manual.
- `src/CapParse/Themes/ThemeService.cs` — resolve a preferência e troca o dicionário de tokens em `Application.Resources.MergedDictionaries`; uma instância única no `App` é o único mecanismo de mudança de tema. A troca re-estiliza ao vivo as janelas que consomem os tokens (sem reinício).
- `src/CapParse/Themes/Theme.Light.xaml` / `Theme.Dark.xaml` — os 16 tokens semânticos (`BackgroundBrush`, `SurfaceBrush`, `SurfaceSecondaryBrush`, `BorderBrush`, `BorderStrongBrush`, `TextPrimaryBrush`, `TextSecondaryBrush`, `TextMutedBrush`, `AccentBrush`, `OnAccentBrush`, `AccentHoverBrush`, `AccentPressedBrush`, `SuccessBrush`, `WarningBrush`, `ErrorBrush`, `FocusBorderBrush`), com os dois dicionários definindo exatamente as mesmas chaves (invariante testada). Os textos atingem contraste WCAG AA (4.5:1) sobre `SurfaceBrush` e `BackgroundBrush` nos dois temas; `OnAccentBrush` atinge 4.5:1 sobre `AccentBrush`.

Alterações:

- `Configuration/AppSettings.cs` — nova propriedade `ThemePreference` (padrão `System`). No arquivo: string `"system"|"light"|"dark"`. Arquivo legado sem a chave → `System` (a única leitura que não muda a aparência de quem atualiza); valor inválido → fallback `System` **sem invalidar o resto do arquivo** (o hotkey e a flag de primeira execução sobrevivem a um valor corrompido de tema). `Version` do arquivo permanece 1 (campo opcional; apps antigos ignoram a chave desconhecida ao ler).
- `App.xaml.cs` — cria o `ThemeService` (resolver com a fonte real do Windows) e aplica `_settings.ThemePreference` no `OnStartup`, antes de qualquer janela ser criada; novo método público `ApplyThemePreference(preference)` — persistir + aplicar — é a única via centralizada para mudar o tema (a fase da `SettingsWindow` chamará este método; nenhum seletor de UI foi construído nesta fase).

## 51.4 Testes

24 novos testes (suíte: 333 → 357):

- `ThemeResolverTests` (5): `System`→claro, `System`→escuro, `Light`/`Dark` forçadas independentes da fonte, fonte nunca consultada para preferência forçada.
- `WindowsSystemThemeTests` (5): interpretação de `AppsUseLightTheme` — 1→claro, 0→escuro, nulo (chave ausente)→claro, 2 e -1 (inválidos)→claro.
- `ThemeServiceTests` (5): `Apply` com luz/escuro forçados e com `System` (fonte injetada) mescla o dicionário correto em `Application.Resources`; uma segunda aplicação substitui o dicionário anterior (nunca dois dicionários de tema coexistem); os dois dicionários definem exatamente os mesmos 16 tokens.
- `AppSettingsTests` (+9): arquivo legado sem a preferência → `System`; round-trip das 3 preferências (valor serializado incluído); valor inválido → fallback `System` preservando hotkey e `FirstRunPresented`; parsing case-insensitive com whitespace.

Nota de teste (WPF fora do app): o host de teste não garante que o esquema `pack:` seja registrado no `UriParser` antes do primeiro uso, como um app WPF real faz no bootstrap. Os `ThemeServiceTests` registram o esquema no construtor estático da classe (a mesma chamada que o WPF faz: `UriParser.Register(new GenericUriParser(GenericUriParserOptions.GenericAuthority), "pack", -1)`) e compartilham uma única `Application` e um único `ThemeService` estáticos — espelhando o app, que tem exatamente um serviço para a vida do processo.

## 51.5 Evidências de build e testes

- `dotnet build` + `dotnet test` em **Debug** — 0 erros, **357/357 aprovados**.
- `dotnet build -c Release` + `dotnet test -c Release` — 0 erros, **357/357 aprovados**.
- Apenas o aviso preexistente xUnit2020 em `OcrCollectionGeneratorTests.cs`; nenhum aviso novo.
- **Validação manual pendente (usuário)**: comportamento real dos dois temas em execução (o `System` seguindo a preferência do Windows; a troca sem reinício quando o seletor existir na fase das configurações) e a leitura do registro `AppsUseLightTheme` na máquina de teste — a leitura do registro não é coberta por teste automatizado (depende da máquina).

## 51.6 Pendências para as próximas fases da V0.2

- Seletor de tema na `SettingsWindow` (chamará `App.ApplyThemePreference`);
- Migração visual da `MainWindow` (título alinhado à esquerda incluído nessa fase), `SettingsWindow` e `FirstRunWindow` para os tokens;
- Asset `.ico` derivado da identidade visual (fase própria);
- Reavaliação de biblioteca de UI somente se a superfície de controles crescer o suficiente (decisão em §51.2).

# 52. V0.2 — Fase 2: MainWindow integrada ao sistema de temas

Sessão de implementação da integração visual da `MainWindow` com os tokens de tema da Fase 1 (direção Modern Fluent). Sem commit nesta sessão; o trabalho da M16 e da Fase 1 permanece intacto no working tree.

## 52.1 Escopo executado

- `MainWindow` migrada de cores fixas para os tokens semânticos (`DynamicResource` em tudo que responde à troca de tema em tempo de execução);
- Estilos de controle mínimos, definidos **no escopo da janela** (`Window.Resources`) — `SettingsWindow` e `FirstRunWindow` mantêm a aparência atual e serão migradas em fases próprias;
- Estilização de `Button` (neutro + variante de destaque `AccentButtonStyle`), `TextBox` (área somente-leitura), `ListBox`/`ListBoxItem`, `ProgressBar` (indeterminado) e `ScrollBar` (fino, sem botões de seta), com estados normal, hover, pressed, focus (anel substituindo o foco pontilhado do WPF) e disabled;
- Composição modernizada: título alinhado à esquerda (decisão de §51.6), painéis com superfície `SurfaceBrush`, borda `BorderBrush` e cantos arredondados, cabeçalhos de seção (`Resultado`, `Histórico da sessão`), espaçamentos e tipografia consistentes;
- Nenhum novo recurso funcional: apresentação do resultado, histórico de sessão, copiar texto/imagem, salvar imagem, copiar do histórico, feedbacks, estado vazio e indicador de processamento permanecem idênticos (mesmos `x:Name`, mesmos handlers, mesma lógica no code-behind — `MainWindow.xaml.cs` não foi modificado);
- Sem dependências novas (decisão de §51.2 mantida).

## 52.2 Tokens

Os 16 tokens da Fase 1 foram complementados com 3 novos (necessidade real dos estados de interação; presentes nos dois dicionários, mantendo a invariante de chaves idênticas testada em `ThemeServiceTests`):

- `SubtleOverlayBrush` — overlay de hover (light `#1F000000` / dark `#1FFFFFFF`);
- `SubtlePressedBrush` — overlay de pressionado (light `#33000000` / dark `#33FFFFFF`);
- `AccentSubtleBrush` — tinta de seleção de itens de lista (light `#1F2E6BE6` / dark `#3C5B93FF`).

Os overlays são de alpha baixo de propósito: compõem sobre a superfície base (botão neutro, botão de destaque, item de lista) sem exigir uma variante por superfície. Total: 19 tokens por tema.

## 52.3 Arquivos

- `src/CapParse/MainWindow.xaml` — reescrita (tokens + estilos + composição);
- `src/CapParse/Themes/Theme.Light.xaml` / `Theme.Dark.xaml` — 3 tokens novos;
- `tests/CapParse.Tests/ThemeServiceTests.cs` — lista esperada de tokens expandida de 16 para 19.

## 52.4 Decisões de implementação

- **Estilos no escopo da janela**: um dicionário de estilos compartilhado em `App.xaml` restilizaria também `SettingsWindow` e `FirstRunWindow` (fora do escopo desta fase). Em `Window.Resources`, a migração fica contida na `MainWindow`; as outras janelas consomem os mesmos tokens na sua própria fase.
- **Um template de botão para as duas variantes**: o template neutro usa overlay de hover/pressed; a variante de destaque reusa o mesmo template com preenchimento `AccentBrush`/`OnAccentBrush` (o overlay transparente também funciona sobre o preenchimento de destaque).
- **`ScrollBar` fino sem setas**: template próprio com `Track` (`PART_Track`), polegar arredondado `BorderStrongBrush` e o truque clássico do WPF — template vertical rotacionado `-90°` para a orientação horizontal. Rolar continua por roda do mouse; os botões de página da trilha (`PageUp`/`PageDown`) permanecem.
- **`ProgressBar` indeterminado próprio**: trilha `SurfaceSecondaryBrush` e barra `AccentBrush` varrendo a trilha (animação `TranslateTransform`), substituindo o template padrão que usava `SystemColors`.
- **Foco visível**: `FocusVisualStyle` nula nos controles restilizados + anel externo `FocusBorderBrush` no template (botões, itens de lista, áreas de texto).

## 52.5 Evidências de build e testes

- `dotnet build` + `dotnet test` em **Debug** — 0 erros, **357/357 aprovados**.
- `dotnet build -c Release` + `dotnet test -c Release` — 0 erros, **357/357 aprovados**.
- Apenas o aviso preexistente xUnit2020 em `OcrCollectionGeneratorTests.cs`; nenhum aviso novo.

## 52.6 Validação visual

- **Executável (tema dark, via `System`)**: o build Release atual foi executado na máquina (preferência do usuário: arquivo legado → `System`; `AppsUseLightTheme=0` → dark). Captura da janela em tela (960×560) confirmou por histograma de cores o dicionário dark completo: fundo `1A1D23`, painéis `22262E`, áreas `2A2F39`, bordas `39404C`, texto `F1F3F6`, botão de destaque desabilitado `3C578C` (= `AccentBrush` a 45% sobre a superfície — exato). Nenhum branco de controle sem tema.
- **Render offscreen dos dois temas**: um harness descartável (fora do repositório, `%TEMP%\CapParseThemeHarness`) instanciou o `App` + `ThemeService` reais, forçou cada tema e renderizou a `MainWindow` para PNG (`RenderTargetBitmap` sobre o conteúdo da janela; a janela nunca é exibida, nenhum `OnStartup`, nenhum contato com o `settings.json` do usuário). Histogramas: light → `F3F4F6`/`FFFFFF`/`E9EBEF`/`D6DAE0`/`1A1E24`/`464E58`/destaque desabilitado `A1BCF3` (exato); dark → idêntico ao do executável.
- PNGs de referência: `%TEMP%\capparse-window-dark.png` (executável) e `%TEMP%\capparse-render-light.png` / `%TEMP%\capparse-render-dark.png` (render offscreen).
- **Limitações (não validadas)**: a inspeção visual humana (o agente não vê imagens — os histogramas são a evidência objetiva); os estados interativos (hover, pressed, anel de foco) existem nos templates, mas não foram exercitados por interação real; a troca de tema em tempo de execução pela `SettingsWindow` não existe ainda (seletor pendente).

## 52.7 Pendências para as próximas fases da V0.2

- Seletor de tema na `SettingsWindow` (chamará `App.ApplyThemePreference`) — **concluído na fase 3** (§53);
- Migração visual da `SettingsWindow` (fase 3, §53) e da `FirstRunWindow` (fase 4, §55) para os tokens — **concluídas**;
- Asset `.ico` derivado da identidade visual (fase própria);
- Validação humana dos dois temas e dos estados interativos (hover/foco) no executável.

# 53. V0.2 — Fase 3: SettingsWindow + seletor de tema

Sessão de implementação da migração visual da `SettingsWindow` para os tokens de tema e do seletor de tema (Sistema/Claro/Escuro) com persistência e troca imediata. Sem commit nesta sessão; o trabalho da M16, da Fase 1 e da Fase 2 permanece intacto no working tree.

## 53.1 Escopo executado

- `SettingsWindow` migrada de cores fixas para os tokens semânticos (`DynamicResource` em tudo que responde à troca de tema em tempo de execução); fundo da janela agora `BackgroundBrush` (antes, o branco padrão do WPF);
- Composição em dois painéis com a linguagem visual da `MainWindow` (fase 2): `SurfaceBrush` + `BorderBrush` + cantos arredondados, cabeçalhos de seção (`Atalho de captura`, `Tema`), título alinhado à esquerda;
- Estilos de controle no escopo da janela (`Window.Resources`), espelhando os da `MainWindow`: `Button` (neutro + `AccentButtonStyle` — o botão `Salvar` do estado de confirmação passou a ser o de destaque), `ListBox`/`ListBoxItem` (hover, seleção com tinta `AccentSubtleBrush`, anel de foco, disabled) e `PaneHeaderStyle`;
- Seletor de tema: `ListBox` com três itens (`Sistema` — acompanha o tema do Windows; `Claro` — força o claro; `Escuro` — força o escuro), cada um com nome + descrição curta;
- Ao abrir, o seletor reflete a **preferência persistida** (novo `App.SavedThemePreference`), não o tema resolvido: com preferência `System` num Windows escuro, a seleção exibida continua sendo `Sistema`;
- Ao selecionar, `App.ApplyThemePreference(preference)` persiste e aplica o tema imediatamente (sem reinício), usando o mecanismo existente (`ThemeService`); a fonte de verdade é única (`AppSettings.ThemePreference`);
- A janela de `FirstRunWindow` e o overlay não foram tocados; pipeline de captura/OCR, bandeja, hotkeys e copiar/salvar inalterados (`SettingsWindow.xaml.cs`: toda a lógica do atalho — estados, handlers, recuperação de hotkey — permanece idêntica).

## 53.2 Decisões de implementação

- **`ApplyThemePreference` agora retorna `bool`** (o resultado da gravação do arquivo): a `SettingsWindow` segue exatamente o padrão de erro do fluxo de hotkey — a preferência fica em memória e o tema é aplicado; se o arquivo não pôde ser escrito, um aviso informa que a escolha vale para a sessão e volta ao valor salvo na próxima inicialização. UI, memória e arquivo nunca fingem sucesso de gravação.
- **Mapeamento direto índice↔enum**: os itens do seletor estão na ordem do enum (`System=0, Light=1, Dark=2`), então o `SelectedIndex` é a preferência; o constructor posiciona o seletor atrás de uma flag `_initializing` para que a inicialização não conte como mudança do usuário (nenhuma gravação ao abrir).
- **Estilos no escopo da janela** (mesma decisão da fase 2): nenhum estilo foi extraído para `App.xaml`, de modo a não alterar a aparência de nenhuma outra janela.
- **`ListBox` com rolagem desabilitada**: três itens, sem scrollbar — o template fino da `MainWindow` não é necessário aqui.

## 53.3 Arquivos

- `src/CapParse/SettingsWindow.xaml` — reescrita (tokens + estilos + painel de tema + seletor);
- `src/CapParse/SettingsWindow.xaml.cs` — seletor: leitura da preferência ao abrir, `ThemeSelector_SelectionChanged` (persistir + aplicar + aviso em falha de gravação);
- `src/CapParse/App.xaml.cs` — `ApplyThemePreference` passa a retornar o resultado da persistência; nova propriedade `SavedThemePreference`;
- `tests/CapParse.Tests/ThemeServiceTests.cs` — novo teste: todo `DynamicResource` referenciado pelo `SettingsWindow.xaml` existe nos dois dicionários de tema (invariante da troca ao vivo).

## 53.4 Evidências de build e testes

- `dotnet build` + `dotnet test` em **Debug** — 0 erros, **358/358 aprovados** (357 anteriores + 1 novo).
- `dotnet build -c Release` + `dotnet test -c Release` — 0 erros, **358/358 aprovados**.
- Apenas o aviso preexistente xUnit2020 em `OcrCollectionGeneratorTests.cs`; nenhum aviso novo.
- Leitura/mapeamento/persistência das três preferências: já coberta pela Fase 1 (`AppSettingsTests` — round-trip, legado, fallback, case-insensitive) e pelo teste novo desta fase (recursos da janela × dicionários).

## 53.5 Validação visual

- **Render offscreen dos dois temas** (harness descartável em `%TEMP%\CapParseThemeHarness`, estendido nesta fase): instanciou o `App` + `ThemeService` reais, forçou cada tema e renderizou a `SettingsWindow` para PNG (`RenderTargetBitmap` sobre o conteúdo; janela nunca exibida, nenhum `OnStartup`, nenhum contato de escrita com o `settings.json` do usuário — hash do arquivo verificado idêntico antes e depois). Histogramas: light → `F3F4F6`/`FFFFFF`/`E9EBEF`/`D6DAE0`/`1A1E24`/`464E58`/`5F6771` + tinta de seleção `E6EDFC` (exata); dark → `1A1D23`/`22262E`/`2A2F39`/`39404C`/`F1F3F6`/`7D8491`/`8B93A0` + `2F405F` (exata). Nenhum branco de controle sem tema.
- **Troca com a janela aberta**: um único `SettingsWindow` renderizado com light, o dicionário trocado para dark ao vivo e renderizado de novo — contagens de pixels idênticas entre os dois renders (mesmo layout, paleta trocada), evidência objetiva de re-tematização sem reinício.
- **Preferência refletida ao abrir**: o seletor exibiu `Sistema` selecionado (a preferência real do arquivo de teste), não o tema resolvido.
- **Estados de seleção por opção**: renders com cada item (0/1/2) selecionado mostram a tinta `AccentSubtleBrush` apenas no item ativo.
- **`MainWindow` preservada**: o render dark continua idêntico à baseline da fase 2 (`1A1D23`/`22262E`/`2A2F39`/`39404C`/`F1F3F6`/`3C578C`).
- PNGs de referência: `%TEMP%\settings-{light,dark}-natural.png`, `%TEMP%\settings-swap-{light,dark}.png`, `%TEMP%\settings-dark-sel{0,1,2}.png`, `%TEMP%\MainWindow-{light,dark}.png`.
- **Limitações (não validadas)**: inspeção visual humana (o agente não vê imagens — os histogramas são a evidência objetiva); estados interativos reais (hover, pressed, anel de foco) não foram exercitados por interação; o fluxo real bandeja → `Configurações` no executável e a gravação efetiva do arquivo ao trocar a preferência não foram exercitados nesta sessão (o harness é somente-leitura por construção; o mecanismo de gravação é o mesmo `AppSettings.TrySave` coberto por teste).

## 53.6 Pendências para as próximas fases da V0.2

- Migração visual da `FirstRunWindow` para os tokens (mesma abordagem) — **concluída na fase 4** (§55);
- Asset `.ico` derivado da identidade visual (fase própria);
- Validação humana dos dois temas, dos estados interativos (hover/foco) e do seletor no executável.

---

# 54. Política permanente: Vision do agente de desenvolvimento

Política permanente para as sessões do agente de desenvolvimento (Pi Coding Agent com Swift 1.5 Qwen 3.8-27B via NInfer, com Vision habilitada). Aplica-se a todas as sessões a partir desta, independentemente do milestone. O resumo curto está em `AGENTS.md` §39; esta seção é a fonte autoritativa dos detalhes operacionais.

## 54.1 Escopo da política

Vision está **permitido e incentivado quando traz valor real à tarefa**.

Casos recomendados:

- inspecionar capturas de tela da `MainWindow`, `SettingsWindow`, `FirstRunWindow` e do overlay;
- avaliar hierarquia visual, alinhamento, espaçamento, legibilidade, contraste e consistência;
- comparar resultados visuais de Light e Dark;
- verificar visualmente estados e problemas de interface que testes automatizados ou análise de pixels não conseguem avaliar adequadamente.

O agente usa Vision livremente dentro do limite estabelecido, sem pedir autorização para cada imagem.

Vision não é obrigatório em tarefas puramente lógicas, documentais ou de backend quando imagens não acrescentam informação útil.

## 54.2 Limite rígido: 12 imagens por sessão

**O agente pode inspecionar no máximo 12 imagens por sessão de trabalho. Nunca ultrapasse esse limite.**

Regras operacionais:

1. Comece cada nova sessão com contador de imagens em `0`.
2. Antes de enviar qualquer imagem para análise, confira o contador.
3. Incremente o contador quando uma imagem for enviada para análise.
4. Ao chegar a 12, interrompa toda inspeção visual adicional naquela sessão.
5. Não contorne o limite agrupando imagens, usando mosaicos, spritesheets, colagens ou múltiplas imagens em um único envio. Conte as imagens-fonte distintas que compõem o material visual enviado.
6. Não reinicie o contador durante a mesma sessão por compactação de contexto, mudança de tarefa, troca de modelo ou reinicialização do agente.
7. Somente uma sessão de trabalho realmente nova inicia um novo contador.

Contagem: cada imagem distinta enviada ao modelo para análise visual conta 1. Se a mesma imagem for enviada novamente para uma nova análise, conte novamente, de forma conservadora. Imagens apenas geradas ou salvas localmente, que nunca tenham sido enviadas ao modelo, não contam.

O teto de 12 é deliberadamente inferior ao limite de 16 imagens que o usuário informa para o Qwen 3.8-27B: a compatibilidade desse limite do modelo com Swift 1.5 não está comprovada, então a regra não depende dela. O teto de 12 do projeto é a restrição operacional autoritativa.

## 54.3 Uso eficiente

Além do teto:

- Prefira inspecionar imagens representativas, de alto valor diagnóstico.
- Evite capturar novamente a mesma janela sem uma mudança visual relevante.
- Não entre em ciclos repetidos de analisar, capturar e analisar sem uma hipótese ou objetivo concreto.
- Agrupe a inspeção visual por etapa quando possível, respeitando o limite por imagem-fonte.
- Use testes automatizados, logs, diffs, acessibilidade, análise de cores e histogramas como evidências complementares.
- Se não for possível usar Vision ou transmitir imagens ao modelo, declare essa limitação explicitamente. Nunca afirme ter feito uma inspeção visual que não aconteceu.
- Não considere um histograma de pixels equivalente à avaliação visual humana/modelada de espaçamento, composição e qualidade estética.

## 54.4 Aplicação nas próximas tarefas

Quando a tarefa envolver interface gráfica, o agente deve considerar proativamente a inspeção visual, reservando imagens para:

1. estado inicial relevante;
2. resultado implementado;
3. comparação entre temas ou estados, se necessário;
4. regressões visuais importantes.

A quantidade efetivamente utilizada deve ser proporcional à complexidade. Não é necessário consumir as 12 imagens em toda sessão.

## 54.5 Relação com os registros anteriores

As notas de sessões anteriores que registram "o agente não vê imagens" (ex.: §52.6 e §53.5) descrevem sessões em que a capacidade de Vision não estava habilitada. Elas são registro histórico, não regra permanente. A partir desta política, a inspeção visual está disponível ao agente dentro do limite de 12 imagens por sessão.

---

# 55. V0.2 — Fase 4: Modernização visual da FirstRunWindow

Sessão de implementação da migração visual da `FirstRunWindow` para os tokens de tema (direção Modern Fluent), a última das três janelas a ser modernizada. Sem commit nesta sessão; o trabalho da M16 e das fases 1–3 da V0.2 permanece intacto no working tree.

## 55.1 Escopo executado

- `FirstRunWindow` migrada de cores padrão do WPF (fundo branco, texto preto, botão com chrome clássico) para os tokens semânticos (`DynamicResource` em tudo que responde à troca de tema em tempo de execução); fundo da janela agora `BackgroundBrush`;
- Composição alinhada à linguagem visual da `MainWindow` (fase 2) e da `SettingsWindow` (fase 3): título alinhado à esquerda (20/SemiBold/`TextPrimaryBrush`), conteúdo em um único painel (`SurfaceBrush` + `BorderBrush` + cantos arredondados, respiro interno 16), margem da grade 20 (antes 28);
- Tipografia do corpo em 13 pt com hierarquia: a frase de propósito em `TextPrimaryBrush`; as linhas de instrução (como capturar — incluída a linha dinâmica `HowToCaptureText` — e o que acontece depois) em `TextSecondaryBrush`;
- Estilos de controle no escopo da janela (`Window.Resources`), espelhando os das outras duas janelas: `Button` (neutro + `AccentButtonStyle`) — o botão `Começar` (única ação primária da janela) passou a ser o de destaque, como o `Salvar` da `SettingsWindow`;
- Comportamento funcional intacto: mesmo título/janela (520×300, `NoResize`, fora da taskbar, centralizada), mesmo `StartButton` (`x:Name`, `Click=StartButton_Click`, `IsDefault=True`, `MinWidth=130`), mesma `HowToCaptureText` preenchida no constructor, mesmo fechamento pelo botão ou pelo close; `FirstRunWindow.xaml.cs` **não foi modificado**;
- Nenhum novo recurso global: os estilos permanecem no escopo da janela (mesma decisão das fases 2–3) e os tokens consumidos são exatamente os 19 compartilhados — nenhum dicionário ou paleta nova.

## 55.2 Arquivos

- `src/CapParse/FirstRunWindow.xaml` — reescrita (tokens + estilos no escopo da janela + painel de conteúdo);
- `tests/CapParse.Tests/ThemeServiceTests.cs` — novo teste: todo `DynamicResource` referenciado pelo `FirstRunWindow.xaml` existe nos dois dicionários de tema (o auxiliar de leitura do XAML foi generalizado para os dois testes de janela);
- `docs/development.md` — este registro + atualização da continuidade (§50).

Fora de escopo (preservado): `FirstRunWindow.xaml.cs`, `App.xaml.cs`, os dicionários de tema, `MainWindow`, `SettingsWindow`, overlay/captura, OCR, tray, hotkeys, `settings.json` do usuário, metadados de versão.

## 55.3 Decisões de implementação

- **Painel único, sem cabeçalho de seção**: a `FirstRunWindow` tem uma única seção (a apresentação), então o título da janela faz o papel de cabeçalho — o painel não repete um `PaneHeader`; mantém a apresentação curta e simples que o `ui.md` §23 exige.
- **Botão de destaque**: `Começar` é a única ação primária da janela (e a que encerra a apresentação) — usa `AccentButtonStyle`, o mesmo tratamento dado à ação primária das outras janelas; `MinWidth=130` preservado (geometria validada na M14).
- **Linhas de instrução em `TextSecondaryBrush`**: hierarquia propósito → detalhes, coerente com o uso de `TextSecondaryBrush` para textos complementares nas outras janelas; contraste WCAG AA mantido (tokens já validados na fase 1).

## 55.4 Evidências de build e testes

- `dotnet build` + `dotnet test` em **Debug** — 0 erros, **359/359 aprovados** (358 anteriores + 1 novo).
- `dotnet build -c Release` + `dotnet test -c Release` — 0 erros, **359/359 aprovados**.
- Apenas o aviso preexistente xUnit2020 em `OcrCollectionGeneratorTests.cs`; nenhum aviso novo.
- `git diff --check` sem erros; diff revisado — apenas `FirstRunWindow.xaml` (arquivos rastreados) e `ThemeServiceTests.cs` (arquivo não rastreado das fases 1–3) mudaram nesta sessão; o trabalho local de M16 e das fases 1–3 permanece intacto.

## 55.5 Validação visual

- **Render offscreen dos dois temas** (harness descartável em `%TEMP%\CapParseThemeHarness`, estendido nesta fase com os modos `firstrun` e `firstrun-swap`): instanciou o `App` + `ThemeService` reais, forçou cada tema e renderizou a `FirstRunWindow` para PNG (520×300; janela nunca exibida, nenhum `OnStartup`). Histogramas: light → `FFFFFF`/`F3F4F6`/`E8EAED`/`D6DAE0`/`1A1E24`/`464E58` + acento `2E6BE6` (exatos); dark → `22262E`/`1A1D23`/`252A33`/`39404C`/`F1F3F6`/`B9C0CA` + acento `5B93FF` (exatos). Nenhum controle sem tema (sem preto/branco padrão do WPF nos renders).
- **Troca com a janela aberta**: um único `FirstRunWindow` renderizado com light, o dicionário trocado para dark ao vivo e renderizado de novo — contagens de pixels idênticas entre os dois renders (mesmo layout, paleta trocada), evidência objetiva de re-tematização sem reinício.
- **Geometria invariante**: bounding box do botão de destaque medida no render — `x=37, y=232, 130×31` nos dois temas (37 = margem 20 + borda 1 + respiro 16 do painel; 130 = `MinWidth` preservado da M14).
- **Regressão do harness**: o modo `swap` da fase 3, refatorado nesta fase, continua produzindo renders idênticos às baselines (`settings-swap` ≡ `settings-*-natural` por tamanho de arquivo e paleta).
- **`settings.json` do usuário intocado**: SHA-256 idêntico antes e depois de todos os runs do harness (o harness é somente-leitura por construção).
- PNGs de referência: `%TEMP%\firstrun-{light,dark}.png`, `%TEMP%\firstrun-swap-{light,dark}.png`.
- **Limitações (não validadas)**: **a inspeção visual por Vision não pôde ser realizada nesta sessão** — o modelo da sessão (`swift-1.5-qwen3.8-27b` via `ninfer-swift`) não aceita imagens; conforme a política (`AGENTS.md` §39 / §54), isso é declarado explicitamente e os histogramas/medições acima são evidência objetiva complementar, **não** substituto da avaliação visual de espaçamento/composição; estados interativos reais (hover, pressed, anel de foco) não foram exercitados por interação; o fluxo real de primeira execução no executável (flag re-armada, janela exibida) não foi exercitado nesta sessão — a validação manual do usuário permanece pendente.

## 55.6 Pendências para as próximas fases da V0.2

- Asset `.ico` derivado da identidade visual (fase própria);
- Validação humana dos dois temas, dos estados interativos (hover/foco), do seletor e da apresentação de primeira execução no executável (usuário);
- Commit do trabalho local (M16 + V0.2 fases 1–4) com aprovação explícita do usuário.

---

# 56. V0.2 — Fase 5: Modernização visual do overlay de captura

Sessão de implementação da modernização visual do overlay de captura (direção Modern Fluent), a última superfície visual da V0.2. Sem commit nesta sessão; o trabalho da M16 e das fases 1–4 da V0.2 permanece intacto no working tree.

## 56.1 Escopo executado

- **Destaque da seleção por corte do escurecimento**: a camada de escurecimento passou de um `Rectangle` de tinta preta uniforme para um `Path` cuja geometria (`GeometryGroup` com `FillRule.EvenOdd`) cobre a superfície inteira e carrega um furo exatamente sobre a seleção — a área selecionada exibe o conteúdo congelado a **brilho pleno** (o destaque), e o restante continua escurecido. O tratamento anterior (seleção escurecida como o resto + preenchimento branco a 10%) deixava a área de seleção praticamente indistinguível do entorno;
- **Borda da seleção em acento**: a borda de 1px branco fixo (invisível sobre conteúdo claro) foi substituída por uma borda de 2px na cor de acento do tema ativo (`AccentBrush` — `#2E6BE6` claro / `#5B93FF` escuro), consistente com a identidade das janelas modernizadas nas fases 2–4;
- **Escurecimento por tema**: novo token semântico `OverlayDimBrush` nos dois dicionários (light `#80000000` = 50% preto, como antes; dark `#66000000` = 40% preto — em telas já escuras o escurecimento de 50% esmagava o conteúdo congelado; o valor menor mantém "congelado, mas reconhecível" nos dois temas);
- **Tokens do tema no overlay**: o overlay é composto em código (não XAML), então os pincéis são resolvidos em `Application.Current.TryFindResource` na criação da janela (o overlay é criado por captura, portanto sempre reflete o tema ativo no início da captura), com fallback fixo (light) para ambientes sem `Application` (harnesses de teste);
- Testes: 4 novos puros de geometria do escurecimento + token incluído na invariante dos dicionários (`ThemeServiceTests`);
- **Preservado (verificado no diff)**: a revelação em dois frames (fix do flash preto — `RevealOnNextRenderFrame`/`OnRevealRendering`/`WaitForFirstRenderAsync`), o congelamento de tela, a captura `BitBlt`, as coordenadas físicas/DIP, a seleção (incluindo a que cruza monitores), o cancelamento por `Esc`, o DPI/multi-monitor, os diagnósticos `CAPPARSE_TRACE`/`CAPPARSE_NO_OVERLAY` e toda a lógica de construção assíncrona da sessão.

## 56.2 Arquivos

- `src/CapParse/UI/MonitorOverlayWindow.cs` — camada de escurecimento virou `Path` com geometria de furo (`BuildDimGeometry`, função pura testável), borda da seleção em `AccentBrush` 2px, resolução de tokens com fallback, construtor sem parâmetros;
- `src/CapParse/UI/FrozenOverlaySession.cs` — apenas a remoção da constante `DimOpacity` e o construtor sem parâmetros (nada mais);
- `src/CapParse/Themes/Theme.Light.xaml` / `Theme.Dark.xaml` — token `OverlayDimBrush` (ambos, mantendo a invariante de chaves idênticas);
- `tests/CapParse.Tests/MonitorOverlayWindowTests.cs` — novo (4 testes de `BuildDimGeometry`);
- `tests/CapParse.Tests/ThemeServiceTests.cs` — `OverlayDimBrush` na lista de tokens esperados (19 → 20 por tema);
- `docs/development.md` — este registro + atualização da continuidade (§50).

Fora de escopo (preservado): texto/instruções/chips no overlay (o overlay continua sem nenhum texto), janelas, OCR, tray, hotkeys, `settings.json` do usuário, metadados de versão, dependências.

## 56.3 Decisões de implementação

- **Corte no escurecimento (e não apenas uma borda melhor)**: a linha de produto da V0.2 pede "escurecimento com área selecionada destacada"; o destaque mais claro e minimalista possível é mostrar a própria tela congelada a brilho pleno dentro da seleção (o padrão dos snipping tools modernos). Não adiciona texto nem controles — apenas realoca a tinta do escurecimento.
- **`GeometryGroup` + `RectangleGeometry`**: o primitivo WPF para "grupo de geometrias com regra de preenchimento" (o `PathFigureCollection` do `PathGeometry` só aceita `PathFigure`, o que exigiria construir segmentos manualmente); o `Path` aceita qualquer `Geometry` como `Data`.
- **Geometria definida em `SetFrozenImage`**: a sessão dimensiona a janela (`Width`/`Height` em DIPs) antes de criar a imagem do monitor, então a superfície completa do escurecimento é conhecida nesse ponto e a primeira frame apresentada já contém imagem + escurecimento (a segurança da revelação em dois frames é a mesma de antes: até a revelação a superfície é transparente — imagem ausente e geometria nula não pintam nada).
- **Resolução de tokens na criação, não `DynamicResource`**: o overlay vive segundos (uma captura) e é recriado a cada captura; `DynamicResource` não se aplica a pincéis criados em código, e a resolução única no construtor é suficiente e mais simples.
- **Fallback fixo**: sem `Application` (harnesses), o overlay usa os valores do tema light — o caminho é exercitado pelos testes de vazamento (que constroem janelas reais sem `Application`).

## 56.4 Evidências de build e testes

- `dotnet build` + `dotnet test` em **Debug** — 0 erros, **363/363 aprovados** (359 anteriores + 4 novos); em **Release** — 0 erros, **363/363 aprovados**; apenas o aviso preexistente xUnit2020 em `OcrCollectionGeneratorTests.cs` — nenhum aviso novo.
- `git diff --check` sem erros; diff revisado: apenas os arquivos da §56.2 mudaram nesta sessão; o trabalho local de M16 e das fases 1–4 permanece intacto.

## 56.5 Validação visual (Vision validado nesta sessão — 11/12 imagens usadas)

- **Verificação prática do Vision**: imagem de teste gerada e inspecionada com sucesso na sessão (1 imagem) — a política de §54 se aplica plenamente a partir desta sessão.
- **Overlay real (executável, tema dark)** — o executável Release atual foi dirigido por um harness descartável (`SendInput`: hotkey `Ctrl+Shift+X` + arrasto sintético + `CopyFromScreen` do monitor primário; o `settings.json` do usuário não foi tocado — SHA-256 idêntico antes/depois):
  - **antes** (build anterior): repouso = desktop congelado escurecido a 50%, conteúdo reconhecível; seleção = borda branca de 1px + preenchimento branco a 10% sobre conteúdo **escurecido como o resto** — destaque fraco;
  - **depois** (build novo): repouso = escurecimento dark de 40% — o wallpaper congelado ficou visivelmente **mais reconhecível** (rosto, lanterna e detalhes do chapéu); seleção = **área a brilho pleno** (corte no escurecimento) com **borda de acento azul de 2px** — destaque nítido em ambos os estados;
  - em todos os checkpoints o processo do app estava vivo; o encerramento foi limpo; nenhum flash preto nem janela residual nos frames capturados;
- **Renders offscreen dos dois temas** (harness estendido com o modo `overlay`, imagem de desktop sintética): light — antes: escurecimento uniforme 50% + borda branca fina; depois: corte de brilho pleno + borda de acento `#2E6BE6` (nítida até sobre o painel branco do sample — o pior caso da borda branca anterior); dark — depois: corte + borda `#5B93FF` sobre escurecimento 40%.
- PNGs de referência: `%TEMP%\overlay-{before,after}-{rest,drag,result}.png` (executável, monitor primário 2560×1440) e `%TEMP%\overlay-{before,after}-{light,dark}-{idle,select}.png` (harness offscreen 960×540).
- **Limitações (não validadas)**: o driver sintético de mouse não produziu a seleção exata enviada (o retângulo visível apareceu deslocado/estreito e a seleção final resultou em cancelamento — a janela principal terminou ociosa, sem OCR) — artefato do driver (`SendInput` em monitor 133% + semântica de ativação de clique do WPF), **não** um defeito do aplicativo: a seleção real (incluindo multi-monitor/DPI) foi validada pelo usuário na M15; o tema light no overlay não foi exercitado no executável (preferência do usuário `System`→dark; alterar o `settings.json` é proibido) — coberto pelo render offscreen; o flash preto não foi validado por gravação de vídeo — a garantia continua sendo a lógica de revelação em dois frames **inalterada** (verificada no diff) + a ausência de flash nos frames capturados após a revelação; a validação humana do overlay (repouso/seleção/multi-monitor) permanece pendente com o usuário.

## 56.6 Pendências para as próximas fases da V0.2

- Asset `.ico` derivado da identidade visual (fase própria);
- Validação humana dos dois temas, dos estados interativos (hover/foco), do seletor, da primeira execução e do overlay no executável (usuário);
- Commit do trabalho local (M16 + V0.2 fases 1–5) com aprovação explícita do usuário.

## 57. P1 — Reconstrução de espaços no OCR + diagnóstico de memória (sessão 2026-10-14)

Trabalho **local, sem commit** (base: `bdfde50`, 4 commits à frente de
`origin/main` — checkpoint intacto; nenhum push/tag/release).

### 57.1 O que foi feito

- **P1 (espaços no OCR)**: o `RapidOcrEngine` passa a rodar com
  `ReturnWordBox=true` e reconstrói o texto de cada linha a partir das
  caixas de palavra com uma regra conservadora por união: o texto do
  modelo é a base (seus espaços preservados exatamente); um espaço é
  adicionado num limite entre boxes **apenas** quando o modelo não
  reportou espaço e o vão horizontal excede `0.5 × altura` (acima do
  intervalo de tiling medido no engine 4.2.0); fallback ao texto do
  modelo quando os boxes ausentam/falham ou não alinham com o texto;
  espaços de borda descartados. `OcrResult.Text` passa a ser o join dos
  blocos (contrato público inalterado). Detalhe técnico completo em
  `docs/ocr-integration.md` §11.
- **Descoberta central**: a heurística de referência MewuAI
  (`gap > 0.22 × altura`) foi implementada, medida e **rejeitada** —
  regressão total no M7-C (CER 0,069 → 0,107; exato 6/15 → 0/15), porque
  o RapidOcrNet 4.2.0 **tileia** as caixas de palavra/caractere (vanos de
  1–2 px em espaços reais de 8–10 px; indistinguíveis de vanos internos de
  token). O MewuAI usa o pacote 4.1.0 e consome as caixas para layout de
  seleção (glifos), não para reconstrução de texto — a regra não se
  transfere.
- **Resultado M7-C** (mesma coleção/referências; Debug ≈ Release):
  CER bruto 0,069 → **0,058**; CER frouxo 0,052 → 0,052; exato bruto
  6/15 → **8/15** (multi-line agora exatos brutos); exato frouxo
  10/15 → 10/15; inserções 14 → 1 (espaços de borda); **nenhum caso
  piorou**. Os baseline reports foram preservados em
  `tests/CapParse.Tests/TestData/Ocr/reports/baseline/`; os relatórios
  gerados pós-P1 estão em `reports/evaluation-{Debug,Release}.md`
  (working tree).
- **Diagnóstico de memória** (sem alterar a configuração do ONNX):
  WorkingSet 72 → 140 MB (init) → pico ~2,08 GB (1ª inferência, depois
  **plano** nas 17 inferências) → ~262 MB após `Dispose`. Hipótese
  dominante: **arena de CPU do ONNX** (`EnableCpuMemArena=true` via
  `InitModels(models)` com `SessionOptions` padrão), retida pelo lifetime
  da sessão — o app mantém o engine vivo por inteiro, explicando os
  1,2–1,4 GB em uso. Ranking de hipóteses + plano do experimento A/B
  (`EnableCpuMemArena=false; EnableMemoryPattern=false`, opt-in por
  env var, 3 repetições, processo limpo, Release) em
  `docs/ocr-integration.md` §11.4. **A configuração de produção não foi
  tocada nesta sessão.**

### 57.2 Arquivos

- `src/CapParse/OCR/RapidOcrEngine.cs` — `ReturnWordBox=true`;
  `ReconstructLineText` + helpers (`GapSpaceBetween`, `WordBoxRectangle`,
  `StripSpaces`, `IndexOfWordAt`); `MapResult` deriva `Text` dos blocos.
- `src/CapParse/OCR/OcrResult.cs` — apenas doc de `Text`.
- `tests/CapParse.Tests/WordTextReconstructionTests.cs` — novo (26 testes).
- `tests/CapParse.Tests/OcrResultMappingTests.cs` — 1 teste atualizado
  (contrato `Text` derivado dos blocos).
- `tests/CapParse.Tests/TestData/Ocr/reports/baseline/` — novos (baseline
  preservado); `reports/evaluation-{Debug,Release}.md` — regenerados pelo
  harness pós-P1.
- `docs/ocr-integration.md` — nova seção 11 (P1 completa);
  `docs/development.md` — este registro.

### 57.3 Evidências

- `dotnet build` + `dotnet test` **Debug**: 0 erros, **387/387**
  (361 pré-existentes + 26 novos); **Release**: 0 erros, **387/387**;
  apenas o aviso preexistente xUnit2020 (`OcrCollectionGeneratorTests`).
- Harness M7-C (`CAPPARSE_OCR_EVAL=1`): 1/1 em Debug e Release, antes e
  depois; tabela antes/depois em `docs/ocr-integration.md` §11.3.
- Probe descartável (`C:\Dev\temp\wordbox-probe`, fora do repo) mediu as
  caixas reais do engine sobre a coleção — base da descoberta de tiling
  (§57.1); os probes em `C:\Dev\temp\rapidocr-probe` e
  `C:\Dev\temp\wordbox-probe` podem ser apagados.

### 57.4 Pendências

- **Commit** do trabalho local (aguarda aprovação explícita do usuário).
- Validação humana de capturas reais (a coleção sintética não representa
  ClearType/escala real — limite documentado em `ocr-integration.md` §10.5
  e §11.3).
