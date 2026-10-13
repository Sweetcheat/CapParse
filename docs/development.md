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
- [ ] inglês funciona (não validado separadamente; o modelo o suporta — coleção M7-C em `docs/ocr-integration.md`);
- [x] texto simples funciona (usuário);
- [x] OCR não bloqueia a UI (usuário — fluxo completo responsivo durante o reconhecimento; indicador de processamento validado no M14).

### Ações

- [x] Generate Text funciona (usuário);
- [x] Copy Image funciona (usuário — colado no Paint);
- [x] Save Image funciona (usuário).

### Aplicação

- [x] Settings funciona (usuário — atalho alterado, programa reiniciado, persistência confirmada);
- [x] Exit funciona (usuário — incluindo saída durante OCR em andamento);
- [ ] erros são tratados (caminhos de falha — diálogos de erro de captura/OCR — não foram exercitados explicitamente na validação informada);
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

Esta seção é o ponto de entrada de uma nova sessão. Reescrita na consolidação documental pós-M11 (2026-10), substitui a versão da auditoria de 2026-10-12 (que descrevia o estado de um clone obsoleto); atualizada na sessão M14 (registro do M13 publicado e do trabalho M14 local) e na sessão M15 (preparação da candidata v0.1.0-beta.2: documentação, metadados de versão, procedimento de publicação reproduzível e pacote de validação). O que está marcado como "executado" tem evidência de comando na sessão indicada; o restante vem do histórico Git e da documentação verificada contra o código.

## 50.1 Repositório de referência

- **Repositório principal de trabalho: `C:\Dev\CapParseIntegI`** (branch `main`, remoto `https://github.com/Sweetcheat/CapParse.git`).
- `C:\Dev\CapParse` é um **clone obsoleto** (HEAD `3324900`, 13 commits atrás da linha principal). Ele serviu apenas como fonte de comparação documental na auditoria de 2026-10-12. **Não trabalhar, não commitar e não fazer push a partir dele** (o `origin/main` local dele está desatualizado; um force-push a partir dele apagaria a linha publicada do GitHub).

## 50.2 Estado do repositório (sessão M15)

- Branch: `main`; HEAD = `origin/main` = `c4e76fd` (refactor: build overlay session asynchronously) — o M14 está **publicado** no GitHub (`a7d2c22` + `c4e76fd` sobre `e3560cc`).
- Working tree: alterações da M15 **não commitadas** (documentação, metadados de versão em `src/CapParse/CapParse.csproj` e `app.manifest`, novo `build/publish-win-x64.ps1`), aguardando aprovação explícita.
- Tag `v0.1.0-beta.1` → `86e3d17`: a release beta publicada **não inclui** o M11, o M12, o M13 nem o M14. A tag não foi movida nem alterada.
- `build/dist/CapParse-v0.1.0-beta.1-win-x64.zip` é **desatualizado** (anterior a M11–M14) e não deve ser reutilizado como artefato final. O pacote da candidata é `build/dist/CapParse-v0.1.0-beta.2-win-x64.zip`, gerado nesta sessão (dados em §50.5).
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
| M15 — V0.1 Validation | Em andamento (sessão atual): documentação consistente, backlog (histórico com imagem), procedimento de publicação reproduzível (`build/publish-win-x64.ps1`), metadados de versão `0.1.0-beta.2`, pacote `CapParse-v0.1.0-beta.2-win-x64.zip` gerado e inspecionado; validação manual do produto informada pelo usuário (§28) | — (não commitado) | restam: commit/push/tag/release com aprovação explícita; validação do pacote beta.2 em computador externo (usuário) |
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

A M15 está tecnicamente completa nesta sessão: documentação consistente com o estado real, melhoria do histórico com imagem registrada no backlog (sem implementação), procedimento de publicação reproduzível e documentado, metadados de versão coerentes (`0.1.0-beta.2`), testes passando em Debug e Release e o pacote candidato `CapParse-v0.1.0-beta.2-win-x64.zip` gerado, inspecionado e com hash registrado (§50.5).

Aguardando aprovação explícita (nada foi commitado, publicado ou tagged nesta sessão):

1. commit das alterações da M15 (documentação + metadados de versão + `build/publish-win-x64.ps1`);
2. push para `origin/main`;
3. criação da tag `v0.1.0-beta.2` e publicação da release com o ZIP de `build/dist/` (a tag e o pacote da beta.1 permanecem intactos).

Restrição operacional: o OCR a partir do pacote beta.2 embalado só pode ser validado interativamente (usuário, em computador externo) — não há gatilho headless para a captura.

## 50.7 Problemas conhecidos, riscos e limitações

- OCR (PP-OCRv6 Small, medido no M7-C): fonte pequena em fundo escuro degrada severamente; espaços entre palavras/tokens são frequentemente perdidos no texto reconstruído.
- A arena de memória do ONNX Runtime cresce durante execuções (WorkingSet do processo ~2 GB na avaliação M7-C; não é memória exclusiva do OCR).
- Modelos: ~32 MB no pacote distribuído (pasta `models/v6` + classificador v5 de ~1 MB) — aceitável para app local-first. Os três arquivos v5 não utilizados pelo preset foram removidos do pacote na M15 (decisão e evidência em §50.5); o classificador v5 permanece porque o preset `PPOCRv6Small` o carrega em runtime.
- O histórico da sessão é somente textual (decisão de produto da M15 para a V0.1) — a melhoria "Histórico de capturas com recuperação de imagem" está no backlog (§38.1) e não foi implementada.
- Os diagnósticos temporários `CAPPARSE_TRACE`/`CAPPARSE_NO_OVERLAY` permanecem no código: o fix do black-flash (overlay layered + revelação em dois frames) foi validado manualmente pelo usuário na M15 (ausência de flash preto e de janelas residuais reportada como OK); a remoção dos diagnósticos ainda exige justificativa documentada e autorização.
- A coleção M7-C é sintética (anti-aliasing em cinza, 96 DPI): não representa capturas reais (ClearType, escalas, DPI ≠ 100%).
- O clone obsoleto `C:\Dev\CapParse` continua existindo em disco com 43 arquivos M7 staged e docs da auditoria — risco de trabalho acidental; arquivá-lo quando o trabalho documental estiver consolidado aqui.
- O OCR a partir do pacote beta.2 embalado (single-file) não pôde ser validado headless nesta sessão (a captura exige seleção interativa): a validação de campo em computador externo fica a cargo do usuário com o novo pacote.

## 50.8 Fora do escopo atual (explícito)

- Lista completa: `product.md` §12 (non-goals do V0.1) e `AGENTS.md` §35 (funcionalidades futuras).
- Em particular: HDR, planilhas, IA, cloud, histórico persistente, outras plataformas, e qualquer milestone não explicitamente solicitado.

## 50.9 Como iniciar uma nova sessão

1. Ler `AGENTS.md` e esta seção.
2. Trabalhar somente em `C:\Dev\CapParseIntegI`; conferir `git status` / `git log` (HEAD deve ser `c4e76fd` ou um descendente que inclua os commits da M15; o working tree deve refletir apenas o trabalho da sessão atual).
3. Executar `dotnet build` e `dotnet test` (suíte atual: 331 testes).
4. Trabalhar apenas no milestone atual; não criar commit, tag ou push sem aprovação explícita.
5. Atualizar esta seção ao final da sessão (estado, evidências, pendências).
