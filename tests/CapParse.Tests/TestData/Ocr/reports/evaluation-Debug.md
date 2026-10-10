# M7-C — Avaliação de qualidade do OCR (relatório gerado)

Gerado em: 2026-10-10 11:09:42 por `OcrQualityEvaluationTests`.

## Ambiente

| Item | Valor |
| --- | --- |
| Configuração | Debug |
| SO | Microsoft Windows 10.0.26200 |
| Arquitetura do processo | X64 |
| Runtime | .NET 8.0.31 |
| Mecanismo | RapidOcrNet 4.2.0, preset `PPOCRv6Small` + `RapidOcrOptions.PPOCRv6`, CPU |
| Modelo | PP-OCRv6 Small (det/rec small + classificador v5), 100% local |

## Inicialização

- Tempo de inicialização do engine: 216,6 ms
- Memória do processo antes da inicialização: 63,0 MB
- Memória do processo após a inicialização: 131,7 MB (Δ 68,7 MB)

## Aquecimento (excluído das estatísticas)

- Passada completa da coleção antes das medições: 12157,2 ms no total.

## Resultados por imagem

CER bruto = distância de Levenshtein(referência, hipótese) / comprimento da referência, com acentos e caso preservados; apenas espaços nas bordas são removidos. CER frouxo aplica ainda o colapso de sequências internas de espaços/quebras de linha em um único espaço.

| Caso | CER bruto | CER frouxo | Exato (bruto) | Exato (frouxo) | Tempo (ms) | Erro |
| --- | --- | --- | --- | --- | --- | --- |
| pt-accent-16-light | 0,000 | 0,000 | sim | sim | 943,3 |  |
| pt-accent-12-light | 0,000 | 0,000 | sim | sim | 952,9 |  |
| pt-accent-24-light | 0,000 | 0,000 | sim | sim | 799,4 |  |
| pt-accent-8-light | 0,020 | 0,020 | não | não | 944,5 |  |
| pt-accent-8-dark | 1,000 | 1,000 | não | não | 954,2 |  |
| pt-accent-12-dark | 0,140 | 0,140 | não | não | 953,0 |  |
| pt-accent-24-dark | 0,000 | 0,000 | sim | sim | 817,5 |  |
| multiline-12-light | 0,022 | 0,000 | não | sim | 517,5 |  |
| multiline-12-dark | 0,022 | 0,000 | não | sim | 499,6 |  |
| contrast-moderate-light | 0,000 | 0,000 | sim | sim | 956,1 |  |
| contrast-moderate-dark | 0,000 | 0,000 | sim | sim | 947,0 |  |
| numeric-12-light | 0,020 | 0,010 | não | não | 680,0 |  |
| punctuation-12-light | 0,015 | 0,015 | não | não | 948,3 |  |
| ui-mockup-light | 0,051 | 0,000 | não | sim | 436,7 |  |
| ui-mockup-dark | 0,051 | 0,000 | não | sim | 415,5 |  |
| no-text-blank | n/d | n/d | — | — | 219,9 |  |
| no-text-shapes | n/d | n/d | — | — | 219,4 |  |

### Detalhes por caso (referência × reconhecimento)

#### pt-accent-16-light

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação, coração, informação e português.  ⏎ `
- Blocos: 1; tempo: 943,3 ms

#### pt-accent-12-light

- Referência: `Árvore, ângulo, órgão, ação, coração, informação, órfão, ânsia, caráter e ênfase.`
- Reconhecido: `Árvore, ângulo, órgão, ação, coração, informação, órfão, ânsia, caráter e ênfase.  ⏎ `
- Blocos: 1; tempo: 952,9 ms

#### pt-accent-24-light

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação, coração, informação e português.  ⏎ `
- Blocos: 1; tempo: 799,4 ms

#### pt-accent-8-light

- Referência: `Ação, coração, informação e português — versão 1.0`
- Reconhecido: `Ação, coração, informação e português— versão 1.0  ⏎ `
- Blocos: 1; tempo: 944,5 ms

#### pt-accent-8-dark

- Referência: `Ação, coração, informação e português — versão 1.0`
- Reconhecido: ``
- Blocos: 0; tempo: 954,2 ms

#### pt-accent-12-dark

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação oração iforação e portugués.  ⏎ `
- Blocos: 1; tempo: 953,0 ms

#### pt-accent-24-dark

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação, coração, informação e português.  ⏎ `
- Blocos: 1; tempo: 817,5 ms

#### multiline-12-light

- Referência: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Reconhecido: `Configuração de captura  ⏎ Monitores: 2 — Resolução: 1920x1080  ⏎ Última captura: 2026-10-10 14:32  ⏎ `
- Blocos: 3; tempo: 517,5 ms

#### multiline-12-dark

- Referência: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Reconhecido: `Configuração de captura  ⏎ Monitores: 2 — Resolução: 1920x1080  ⏎ Última captura: 2026-10-10 14:32  ⏎ `
- Blocos: 3; tempo: 499,6 ms

#### contrast-moderate-light

- Referência: `Atenção: o texto está em cinza médio sobre fundo branco. Contraste moderado.`
- Reconhecido: `Atenção: o texto está em cinza médio sobre fundo branco. Contraste moderado.  ⏎ `
- Blocos: 1; tempo: 956,1 ms

#### contrast-moderate-dark

- Referência: `Atenção: o texto está em cinza claro sobre fundo escuro. Contraste moderado.`
- Reconhecido: `Atenção: o texto está em cinza claro sobre fundo escuro. Contraste moderado.  ⏎ `
- Blocos: 1; tempo: 947,0 ms

#### numeric-12-light

- Referência: `Data: 2026-10-10 14:32:05 — ID: 0x1F3A-9920 ⏎ Itens: 42/100 — Total: 1.234,56 R$ — Versão: 2.4.1-beta.7`
- Reconhecido: `Data: 2026-10-1014:32:05 — ID: 0x1F3A-9920  ⏎ Itens: 42/100 — Total: 1.234,56 R$ — Versão: 2.4.1-beta.7  ⏎ `
- Blocos: 2; tempo: 680,0 ms

#### punctuation-12-light

- Referência: `Atenção (item 3): não é 'erro' — é um aviso! [OK / Cancelar] <<fim>>`
- Reconhecido: `Atenção (item 3): não é 'erro' — é um aviso! [OK / Cancelar] < <fim>>  ⏎ `
- Blocos: 1; tempo: 948,3 ms

#### ui-mockup-light

- Referência: `CapParse — Captura de Tela ⏎ Arquivo  Editar  Ver  Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Reconhecido: `CapParse — Captura de Tela  ⏎ Arquivo Editar Ver Ajuda  ⏎ Status: pronto  ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45%  ⏎ Atalho: Ctrl+Shift+X  ⏎ `
- Blocos: 5; tempo: 436,7 ms

#### ui-mockup-dark

- Referência: `CapParse — Captura de Tela ⏎ Arquivo  Editar  Ver  Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Reconhecido: `CapParse — Captura de Tela  ⏎ Arquivo Editar Ver Ajuda  ⏎ Status: pronto  ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45%  ⏎ Atalho: Ctrl+Shift+X  ⏎ `
- Blocos: 5; tempo: 415,5 ms

#### no-text-blank

- Referência: ``
- Reconhecido: ``
- Blocos: 0; tempo: 219,9 ms

#### no-text-shapes

- Referência: ``
- Reconhecido: ``
- Blocos: 0; tempo: 219,4 ms

## Casos sem texto

Para referência vazia o CER não é definido (não há divisão por zero): o critério é se o motor retornou texto/blocos vazios.

| Caso | Retornou texto vazio? | Blocos | Texto retornado |
| --- | --- | --- | --- |
| no-text-blank | sim | 0 | `` |
| no-text-shapes | sim | 0 | `` |

## Agregados (casos com texto)

| Métrica | Valor |
| --- | --- |
| Casos com texto avaliados | 15 |
| Caracteres de referência (total) | 1130 |
| Distância de edição total | 78 |
| CER agregado (soma das distâncias / soma das referências) | 0,069 |
| CER agregado (visão frouxa) | 0,052 |
| Inserções / exclusões / substituições (total) | 14 / 63 / 1 |
| Transcrição exata (bruta) | 6/15 |
| Transcrição exata (visão frouxa) | 10/15 |
| Tempo de reconhecimento — média | 717,9 ms |
| Tempo de reconhecimento — mediana | 817,5 ms |
| Tempo de reconhecimento — mín/máx | 219,4 / 956,1 ms |

## Memória

`Process.WorkingSet64`: amostra da memória residente do processo inteiro (test host + engine + modelos), não uma medição exclusiva do OCR.

| Ponto | MB |
| --- | --- |
| Antes da inicialização | 63,0 |
| Após a inicialização | 131,7 |
| Mínimo durante a coleção | 1984,0 |
| Máximo durante a coleção | 2140,4 |
| Após o Dispose do engine | 253,9 |

## Aquecimento — tempos por caso (para referência)

| Caso | Tempo (ms) |
| --- | --- |
| pt-accent-16-light | 974,8 |
| pt-accent-12-light | 926,4 |
| pt-accent-24-light | 794,4 |
| pt-accent-8-light | 973,7 |
| pt-accent-8-dark | 944,9 |
| pt-accent-12-dark | 938,6 |
| pt-accent-24-dark | 787,6 |
| multiline-12-light | 507,9 |
| multiline-12-dark | 509,0 |
| contrast-moderate-light | 961,4 |
| contrast-moderate-dark | 947,6 |
| numeric-12-light | 655,2 |
| punctuation-12-light | 948,3 |
| ui-mockup-light | 417,8 |
| ui-mockup-dark | 418,3 |
| no-text-blank | 218,7 |
| no-text-shapes | 232,7 |
