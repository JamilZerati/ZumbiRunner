---
cobre:
  - "Assets/_Game/Scripts/Editor/SceneBuilder.cs"
  - "Assets/_Game/Scripts/Editor/Cli.cs"
  - "Assets/_Game/Scripts/Editor/Tools/**"
  - "tools/**"
revisado: 2026-11-13
---
# Harness e cenas

O harness `tools/unity` chama métodos de `Cli` em batchmode. As cenas são geradas pelo `SceneBuilder`: o que ele não adiciona e liga não existe em jogo, por mais testado que esteja. Operação do harness: `docs/agentes/harness-unity.md`.

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Cenas M1 a M3 | `Assets/_Game/Scripts/Editor/SceneBuilder.cs:BuildM3GreyboxScene` | General, lanes, tropa e portões; sem arma, inimigo ou combate |
| Cenas M4 a M6 | `Assets/_Game/Scripts/Editor/SceneBuilder.cs:BuildCombatGreyboxScene` | método único parametrizado; adiciona `CombatDirector`, `WeaponController`, `HordeSpawner`, poder do General, HUDs, UI e composer |
| Cena Bootstrap | `Assets/_Game/Scripts/Editor/SceneBuilder.cs:BuildBootstrapScene` | só o `GameLifetimeScope`; fora do build |
| Build Android | `Assets/_Game/Scripts/Editor/Cli.cs:BuildAndroid` | cena única, `DefaultAndroidScenePath` (M6) |
| Simulador headless | `Assets/_Game/Scripts/Editor/Tools/SimulateCommand.cs:SimulateSingleRun` | roda a fase por um motor próprio, sem cena |
| Validação de conteúdo | `Assets/_Game/Scripts/Editor/Tools/ValidateCommand.cs:Run` | fases por `LevelValidator`; habilidades, portões, jaulas e barricadas por validadores locais |

## Invariantes

- Cena é gerada: muda-se o `SceneBuilder` e regenera-se.
- O barramento e a máquina de estados criados no `SceneBuilder` não são serializados; o que vale em jogo são os do composer — ver [composição e boot](composicao-e-boot.md).

## Mexer junto

- Componente novo de gameplay ou apresentação → `BuildCombatGreyboxScene` e a fiação do `GreyboxRunComposer` por `WireSerialized`.
- Regra de fase que muda → o equivalente no `SimulateCommand`, enquanto ele tiver motor próprio.

## Divergências

- **As hordas do jogo são fixas na cena.** As seis ondas de M6 são criadas em tempo de edição por `spawner.SpawnWave(...)` dentro do `SceneBuilder`; nenhum código de produção chama `HordeSpawner.SpawnWave`. A fase não lê `LevelDefinition`.
- **Componentes de produção que nenhuma cena gerada instancia**: `LevelDirector`, `MeleeEngagementManager`, `HoldoutController`, `Barricade`, `Cage`, `MultiplierLane`, `HeroUnit`, `PetUnit`, `RevivePromptWidget`, `BarricadeView`, `CageView`, `AllyHudView`, `WeatherVisualController`. Tudo o que depende deles (segmentos da fase, engajamento, barricada, jaula, boss, pista de multiplicador, aliados, clima, escape, estrelas, revive) só roda em teste e no simulador — ver [laço da fase](../fluxos/laco-da-fase.md).
- **O simulador é um segundo jogo.** `SimulateCommand` reusa `StageModifierController` e reimplementa por conta própria dano, portões, hordas, barricada, jaula, boss e pista de multiplicador (`ProcessHordeSegment`, `ProcessGateSegment`, `ProcessHoldoutSegment`, `ProcessCageSegment`, `ProcessBossSegment`, `ProcessMultiplierSegment`). O relatório de balanceamento mede esse motor, não o que a cena executa.
- O `AGENTS.md` ainda diz que `validate` e `simulate` são stubs que saem com 0; os dois têm implementação e `validate` devolve 1 com erro.
- Números de balanceamento no `SceneBuilder`: distância de vitória 120, posição e tamanho das ondas, perks dos portões por id, velocidade da pista 8 e tropa inicial 10 (3 nas cenas M2 e M3).
- M1 a M3 são cópias manuais uma da outra com um bloco a mais cada.
