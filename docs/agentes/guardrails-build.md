# Guardrails de build

O porquê e o procedimento de cada guardrail do `AGENTS.md`.

## Versão do editor e pacotes

Abrir o projeto com outra versão do Unity reescreve assets e `ProjectSettings/` inteiros, e o diff resultante é irreversível na prática. O harness sempre resolve a versão de `ProjectSettings/ProjectVersion.txt`.

O Unity também reescreve `Packages/manifest.json`, `Packages/packages-lock.json` e `ProjectSettings/*.asset` ao importar. Se a tarefa não mudou pacotes nem configurações, esses diffs são efeito colateral: restaure antes de commitar. Mudança intencional (camada nova em `TagManager.asset`, matriz de colisão em `DynamicsManager.asset`) é declarada no PR.

## Fontes e gerados

| Fonte (editável) | Gerado (não editar) | Como regenerar |
|---|---|---|
| `Content/Source/Perks/*.json` | `Assets/_Game/Data/Perks/*.asset` | `import-content` |
| `Content/Source/Weapons/*.json` | `Assets/_Game/Data/Weapons/*.asset` | `import-content` |
| `Content/Source/Statuses/*.json`, `Content/Source/Interactions/*.json` | `Assets/_Game/Data/Statuses/StatusCatalog.asset`, `EffectInteractionTable.asset` | `import-content` |
| `SceneBuilder.Build<Cena>` | `Assets/_Game/Scenes/<Cena>.unity` | testes `SceneBuilder*Tests` no `test-edit`, ou `-executeMethod Game.Editor.SceneBuilder.Build<Cena>Cli` |

Conteúdo novo (tipo novo de JSON) nasce com importador e validação no `Game.Editor` e entra na ordem do `Cli.ImportContent`, que importa armas antes de status, sinergias e perks porque a validação de perk lê o catálogo de armas recém-gerado.

## Ruído de regeneração

Os testes de `SceneBuilder` regravam as cenas e os testes de importador regravam os SOs a cada `test-edit`. O conteúdo é o mesmo, mas IDs mudam:

- `.asset` com `[SerializeReference]`: só as linhas `rid:` mudam.
- `.unity`: milhares de linhas trocando `fileID`, com o mesmo número de inserções e remoções no `git diff --stat`.

Procedimento antes de cada commit:

1. `git status` e `git diff --stat`.
2. Para cada asset gerado que a tarefa não pretendia mudar: `git checkout -- <arquivo>`.
3. Se a tarefa mudou a fonte (JSON ou `SceneBuilder`), o gerado correspondente entra num commit `chore(scenes|content): regenerar ... [NEX-n]`, separado do commit de comportamento e citado no PR.

## Cena acompanha o código

Campo serializado novo, renomeado ou com fiação nova em MonoBehaviour exige atualizar o `SceneBuilder` e regenerar as cenas afetadas. O `SceneBuilder` localiza campos por nome com `SerializedObject.FindProperty` e ignora o que não encontra: um campo renomeado deixa a cena com a referência vazia **sem erro**. Os testes `SceneBuilder<Cena>Tests`, que reabrem a cena do disco, são a rede de segurança; toda cena nova ganha o seu.

## `.meta` e LFS

- Todo arquivo novo sob `Assets/` (script, pasta, asset) tem `.meta` gerado pelo Unity; commite os dois. `.meta` órfão ou ausente quebra referências por GUID em outros clones.
- Binários (arte, áudio, fontes, libs nativas) vão por LFS conforme `.gitattributes`; YAML do Unity usa o driver `unityyamlmerge`. `./tools/unity setup` configura os dois no clone.

## Paralelismo

Cada tarefa roda no próprio worktree (`D:/Projects/worktrees/ZumbiRunner/nex-<n>`), ramificado da branch da história. A pasta principal pode pertencer a outra sessão: nela, a branch não é trocada e nada é commitado por você.

## Evidência

- Bug: teste que reproduz o sintoma, visto falhando antes da correção (Red), e passando depois (Green). Sintoma de física pede teste PlayMode; o EditMode não roda callbacks de física.
- Até o CI entrar, o PR registra os comandos rodados e as contagens.
