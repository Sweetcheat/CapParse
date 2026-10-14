# M7-C — Avaliação de qualidade do OCR (relatório gerado)

Gerado em: 2026-10-14 17:45:16 por `OcrQualityEvaluationTests`.

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

- Tempo de inicialização do engine: 182,6 ms
- Memória do processo antes da inicialização: 72,3 MB
- Memória do processo após a inicialização: 140,2 MB (Δ 67,9 MB)

## Aquecimento (excluído das estatísticas)

- Passada completa da coleção antes das medições: 12791,9 ms no total.

## Resultados por imagem

CER bruto = distância de Levenshtein(referência, hipótese) / comprimento da referência, com acentos e caso preservados; apenas espaços nas bordas são removidos. CER frouxo aplica ainda o colapso de sequências internas de espaços/quebras de linha em um único espaço.

| Caso | CER bruto | CER frouxo | Exato (bruto) | Exato (frouxo) | Tempo (ms) | Erro |
| --- | --- | --- | --- | --- | --- | --- |
| pt-accent-16-light | 0,000 | 0,000 | sim | sim | 974,5 |  |
| pt-accent-12-light | 0,000 | 0,000 | sim | sim | 1056,2 |  |
| pt-accent-24-light | 0,000 | 0,000 | sim | sim | 841,0 |  |
| pt-accent-8-light | 0,020 | 0,020 | não | não | 1051,8 |  |
| pt-accent-8-dark | 1,000 | 1,000 | não | não | 1072,3 |  |
| pt-accent-12-dark | 0,140 | 0,140 | não | não | 1156,6 |  |
| pt-accent-24-dark | 0,000 | 0,000 | sim | sim | 988,6 |  |
| multiline-12-light | 0,000 | 0,000 | sim | sim | 571,1 |  |
| multiline-12-dark | 0,000 | 0,000 | sim | sim | 598,2 |  |
| contrast-moderate-light | 0,000 | 0,000 | sim | sim | 1030,6 |  |
| contrast-moderate-dark | 0,000 | 0,000 | sim | sim | 984,4 |  |
| numeric-12-light | 0,010 | 0,010 | não | não | 678,2 |  |
| punctuation-12-light | 0,015 | 0,015 | não | não | 1050,9 |  |
| ui-mockup-light | 0,022 | 0,000 | não | sim | 426,9 |  |
| ui-mockup-dark | 0,022 | 0,000 | não | sim | 427,9 |  |
| no-text-blank | n/d | n/d | — | — | 228,6 |  |
| no-text-shapes | n/d | n/d | — | — | 234,7 |  |

### Detalhes por caso (referência × reconhecimento)

#### pt-accent-16-light

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação, coração, informação e português.`
- Blocos: 1; tempo: 974,5 ms

#### pt-accent-12-light

- Referência: `Árvore, ângulo, órgão, ação, coração, informação, órfão, ânsia, caráter e ênfase.`
- Reconhecido: `Árvore, ângulo, órgão, ação, coração, informação, órfão, ânsia, caráter e ênfase.`
- Blocos: 1; tempo: 1056,2 ms

#### pt-accent-24-light

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação, coração, informação e português.`
- Blocos: 1; tempo: 841,0 ms

#### pt-accent-8-light

- Referência: `Ação, coração, informação e português — versão 1.0`
- Reconhecido: `Ação, coração, informação e português— versão 1.0`
- Blocos: 1; tempo: 1051,8 ms

#### pt-accent-8-dark

- Referência: `Ação, coração, informação e português — versão 1.0`
- Reconhecido: ``
- Blocos: 0; tempo: 1072,3 ms

#### pt-accent-12-dark

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação oração iforação e portugués.`
- Blocos: 1; tempo: 1156,6 ms

#### pt-accent-24-dark

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação, coração, informação e português.`
- Blocos: 1; tempo: 988,6 ms

#### multiline-12-light

- Referência: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Reconhecido: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Blocos: 3; tempo: 571,1 ms

#### multiline-12-dark

- Referência: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Reconhecido: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Blocos: 3; tempo: 598,2 ms

#### contrast-moderate-light

- Referência: `Atenção: o texto está em cinza médio sobre fundo branco. Contraste moderado.`
- Reconhecido: `Atenção: o texto está em cinza médio sobre fundo branco. Contraste moderado.`
- Blocos: 1; tempo: 1030,6 ms

#### contrast-moderate-dark

- Referência: `Atenção: o texto está em cinza claro sobre fundo escuro. Contraste moderado.`
- Reconhecido: `Atenção: o texto está em cinza claro sobre fundo escuro. Contraste moderado.`
- Blocos: 1; tempo: 984,4 ms

#### numeric-12-light

- Referência: `Data: 2026-10-10 14:32:05 — ID: 0x1F3A-9920 ⏎ Itens: 42/100 — Total: 1.234,56 R$ — Versão: 2.4.1-beta.7`
- Reconhecido: `Data: 2026-10-1014:32:05 — ID: 0x1F3A-9920 ⏎ Itens: 42/100 — Total: 1.234,56 R$ — Versão: 2.4.1-beta.7`
- Blocos: 2; tempo: 678,2 ms

#### punctuation-12-light

- Referência: `Atenção (item 3): não é 'erro' — é um aviso! [OK / Cancelar] <<fim>>`
- Reconhecido: `Atenção (item 3): não é 'erro' — é um aviso! [OK / Cancelar] < <fim>>`
- Blocos: 1; tempo: 1050,9 ms

#### ui-mockup-light

- Referência: `CapParse — Captura de Tela ⏎ Arquivo  Editar  Ver  Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Reconhecido: `CapParse — Captura de Tela ⏎ Arquivo Editar Ver Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Blocos: 5; tempo: 426,9 ms

#### ui-mockup-dark

- Referência: `CapParse — Captura de Tela ⏎ Arquivo  Editar  Ver  Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Reconhecido: `CapParse — Captura de Tela ⏎ Arquivo Editar Ver Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Blocos: 5; tempo: 427,9 ms

#### no-text-blank

- Referência: ``
- Reconhecido: ``
- Blocos: 0; tempo: 228,6 ms

#### no-text-shapes

- Referência: ``
- Reconhecido: ``
- Blocos: 0; tempo: 234,7 ms

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
| Distância de edição total | 65 |
| CER agregado (soma das distâncias / soma das referências) | 0,058 |
| CER agregado (visão frouxa) | 0,052 |
| Inserções / exclusões / substituições (total) | 1 / 63 / 1 |
| Transcrição exata (bruta) | 8/15 |
| Transcrição exata (visão frouxa) | 10/15 |
| Tempo de reconhecimento — média | 786,6 ms |
| Tempo de reconhecimento — mediana | 974,5 ms |
| Tempo de reconhecimento — mín/máx | 228,6 / 1156,6 ms |

## Memória

`Process.WorkingSet64`: amostra da memória residente do processo inteiro (test host + engine + modelos), não uma medição exclusiva do OCR.

| Ponto | MB |
| --- | --- |
| Antes da inicialização | 72,3 |
| Após a inicialização | 140,2 |
| Mínimo durante a coleção | 2002,6 |
| Máximo durante a coleção | 2078,0 |
| Após o Dispose do engine | 262,5 |

## Aquecimento — tempos por caso (para referência)

| Caso | Tempo (ms) |
| --- | --- |
| pt-accent-16-light | 1019,2 |
| pt-accent-12-light | 1021,0 |
| pt-accent-24-light | 826,1 |
| pt-accent-8-light | 992,1 |
| pt-accent-8-dark | 991,6 |
| pt-accent-12-dark | 989,9 |
| pt-accent-24-dark | 864,9 |
| multiline-12-light | 535,7 |
| multiline-12-dark | 523,6 |
| contrast-moderate-light | 1010,6 |
| contrast-moderate-dark | 991,3 |
| numeric-12-light | 690,7 |
| punctuation-12-light | 999,5 |
| ui-mockup-light | 420,6 |
| ui-mockup-dark | 436,6 |
| no-text-blank | 236,7 |
| no-text-shapes | 241,6 |
