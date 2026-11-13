---
cobre:
  - "Assets/_Game/Scripts/Presentation/UI/HubView.cs"
  - "Assets/_Game/Scripts/Presentation/UI/StageSelectionWidget.cs"
  - "Assets/_Game/Scripts/Presentation/UI/ResultView.cs"
  - "Assets/_Game/Scripts/Presentation/UI/ResultRewardPresenter.cs"
  - "Assets/_Game/Scripts/Presentation/UI/RevivePromptWidget.cs"
  - "Assets/_Game/Scripts/Meta/Run/**"
  - "Assets/_Game/Scripts/Meta/Rewards/**"
  - "Assets/_Game/Scripts/Meta/Wallet/**"
revisado: 2026-11-13
---
# Início e fim de fase

Do botão de jogar no Hub até as moedas na carteira. O Hub monta o `RunConfig` (fase, mutações, tropa inicial, bônus do Quartel), a fase devolve um `RunResult` (vitória, estrelas, moedas, multiplicador), e a tela de resultado converte isso em recompensa.

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Jogar | `Assets/_Game/Scripts/Presentation/UI/HubView.cs:StartSelectedRun` | transiciona para `Run`, monta o `RunConfig` e publica `RunStartedEvent` |
| Config pelo construtor da meta | `Assets/_Game/Scripts/Meta/Run/RunConfigBuilder.cs:Build` | soma tropa, dano, munição e recuperação do Quartel |
| Config inline | `Assets/_Game/Scripts/Presentation/UI/HubView.cs:StartSelectedRun` | só fase, mutações e tropa 10 (ou 20 com o reforço por anúncio) |
| Vitória por distância | `Assets/_Game/Scripts/Gameplay/Combat/CombatDirector.cs:CheckVictory` | pista percorreu `VictoryDistance` → estado `Victory` |
| Derrota por contato | `Assets/_Game/Scripts/Gameplay/Combat/CombatDirector.cs:TriggerDefeat` | zumbi toca o General com a tropa zerada → estado `Defeat` |
| Fim pelo diretor de fase | `Assets/_Game/Scripts/Gameplay/LevelDirector.cs:EndRun` | calcula estrelas, monta o `RunResult` e publica `RunEndedEvent` |
| Recompensa | `Assets/_Game/Scripts/Presentation/UI/ResultRewardPresenter.cs:PresentResult` | `RewardCalculator.Calculate` → `RewardSink` → `Wallet` |
| Continuar | `Assets/_Game/Scripts/Presentation/UI/ResultRewardPresenter.cs:ClaimAndContinue` | volta ao `Hub` |

## Invariantes

- Fase bloqueada no mapa não inicia.
- Moedas finais = moedas da fase × multiplicador alcançado × multiplicador do config × (1 + soma dos bônus de mutação), em dobro com anúncio — `Assets/_Game/Scripts/Meta/Run/RewardCalculator.cs:Calculate`.
- Cada concessão tem um id; a mesma não é concedida duas vezes — `Assets/_Game/Scripts/Meta/Rewards/RewardSink.cs:HasGranted`.
- Saldo não fica negativo — `Assets/_Game/Scripts/Meta/Wallet/Wallet.cs:TrySpend`.

## Mexer junto

- Campo novo no `RunConfig` → `RunConfigBuilder.Build`, o config inline do Hub e um leitor no gameplay.
- Mudança na regra de moedas → `RewardCalculator` e o equivalente no simulador ([harness](../arquitetura/harness-e-cenas.md)).

## Divergências

- **A fase não termina.** Vitória e derrota param a pista e mudam o estado, e mais nada acontece: nenhum código transiciona para `Result`, `PresentResult` não tem chamador e `RunEndedEvent` só é publicado pelo `LevelDirector`, que não está em cena. Não há tela de resultado, recompensa nem volta ao Hub além de pausar e sair.
- **O `RunConfig` não chega ao jogo.** O Hub é inicializado sem `IRunConfigBuilder`, então vale o config inline; `InitialSquad` só é lido pelo `LevelDirector`. O reforço "+10 Tropa" pago com anúncio não muda a tropa.
- Fase jogada não fica registrada: `RecordStageResult` do mapa e `ProcessRunResult` da progressão não têm chamador — ver [progressão e trilhas](progressao-e-trilhas.md).
- O pedido de revive não aparece: `RevivePromptWidget` não está em nenhuma cena e depende de `LevelDirector.OnReviveOffered`.
- Dois critérios de vitória e derrota: o do `CombatDirector` (distância fixa e contato) e o do `LevelDirector` (fim da `LevelDefinition`, boss, tropa zerada com revive). Só o primeiro roda — ver [laço da fase](laco-da-fase.md).
- Escolher outra fase no Hub não muda o que se joga: a cena é sempre a mesma pista, com as mesmas hordas.
