# M7-C — Avaliação de qualidade do OCR (relatório gerado)

Gerado em: 2026-10-14 18:14:23 por `OcrQualityEvaluationTests`.

## Ambiente

| Item | Valor |
| --- | --- |
| Configuração | Release |
| Variante de sessão ORT | nopattern |
| SO | Microsoft Windows 10.0.26200 |
| Arquitetura do processo | X64 |
| Runtime | .NET 8.0.31 |
| Mecanismo | RapidOcrNet 4.2.0, preset `PPOCRv6Small` + `RapidOcrOptions.PPOCRv6`, CPU |
| Modelo | PP-OCRv6 Small (det/rec small + classificador v5), 100% local |

## Inicialização

- Tempo de inicialização do engine: 194,5 ms
- Memória do processo (Working Set) antes da inicialização: 73,0 MB
- Memória do processo (Working Set) após a inicialização: 141,4 MB (Δ 68,4 MB)

## Aquecimento (excluído das estatísticas)

- Passada completa da coleção antes das medições: 12097,9 ms no total.

## Resultados por imagem

CER bruto = distância de Levenshtein(referência, hipótese) / comprimento da referência, com acentos e caso preservados; apenas espaços nas bordas são removidos. CER frouxo aplica ainda o colapso de sequências internas de espaços/quebras de linha em um único espaço.

| Caso | CER bruto | CER frouxo | Exato (bruto) | Exato (frouxo) | Tempo (ms) | Erro |
| --- | --- | --- | --- | --- | --- | --- |
| pt-accent-16-light | 0,000 | 0,000 | sim | sim | 938,6 |  |
| pt-accent-12-light | 0,000 | 0,000 | sim | sim | 956,1 |  |
| pt-accent-24-light | 0,000 | 0,000 | sim | sim | 800,4 |  |
| pt-accent-8-light | 0,020 | 0,020 | não | não | 929,9 |  |
| pt-accent-8-dark | 1,000 | 1,000 | não | não | 971,0 |  |
| pt-accent-12-dark | 0,140 | 0,140 | não | não | 946,6 |  |
| pt-accent-24-dark | 0,000 | 0,000 | sim | sim | 794,2 |  |
| multiline-12-light | 0,000 | 0,000 | sim | sim | 500,2 |  |
| multiline-12-dark | 0,000 | 0,000 | sim | sim | 492,4 |  |
| contrast-moderate-light | 0,000 | 0,000 | sim | sim | 944,8 |  |
| contrast-moderate-dark | 0,000 | 0,000 | sim | sim | 959,1 |  |
| numeric-12-light | 0,010 | 0,010 | não | não | 723,4 |  |
| punctuation-12-light | 0,015 | 0,015 | não | não | 973,3 |  |
| ui-mockup-light | 0,022 | 0,000 | não | sim | 493,0 |  |
| ui-mockup-dark | 0,022 | 0,000 | não | sim | 504,4 |  |
| no-text-blank | n/d | n/d | — | — | 246,1 |  |
| no-text-shapes | n/d | n/d | — | — | 234,9 |  |

### Detalhes por caso (referência × reconhecimento)

#### pt-accent-16-light

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação, coração, informação e português.`
- Blocos: 1; tempo: 938,6 ms

#### pt-accent-12-light

- Referência: `Árvore, ângulo, órgão, ação, coração, informação, órfão, ânsia, caráter e ênfase.`
- Reconhecido: `Árvore, ângulo, órgão, ação, coração, informação, órfão, ânsia, caráter e ênfase.`
- Blocos: 1; tempo: 956,1 ms

#### pt-accent-24-light

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação, coração, informação e português.`
- Blocos: 1; tempo: 800,4 ms

#### pt-accent-8-light

- Referência: `Ação, coração, informação e português — versão 1.0`
- Reconhecido: `Ação, coração, informação e português— versão 1.0`
- Blocos: 1; tempo: 929,9 ms

#### pt-accent-8-dark

- Referência: `Ação, coração, informação e português — versão 1.0`
- Reconhecido: ``
- Blocos: 0; tempo: 971,0 ms

#### pt-accent-12-dark

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação oração iforação e portugués.`
- Blocos: 1; tempo: 946,6 ms

#### pt-accent-24-dark

- Referência: `Olá, ação, coração, informação e português.`
- Reconhecido: `Olá, ação, coração, informação e português.`
- Blocos: 1; tempo: 794,2 ms

#### multiline-12-light

- Referência: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Reconhecido: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Blocos: 3; tempo: 500,2 ms

#### multiline-12-dark

- Referência: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Reconhecido: `Configuração de captura ⏎ Monitores: 2 — Resolução: 1920x1080 ⏎ Última captura: 2026-10-10 14:32`
- Blocos: 3; tempo: 492,4 ms

#### contrast-moderate-light

- Referência: `Atenção: o texto está em cinza médio sobre fundo branco. Contraste moderado.`
- Reconhecido: `Atenção: o texto está em cinza médio sobre fundo branco. Contraste moderado.`
- Blocos: 1; tempo: 944,8 ms

#### contrast-moderate-dark

- Referência: `Atenção: o texto está em cinza claro sobre fundo escuro. Contraste moderado.`
- Reconhecido: `Atenção: o texto está em cinza claro sobre fundo escuro. Contraste moderado.`
- Blocos: 1; tempo: 959,1 ms

#### numeric-12-light

- Referência: `Data: 2026-10-10 14:32:05 — ID: 0x1F3A-9920 ⏎ Itens: 42/100 — Total: 1.234,56 R$ — Versão: 2.4.1-beta.7`
- Reconhecido: `Data: 2026-10-1014:32:05 — ID: 0x1F3A-9920 ⏎ Itens: 42/100 — Total: 1.234,56 R$ — Versão: 2.4.1-beta.7`
- Blocos: 2; tempo: 723,4 ms

#### punctuation-12-light

- Referência: `Atenção (item 3): não é 'erro' — é um aviso! [OK / Cancelar] <<fim>>`
- Reconhecido: `Atenção (item 3): não é 'erro' — é um aviso! [OK / Cancelar] < <fim>>`
- Blocos: 1; tempo: 973,3 ms

#### ui-mockup-light

- Referência: `CapParse — Captura de Tela ⏎ Arquivo  Editar  Ver  Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Reconhecido: `CapParse — Captura de Tela ⏎ Arquivo Editar Ver Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Blocos: 5; tempo: 493,0 ms

#### ui-mockup-dark

- Referência: `CapParse — Captura de Tela ⏎ Arquivo  Editar  Ver  Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Reconhecido: `CapParse — Captura de Tela ⏎ Arquivo Editar Ver Ajuda ⏎ Status: pronto ⏎ Monitores: 2 | Memória: 1.234 MB | Disco: 45% ⏎ Atalho: Ctrl+Shift+X`
- Blocos: 5; tempo: 504,4 ms

#### no-text-blank

- Referência: ``
- Reconhecido: ``
- Blocos: 0; tempo: 246,1 ms

#### no-text-shapes

- Referência: ``
- Reconhecido: ``
- Blocos: 0; tempo: 234,9 ms

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
| Tempo de reconhecimento — média | 729,9 ms |
| Tempo de reconhecimento — mediana | 800,4 ms |
| Tempo de reconhecimento — mín/máx | 234,9 / 973,3 ms |

## Memória

`Process.WorkingSet64` (Working Set): memória residente do processo inteiro (test host + engine + modelos), não uma medição exclusiva do OCR. `Process.PrivateMemorySize64` (Privada): memória comprometida (commit) pelo processo. As duas são reportadas separadamente porque uma queda no Working Set não é equivalente a uma queda na memória comprometida.

| Ponto | Working Set (MB) | Privada (MB) |
| --- | --- | --- |
| Antes da inicialização | 73,0 | 32,1 |
| Após a inicialização | 141,4 | 89,8 |
| Após a 1ª inferência | 942,5 | 1264,7 |
| Mínimo durante a coleção | 1166,9 | 1461,4 |
| Máximo durante a coleção | 1243,5 | 1539,4 |
| Após o Dispose do engine | 264,8 | 204,2 |

## Aquecimento — tempos por caso (para referência)

| Caso | Tempo (ms) |
| --- | --- |
| pt-accent-16-light | 978,9 |
| pt-accent-12-light | 918,4 |
| pt-accent-24-light | 766,1 |
| pt-accent-8-light | 945,9 |
| pt-accent-8-dark | 951,0 |
| pt-accent-12-dark | 924,0 |
| pt-accent-24-dark | 796,1 |
| multiline-12-light | 512,3 |
| multiline-12-dark | 502,4 |
| contrast-moderate-light | 950,4 |
| contrast-moderate-dark | 935,1 |
| numeric-12-light | 658,2 |
| punctuation-12-light | 952,2 |
| ui-mockup-light | 415,5 |
| ui-mockup-dark | 422,4 |
| no-text-blank | 231,9 |
| no-text-shapes | 236,9 |
