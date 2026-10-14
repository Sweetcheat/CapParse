# Experimento A/B — Memória do ONNX Runtime (sessão 2026-10-14)

Comparação das opções de alocação de memória do ONNX Runtime no engine OCR
(CapParse, RapidOcrNet 4.2.0 / ONNX Runtime 1.29.0, modelo PP-OCRv6 Small).
Objetivo: determinar se desabilitar a arena de memória da CPU e/ou o padrão de
memória reduz o consumo de memória, especialmente o pico observado durante as
primeiras inferências, e a que custo de latência.

**Nenhum commit, push, tag ou release foi criado. A configuração de produção
(variante `default`) permanece inalterada; as variantes experimentais são
opt-in por variável de ambiente.**

## 1. Variantes

| Variante | `CAPPARSE_ORT_SESSION` | `EnableCpuMemArena` | `EnableMemoryPattern` | Papel |
| --- | --- | --- | --- | --- |
| A | (ausente / `default`) | `true` (padrão) | `true` (padrão) | Baseline — comportamento atual de produção |
| B | `compact` | `false` | `false` | A/B principal — ambas desabilitadas (par do MewuAI) |
| C | `noarena` | `false` | `true` (padrão) | Isola a arena |
| D | `nopattern` | `true` (padrão) | `false` | Isola o padrão de memória |

A variante é resolvida **antes** da criação das sessões (no construtor do
`RapidOcrEngine`). As variantes B/C/D partem de `RapidOcr.GetDefaultSessionOptions()`
(idêntico à variante A) e desabilitam apenas a(s) opção(ões) do alvo, de modo que
a **única** diferença em relação à produção é a estratégia de alocação — nenhum
outro parâmetro (modelo, `TextScore`, `ReturnWordBox`, threads, otimiz. de grafo)
muda. O `SessionOptions` é de propriedade do chamador e é descartado logo após a
criação das três sessões (o `InferenceSession` lê apenas o handle das opções no
momento da criação).

## 2. Método de medição

- **Processo limpo por execução:** cada uma das 12 execuções foi um processo
  `dotnet test -c Release` independente (o test host é criado e destruído a cada
  invocação), então não há reuso de sessões/buffers entre variantes ou repetições.
- **Métrica de memória:** `Process.WorkingSet64` (Working Set — memória residente
  do processo inteiro: test host + engine + modelos) e `Process.PrivateMemorySize64`
  (memória privada/comprometida). São reportadas separadamente: uma queda no
  Working Set **não** é equivalente a uma queda na memória comprometida.
- **Pontos medidos:** antes do init, após o init, após a 1ª inferência (fim do
  1º item do aquecimento), mín/máx ao longo da coleção medida (17 imagens), e após
  o `Dispose` do engine.
- **Qualidade:** CER bruto e frouxo, distância de edição, transcrição exata — os
  mesmos fixtures e referências do harness M7-C.
- **Repetições:** 3 por variante, em Release, mesma sequência de imagens.

> O Working Set inclui o test host e o engine; **não** é memória exclusiva do
> ONNX. Os valores absolutos devem ser lidos como "memória do processo durante a
> avaliação", não como "memória do OCR".

## 3. Resultados — memória (Working Set, MB)

Valores individuais das 3 execuções e a mediana.

| Ponto | A: r1 / r2 / r3 → **med** | B: r1 / r2 / r3 → **med** | C: r1 / r2 / r3 → **med** | D: r1 / r2 / r3 → **med** |
| --- | --- | --- | --- | --- |
| Antes do init | 72,7 / 72,8 / 72,8 → **72,8** | 72,8 / 72,8 / 72,7 → **72,8** | 72,8 / 72,8 / 72,7 → **72,8** | 72,8 / 72,8 / 72,7 → **72,8** |
| Após o init | 141,4 / 141,7 / 141,4 → **141,4** | 141,3 / 140,8 / 141,6 → **141,3** | 141,4 / 141,7 / 141,4 → **141,4** | 141,4 / 140,9 / 141,5 → **141,4** |
| Após a 1ª inferência | 942,6 / 943,0 / 942,6 → **942,6** | 230,9 / 228,6 / 231,8 → **230,9** | 230,9 / 231,2 / 229,2 → **230,9** | 942,5 / 942,5 / 942,8 → **942,5** |
| Mínimo na coleção | 2003,3 / 1996,1 / 2004,9 → **2003,3** | 404,2 / 390,5 / 391,6 → **391,6** | 396,2 / 393,3 / 481,6 → **396,2** | 1165,1 / 1166,9 / 1159,5 → **1165,1** |
| Máximo na coleção (pico) | 2082,2 / 2148,2 / 2136,1 → **2136,1** | 578,4 / 464,7 / 465,4 → **465,4** | 468,5 / 466,8 / 586,5 → **468,5** | 1243,1 / 1243,5 / 1352,4 → **1243,5** |
| Após o `Dispose` | 264,7 / 264,4 / 375,4 → **264,7** | 250,1 / 263,5 / 265,4 → **263,5** | 265,4 / 265,2 / 326,6 → **265,4** | 264,6 / 264,8 / 233,7 → **264,6** |

Variação entre repetições (pico,Working Set): A 2082–2148 (Δ66), B 465–578 (Δ114),
C 467–587 (Δ120), D 1243–1352 (Δ109) MB. A diferença A→B/C (~1670 MB) é ~14–25×
a variação interna de qualquer variante — o efeito está muito além do ruído.

## 4. Resultados — memória privada (commit, MB)

| Ponto | A **med** | B **med** | C **med** | D **med** |
| --- | --- | --- | --- | --- |
| Após a 1ª inferência | 1264,8 | 172,4 | 172,3 | 1264,7 |
| Mínimo na coleção | 2518,4 | 330,6 | 335,4 | 1459,5 |
| Máximo na coleção (pico) | 2644,6 | 405,8 | 409,9 | 1539,4 |
| Após o `Dispose` | 204,7 | 202,7 | 205,3 | 203,5 |

A memória **comprometida** cai na mesma proporção (pico A→B/C: 2645→~406 MB,
−84%). Isso descarta a leitura de que o pico seria só um artefato de paginação do
Working Set: a retenção é memória nativa real, comprometida.

## 5. Resultados — latência (ms)

Mediana das medianas por variante (e a mediana das médias) e tempo de init.

| Variante | Init (med) | Reconhecer — mediana (med) | Reconhecer — média (med) |
| --- | --- | --- | --- |
| A (default) | 187,8 | 807,9 | 720,1 |
| B (compact) | 181,2 | 938,3 | 836,8 |
| C (noarena) | 191,0 | 828,2 | 745,9 |
| D (nopattern) | 194,5 | 800,4 | 729,9 |

- **B** (ambas off): mediana ~938 ms vs A ~808 ms → **~+16%**.
- **C** (só arena off): mediana ~828 ms vs A ~808 ms → **~+2,5%**.
- **D** (só pattern off): mediana ~800 ms ≈ A → **sem custo mensurável**.
- Tempo de init idêntico em todas as variantes (~180–195 ms).

O custo de latência vem da **arena**: desabilitá-la (C) adiciona ~2,5%;
desabilitar também o padrão (B) eleva para ~16%. O padrão de memória, isolado
(D), não custa latência.

## 6. Qualidade do OCR

Idêntica nas **12 execuções** (texto reconhecido byte-idêntico entre variantes e
entre repetições — verificado por `diff` dos relatórios):

| Métrica | A | B | C | D |
| --- | --- | --- | --- | --- |
| CER bruto | 0,058 | 0,058 | 0,058 | 0,058 |
| CER frouxo | 0,052 | 0,052 | 0,052 | 0,052 |
| Distância de edição total | 65 | 65 | 65 | 65 |
| Exato (bruto) | 8/15 | 8/15 | 8/15 | 8/15 |
| Exato (frouxo) | 10/15 | 10/15 | 10/15 | 10/15 |

Como esperado: opções de alocação de memória não alteram o algoritmo de
reconhecimento.

## 7. Interpretação

**G1 — A variante B reduziu o pico?** Sim, de forma robusta. Pico (máx. da
coleção, Working Set): A ~2136 MB → B ~465 MB (−78%). A redução (~1670 MB) é
~14–25× a variação entre repetições de qualquer variante.

**G2 — A variante B reduziu a memória estabilizada?** Sim, e essa é a parte mais
importante na prática. O platô (mín. da coleção): A ~2003 MB → B ~392 MB (−80%).
Na variante A a arena **retém** o alocamento de pico pelo lifetime inteiro da
sessão, mantendo o processo em ~2 GB; na variante B a memória é liberada após cada
inferência e o processo fica em ~400 MB. Como o app mantém o engine vivo pelo
processo inteiro (`OcrCoordinator`), a diferença se sustenta durante todo o uso —
não é só um pico transitório.

**G3 — Custo de desempenho?** B: ~+16% na latência mediana de reconhecimento.
C (só arena): ~+2,5%. D (só pattern): ~0%. Init inalterado.

**G4 — Qualidade equivalente?** Sim — texto byte-idêntico, CER/exatos idênticos
nas 12 execuções.

**G5 — Hipótese da arena?** **Forte e isolada.** A separação C vs D é decisiva:
- Desabilitar **apenas a arena** (C) elimina praticamente todo o efeito: pico
  ~469 MB, platô ~396 MB — indistinguível de B (ambas off: ~465 / ~392 MB).
- Desabilitar **apenas o padrão** (D) reduz parcialmente: pico ~1244 MB, platô
  ~1165 MB (−42%).
- Conclusão: a **arena de CPU é a causa dominante** da retenção (~99% do ganho);
  o padrão de memória é um fator secundário menor. A hipótese original (arena) é
  confirmada; a contribuição do padrão é real mas pequena.

**O que continua desconhecido / limitações:**
- 3 repetições por variante: não sustenta inferência estatística formal
  (significância); as conclusões se apoiam no fato de o efeito ser muito maior
  que a variação observada.
- A medição é do processo inteiro (test host + engine), não isolada do ONNX; os
  valores são "memória do processo", não "memória do OCR".
- A coleção M7-C é sintética (imagens pequenas, máx. ~1200×360). Em capturas
  reais (maiores, mais linhas) os absolutos mudam, mas o *mecanismo* (arena
  retendo o pico) é o mesmo; a proporção de redução tende a se manter.
- "Após a 1ª inferência" é um proxy do pico: a arena cresce ao longo das primeiras
  inferências do aquecimento (A: 943 MB após a 1ª → ~2000 MB ao fim do
  aquecimento), então o pico real é o platô, não o valor após a 1ª.
- O `Dispose` devolve a maior parte da memória em todas as variantes (~250–265 MB
  residuais = heap do test host), o que já afastava vazamento acumulado; o
  experimento localiza a causa na estratégia de alocação, não em vazamento.

## 8. Recomendação técnica

**A causa do consumo elevado é a arena de memória da CPU do ONNX Runtime**
(`EnableCpuMemArena=true`, padrão), que retém o alocamento de pico pelo lifetime
da sessão. Desabilitá-la reduz o pico de ~2,1 GB para ~0,47 GB e a memória
estabilizada de ~2,0 GB para ~0,40 GB, com custo de latência de ~2,5% e **zero**
impacto na qualidade.

Recomenda-se, **sujeita à autorização do usuário** (esta sessão não altera a
produção):

1. **Opção preferida:** desabilitar apenas `EnableCpuMemArena` (variante C).
   Captura praticamente todo o ganho de memória (pico ~469 MB, platô ~396 MB)
   ao menor custo de latência (~+2,5%), mantendo o padrão de memória ativo.
2. **Opção alternativa:** desabilitar as duas (variante B, o par do MewuAI).
   Ganho marginalmente maior em memória (~2 MB no pico — dentro do ruído) mas
   custo de latência maior (~+16%). Só vale a pena se a latência for irrelevante
   e se se quiser alinhar com o padrão do MewuAI.

O ganho é particularmente relevante porque o app mantém o engine vivo por todo o
processo: a arena retida significaria ~2 GB em uso contínuo, enquanto a variante
C o manteria em ~0,4 GB.

**Decisão pendente do usuário:** adotar a variante C (recomendada) ou B como
configuração padrão de produção, ou manter o padrão e encerrar. Nenhuma mudança
de produção foi feita nesta sessão.

## 9. Artefatos

- Relatórios por execução (12): `memory-ab/{A,B,C,D}-Release-run{1,2,3}.md`
- Relatórios P1 preservados (pré-experimento): `memory-ab/p1-{Debug,Release}.md`
- Baseline pré-P1 (intocado): `../baseline/evaluation-{Debug,Release}.md`
- Código: `src/CapParse/OCR/RapidOcrEngine.cs` (seleção de variante opt-in);
  harness `tests/CapParse.Tests/OcrQualityEvaluationTests.cs` (métricas de
  Working Set + privada + variante no relatório); testes
  `tests/CapParse.Tests/OrtSessionVariantTests.cs`.

## 10. Como reproduzir

A partir da raiz do repositório (Release), uma execução por processo:

```powershell
# Variante A (baseline / produção)
$env:CAPPARSE_OCR_EVAL="1"; dotnet test -c Release --filter "FullyQualifiedName~OcrQualityEvaluationTests"
# Variante B (compact)
$env:CAPPARSE_OCR_EVAL="1"; $env:CAPPARSE_ORT_SESSION="compact"; dotnet test -c Release --filter "FullyQualifiedName~OcrQualityEvaluationTests"
# Variante C (noarena)  -> CAPPARSE_ORT_SESSION="noarena"
# Variante D (nopattern) -> CAPPARSE_ORT_SESSION="nopattern"
```

Cada invocação do `dotnet test` é um processo novo (isolamento). Os relatórios
são gravados em `tests/CapParse.Tests/TestData/Ocr/reports/evaluation-Release[-<variante>].md`.
