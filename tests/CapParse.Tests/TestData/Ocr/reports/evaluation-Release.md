# M7-C — Avaliação de qualidade do OCR (relatório gerado)

Gerado em: 2026-10-14 18:28:52 por `OcrQualityEvaluationTests`.

## Ambiente

| Item | Valor |
| --- | --- |
| Configuração | Release |
| Variante de sessão ORT | default |
| EnableCpuMemArena (ORT) | false |
| EnableMemoryPattern (ORT) | true |
| SO | Microsoft Windows 10.0.26200 |
| Arquitetura do processo | X64 |
| Runtime | .NET 8.0.31 |
| Mecanismo | RapidOcrNet 4.2.0, preset `PPOCRv6Small` + `RapidOcrOptions.PPOCRv6`, CPU |
| Modelo | PP-OCRv6 Small (det/rec small + classificador v5), 100% local |

## Inicialização

- Tempo de inicialização do engine: 228,0 ms
- Memória do processo (Working Set) antes da inicialização: 73,3 MB
- Memória do processo (Working Set) após a inicialização: 141,8 MB (Δ 68,5 MB)

## Aquecimento (excluído das estatísticas)

- Passada completa da coleção antes das medições: 13013,3 ms no total.

## Resultados por imagem

CER bruto = distância de Levenshtein(referência, hipótese) / comprimento da referência, com acentos e caso preservados; apenas espaços nas bordas são removidos. CER frouxo aplica ainda o colapso de sequências internas de espaços/quebras de linha em um único espaço.

| Caso | CER bruto | CER frouxo | Exato (bruto) | Exato (frouxo) | Tempo (ms) | Erro |
| --- | --- | --- | --- | --- | --- | --- |
| pt-accent-16-light | 0,000 | 0,000 | sim | sim | 970,0 |  |
| pt-accent-12-light | 0,000 | 0,000 | sim | sim | 970,4 |  |
| pt-accent-24-light | 0,000 | 0,000 | sim | sim | 832,5 |  |
| pt-accent-8-light | 0,020 | 0,020 | não | não | 955,5 |  |
| pt-accent-8-dark | 1,000 | 1,000 | não | não | 957,6 |  |
| pt-accent-12-dark | 0,140 | 0,140 | não | não | 956,7 |  |
| pt-accent-24-dark | 0,000 | 0,000 | sim | sim | 808,3 |  |
| multiline-12-light | 0,000 | 0,000 | sim | sim | 513,7 |  |
| multiline-12-dark | 0,000 | 0,000 | sim | sim | 523,3 |  |
| contrast-moderate-light | 0,000 | 0,000 | sim | sim | 981,3 |  |
| contrast-moderate-dark | 0,000 | 0,000 | sim | sim | 974,4 |  |
| numeric-12-light | 0,010 | 0,010 | não | não | 675,6 |  |
| punctuation-12-light | 0,015 | 0,015 | não | não | 977,7 |  |
| ui-mockup-light | 0,022 | 0,000 | não | sim | 445,3 |  |
| ui-mockup-dark | 0,022 | 0,000 | não | sim | 430,9 |  |
| no-text-blank | n/d | n/d | — | — | 236,4 |  |
| no-text-shapes | n/d | n/d | — | — | 240,1 |  |

### Detalhes por caso (referência × reconhecimento)

#### pt-accent-16-light

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação, coração, informação e português.`
- Blocos: 1; tempo: 970,0 ms

#### pt-accent-12-light

- Referência: `Árvore, ângulo, órgão, ação, coração, informação, órfão, ânsia, caráter e ênfase.`
- Reconhecido: `Árvore, ângulo, órgão, ação, coração, informação, órfão, ânsia, caráter e ênfase.`
- Blocos: 1; tempo: 970,4 ms

#### pt-accent-24-light

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação, coração, informação e português.`
- Blocos: 1; tempo: 832,5 ms

#### pt-accent-8-light

- Referência: `Ação, coração, informação e português — versão 1.0`
- Reconhecido: `Ação, coração, informação e português— versão 1.0`
- Blocos: 1; tempo: 955,5 ms

#### pt-accent-8-dark

- Referência: `Ação, coração, informação e português — versão 1.0`
- Reconhecido: ``
- Blocos: 0; tempo: 957,6 ms

#### pt-accent-12-dark

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação oração iforação e portugués.`
- Blocos: 1; tempo: 956,7 ms

#### pt-accent-24-dark

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação, coração, informação e português.`
- Blocos: 1; tempo: 808,3 ms

#### multiline-12-light

- Referência: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Reconhecido: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Blocos: 3; tempo: 513,7 ms

#### multiline-12-dark

- Referência: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Reconhecido: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Blocos: 3; tempo: 523,3 ms

#### contrast-moderate-light

- Referência: `Atenção: o texto está em cinza médio sobre fundo branco. Contraste moderado.`
- Reconhecido: `Atenção: o texto está em cinza médio sobre fundo branco. Contraste moderado.`
- Blocos: 1; tempo: 981,3 ms

#### contrast-moderate-dark

- Referência: `Atenção: o texto está em cinza claro sobre fundo escuro. Contraste moderado.`
- Reconhecido: `Atenção: o texto está em cinza claro sobre fundo escuro. Contraste moderado.`
- Blocos: 1; tempo: 974,4 ms

#### numeric-12-light

- Referência: `Data: 2026-10-10 14:32:05 — ID: 0x1F3A-9920 ⏎ Itens: 42/100 — Total: 1.234,56 R$ — Versão: 2.4.1-beta.7`
- Reconhecido: `Data: 2026-10-1014:32:05 — ID: 0x1F3A-9920 ⏎ Itens: 42/100 — Total: 1.234,56 R$ — Versão: 2.4.1-beta.7`
- Blocos: 2; tempo: 675,6 ms

#### punctuation-12-light

- Referência: `Atenção (item 3): não é 'erro' — é um aviso! [OK / Cancelar] <<fim>>`
- Reconhecido: `Atenção (item 3): não é 'erro' — é um aviso! [OK / Cancelar] < <fim>>`
- Blocos: 1; tempo: 977,7 ms

#### ui-mockup-light

- Referência: `CapParse — Captura de Tela ⏎ Arquivo  Editar  Ver  Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Reconhecido: `CapParse — Captura de Tela ⏎ Arquivo Editar Ver Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Blocos: 5; tempo: 445,3 ms

#### ui-mockup-dark

- Referência: `CapParse — Captura de Tela ⏎ Arquivo  Editar  Ver  Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Reconhecido: `CapParse — Captura de Tela ⏎ Arquivo Editar Ver Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Blocos: 5; tempo: 430,9 ms

#### no-text-blank

- Referência: ``
- Reconhecido: ``
- Blocos: 0; tempo: 236,4 ms

#### no-text-shapes

- Referência: ``
- Reconhecido: ``
- Blocos: 0; tempo: 240,1 ms

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
| Tempo de reconhecimento — média | 732,3 ms |
| Tempo de reconhecimento — mediana | 832,5 ms |
| Tempo de reconhecimento — mín/máx | 236,4 / 981,3 ms |

## Memória

`Process.WorkingSet64` (Working Set): memória residente do processo inteiro (test host + engine + modelos), não uma medição exclusiva do OCR. `Process.PrivateMemorySize64` (Privada): memória comprometida (commit) pelo processo. As duas são reportadas separadamente porque uma queda no Working Set não é equivalente a uma queda na memória comprometida.

| Ponto | Working Set (MB) | Privada (MB) |
| --- | --- | --- |
| Antes da inicialização | 73,3 | 32,3 |
| Após a inicialização | 141,8 | 89,1 |
| Após a 1ª inferência | 231,8 | 173,2 |
| Mínimo durante a coleção | 392,6 | 332,5 |
| Máximo durante a coleção | 468,1 | 408,9 |
| Após o Dispose do engine | 265,8 | 204,7 |

## Aquecimento — tempos por caso (para referência)

| Caso | Tempo (ms) |
| --- | --- |
| pt-accent-16-light | 1124,8 |
| pt-accent-12-light | 918,7 |
| pt-accent-24-light | 905,2 |
| pt-accent-8-light | 970,7 |
| pt-accent-8-dark | 965,8 |
| pt-accent-12-dark | 959,3 |
| pt-accent-24-dark | 816,8 |
| multiline-12-light | 597,7 |
| multiline-12-dark | 525,5 |
| contrast-moderate-light | 981,2 |
| contrast-moderate-dark | 978,5 |
| numeric-12-light | 758,8 |
| punctuation-12-light | 1071,6 |
| ui-mockup-light | 492,6 |
| ui-mockup-dark | 440,3 |
| no-text-blank | 265,2 |
| no-text-shapes | 240,6 |
