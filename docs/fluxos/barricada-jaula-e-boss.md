---
cobre:
  - "Assets/_Game/Scripts/Core/Holdout/**"
  - "Assets/_Game/Scripts/Gameplay/Holdout/**"
  - "Assets/_Game/Scripts/Data/Holdout/**"
  - "Assets/_Game/Scripts/Core/Rescue/**"
  - "Assets/_Game/Scripts/Gameplay/Rescue/**"
  - "Assets/_Game/Scripts/Data/Cages/**"
  - "Assets/_Game/Scripts/Core/Boss/**"
  - "Assets/_Game/Scripts/Presentation/BarricadeView.cs"
  - "Assets/_Game/Scripts/Presentation/CageView.cs"
revisado: 2026-11-13
---
# Barricada, Jaula e Boss

Três tipos de trecho que quebram o ritmo da corrida. Na Barricada a pista para e a tropa defende sobreviventes por um tempo. A Jaula é um obstáculo que, aberto a tiros, libera sobreviventes como soldados. O Boss é uma luta em arena com fases por faixa de vida. Os três são disparados pelo diretor de fase.

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Início da barricada | `Assets/_Game/Scripts/Gameplay/Holdout/HoldoutController.cs:StartHoldout` | pausa a pista e publica `HoldoutStartedEvent` |
| Dano à barricada | `Assets/_Game/Scripts/Gameplay/Holdout/Barricade.cs:TickDamage` | soma o dano por segundo dos zumbis em contato fora da lane guardada |
| Barricada mantida | `Assets/_Game/Scripts/Gameplay/Holdout/HoldoutController.cs:HandleHoldoutVictory` | soma os sobreviventes salvos à tropa |
| Barricada destruída | `Assets/_Game/Scripts/Gameplay/Holdout/HoldoutController.cs:HandleBarricadeDestroyed` | perde todos os sobreviventes |
| Jaula aberta | `Assets/_Game/Scripts/Gameplay/Rescue/Cage.cs:TakeDamage` | vida zerada a tiros → sobreviventes viram soldados |
| Jaula ignorada | `Assets/_Game/Scripts/Gameplay/Rescue/Cage.cs:CheckEscape` | passou do General sem abrir → sobreviventes perdidos |
| Luta de boss | `Assets/_Game/Scripts/Core/Boss/BossCombatRuntimeState.cs:StartArena` | vida multiplicada pelos modificadores de fase; a fase só termina com o boss morto |

## Invariantes

- Sobreviventes salvos na barricada = teto de (total × vida restante ÷ vida máxima); zero se ela cair — `Assets/_Game/Scripts/Core/Holdout/HoldoutRuntimeState.cs:HoldoutRuntimeState`.
- A lane em que o General está é a guardada; só zumbis das outras ferem a barricada.
- Sobrevivente perdido volta como zumbi na próxima horda da mesma lane.
- Barricada tentada e não mantida limita a fase a duas estrelas.

## Mexer junto

- Campo novo de barricada ou jaula → a `Definition`, o importador, o estado de runtime, `LevelValidator` e o simulador.
- Regra de sobrevivente → `HoldoutRuntimeState`, `CageRuntimeState` e os contadores do `LevelDirector`.

## Divergências

- **Nenhum dos três existe em jogo.** `HoldoutController`, `Barricade`, `Cage`, `BarricadeView` e `CageView` não estão em nenhuma cena; `StartHoldout` e `TrackCage` não têm chamador de produção; a luta de boss é iniciada pelo `LevelDirector`, que não roda — ver [laço da fase](laco-da-fase.md). As regras acima só são exercitadas por teste e pelo simulador, que as reimplementa.
- `BossTelegraphEvent` não é publicado nem assinado; `BossSegmentSpec` só é usado por testes.
- Os comportamentos do boss (`BossPingPongBehavior`, `BossSummonWaveBehavior`, `BossLaneTelegraphDiveBehavior`) são dados sem executor — ver [hordas e inimigos](hordas-e-inimigos.md).
- Barricada e jaula guardam vida no próprio estado de runtime, fora do `HealthComponent`; a jaula implementa `IDamageable` e a barricada não.
- Sem definição, a barricada usa 10 de dano por segundo por zumbi, literal em `TickDamage`.
