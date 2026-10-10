# M6-A — Plano de integração do OCR

Data: 2026-10-09.

Este documento registra apenas o que a análise M6-A acrescenta às decisões já
existentes em `architecture.md` (§12–16), `product.md` e `development.md` (M7).
Decisões já documentadas (mecanismo, modelo, interface, resultado, execução
local, inicialização, UI thread) não são reabertas aqui.

## 1. Diagnóstico de compatibilidade (confirmado em fontes primárias)

| Item | Verificado |
| --- | --- |
| Pacote | `RapidOcrNet` (BobLd/RapidOcrNet), versão 4.2.0 (2026-09-09) |
| Target framework | `net8.0` e `net10.0` → compatível com o `net8.0-windows` do projeto |
| Licença da biblioteca | Apache-2.0 |
| Modelo | Preset `RapidOcrModelSet.PPOCRv6Small` existe no pacote (corresponde a "PP-OCRv6 Small" da documentação) |
| Licença dos modelos | Apache-2.0 (modelos PaddleOCR) |
| ONNX Runtime | `Microsoft.ML.OnnxRuntime 1.29.0` (publicado 2026-08-12; 1.31.0 de 2026-10-09 também existe no NuGet); targets `netstandard2.0`/`netstandard2.1`/`net9.0-*` → compatível com .NET 8 via `netstandard2.0`; licença MIT; inclui `runtimes/win-x64/native/onnxruntime.dll` (~16 MB) |
| Outras dependências transitivas | Conforme o nuspec 4.2.0: `Microsoft.ML.OnnxRuntime.Managed 1.29.0` (bindings C#), `SkiaSharp 3.119.1`, `SkiaSharp.NativeAssets.Linux.NoDependencies 3.119.1` (inerte no Windows), `Clipper2 2.0.0` |
| Pipeline | DBNet (detecção) → classificador de orientação (180°) → CRNN (reconhecimento), em CPU, 100% local |
| Cancellation | `DetectAsync` aceita `CancellationToken`; cancela entre etapas e dentro das execuções ONNX; lança `OperationCanceledException` |
| AOT | O pacote declara compatibilidade com AOT (relevante apenas se o projeto migrar no futuro) |

## 2. Modelos: formato, origem e distribuição

Arquivos necessários para o preset Small:

```text
PP-OCRv6_det_small.onnx   (SHA256 090f04abcd9d9a7498bc4ebf677e4cb9bdce1fe4197ddb7e529f1ef44e1ff94f — verificado no M7-A)
PP-OCRv6_rec_small.onnx   (SHA256 6f327246b50388f3c176ae304bd95767ea6dc0c9ae92153ef8cbe210b3c14884 — verificado no M7-A)
ppocrv6_dict.txt          (SHA256 b5f2bfe2bdd9448429e3e82b51c789775d9b42f2403d082b00662eb77e401c5d — calculado no M7-A; a fonte primária não publica hash para o dicionário)
ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx   (classificador v5 reutilizado pelo v6; SHA256 54379ae5174d026780215fc748a7f31910dee36818e63d49e17dc598ecc82df7 — o arquivo distribuído no NuGet 4.2.0 bate com a fonte primária)
```

Fontes e limitações:

- Os arquivos **não** estão no pacote NuGet (exceto o classificador v5). O `default_models.yaml`
  do projeto RapidOCR (fonte primária) aponta para um espelho no ModelScope
  (`RapidAI/RapidOCR`, tag `v3.10.0`) com os SHA256 acima.
- Confirmado no M7-A: o pacote 4.2.0 distribui o classificador v5 em
  `models/v5/` e um target MSBuild o copia para `models/v5/` na saída de
  build/publish.
- **Divergência README × pacote:** a tabela do README (master) lista
  `ppocrv6_small_dict.txt` como nome do dicionário v6 small; a DLL 4.2.0
  (commit `708cae2f`) resolve o preset `PPOCRv6Small` para
  `models\v6\ppocrv6_dict.txt` — confirmado por reflexão e presente na fonte
  primária. O README descreve uma versão à frente da 4.2.0; quando divergem,
  o pacote inspecionado prevalece.
- O dicionário `ppocrv6_dict.txt` foi verificado diretamente (18.708 linhas;
  38.320 caracteres).

Decisão de distribuição (respeita `AGENTS.md` §33 — sem rede em runtime):

- baixar os arquivos **uma única vez** no momento da implementação (M7);
- commitá-los no repositório em `models/` e copiá-los para a saída do build;
- a aplicação carrega os modelos do disco, sem nenhum download em runtime.

Tamanho estimado do conjunto Small ≈ 31 MB (os três tamanhos somam ≈ 176 MB,
conforme README). Esse é o maior item do repositório; se o tamanho passar a
ser problema, a alternativa é um script de download no build (fora do
runtime), não uma rede em runtime.

## 3. Contrato de integração

A assinatura de `IOcrEngine` permanece como documentada em
`architecture.md` §12. O que a análise define:

### 3.1 `ImageData`

O tipo era referenciado mas não definido. Decisão:

```csharp
public sealed class ImageData
{
    public ImageData(Bitmap bitmap) { ... }

    public Bitmap Bitmap { get; }
}
```

- embrulha `System.Drawing.Bitmap`, o formato que o pipeline já produz
  (`CaptureCropper`);
- propriedade: o consumidor detém o `Bitmap`; o OCR **não** o descarta;
- a conversão para `SKBitmap` (formato que o RapidOcrNet consome) acontece
  dentro de `RapidOcrEngine`, mantendo `IOcrEngine` livre de SkiaSharp e de
  tipos do RapidOcrNet.

### 3.2 `OcrOptions`

```csharp
public sealed class OcrOptions
{
    public static OcrOptions Default { get; }
}
```

Mantido na assinatura porque o contrato documentado o inclui. No V0.1 não há
opções de OCR visíveis ao usuário, então o tipo não tem campos. Campos só
serão adicionados quando um uso real existir.

### 3.3 Mapeamento do resultado

`OcrResult`/`OcrBlock` permanecem como em `architecture.md` §13. Mapeamento
concreto a partir da API do RapidOcrNet (confirmado no M7-A por reflexão na
DLL 4.2.0):

- `Text` → texto completo (linhas concatenadas no resultado do pacote);
- `Blocks` → um `OcrBlock` por linha detectada:
  - `Text` → texto da linha;
  - `Confidence` → média das confianças por caractere (`CharScores`); é o
    valor de confiança confiável que o mecanismo expõe;
  - `Bounds` → retângulo alinhado aos eixos derivado dos 4 cantos
    (`BoxPoints`), em pixels da imagem de entrada;
- `ProcessingTime` → medido com `Stopwatch` dentro de `RapidOcrEngine`.

Regras de resultado:

- imagem sem texto → resultado **válido e vazio** (`Text` vazio, `Blocks`
  vazio), não é erro;
- imagem inválida (disponibilidade do `Bitmap` perdida, dimensões nulas) →
  falha tratada como erro de processamento;
- falha de inicialização (arquivos de modelo ausentes/corrompidos) e falha de
  reconhecimento → exceção do engine; a camada de UI converte para mensagem
  amigável (M8 em diante, conforme `architecture.md`).

### 3.4 Propriedade e liberação

- `RapidOcrEngine` cria e detém a instância `RapidOcr` (que implementa
  `IDisposable`) e a descarta em `Dispose`;
- o `App` cria um único engine (inicialização antecipada na startup; lazy é
  aceitável conforme `architecture.md` §15) e o descarta no encerramento;
- `OcrResult`/`OcrBlock` são dados puros, sem recursos a liberar.

## 4. Português

- `pt` está na lista oficial de idiomas do modelo v6 (`default_models.yaml`):
  o v6 é um modelo único multilíngue (chinês, inglês, japonês e 46 idiomas de
  script latino), confirmado também na documentação do PaddleOCR.
- O dicionário do modelo contém todos os caracteres acentuados do português
  (à, á, â, ã, ç, é, ê, í, ó, ô, õ, ú), verificado no arquivo original.
- Cobertura de dicionário não equivale a qualidade de reconhecimento: a
  precisão real em capturas de tela será medida no M7 (seção 5). Não há
  número de qualidade declarado antes dessa medição.

## 5. Estratégia de testes e qualidade

Testes unitários (sem modelo, determinísticos):

- validação de `ImageData`;
- mapeamento resultado do pacote → `OcrResult`/`OcrBlock` (com dados
  sintéticos, incluindo `BoxPoints` e `CharScores`);
- resultado vazio;
- caminho de erro (modelo ausente) — quando isolável do binário nativo.

Avaliação com o modelo real (execução manual/automatizada no M7, fora do
desktop interativo não é possível para capturas reais):

- coleção pequena e reproduzível de imagens de referência com transcrição
  conhecida: texto impresso com acentos, tamanhos de fonte variados (8–24 pt
  equivalentes), fundos claros/escuros, contraste alto e moderado, interface
  de aplicativo, e um caso sem texto;
- métrica: **CER** (Character Error Rate = distância de Levenshtein /
  comprimento da referência), por imagem e agregado; registrar também
  acerto exato de linha;
- os números medidos serão registrados em um relatório no repositório;
  nenhum valor de qualidade será declarado antes de medi-lo;
- desempenho (tempo por imagem) e memória (pico de RSS) serão medidos na
  primeira execução real — sem estimativas prévias.

## 6. Riscos e questões em aberto

| Risco / questão | Situação |
| --- | --- |
| Qualidade real do v6 em texto pequeno de UI (anti-aliasing, dark mode) | Não medida; será medida no M7 |
| Download único dos modelos (≈ 31 MB) | Executado no M7-A: os 3 arquivos v6 baixados do ModelScope (tag `v3.10.0`) com SHA256 conferidos; arquivos em `models/v6/` (fora do Git nesta etapa) |
| 31 MB no repositório Git | Aceitável para app local-first; reavaliar se incomodar no fluxo de clone (decisão de distribuição mantida; commit dos modelos é o M7-B) |
| Tamanho dos binários nativos (ONNX Runtime + SkiaSharp) no publish | `onnxruntime.dll` win-x64 ≈ 16 MB + `libSkiaSharp.dll` win-x64 ≈ 11 MB na saída; medir no publish real no M7 |
| Nomes exatos da API do RapidOcrNet (`RapidOcrOptions.PPOCRv6`, preset, `TextBlocks`) | Confirmado no M7-A por reflexão na DLL 4.2.0 (seção 8) |
| Inicialização antecipada vs. lazy | Decidir no M7 com base no tempo medido de `InitModels()` |

## 7. Plano incremental (M7)

1. Adicionar o pacote `RapidOcrNet` (e as dependências transitivas) e
   adicionar `models/` com os 4 arquivos + SHA256 verificados.
2. Criar `ImageData`, `OcrOptions`, `OcrResult`, `OcrBlock` e `IOcrEngine`
   conforme as seções 3 e 12–13 de `architecture.md`.
3. Implementar `RapidOcrEngine` (inicialização, `RecognizeAsync`,
   mapeamento, `Dispose`) sem nenhum contato com UI.
4. Testes unitários da seção 5 (mapeamento, vazio, `ImageData`, erro).
5. Execução real do modelo: coleção de referência, CER, tempo e memória
   registrados em relatório.
6. Critério de aceite do M7: uma imagem capturada entra no OCR e produz um
   `OcrResult`, funcionando localmente sem internet.

A integração com o fluxo visual (loading, apresentação do resultado) é o M8 e
não faz parte desta análise.

## 8. M7-A — Validação executada (2026-10-09)

Evidências coletadas no M7-A, complementando as seções acima. Fontes
primárias: NuGet (nuspec/catalogo), `default_models.yaml` do
RapidAI/RapidOCR (main), fonte do BobLd/RapidOcrNet no commit `708cae2f`
(fixado no nuspec do 4.2.0) e reflexão na DLL do pacote 4.2.0.

### 8.1 Pacote

- `RapidOcrNet 4.2.0` adicionado a `src/CapParse/CapParse.csproj` como único
  pacote direto; as demais dependências são transitivas (seção 1).
- Compatibilidade confirmada: o pacote tem `lib/net8.0` → usa-se a DLL
  `net8.0` no projeto `net8.0-windows`.
- Builds Debug e Release passam com 0 avisos/erros; 100/100 testes passam nos
  dois configurations. Nenhum código de produção foi alterado.

### 8.2 API real da 4.2.0 (por reflexão na DLL)

- `RapidOcr : IDisposable`, `ctor()` sem parâmetros.
  - `InitModels(int numThread = 0)`, `InitModels(SessionOptions)`,
    `InitModels(RapidOcrModelSet, int numThread = 0)`,
    `InitModels(RapidOcrModelSet, SessionOptions)`,
    `InitModels(string detPath, string clsPath, string recPath, string keysPath, int numThread = 0)`,
    `InitModels(string detPath, string clsPath, string recPath, string keysPath, SessionOptions)`;
  - `static SessionOptions GetDefaultSessionOptions(int numThread = 0)`
    (GraphOptimizationLevel = ORT_ENABLE_EXTENDED);
  - `Detect(string|SKBitmap, RapidOcrOptions, [IProgress<(int,int)>], CancellationToken)`
    e `DetectAsync(...)` (mesmas sobrecargas, `Task<OcrResult>`);
  - `DetectBoxes`/`DetectBoxesAsync` (só detecção) também existem.
- `RapidOcrOptions`: record com presets **estáticos `Default`, `PythonCompat`
  e `PPOCRv6`** (campos `static readonly`; `PPOCRv6` == `PythonCompat`:
  Padding=0, ImgResize=0, LimitSideLen=736, MaxSideLen=2000, MinSideLen=30,
  WidthHeightRatio=8, MinHeight=30, TextScore=0.5, ClsThresh=0.9,
  ClsPreserveAspectRatio=true, BoxScoreThresh=0.5, BoxThresh=0.3,
  UnClipRatio=1.6, DoAngle=true). `new RapidOcrOptions()` zera todos os
  campos — usar sempre um preset.
- `RapidOcrModelSet`: record (`with` disponível) com `DetModelPath`,
  `ClsModelPath`, `RecModelPath`, `KeysPath`, `DetMean`, `DetStd` e os
  presets `PPOCRv5Latin`, `PPOCRv6Tiny/Small/Medium`.
  - `PPOCRv6Small` → `models\v6\PP-OCRv6_det_small.onnx`,
    `models\v5\ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx`,
    `models\v6\PP-OCRv6_rec_small.onnx`, `models\v6\ppocrv6_dict.txt`,
    DetMean/DetStd = [127.5, 127.5, 127.5].
- `OcrResult`: `StrRes` (texto completo, `\n` por bloco), `TextBlocks`
  (`TextBlock[]`), `DbNetTime`, `DetectTime` (ms).
- `TextBlock` (`ITextBox`, `IBoxPoints`): `Text` (caracteres concatenados
  sem espaço), `BoxPoints` (`SKPointI[]`, 4 cantos, pixels da imagem de
  entrada), `CharScores` (`float[]`), `BoxScore`, `AngleIndex`/`AngleScore`,
  `Chars`, `WordResults` (`WordBox[]`, nulo sem `ReturnWordBox`), tempos por
  etapa.
- 4.2.0 **não** possui `RapidOcrSessionOptions` (opções por modelo, presente
  apenas no master): um único `SessionOptions` é aplicado aos 3 modelos.

### 8.3 Implicações para o M7-B

- Os caminhos dos presets são **relativos ao working directory do processo**
  (ex.: `models\v6\...`), não ao `AppContext.BaseDirectory`. Em M7-B, o
  `RapidOcrModelSet` deve ser derivado do preset com caminhos absolutos
  (ex.: `PPOCRv6Small with { DetModelPath = ..., ... }`) ancorados em
  `AppContext.BaseDirectory`, evitando falha de inicialização quando o app é
  iniciado com CWD diferente (atalho, shell).
- Os 3 arquivos v6 precisam estar em `models/v6/` ao lado do binário (o
  target do NuGet só copia `models/v5/`); M7-B adiciona a cópia de
  `models/v6/` do repositório para a saída de build/publish.
- Arquivo ausente/corrompido → `InitModels` falha (exceção do ONNX Runtime
  ao carregar o modelo); `RapidOcrEngine` a converte em falha de
  inicialização (seção 3.3).

### 8.4 Modelos preparados localmente (fora do Git)

Local: `models/v6/` na raiz do repositório (arquivos não rastreados; nada
commitado, `.gitignore` inalterado).

| Arquivo | Origem | SHA-256 | Resultado |
| --- | --- | --- | --- |
| `PP-OCRv6_det_small.onnx` (9.93 MB) | ModelScope `RapidAI/RapidOCR` tag `v3.10.0`, `onnx/PP-OCRv6/det/` | `090f04abcd9d9a7498bc4ebf677e4cb9bdce1fe4197ddb7e529f1ef44e1ff94f` | confere com o documentado |
| `PP-OCRv6_rec_small.onnx` (21.2 MB) | ModelScope, `onnx/PP-OCRv6/rec/` | `6f327246b50388f3c176ae304bd95767ea6dc0c9ae92153ef8cbe210b3c14884` | confere com o documentado |
| `ppocrv6_dict.txt` (73 KB) | ModelScope, `paddle/PP-OCRv6/rec/PP-OCRv6_rec_small/` | `b5f2bfe2bdd9448429e3e82b51c789775d9b42f2403d082b00662eb77e401c5d` | valor novo (fonte primária não publica hash do dicionário); conteúdo confere (18.708 linhas; acentos do português presentes) |
| classificador v5 | distribuído no NuGet 4.2.0 (`models/v5/`) | `54379ae5174d026780215fc748a7f31910dee36818e63d49e17dc598ecc82df7` | confere com o hash da fonte primária |

Licenças: modelos PaddleOCR = Apache-2.0; RapidOcrNet = Apache-2.0;
ONNX Runtime = MIT; SkiaSharp = MIT; Clipper2 = Boost Software License 1.0
(todas permissivas e compatíveis com distribuição).

## 9. M7-B — Implementação do motor OCR local (2026-10-10)

### 9.1 Tipos criados (`src/CapParse/OCR/`)

| Tipo | Papel |
| --- | --- |
| `ImageData` | embrulha o `System.Drawing.Bitmap` do consumidor; valida não-nulo, liveness (bitmaps descartados) e dimensões positivas; nunca descarta o bitmap |
| `OcrOptions` | sem campos no V0.1 (preset fixo do engine); existe para a assinatura de `IOcrEngine` evoluir sem quebra |
| `OcrBlock` | `Text`, `Confidence` (média de `CharScores`; `0` quando não há pontuações), `Bounds` (retângulo eixo-alinhado dos 4 cantos da caixa, em pixels da imagem; `Rectangle.Empty` sem caixa) |
| `OcrResult` | `Text` (a partir de `StrRes`), `Blocks` (um por linha de `TextBlocks`), `ProcessingTime` (`Stopwatch`) |
| `IOcrEngine` | `RecognizeAsync(ImageData, OcrOptions, CancellationToken)` — contrato independente de RapidOcrNet/SkiaSharp |
| `RapidOcrEngine` | implementação `IDisposable`; detém a instância `RapidOcr` que cria |

Decisões de comportamento (explícitas e testadas):

- imagem sem texto → resultado válido e vazio (`Text` vazio, `Blocks` vazio);
- `Confidence = 0` quando `CharScores` é nulo/vazio (confiança não é inventada);
- `Bounds = Rectangle.Empty` quando `BoxPoints` é nulo/vazio;
- imagem inválida (nula, descartada, dimensões nulas) → `ArgumentNullException`/`ArgumentException` na `ImageData`;
- **descoberta de plataforma:** no System.Drawing gerenciado do .NET 8, um `Bitmap`
  descartado lança `ArgumentException` ("Parameter is not valid.") nos acessores
  `Width`/`LockBits` — não `ObjectDisposedException` como no GDI+ legado. O
  contrato de `ImageData` documenta esse comportamento real;
- cancelamento prévio do token → `OperationCanceledException` síncrona;
  cancelamento durante a operação → propagado pelo `DetectAsync`;
- falha de inicialização (modelos ausentes/corrompidos) → exceção do construtor;
  a instância `RapidOcr` é descartada antes de re-lançar (nada fica
  parcialmente inicializado);
- uso após `Dispose` → `ObjectDisposedException`; o `Dispose` é idempotente;
- o bitmap do consumidor nunca é descartado pelo engine.

### 9.2 API real utilizada (RapidOcrNet 4.2.0, confirmada por reflexão na DLL)

```text
new RapidOcr()
  .InitModels(RapidOcrModelSet models)
  .DetectAsync(SKBitmap, RapidOcrOptions, CancellationToken) -> Task<OcrResult>

RapidOcrModelSet.PPOCRv6Small  (record; derivado com `with` para re-ancorar caminhos)
RapidOcrOptions.PPOCRv6        (preset estático; `new RapidOcrOptions()` zera campos — nunca usado)
OcrResult.StrRes / .TextBlocks
TextBlock.Text / .BoxPoints (SKPointI[]) / .CharScores (float[])
```

Conversão de imagem (dentro do `RapidOcrEngine`, fora do contrato público):
`Bitmap` 32bpp ARGB → `LockBits` → cópia de linhas para `SKBitmap`
(`Rgba8888`, alpha pré-multiplicado). Os layouts byte a byte são idênticos em
máquinas little-endian, então o conteúdo de tela opaco do pipeline de captura é
copiado verbatim. Formatos distintos de 32bpp ARGB são convertidos antes da
cópia. O `SKBitmap` intermediário é descartado ao final de cada reconhecimento.

### 9.3 Caminhos dos modelos

- o preset `PPOCRv6Small` aponta para caminhos relativos
  (`models\v6\...`, `models\v5\...`) resolvidos contra o *working directory*;
- `RapidOcrEngine.ResolveDefaultModels()` deriva o preset com
  `preset with { DetModelPath = ..., ... }` re-ancorando os quatro caminhos em
  `AppContext.BaseDirectory` (independente do CWD do processo);
- `build/OcrModels.targets` (importado pelo projeto do app e pelo projeto de
  testes) copia para a saída de build e de publish:
  - `models/v6/*` — os 3 arquivos commitados no repositório;
  - o classificador v5 `ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx` a
    partir do pacote NuGet (`$(NuGetPackageRoot)`). Para o app, o target do
    pacote RapidOcrNet já copia o conjunto v5 completo; a cópia extra do
    classificador é idempotente (mesmo arquivo, mesmo destino). Para o projeto
    de testes é obrigatória, pois os `build/` targets de um pacote não se
    aplicam a projetos que o referenciam apenas transitivamente;
- publicação: `dotnet publish` preserva `models/v6/` + `models/v5/` ao lado do
  binário; hashes dos arquivos publicados conferem com os referenciais
  (seção 8.4).

### 9.4 Inicialização

Estratégia: **inicialização antecipada no construtor** (a mais simples para a
arquitetura: o `App` criará um único engine na startup e o descartará no
encerramento, conforme a seção 3.4). O construtor carrega os modelos
sincronamente; falha de carregamento é uma falha de inicialização visível, sem
recursos pendentes.

Tempos de inicialização **medidos** pelo smoke test (engine completo: 3
modelos ONNX + dicionário):

| Configuração | Inicialização | Reconhecimento (imagem 1200x220) |
| --- | --- | --- |
| Debug | 462,4 ms | 713,9 ms |
| Release | 204,4 ms | 683,6 ms |

### 9.5 Testes

116/116 aprovados em Debug e Release (100 pré-existentes + 16 novos):

- `ImageDataTests` (3): nulo, instância preservada, bitmap descartado;
- `OcrResultMappingTests` (8): mapeamento sintético `OcrResult`/`TextBlock`
  do pacote → tipos públicos (texto, caixas incl. quadrilátero rotacionado,
  confiança média, pontuações ausentes/vazias, caixas ausentes, resultado
  vazio, campos nulos);
- `RapidOcrEngineTests` (1): inicialização com modelos ausentes falha no
  construtor;
- `OcrSmokeTests` (4, modelo real): imagem sintética em português
  reconhecida, imagem em branco → resultado vazio válido, token pré-cancelado
  → `OperationCanceledException`, uso após `Dispose` →
  `ObjectDisposedException`. O engine é compartilhado pela classe de smoke
  tests (fixture) para não repetir a inicialização.

### 9.6 Smoke test real (resultado)

Imagem sintética 1200x220, Segoe UI 32 pt, texto preto sobre fundo branco:
`Olá, ação, coração, informação e português.`

| Item | Resultado |
| --- | --- |
| Inicialização | sucesso (modelos v6 do repositório + classificador v5 do pacote) |
| Texto reconhecido | `Olá, ação, coração, informação e português.` (transcrição exata) |
| Bloco único | `confidence=0.98`, `bounds={X=33,Y=57,Width=855,Height=76}` |
| Mecanismo | RapidOcrNet 4.2.0, preset `PPOCRv6Small` + `RapidOcrOptions.PPOCRv6`, CPU |

Independência de CWD verificada: os 4 smoke tests executam via
`dotnet vstest` com working directory `C:\` (modelos resolvidos por
`AppContext.BaseDirectory`) — 4/4 aprovados.

### 9.7 Git

- 3 arquivos v6 em `models/v6/` prontos para versionamento (não ignorados por
  `.gitignore`; hashes da seção 8.4);
- nenhum commit/push nesta etapa (M7-B); artefatos de build/publicação
  permanecem fora do repositório.

### 9.8 Pendências para M7-C e adiante

- avaliação de qualidade (CER) em coleção de referência com acentos,
  tamanhos, fundos e dark mode (seção 5) — fora do M7-B;
- integração do engine ao fluxo do `App` (criação/descarte único, estado
  loading) pertence ao M8;
- se o tempo de inicialização chegar a importar na experiência de startup, a
  decisão eager/lazy da arquitetura (§15) pode ser reavaliada com essas
  medições como baseline.

## 10. M7-C — Avaliação de qualidade do OCR local (2026-10-10)

Avaliação reproduzível do PP-OCRv6 Small (RapidOcrNet 4.2.0) em uma coleção
sintética de referência. Nenhum resultado de qualidade foi declarado antes
desta medição; os números abaixo são de uma execução real nesta máquina, em
cada configuração, e não se generalizam para outros computadores.

### 10.1 Coleção de teste

Local: `tests/CapParse.Tests/TestData/Ocr/`. O `manifest.json` é a fonte de
verdade: para cada caso ele guarda a **transcrição de referência autorada
(aqui, e nunca copiada da saída do OCR)** e todos os parâmetros de
geração (resolução, fonte, tamanho em pt, cores de texto/fundo, formas e
coordenadas). As 17 imagens PNG commitadas ao lado são renderizadas a
partir dele.

| Grupo | Casos | Condição |
| --- | --- | --- |
| Acentos pt-BR (luz) | `pt-accent-16-light`, `pt-accent-12-light`, `pt-accent-24-light`, `pt-accent-8-light` | Segoe UI 8/12/16/24 pt, `#1A1A1A` sobre `#FFFFFF` |
| Acentos pt-BR (escuro) | `pt-accent-8-dark`, `pt-accent-12-dark`, `pt-accent-24-dark` | Segoe UI 8/12/24 pt, `#E8E8E8` sobre `#1E1E1E` |
| Multi-linha (UI) | `multiline-12-light`, `multiline-12-dark` | 3 linhas, 12 pt, luz e escuro |
| Contraste moderado | `contrast-moderate-light`, `contrast-moderate-dark` | `#6B6B6B`/`#FFFFFF` e `#9A9A9A`/`#1E1E1E`, 12 pt |
| Numérico/alfanumérico | `numeric-12-light` | datas, IDs hex, `1.234,56 R$`, versão semântica, 12 pt |
| Pontuação | `punctuation-12-light` | parênteses, aspas, travessão, `!`, colchetes, `<<fim>>` |
| Interface realista | `ui-mockup-light`, `ui-mockup-dark` | barra de título (16 pt negrito), menu, 3 linhas de status; separadores e painéis desenhados |
| Sem texto | `no-text-blank`, `no-text-shapes` | fundo liso; e fundo com formas geométricas (sem texto) |

Detalhes da renderização: GDI+ (`Graphics.DrawString`), `Segoe UI` (fonte
local do Windows, nada é baixado), `TextRenderingHint.AntiAliasGridFit`
(anti-aliasing em cinza; as capturas reais de tela mostram ClearType, o que
é uma diferença conhecida e registrada na seção 10.6), 96 DPI (1 pt = 4/3
px, como em tela com escala de 100%).

**Referências.** A transcrição de cada caso está no `manifest.json`
(campo `reference`; casos multi-linha usam `\n` entre linhas, como o engine
junta as linhas reconhecidas). Casos sem texto têm referência vazia e são
avaliados separadamente (nenhum CER é atribuído a eles).

**Como regenerar.** As PNGs commitadas devem corresponder à renderização do
manifesto; o teste `OcrCollectionGeneratorTests` (na suíte normal, rápido,
sem modelo) re-renderiza cada caso e compara pixel a pixel com tolerância
(≥0,5% de pixels diferentes ou delta de canal >96 → falha; o ruído medido
de anti-aliasing entre processos é de 0–2 pixels com delta ≤36). Para
regenerar deliberadamente: apagar as PNGs em
`tests/CapParse.Tests/TestData/Ocr/` e rodar
`dotnet test --filter "FullyQualifiedName~OcrCollectionGeneratorTests"`;
na primeira execução os arquivos são recriados e o teste falha pedindo a
revisão da coleção.

### 10.2 Métricas

- **CER (Character Error Rate)** = distância de Levenshtein(referência,
  hipótese) / comprimento da referência. Implementado em
  `tests/CapParse.Tests/CharacterErrorRate.cs` e validado por
  `CharacterErrorRateTests` (casos conhecidos de inserção, exclusão,
  substituição, correspondência exata e referência vazia).
- **Política de normalização (primária, "bruta")**: acentos, caso e todos os
  caracteres não-espaçadores são comparados exatamente — nada é
  normalizado de modo a esconder erros ortográficos; apenas espaços
  **nas bordas** são removidos (o resultado do engine termina com
  quebra de linha/espaco de box, que não são conteúdo).
- **CER frouxo (secundário)**: aplica ainda o colapso de sequências
  internas de espaços/quebras de linha em um único espaço, separando erros
  de conteúdo de erros de estrutura de linha/espaçamento. É apresentado
  como visão complementar, não substitui o bruto.
- **Agregado**: soma das distâncias de edição / soma dos comprimentos das
  referências (apenas os 15 casos com texto; os 2 casos sem texto ficam
  fora do CER, por definição).
- **Casos sem texto**: sem CER (divisão por zero não é inventada); o
  critério é o motor ter retornado texto e blocos vazios.
- As contagens de inserções/exclusões/substituições seguem um
  backtracking determinístico da edição ótima; em caso de empate entre
  caminhos, substituições são preferidas, então a *divisão* entre os três
  tipos é um artefato determinístico, enquanto a distância total é exata.

### 10.3 Como executar a avaliação

O harness (`OcrQualityEvaluationTests`) é **opt-in** para não deixar a suíte
normal lenta: ele só executa com a variável de ambiente
`CAPPARSE_OCR_EVAL=1`. A suíte normal mantém os smoke tests com o modelo
real (seção 9.6), que cobrem a integração.

```powershell
# a partir da raiz do repositório
$env:CAPPARSE_OCR_EVAL="1"
dotnet test -c Debug   --filter "FullyQualifiedName~OcrQualityEvaluationTests"
dotnet test -c Release --filter "FullyQualifiedName~OcrQualityEvaluationTests"
Remove-Item Env:CAPPARSE_OCR_EVAL
```

O harness cria **um** engine (inicialização medida), mede a memória do
processo antes/depois da inicialização, faz **uma passada de aquecimento**
sobre a coleção (identificada separadamente no relatório, excluída das
estatísticas), executa a coleção medindo tempo por imagem e amostras de
`WorkingSet64`, descarta o engine e escreve o relatório em
`tests/CapParse.Tests/TestData/Ocr/reports/evaluation-<Config>.md` (e na
saída do teste). `Process.WorkingSet64` é a memória **residente do processo
inteiro** (test host + engine + modelos + arena do ONNX Runtime), não uma
medição exclusiva do OCR.

### 10.4 Resultados (execução real de 2026-10-10)

Ambiente: Windows 10.0.26200, x64, .NET 8.0.31, CPU, RapidOcrNet 4.2.0
(preset `PPOCRv6Small` + `RapidOcrOptions.PPOCRv6`). Coleção: 17 imagens
(15 com texto, 1.130 caracteres de referência; 2 sem texto).

**Por imagem** (CER bruto; idêntico em Debug e Release — a qualidade do
reconhecimento não depende da configuração):

| Caso | CER bruto | CER frouxo | Exato (bruto) | Exato (frouxo) | Observação |
| --- | --- | --- | --- | --- | --- |
| pt-accent-16-light | 0,000 | 0,000 | sim | sim | — |
| pt-accent-12-light | 0,000 | 0,000 | sim | sim | — |
| pt-accent-24-light | 0,000 | 0,000 | sim | sim | — |
| pt-accent-8-light | 0,020 | 0,020 | não | não | espaço antes do `—` perdido (`português— versão`) |
| pt-accent-8-dark | 1,000 | 1,000 | não | não | **nenhum texto detectado** (0 blocos) |
| pt-accent-12-dark | 0,140 | 0,140 | não | não | `coração`→`oração`, `informação`→`iforação`, `português`→`portugués` |
| pt-accent-24-dark | 0,000 | 0,000 | sim | sim | — |
| multiline-12-light | 0,022 | 0,000 | não | sim | só espaços em branco extras entre linhas |
| multiline-12-dark | 0,022 | 0,000 | não | sim | idem |
| contrast-moderate-light | 0,000 | 0,000 | sim | sim | — |
| contrast-moderate-dark | 0,000 | 0,000 | sim | sim | — |
| numeric-12-light | 0,020 | 0,010 | não | não | espaço de `14:32:05` perdido → `14:32:05` |
| punctuation-12-light | 0,015 | 0,015 | não | não | `<<fim>>` → `< <fim>>` |
| ui-mockup-light | 0,051 | 0,000 | não | sim | espaços duplos do menu colapsados em simples |
| ui-mockup-dark | 0,051 | 0,000 | não | sim | idem |
| no-text-blank | n/d | n/d | — | — | retornou texto/blocos vazios (correto) |
| no-text-shapes | n/d | n/d | — | — | retornou texto/blocos vazios (correto) |

**Agregados (15 casos com texto; os valores são idênticos em Debug e
Release):**

| Métrica | Valor |
| --- | --- |
| Caracteres de referência (total) | 1.130 |
| Distância de edição total | 78 |
| CER agregado (bruto) | 0,069 |
| CER agregado (frouxo) | 0,052 |
| Inserções / exclusões / substituições | 14 / 63 / 1 |
| Transcrição exata (bruta) | 6/15 (40%) |
| Transcrição exata (frouxa) | 10/15 (67%) |
| Casos sem texto com resultado vazio | 2/2 |

**Desempenho** (passada medida, após aquecimento; Debug ≈ Release):

| Medida | Debug | Release |
| --- | --- | --- |
| Inicialização do engine | 186,4 ms | 227,0 ms |
| Reconhecimento — média | 721,3 ms | 726,8 ms |
| Reconhecimento — mediana | 810,8 ms | 827,5 ms |
| Reconhecimento — mín/máx | 230,0 / 999,0 ms | 229,7 / 977,3 ms |
| Passada de aquecimento (total, excluída) | 12.326 ms | 12.378 ms |

**Memória** (`WorkingSet64` do processo inteiro — ver 10.3):

| Ponto | Debug | Release |
| --- | --- | --- |
| Antes da inicialização | 63,1 MB | 62,9 MB |
| Após a inicialização | 131,2 MB (Δ 68,2 MB) | 131,2 MB (Δ 68,3 MB) |
| Mínimo durante a coleção | 1.986,5 MB | 1.985,1 MB |
| Máximo durante a coleção | 2.139,1 MB | 2.136,9 MB |
| Após o `Dispose` do engine | 254,8 MB | 254,3 MB |

O salto de ~131 MB para ~2 GB durante a coleção é o comportamento da
**arena de alocação do ONNX Runtime**, que cresce conforme as execuções e
não devolve a memória a cada inferência; o valor é do processo completo e
não é a memória exclusiva do OCR. O `Dispose` do engine devolve a maior
parte ao fim da execução.

### 10.5 Análise

1. **Erros se concentram em fonte pequena sobre fundo escuro.** O pior
   caso da coleção é `pt-accent-8-dark` (falha total: nada detectado) e o
   segundo pior é `pt-accent-12-dark` (CER 0,140, com corrupção de
   acentos: `coração`→`oração`, `informação`→`iforação`, e a única
   substituição da coleção, `ç`→`s` em `portugués`). Em fundo claro, o
   mesmo texto em 8 pt tem CER de apenas 0,020. O dark mode em tamanhos
   pequenos é a condição mais adversa do conjunto.
2. **Acentos em si não são o problema principal em condição boa.** Em
   fundo claro, todos os casos com acentos (12/16/24 pt) foram
   transcritos exatamente, incluindo `â`, `ã`, `ç`, `ê`, `ô` e `ú`. As
   confusões de acento aparecem no 12 pt escuro, onde o motor troca
   dígrafos inteiros (coração→oração), indicando perda de informação da
   caixa detectada, não erro pontual de caractere.
3. **O erro dominante é a perda de espaços (exclusões: 63 de 78).**
   O modelo de reconhecimento do PP-OCRv6 concatena as caixas de
   caracteres e não emite consistentemente o token de espaço: `14:32:05`
   vira `14:32:05`, o menu `Arquivo  Editar  Ver  Ajuda` perde os espaços
   duplos, e o espaço antes do travessão some em 8 pt. Em conteúdo de UI,
   onde espaços separam itens, isso é o principal risco de legibilidade
   do texto extraído — visível no CER bruto e praticamente desaparece na
   visão frouxa (0,069 → 0,052; exato 6/15 → 10/15).
4. **Pontuação é reconhecida com pequenas imperfeições** (`<<fim>>` →
   `< <fim>>`; demais pontuações — parênteses, aspas, `!`, `:`, `/`, `|` —
   corretas).
5. **Sem alucinação**: as duas imagens sem texto (lisa e com formas
   geométricas) retornaram texto e blocos vazios, o comportamento
   esperado e projetado (seção 3.3).
6. **Contraste moderado é suportado**: os dois casos de cinza médio
   (luz e escuro) foram transcritos exatamente.

**Limitações desta avaliação** (interpretar os números à luz delas):

- a coleção é **sintética e pequena** (17 imagens, 1.130 caracteres):
  mede o mecanismo em condições controladas, não a qualidade em capturas
  reais de tela (ClearType, JPEG-like artefatos de escala, DPI ≠ 100%,
  fotos de documento, mão/curva de texto). Nenhum limite universal de
  qualidade é declarado a partir dela;
- o anti-aliasing usado na geração é em cinza (`AntiAliasGridFit`), não
  ClearType como nas telas reais — o gap favorece levemente o motor;
- uma única execução por configuração: variabilidade de execução não foi
  medida (os tempos têm variável de ±5% entre as passadas de aquecimento e
  medida);
- `WorkingSet64` mede o processo inteiro, não o OCR isolado;
- os parâmetros do preset `PPOCRv6` foram mantidos intactos: nenhuma
  configuração foi ajustada para melhorar a pontuação, e nenhuma mudança
  de modelo/parâmetro foi proposta porque a coleção não justifica uma
  comparação controlada além de registrar as limitações.

**Conclusão do M7-C**: o PP-OCRv6 Small via RapidOcrNet 4.2.0 reconhece
português com acentos com alta fidelidade em condição boa (fundo claro,
12 pt ou maior, contraste alto/moderado — 6 de 15 exatos na métrica bruta
e 10 de 15 na frouxa; CER agregado bruto 0,069), falha em alucinar texto
onde não há, e apresenta duas limitações reais a levar ao M8: (a) fonte
pequena em fundo escuro degrada severamente (12 pt escuro: CER 0,140;
8 pt escuro: nada detectado) e (b) espaços entre palavras/tokens são
frequentemente perdidos no texto reconstruído. Nenhum código de produção
foi alterado no M7-C: as limitações observadas são do modelo/preset, e
ajustá-las (ex.: thresholds do preset, pré-processamento de contraste)
exigiria uma comparação controlada com hipótese registrada, fora do escopo
desta etapa de medição.

### 10.6 Arquivos do M7-C

| Arquivo | Papel |
| --- | --- |
| `tests/CapParse.Tests/TestData/Ocr/manifest.json` | fonte de verdade: referências + parâmetros de geração |
| `tests/CapParse.Tests/TestData/Ocr/*.png` (17) | coleção de imagens commitada |
| `tests/CapParse.Tests/TestData/Ocr/reports/evaluation-Debug.md`, `evaluation-Release.md` | relatórios gerados do harness |
| `tests/CapParse.Tests/CharacterErrorRate.cs` | Levenshtein + CER + normalização |
| `tests/CapParse.Tests/CharacterErrorRateTests.cs` | validação do cálculo do CER |
| `tests/CapParse.Tests/OcrCollectionManifest.cs` | manifesto + renderização dos casos |
| `tests/CapParse.Tests/OcrCollectionGeneratorTests.cs` | bootstrap/verificação da coleção (na suíte normal) |
| `tests/CapParse.Tests/OcrQualityEvaluationTests.cs` | harness opt-in (`CAPPARSE_OCR_EVAL=1`) |

Nenhuma mudança em `src/CapParse/` (nenhuma integração com UI, overlay ou
clipboard) e nenhuma nova dependência: o M7-C é exclusivamente medição.
