# M7-C — Avaliação de qualidade do OCR (relatório gerado)

Gerado em: 2026-10-14 18:07:16 por `OcrQualityEvaluationTests`.

## Ambiente

| Item | Valor |
| --- | --- |
| Configuração | Release |
| Variante de sessão ORT | default |
| SO | Microsoft Windows 10.0.26200 |
| Arquitetura do processo | X64 |
| Runtime | .NET 8.0.31 |
| Mecanismo | RapidOcrNet 4.2.0, preset `PPOCRv6Small` + `RapidOcrOptions.PPOCRv6`, CPU |
| Modelo | PP-OCRv6 Small (det/rec small + classificador v5), 100% local |

## Inicialização

- Tempo de inicialização do engine: 199,2 ms
- Memória do processo (Working Set) antes da inicialização: 72,7 MB
- Memória do processo (Working Set) após a inicialização: 141,4 MB (Δ 68,7 MB)

## Aquecimento (excluído das estatísticas)

- Passada completa da coleção antes das medições: 12594,4 ms no total.

## Resultados por imagem

CER bruto = distância de Levenshtein(referência, hipótese) / comprimento da referência, com acentos e caso preservados; apenas espaços nas bordas são removidos. CER frouxo aplica ainda o colapso de sequências internas de espaços/quebras de linha em um único espaço.

| Caso | CER bruto | CER frouxo | Exato (bruto) | Exato (frouxo) | Tempo (ms) | Erro |
| --- | --- | --- | --- | --- | --- | --- |
| pt-accent-16-light | 0,000 | 0,000 | sim | sim | 986,1 |  |
| pt-accent-12-light | 0,000 | 0,000 | sim | sim | 970,7 |  |
| pt-accent-24-light | 0,000 | 0,000 | sim | sim | 816,6 |  |
| pt-accent-8-light | 0,020 | 0,020 | não | não | 939,7 |  |
| pt-accent-8-dark | 1,000 | 1,000 | não | não | 941,9 |  |
| pt-accent-12-dark | 0,140 | 0,140 | não | não | 1028,4 |  |
| pt-accent-24-dark | 0,000 | 0,000 | sim | sim | 882,5 |  |
| multiline-12-light | 0,000 | 0,000 | sim | sim | 530,0 |  |
| multiline-12-dark | 0,000 | 0,000 | sim | sim | 524,0 |  |
| contrast-moderate-light | 0,000 | 0,000 | sim | sim | 978,9 |  |
| contrast-moderate-dark | 0,000 | 0,000 | sim | sim | 1069,2 |  |
| numeric-12-light | 0,010 | 0,010 | não | não | 794,0 |  |
| punctuation-12-light | 0,015 | 0,015 | não | não | 1108,8 |  |
| ui-mockup-light | 0,022 | 0,000 | não | sim | 472,4 |  |
| ui-mockup-dark | 0,022 | 0,000 | não | sim | 1037,3 |  |
| no-text-blank | n/d | n/d | — | — | 1469,9 |  |
| no-text-shapes | n/d | n/d | — | — | 553,6 |  |

### Detalhes por caso (referência × reconhecimento)

#### pt-accent-16-light

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação, coração, informação e português.`
- Blocos: 1; tempo: 986,1 ms

#### pt-accent-12-light

- Referência: `Árvore, ângulo, órgão, ação, coração, informação, órfão, ânsia, caráter e ênfase.`
- Reconhecido: `Árvore, ângulo, órgão, ação, coração, informação, órfão, ânsia, caráter e ênfase.`
- Blocos: 1; tempo: 970,7 ms

#### pt-accent-24-light

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação, coração, informação e português.`
- Blocos: 1; tempo: 816,6 ms

#### pt-accent-8-light

- Referência: `Ação, coração, informação e português — versão 1.0`
- Reconhecido: `Ação, coração, informação e português— versão 1.0`
- Blocos: 1; tempo: 939,7 ms

#### pt-accent-8-dark

- Referência: `Ação, coração, informação e português — versão 1.0`
- Reconhecido: ``
- Blocos: 0; tempo: 941,9 ms

#### pt-accent-12-dark

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação oração iforação e portugués.`
- Blocos: 1; tempo: 1028,4 ms

#### pt-accent-24-dark

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação, coração, informação e português.`
- Blocos: 1; tempo: 882,5 ms

#### multiline-12-light

- Referência: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Reconhecido: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Blocos: 3; tempo: 530,0 ms

#### multiline-12-dark

- Referência: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Reconhecido: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Blocos: 3; tempo: 524,0 ms

#### contrast-moderate-light

- Referência: `Atenção: o texto está em cinza médio sobre fundo branco. Contraste moderado.`
- Reconhecido: `Atenção: o texto está em cinza médio sobre fundo branco. Contraste moderado.`
- Blocos: 1; tempo: 978,9 ms

#### contrast-moderate-dark

- Referência: `Atenção: o texto está em cinza claro sobre fundo escuro. Contraste moderado.`
- Reconhecido: `Atenção: o texto está em cinza claro sobre fundo escuro. Contraste moderado.`
- Blocos: 1; tempo: 1069,2 ms

#### numeric-12-light

- Referência: `Data: 2026-10-10 14:32:05 — ID: 0x1F3A-9920 ⏎ Itens: 42/100 — Total: 1.234,56 R$ — Versão: 2.4.1-beta.7`
- Reconhecido: `Data: 2026-10-1014:32:05 — ID: 0x1F3A-9920 ⏎ Itens: 42/100 — Total: 1.234,56 R$ — Versão: 2.4.1-beta.7`
- Blocos: 2; tempo: 794,0 ms

#### punctuation-12-light

- Referência: `Atenção (item 3): não é 'erro' — é um aviso! [OK / Cancelar] <<fim>>`
- Reconhecido: `Atenção (item 3): não é 'erro' — é um aviso! [OK / Cancelar] < <fim>>`
- Blocos: 1; tempo: 1108,8 ms

#### ui-mockup-light

- Referência: `CapParse — Captura de Tela ⏎ Arquivo  Editar  Ver  Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Reconhecido: `CapParse — Captura de Tela ⏎ Arquivo Editar Ver Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Blocos: 5; tempo: 472,4 ms

#### ui-mockup-dark

- Referência: `CapParse — Captura de Tela ⏎ Arquivo  Editar  Ver  Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Reconhecido: `CapParse — Captura de Tela ⏎ Arquivo Editar Ver Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Blocos: 5; tempo: 1037,3 ms

#### no-text-blank

- Referência: ``
- Reconhecido: ``
- Blocos: 0; tempo: 1469,9 ms

#### no-text-shapes

- Referência: ``
- Reconhecido: ``
- Blocos: 0; tempo: 553,6 ms

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
| Tempo de reconhecimento — média | 888,5 ms |
| Tempo de reconhecimento — mediana | 941,9 ms |
| Tempo de reconhecimento — mín/máx | 472,4 / 1469,9 ms |

## Memória

`Process.WorkingSet64` (Working Set): memória residente do processo inteiro (test host + engine + modelos), não uma medição exclusiva do OCR. `Process.PrivateMemorySize64` (Privada): memória comprometida (commit) pelo processo. As duas são reportadas separadamente porque uma queda no Working Set não é equivalente a uma queda na memória comprometida.

| Ponto | Working Set (MB) | Privada (MB) |
| --- | --- | --- |
| Antes da inicialização | 72,7 | 31,8 |
| Após a inicialização | 141,4 | 89,5 |
| Após a 1ª inferência | 942,6 | 1264,7 |
| Mínimo durante a coleção | 2003,3 | 2518,4 |
| Máximo durante a coleção | 2082,2 | 2595,8 |
| Após o Dispose do engine | 264,7 | 204,7 |

## Aquecimento — tempos por caso (para referência)

| Caso | Tempo (ms) |
| --- | --- |
| pt-accent-16-light | 1024,5 |
| pt-accent-12-light | 1001,6 |
| pt-accent-24-light | 812,9 |
| pt-accent-8-light | 981,1 |
| pt-accent-8-dark | 975,4 |
| pt-accent-12-dark | 954,0 |
| pt-accent-24-dark | 881,2 |
| multiline-12-light | 532,8 |
| multiline-12-dark | 512,9 |
| contrast-moderate-light | 974,5 |
| contrast-moderate-dark | 943,7 |
| numeric-12-light | 667,5 |
| punctuation-12-light | 981,3 |
| ui-mockup-light | 417,0 |
| ui-mockup-dark | 426,1 |
| no-text-blank | 240,4 |
| no-text-shapes | 267,6 |
