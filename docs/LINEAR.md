---
overlay: on
---

# Linear — overlay for Horde Runner

Layout canon: personal skill `linear-overlay`. This file is **this repo's IDs**.

Read this file before any Linear `save_issue`.

## IDs (stable)

Workspace: **not in this file.** Resolve with Linear MCP `get_workspace`.

| Surface | Value |
|---|---|
| Team | `NexusLearning` (`NEX`) id `7141c74c-2c4a-4e60-9291-ebf757dc4265` |
| Project | `Horde Runner` id `f8aff775-07eb-4bc2-8ae7-7df63a42c0c1` |

Always filter `project: Horde Runner`. Do not rename the team to match another product.

## Shape

O Milestone simula o Épico. Duas camadas nativas de issues com labels do grupo **Type**:
Project → Milestone (Épico) → História (issue-pai raiz, Story) → Tarefa (sub-issue, Task).

| Milestone (Épico) | id |
|---|---|
| E1 · Protótipo jogável (Current) | `c379bbb3-da93-4cd5-b8ba-cc2461f948dd` |
| E2 · Profundidade de combate (Next) | `543515eb-3e2d-406a-b7a9-2611b4e7d72a` |
| E3 · Progressão e fases Boss (Later) | `9cd5a812-344d-476e-912b-d83587d75505` |
| E4 · Monetização e polimento (Later) | `43fb2c96-90d0-4df1-8683-ea54aa6d666d` |

## Current horizon

| História (issue-pai) | Milestone | Doc |
|---|---|---|
| `NEX-504` Inbox | E1 | Working agreement |
| `NEX-505` M0 · Fundação + harness CLI (tarefas NEX-520…525) | E1 | Plano do jogo — GDD e arquitetura |
| `NEX-506` M1 · Corredor greybox | E1 | — |
| `NEX-507` M2 · Tropa e formação | E1 | — |
| `NEX-508` M3 · Portões aritméticos | E1 | — |
| `NEX-509` M4 · Tiro e hordas | E1 | — |
| `NEX-510`…`NEX-514` M5–M9 | E2 | — |
| `NEX-515`…`NEX-517` M10–M12 | E3 | — |
| `NEX-518`, `NEX-519` M13–M14 | E4 | — |

## Verificação (harness CLI)

Toda tarefa fecha com os portões do bloco `dispatch.verify`, via `tools/unity <compile|test-edit|test-play|import-content|validate|simulate|build-android>`.

## Working agreement

1. GitHub creates code bugs; Linear prioritizes.
2. Never duplicate an imported `(GH #N)` title.
3. Do not mark imported issues Done to mean triaged.
4. Linear Documents are pointers, not copies of product canon.
5. New planned work = sub-issue under a história.
6. PRs: tarefa ramifica de `gitBranchName` da história; PR da tarefa **→ branch da história**. Só a história PRa para default. Mention `NEX-n`.
7. Always on a card. Mid-flight: comment or new sub-issue under the história (see kit `global/TRACK.md`).
8. Tarefas that an agent implements start with a `dispatch` block (`global/DISPATCH.md`). Histórias do not.
9. Invariante Pai-Filho: Linear tem auto-close, mas não tem auto-start de pai. Ao mover sub-issue para `In Progress`, promova a história-pai para `In Progress` no mesmo passo. Sub-issues nunca nascem em `In Progress`.
10. Ciclo: In Progress só empilhado na história (ou irmão anterior merged nela). `/review` não marca Done. Done = merge.
