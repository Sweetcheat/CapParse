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

O agente principal de desenvolvimento será utilizado para implementar o projeto, principalmente através do OpenCode.

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
M14 — Error Handling and Polish
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

# 22. M9 — Action Selector

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

## Objetivo

Copiar a região capturada para o clipboard.

## Critérios

- imagem disponível imediatamente para paste;
- nenhuma gravação temporária desnecessária;
- feedback de sucesso;
- erro tratado de maneira amigável.

---

# 25. M12 — Save Image

## Objetivo

Salvar a captura usando o diálogo padrão do Windows.

## Critérios

- usuário escolhe caminho;
- usuário escolhe nome;
- imagem é salva corretamente;
- cancelamento não é tratado como erro;
- erro de gravação é apresentado de maneira amigável.

---

# 26. M13 — Settings

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

---

# 27. M14 — Error Handling and Polish

## Objetivo

Consolidar qualidade do V0.1.

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

---

# 28. M15 — V0.1 Validation

## Objetivo

Validar o produto completo.

Checklist mínimo:

### Captura

- [ ] hotkey funciona;
- [ ] tray double-click funciona;
- [ ] Capture do tray funciona;
- [ ] ESC cancela;
- [ ] seleção funciona;
- [ ] múltiplos monitores funcionam;
- [ ] DPI diferente funciona.

### OCR

- [ ] OCR local funciona;
- [ ] português funciona;
- [ ] inglês funciona;
- [ ] texto simples funciona;
- [ ] OCR não bloqueia a UI.

### Ações

- [ ] Generate Text funciona;
- [ ] Copy Image funciona;
- [ ] Save Image funciona.

### Aplicação

- [ ] Settings funciona;
- [ ] Exit funciona;
- [ ] erros são tratados;
- [ ] não há dependência de internet;
- [ ] aplicação pode permanecer no tray;
- [ ] não existem janelas residuais;
- [ ] build Release funciona.

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
└── development.md
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