---
cobre:
  - "Assets/_Game/Scripts/Meta/Stages/**"
  - "Assets/_Game/Scripts/Meta/Progression/**"
  - "Assets/_Game/Scripts/Meta/Tracks/**"
  - "Assets/_Game/Scripts/Meta/Objectives/**"
  - "Assets/_Game/Scripts/Meta/Debut/**"
  - "Assets/_Game/Scripts/Presentation/UI/DebutJourneyView.cs"
revisado: 2026-11-13
---
# Progressão e trilhas

O que o jogador libera ao longo do tempo. O mapa de fases guarda estrelas e libera a fase seguinte; vencer uma Fase Boss desbloqueia conteúdo; objetivos contam abates, portões e resgates; a Trilha é uma sequência de marcos com recompensa, usada hoje pela jornada de estreia de sete dias.

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Resultado no mapa | `Assets/_Game/Scripts/Meta/Stages/StageMapService.cs:RecordStageResult` | guarda a melhor nota (0 a 3) e libera a fase seguinte com pelo menos uma estrela |
| Desbloqueio por boss | `Assets/_Game/Scripts/Meta/Progression/ProgressionService.cs:ProcessRunResult` | vitória em Fase Boss concede o conteúdo uma vez |
| Objetivos | `Assets/_Game/Scripts/Meta/Objectives/ObjectiveTracker.cs:IngestRunResult` | soma métricas da fase e publica `ObjectiveCompletedEvent` |
| Marco de trilha | `Assets/_Game/Scripts/Meta/Tracks/ProgressTrack.cs:ClaimMilestone` | exige pontos e, no marco premium, o direito da trilha |
| Jornada de estreia | `Assets/_Game/Scripts/Meta/Debut/DebutJourneyService.cs:ClaimDay` | um prêmio por dia de login já alcançado |

## Invariantes

- Estrelas só sobem: resultado pior não rebaixa a nota da fase.
- Fase só é jogável se liberada — conferido no Hub antes de iniciar.
- Conteúdo de boss e marco de trilha são concedidos uma vez, pelo id da concessão.

## Mexer junto

- Fase nova → a ordem do mapa em `CreateDefaultStages`, o catálogo de desbloqueio e `Content/Source/Levels`.
- Trilha nova (passe, evento) → `ProgressTrackDefinition`, estado persistido e a tela.

## Divergências

- **Vencer não libera nada.** `RecordStageResult`, `ProcessRunResult` e `IngestRunResult` não têm chamador de produção: o mapa fica para sempre com a primeira fase, nenhum conteúdo de boss é desbloqueado e nenhum objetivo progride — consequência de a fase não terminar; ver [início e fim de fase](inicio-e-fim-de-fase.md).
- `ObjectiveTracker` não é instanciado em jogo; os eventos que ele assinaria (`EnemyKilledEvent`, `GatePassedEvent`, `SurvivorsRescuedEvent`) seguem sem esse leitor.
- A jornada de estreia recebe o dia de login fixo em 1 e uma trilha nova a cada carga: o jogador pode pegar o prêmio do dia 1 toda vez que abre o app, e os dias 2 a 7 nunca liberam.
- A ordem das fases é código (`Assets/_Game/Scripts/Meta/Stages/StageMapService.cs:CreateDefaultStages`), ao lado de dois arquivos em `Content/Source/Levels`.
- Sem `MetaState`, mapa e progressão guardam em coleções próprias que somem ao fechar — ver [save](../regras/save-e-integridade.md).
- Passe de temporada, evento e login diário, descritos no `CONTEXT.md` como usos da Trilha, não têm código além da própria `ProgressTrack`.
