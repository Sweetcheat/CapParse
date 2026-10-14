# M7-C — Avaliação de qualidade do OCR (relatório gerado)

Gerado em: 2026-10-10 11:10:09 por `OcrQualityEvaluationTests`.

## Ambiente

| Item | Valor |
| --- | --- |
| Configuração | Release |
| SO | Microsoft Windows 10.0.26200 |
| Arquitetura do processo | X64 |
| Runtime | .NET 8.0.31 |
| Mecanismo | RapidOcrNet 4.2.0, preset `PPOCRv6Small` + `RapidOcrOptions.PPOCRv6`, CPU |
| Modelo | PP-OCRv6 Small (det/rec small + classificador v5), 100% local |

## Inicialização

- Tempo de inicialização do engine: 227,0 ms
- Memória do processo antes da inicialização: 62,9 MB
- Memória do processo após a inicialização: 131,2 MB (Δ 68,3 MB)

## Aquecimento (excluído das estatísticas)

- Passada completa da coleção antes das medições: 12377,8 ms no total.

## Resultados por imagem

CER bruto = distância de Levenshtein(referência, hipótese) / comprimento da referência, com acentos e caso preservados; apenas espaços nas bordas são removidos. CER frouxo aplica ainda o colapso de sequências internas de espaços/quebras de linha em um único espaço.

| Caso | CER bruto | CER frouxo | Exato (bruto) | Exato (frouxo) | Tempo (ms) | Erro |
| --- | --- | --- | --- | --- | --- | --- |
| pt-accent-16-light | 0,000 | 0,000 | sim | sim | 957,7 |  |
| pt-accent-12-light | 0,000 | 0,000 | sim | sim | 971,4 |  |
| pt-accent-24-light | 0,000 | 0,000 | sim | sim | 827,5 |  |
| pt-accent-8-light | 0,020 | 0,020 | não | não | 973,6 |  |
| pt-accent-8-dark | 1,000 | 1,000 | não | não | 950,3 |  |
| pt-accent-12-dark | 0,140 | 0,140 | não | não | 953,7 |  |
| pt-accent-24-dark | 0,000 | 0,000 | sim | sim | 806,7 |  |
| multiline-12-light | 0,022 | 0,000 | não | sim | 510,5 |  |
| multiline-12-dark | 0,022 | 0,000 | não | sim | 507,9 |  |
| contrast-moderate-light | 0,000 | 0,000 | sim | sim | 977,3 |  |
| contrast-moderate-dark | 0,000 | 0,000 | sim | sim | 975,4 |  |
| numeric-12-light | 0,020 | 0,010 | não | não | 676,1 |  |
| punctuation-12-light | 0,015 | 0,015 | não | não | 961,1 |  |
| ui-mockup-light | 0,051 | 0,000 | não | sim | 424,3 |  |
| ui-mockup-dark | 0,051 | 0,000 | não | sim | 420,5 |  |
| no-text-blank | n/d | n/d | — | — | 232,0 |  |
| no-text-shapes | n/d | n/d | — | — | 229,7 |  |

### Detalhes por caso (referência × reconhecimento)

#### pt-accent-16-light

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação, coração, informação e português.  ⏎ `
- Blocos: 1; tempo: 957,7 ms

#### pt-accent-12-light

- Referência: `Árvore, ângulo, órgão, ação, coração, informação, órfão, ânsia, caráter e ênfase.`
- Reconhecido: `Árvore, ângulo, órgão, ação, coração, informação, órfão, ânsia, caráter e ênfase.  ⏎ `
- Blocos: 1; tempo: 971,4 ms

#### pt-accent-24-light

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação, coração, informação e português.  ⏎ `
- Blocos: 1; tempo: 827,5 ms

#### pt-accent-8-light

- Referência: `Ação, coração, informação e português — versão 1.0`
- Reconhecido: `Ação, coração, informação e português— versão 1.0  ⏎ `
- Blocos: 1; tempo: 973,6 ms

#### pt-accent-8-dark

- Referência: `Ação, coração, informação e português — versão 1.0`
- Reconhecido: ``
- Blocos: 0; tempo: 950,3 ms

#### pt-accent-12-dark

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação oração iforação e portugués.  ⏎ `
- Blocos: 1; tempo: 953,7 ms

#### pt-accent-24-dark

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação, coração, informação e português.  ⏎ `
- Blocos: 1; tempo: 806,7 ms

#### multiline-12-light

- Referência: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Reconhecido: `Configuração de captura  ⏎ Monitores: 2 — Resolução: 1920x1080  ⏎ Última captura: 2026-10-10 14:32  ⏎ `
- Blocos: 3; tempo: 510,5 ms

#### multiline-12-dark

- Referência: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Reconhecido: `Configuração de captura  ⏎ Monitores: 2 — Resolução: 1920x1080  ⏎ Última captura: 2026-10-10 14:32  ⏎ `
- Blocos: 3; tempo: 507,9 ms

#### contrast-moderate-light

- Referência: `Atenção: o texto está em cinza médio sobre fundo branco. Contraste moderado.`
- Reconhecido: `Atenção: o texto está em cinza médio sobre fundo branco. Contraste moderado.  ⏎ `
- Blocos: 1; tempo: 977,3 ms

#### contrast-moderate-dark

- Referência: `Atenção: o texto está em cinza claro sobre fundo escuro. Contraste moderado.`
- Reconhecido: `Atenção: o texto está em cinza claro sobre fundo escuro. Contraste moderado.  ⏎ `
- Blocos: 1; tempo: 975,4 ms

#### numeric-12-light

- Referência: `Data: 2026-10-10 14:32:05 — ID: 0x1F3A-9920 ⏎ Itens: 42/100 — Total: 1.234,56 R$ — Versão: 2.4.1-beta.7`
- Reconhecido: `Data: 2026-10-1014:32:05 — ID: 0x1F3A-9920  ⏎ Itens: 42/100 — Total: 1.234,56 R$ — Versão: 2.4.1-beta.7  ⏎ `
- Blocos: 2; tempo: 676,1 ms

#### punctuation-12-light

- Referência: `Atenção (item 3): não é 'erro' — é um aviso! [OK / Cancelar] <<fim>>`
- Reconhecido: `Atenção (item 3): não é 'erro' — é um aviso! [OK / Cancelar] < <fim>>  ⏎ `
- Blocos: 1; tempo: 961,1 ms

#### ui-mockup-light

- Referência: `CapParse — Captura de Tela ⏎ Arquivo  Editar  Ver  Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Reconhecido: `CapParse — Captura de Tela  ⏎ Arquivo Editar Ver Ajuda  ⏎ Status: pronto  ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45%  ⏎ Atalho: Ctrl+Shift+X  ⏎ `
- Blocos: 5; tempo: 424,3 ms

#### ui-mockup-dark

- Referência: `CapParse — Captura de Tela ⏎ Arquivo  Editar  Ver  Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Reconhecido: `CapParse — Captura de Tela  ⏎ Arquivo Editar Ver Ajuda  ⏎ Status: pronto  ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45%  ⏎ Atalho: Ctrl+Shift+X  ⏎ `
- Blocos: 5; tempo: 420,5 ms

#### no-text-blank

- Referência: ``
- Reconhecido: ``
- Blocos: 0; tempo: 232,0 ms

#### no-text-shapes

- Referência: ``
- Reconhecido: ``
- Blocos: 0; tempo: 229,7 ms

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
| Tempo de reconhecimento — média | 726,8 ms |
| Tempo de reconhecimento — mediana | 827,5 ms |
| Tempo de reconhecimento — mín/máx | 229,7 / 977,3 ms |

## Memória

`Process.WorkingSet64`: amostra da memória residente do processo inteiro (test host + engine + modelos), não uma medição exclusiva do OCR.

| Ponto | MB |
| --- | --- |
| Antes da inicialização | 62,9 |
| Após a inicialização | 131,2 |
| Mínimo durante a coleção | 1985,1 |
| Máximo durante a coleção | 2136,9 |
| Após o Dispose do engine | 254,3 |

## Aquecimento — tempos por caso (para referência)

| Caso | Tempo (ms) |
| --- | --- |
| pt-accent-16-light | 982,9 |
| pt-accent-12-light | 931,0 |
| pt-accent-24-light | 774,8 |
| pt-accent-8-light | 965,3 |
| pt-accent-8-dark | 963,3 |
| pt-accent-12-dark | 953,1 |
| pt-accent-24-dark | 870,9 |
| multiline-12-light | 518,7 |
| multiline-12-dark | 515,6 |
| contrast-moderate-light | 986,5 |
| contrast-moderate-dark | 975,8 |
| numeric-12-light | 671,7 |
| punctuation-12-light | 966,9 |
| ui-mockup-light | 414,4 |
| ui-mockup-dark | 427,3 |
| no-text-blank | 226,9 |
| no-text-shapes | 232,9 |
