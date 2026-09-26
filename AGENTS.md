# AGENTS.md

Instruções para qualquer agente (Claude Code, Codex, Cursor, cloud) neste repositório.

## O projeto

Horde Runner: runner mobile (retrato, Android) em Unity. O General lidera uma tropa por lanes discretas contra hordas de zumbis e escolhe portões de perk. Vocabulário em `CONTEXT.md`. Plano-mãe (gameplay, arquitetura, roadmap, harness CLI) em `docs/planos/2026-09-26-horde-runner-gdd-arquitetura.md`.

Desenvolvimento **CLI-first**: o Editor GUI não é aberto por agentes. Todo build, teste e validação roda em batchmode e é julgado por exit code + log.

## Unity

- Versão fixada em `ProjectSettings/ProjectVersion.txt` (hoje 6000.6.3f1). O executável fica em `C:\Program Files\Unity\Hub\Editor\<versão>\Editor\Unity.exe`; nunca use outra versão para abrir o projeto (upgrade/downgrade reescreve assets).
- Asset Serialization = Force Text, Visible Meta Files. Todo asset novo precisa do seu `.meta` commitado.
- Binários (arte, áudio, fontes, libs nativas) vão por Git LFS (`.gitattributes`). YAML do Unity usa o driver `unityyamlmerge`, que é configuração local de cada clone:

```bash
git lfs install --local
git config merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
```

## Comandos

O harness `tools/unity` (`tools/unity.ps1` no PowerShell, `tools/unity.sh` ou `./tools/unity` no Bash) é o caminho canônico de verificação:

```bash
# Setup local de Git LFS e driver UnityYAMLMerge
./tools/unity setup

# Compilação e checagem de erros de scripts
./tools/unity compile

# Testes EditMode
./tools/unity test-edit

# Testes PlayMode
./tools/unity test-play

# Importação de conteúdo JSON -> ScriptableObjects
./tools/unity import-content

# Validação de integridade de dados e fases
./tools/unity validate

# Simulação headless de balanceamento
./tools/unity simulate

# Build de desenvolvimento Android
./tools/unity build-android
```

## Arquitetura-alvo

Hoje `Assets/` tem só o template URP (cena, settings, input actions). A estrutura abaixo é o contrato das próximas tarefas (NEX-521, NEX-523) e do plano-mãe, seção 3:

- Código em `Assets/_Game/Scripts/`, separado por asmdef com dependência em uma direção só: `Game.Core` (C# puro, `noEngineReferences: true`) ← `Game.Data` (ScriptableObjects) ← `Game.Gameplay` (MonoBehaviours) ← `Game.Presentation` (VFX, UI, câmera). `Game.Infrastructure` (input, save, áudio, ads), `Game.Composition` (VContainer), `Game.Editor` (tooling CLI), `Game.Tests.EditMode` / `Game.Tests.PlayMode`.
- Regra de negócio fica em `Game.Core` para ser testada em EditMode sem cena.
- Conteúdo (armas, perks, fases) é JSON em `Content/Source/`, importado para ScriptableObject por um importador de Editor. **Não edite à mão SO nem cena gerados**; mude o JSON ou o gerador (`SceneBuilder`).
- Fases avançam por **distância**, não tempo. Stats usam modificadores com `Source` para remoção limpa. Perks e efeitos são composição via `[SerializeReference]`, não herança.
- Nº de lanes vem de `LaneLayout`; nunca assuma 2.

## Fluxo Linear + Git

- **Leia `docs/LINEAR.md` antes de qualquer escrita no Linear** (IDs do time NEX e do projeto; workspace via MCP, nunca hardcode).
- Hierarquia: Milestone (épico) → História (`Story`) → Tarefa (`Task`). Tarefas de agente trazem um bloco `dispatch` na descrição (`needs`, `faixa`, `runtime`, `verify`); o `verify` é o portão obrigatório antes do PR.
- Git: cada tarefa ramifica do `gitBranchName` da **história** e abre PR (Ready for Review, não draft) **para a branch da história**. Só a história abre PR para `main`. Cite `NEX-n` no título.
- Status: `In Progress` ao começar (promova a história-pai se estiver em Backlog/Todo) → `In Review` com PR anexado → `Done` no merge. `Testing` existe só na história.
- Achado no meio do trabalho: comentário na issue atual ou nova sub-issue sob a história; nunca sub-issue de tarefa.
- Bugs de código nascem como issue no GitHub; o Linear recebe o overlay `(GH #N)`.

## Regras de código

- Máximo de 300 linhas de mudança de comportamento por PR. Scaffold gerado pelo Unity ou movimentação pura vão em commit separado e são declarados no PR.
- Comentários só para o "porquê" (invariante não óbvia, contorno de bug externo). Nada que narre a linha seguinte.
- Não reformatar, reordenar imports nem tocar docstrings fora do escopo da tarefa.
- Nunca declarar que algo funciona sem rodar o comando de verificação e ler o resultado.

## O que é do humano

Licença Unity, secrets de CI (`UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`), contas de loja/ads, sensação de jogo no aparelho e decisões de produto. Pare e pergunte.
