---
cobre:
  - "Assets/_Game/Scripts/Core/Events/**"
revisado: 2026-11-13
---
# Barramento de eventos

`IEventBus` é o canal entre gameplay, meta e apresentação: quem publica não conhece quem assina. A entrega é síncrona, na ordem de inscrição, sobre uma cópia da lista de assinantes. Esta doc registra quem publica e quem assina cada evento em produção, porque evento sem um dos dois lados é ligação que falta.

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Barramento tipado | `Assets/_Game/Scripts/Core/Events/EventBus.cs:Publish` | um por cena, criado pelo composer; `Subscribe` devolve `IDisposable` |
| Eventos C# locais | `Assets/_Game/Scripts/Gameplay/LevelDirector.cs:OnReviveOffered` | `LevelDirector` expõe `OnRunEnded`, `OnHordeSpawned`, `OnBossFightEnded` e outros fora do barramento; só quem tem a referência do componente assina |
| Evento C# espelhado | `Assets/_Game/Scripts/Gameplay/SquadController.cs:ApplyCountChange` | publica `SquadSizeChangedEvent` no barramento e também no evento local `SquadSizeChanged` |

## Invariantes

- Handler nulo em `Subscribe` lança exceção; publicar sem assinante não faz nada.
- Exceção num handler interrompe a entrega aos seguintes: não há isolamento por assinante.
- Evento descreve fato consumado. Nenhuma regra espera retorno de um `Publish`.

## Mexer junto

- Evento novo → publicador e pelo menos um assinante de produção no mesmo PR, e a tabela abaixo.
- Assinatura nova em `MonoBehaviour` → guardar o `IDisposable` e descartar no `OnDestroy`.

## Divergências

Sem publicador em produção:

- `BossTelegraphEvent` não é publicado nem assinado por ninguém.

Sem assinante em produção (publicados e descartados): `AllyDamagedEvent`, `AllyRecruitedEvent`, `BossSpawnedEvent`, `BulletTimeStartedEvent`, `BulletTimeEndedEvent`, `DamageTakenEvent`, `EntityDiedEvent`, `EnemyConsumedEvent`, `EnemyEngagedEvent`, `EnemyDisengagedEvent`, `EnemyEscapedEvent`, `SoldierDamagedEvent`, `WeaponEquippedEvent`, `ObjectiveCompletedEvent`.

Com assinante que não está em nenhuma cena:

- `RunEndedEvent`, `HoldoutStartedEvent`, `HoldoutEndedEvent`, `MultiplierReachedEvent`, `SurvivorsRescuedEvent`, `SurvivorsLostEvent` e `BossDefeatedEvent` dependem do `LevelDirector`, do `HoldoutController`, da `Cage` ou do `ObjectiveTracker`, que nenhuma cena gerada instancia — ver [laço da fase](../fluxos/laco-da-fase.md).
- `GatePassedEvent` só é assinado pelo `ObjectiveTracker`, que ninguém cria em jogo.

Outros:

- `RunStartedEvent` tem dois publicadores, `Assets/_Game/Scripts/Presentation/UI/HubView.cs:StartSelectedRun` e `Assets/_Game/Scripts/Gameplay/LevelDirector.cs:Initialize`. Com os dois ativos, o composer ativaria as mutações duas vezes.
- `SquadSizeChangedEvent` é publicado por `SquadController` e, no revive, direto por `Assets/_Game/Scripts/Gameplay/LevelDirector.cs:ReviveSquad`, sem passar pela tropa.
- Os eventos de meta `WalletChangedEvent`, `BuildingUpgradeCompletedEvent` e `ObjectiveCompletedEvent` moram em `Game.Meta`, fora desta pasta.
