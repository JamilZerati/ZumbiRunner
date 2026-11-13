---
cobre:
  - "Assets/_Game/Scripts/Core/*.cs"
  - "Assets/_Game/Scripts/Gameplay/LaneMover.cs"
  - "Assets/_Game/Scripts/Gameplay/SquadController.cs"
  - "Assets/_Game/Scripts/Infrastructure/Input/**"
  - "Assets/_Game/Scripts/Infrastructure/Pooling/**"
  - "Assets/_Game/Scripts/Data/LaneLayoutDefinition.cs"
  - "Assets/_Game/Scripts/Presentation/FollowCamera.cs"
  - "Assets/_Game/Scripts/Presentation/SoldierView.cs"
  - "Assets/_Game/Scripts/Presentation/SquadCountHud.cs"
  - "Assets/_Game/Scripts/Presentation/SquadVisualController.cs"
revisado: 2026-11-13
---
# Lanes, input e tropa

O General troca de lane por toque ou tecla, e a tropa o segue em formação. A contagem da tropa é a vida e o poder de fogo do jogador: tudo o que acrescenta ou tira soldado passa pelo `SquadController`.

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Entrada | `Assets/_Game/Scripts/Infrastructure/Input/StandaloneLaneInput.cs:StandaloneLaneInput` | teclado e gesto de deslize; emite direção -1 ou +1 |
| Deslize | `Assets/_Game/Scripts/Core/SwipeDetector.cs:SwipeDetector` | só aceita gesto mais horizontal que vertical e acima da distância mínima |
| Troca de lane | `Assets/_Game/Scripts/Gameplay/LaneMover.cs:RequestMove` | muda a lane alvo, publica `LaneChangedEvent` e interpola a posição |
| Alteração da tropa | `Assets/_Game/Scripts/Gameplay/SquadController.cs:ApplyCountChange` | ponto único; publica `SquadSizeChangedEvent` |
| Formação | `Assets/_Game/Scripts/Core/FormationSolver.cs:CalculatePositions` | posições dos soldados para uma contagem |
| Visual da tropa | `Assets/_Game/Scripts/Presentation/SquadVisualController.cs:SynchronizeSquad` | pool de `SoldierView` sincronizado com a contagem |

## Invariantes

- Número e largura das lanes vêm de `LaneLayout`; lane fora do layout é recusada.
- A contagem nunca é escrita por fora de `Add`, `Remove`, `Multiply`, `Divide` e `SetCount`, e toda mudança publica o evento.
- O `ObjectPool` devolve instância inativa; quem aluga ativa e posiciona.

## Mexer junto

- Operação nova sobre a tropa → `ISquad`, `SquadController` e o duplo de teste que implementa `ISquad`.
- Mudança de layout de lanes → `LaneLayoutDefinition`, `SceneBuilder` e as fases em `Content/Source/Levels`.

## Divergências

Quem altera a tropa em jogo, fora de portão e perk:

- contato com zumbi: `Assets/_Game/Scripts/Gameplay/Combat/CombatDirector.cs:ResolveEnemyContact`;
- tropa inicial forçada por mutação: `Assets/_Game/Scripts/Composition/GreyboxRunComposer.cs:OnRunStarted`.

Caminhos que existem e não rodam: `MeleeEngagementManager`, `HoldoutController`, `Cage` e `MultiplierLane` chamam `Add` e `Remove`, e nenhum está em cena — ver [engajamento](engajamento-e-perda-de-tropa.md).

Outros:

- A tropa inicial da cena de combate é 10, fixa no `SceneBuilder`; `RunConfig.InitialSquad` (10, ou 20 com o reforço) não é aplicado — ver [início e fim de fase](inicio-e-fim-de-fase.md).
- O `SquadController` também é o dono do elenco de aliados (`TryRecruit`, `PositionAlly`): duas responsabilidades no mesmo componente — ver [aliados](aliados.md).
- `SetTimeService` não tem chamador — ver [tempo](../regras/tempo.md).
