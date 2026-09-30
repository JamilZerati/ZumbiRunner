# AGENTS.md

Instruções para qualquer agente (Claude Code, Codex, Cursor, cloud) neste repositório. Este arquivo é o índice: o que vale em toda tarefa está aqui; o resto mora em `docs/agentes/` e é aberto quando o gatilho da seção bate.

## O projeto

Horde Runner: runner mobile (retrato, Android) em Unity. O General lidera uma tropa por lanes contra hordas de zumbis e escolhe portões de perk.

- Vocabulário: `CONTEXT.md`.
- Visão e arquitetura base: `docs/planos/2026-09-26-horde-runner-gdd-arquitetura.md`.
- Regras de combate, meta, monetização, live-ops e roadmap: `docs/planos/2026-09-29-horde-runner-gameplay-meta-liveops.md`.
- Plano da história em curso: `docs/planos/<data>-NEX-<n>-*.md`.

**CLI-first**: agentes trabalham sem o Editor GUI. Build, teste e validação rodam em batchmode pelo harness `tools/unity` e são julgados por exit code e pelos artefatos em `.artifacts/`.

## Portão de verificação

Tarefa pronta = o `verify` do bloco `dispatch` passou **e** você leu o resultado. Sequência padrão:

1. `import-content` quando a tarefa mexeu em `Content/Source/`.
2. `compile` com 0 erros.
3. `test-edit` com `SUCCESS n/n`; em falha, a causa está em `.artifacts/editmode.xml`.
4. `test-play` quando a mudança toca física, colisão, cena ou ciclo de vida de MonoBehaviour.
5. `git status` contendo só a intenção da tarefa (guardrails abaixo).

`validate` e `simulate` são stubs que saem com 0 até a M7: exit 0 deles não é evidência. Comandos por shell, leitura de artefatos, tempos de importação e build Android: `docs/agentes/harness-unity.md`.

## Guardrails de build

- **Editor fixo** em `ProjectSettings/ProjectVersion.txt`. Abra o projeto só com essa versão; upgrade de editor ou pacote é decisão humana.
- **Gerado se regenera.** Assets em `Assets/_Game/Data/` vêm de `Content/Source/*.json` (`import-content`); cenas `Assets/_Game/Scenes/*.unity` vêm do `SceneBuilder`. Mude a fonte e regenere.
- **Commit = intenção.** Testes e importação reescrevem assets gerados com IDs novos (`rid`, `fileID`) sem mudança real, e o Unity reescreve `Packages/` e `ProjectSettings/` ao abrir. Restaure com `git checkout -- <arquivo>` tudo que a tarefa não pretendia mudar; regeneração legítima vai num commit `chore` separado, declarado no PR.
- **`.meta` junto** de todo asset novo; binários por Git LFS.
- **Um Unity por pasta.** Tarefa paralela ganha `git worktree` próprio; a pasta principal pode estar em uso por outra sessão.
- **Evidência antes de afirmar**: bug começa com um teste que você viu falhar; "funciona" só depois do comando verde lido.

O porquê de cada regra, como reconhecer ruído de regeneração e como regenerar cada tipo de asset: `docs/agentes/guardrails-build.md`.

## Arquitetura

- Asmdefs em uma direção: `Game.Core` (C# puro) ← `Game.Data` ← `Game.Gameplay` ← `Game.Presentation`; mais `Game.Infrastructure`, `Game.Composition` (VContainer), `Game.Editor` e os testes. `Game.Meta` (C# puro, a partir da M12) serve Data, Infrastructure e Presentation; `Game.Gameplay` fala com a meta só por `RunConfig`, sinais no `IEventBus` e `RunResult`.
- Regra de negócio em C# puro, testada em EditMode sem cena.
- Números de balanceamento vivem em `Content/Source/`, fora de MonoBehaviour e do `SceneBuilder`.
- Fases avançam por distância; stats usam modificadores com `Source`; perks, efeitos e comportamentos são composição via `[SerializeReference]`; o nº de lanes vem de `LaneLayout`.
- Tudo que colide tem camada própria e Rigidbody cinemático próprio, na raiz da cena.

Ao escrever MonoBehaviour, física, pooling, `[SerializeReference]`, fiação do `SceneBuilder` ou teste PlayMode: `docs/agentes/unity-runtime.md`.

## Fluxo Linear + Git

- Toda mudança nasce num card: tarefa (`Task`) sob uma história (`Story`). IDs, estados e working agreement: `docs/LINEAR.md`, lido antes de qualquer escrita no Linear.
- Tarefa ramifica da branch da história e abre PR (Ready for Review) para ela; só a história abre PR para `main`. Título com `NEX-n`.
- Bug de código nasce como issue no GitHub e ganha o overlay `(GH #N)` no Linear.

## Regras de código

- Até 300 linhas de mudança de comportamento por PR; scaffold gerado e movimentação pura em commit separado, declarados no PR.
- Comentário só para o porquê: invariante não óbvia ou contorno de bug externo.
- Diff cirúrgico: formatação, imports e docstrings fora do escopo ficam como estão.
- Português do Brasil em docs, commits (`tipo(escopo): resumo [NEX-n]`) e PRs.

## O que é do humano

Licença Unity, secrets de CI, contas de loja, anúncios e analytics, preços, sensação de jogo no aparelho e decisões de produto. Pare e pergunte.
