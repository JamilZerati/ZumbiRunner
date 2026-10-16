# Plano de Implementação: M12 · Núcleo de meta (Game.Meta), mapa de fases e save
> Data: 2026-10-15
> Issue: NEX-517
> Status: Planejamento — Proposta de Fatiamento e Marcos

## 1. Contexto & Arquitetura
- **Resumo**: Implementar o núcleo de progressão e meta (`Game.Meta`) da Story M12 (NEX-517) no Horde Runner. A arquitetura estabelece as fundações puras para todo o ecossistema de meta, live-ops, monetização e progressão (passe de temporada, missões diárias, eventos temporários, login diário, quartel e mapa de fases). Componentes centrais:
  1. `Game.Meta`: Assembly em C# puro (`noEngineReferences: true`) desacoplado de `UnityEngine` e referenciado apenas por camadas superiores (`Game.Data`, `Game.Infrastructure`, `Game.Presentation`, testes); `Game.Gameplay` **NÃO** referencia `Game.Meta`.
  2. `Wallet`: Gerenciamento multimoedas (`Coins`, `Recruits`, `Medals`, `EventToken: <id>`) com rastreamento obrigatório de `source` em cada movimentação (auditoria e telemetria) e eventos no bus.
  3. `RewardBundle` & `IReward`: Sistema de concessão idempotente por `grantId` determinístico via `IRewardSink`, suportando moedas, itens/boosts, cosméticos, desbloqueios de conteúdo e direitos (`Entitlements`).
  4. `ObjectiveTracker`: Monitoramento e liquidação de objetivos por sinais do gameplay (`IEventBus`) e consolidação ao término da corrida (`RunResult`), suportando escopos `Run` e `Cumulative`.
  5. `ProgressTrack`: Trilha de progressão com marcos e ramificação Grátis / Premium para passes, eventos, login diário e onboarding; integrado ao gerenciador de `Entitlements`.
  6. `ITimeProvider` & Monotonic Clock Guard: Provedor de tempo UTC desacoplado com guarda monotônica contra retrocesso fraudulento de relógio do aparelho (> 10 min).
  7. `ISaveStore` & `MetaState`: Agregado persistível versionado com migrações automáticas encadeadas e hash de integridade contra adulteração casual.
  8. Fronteira Gameplay ↔ Meta: `RunConfigBuilder` compila o estado da meta em `RunConfig` imutável para a partida; `RewardCalculator` processa `(RunResult, RunConfig)` gerando o recibo de liquidação auditável da corrida; `StageMap` gerencia progressão de fases e fases Boss.
- **Stack**: Unity 6 (6000.6.3f1), C# (.NET Standard / C# 9+), JSON local, Harness CLI (`tools/unity`).
- **Verificação disponível**: Suíte automatizada de testes EditMode (`tools/unity compile`, `test-edit`).

## 2. Referência Técnica REA (Top Lords / Last War v1.0.39)
> Origem da evidência: engenharia reversa via REA do pacote `com.gamespark.topking.gp` v1.0.39 (`Assembly-CSharp.dll`, evidência `ev_4847563dadd0903795d0f44d08eb2d485399f1187386b3a7188c6b229262da0f`, `LWScripts.data` e `table_3933.zip`, evidência `ev_c15a5164fc984a9de8b838113d8ffa27d3fd545a86f18597a03ba06e8237b35f`).

### 1. Recibo Auditável da Partida (`ParkourBattleData.luac` & `Net/Msgs/SurfingBattle/*`)
- Durante a corrida, a gameplay não altera diretamente moedas na carteira persistente nem concede recompensas parciais no meio do frame.
- O resultado é consolidado num recibo de partida (`ParkourFightEndMessage.luac` / `ParkourStageEndMessage.luac`) com `seed`, `stageId`, tempo e métricas de abates/resgates, entregue ao módulo de liquidação da meta (`RewardCalculator`).
- No Horde Runner: `RewardCalculator` gera um `RewardBundle` com `grantId = $"{runConfig.LevelId}_{runResult.RunSessionId}"` (ou chave determinística de sessão), garantindo idempotência total contra duplicação de crédito por reentrada ou falha.

### 2. Separação Estrita de Estado e Catálogo (`StageFeatureChapterRecordMessage.luac` & `lw_parkour_stage.lua`)
- O estado de progressão (`StageRecord`: estrelas obtidas, desbloqueio, tentativas) é persistido no save do jogador separadamente das definições imutáveis de fase (`lw_parkour_stage`).
- Fases Boss operam como divisores de capítulo (gates de progressão) que destravam novas mecânicas de meta.
- No Horde Runner: `StageMap` mapeia o histórico de fases superadas e estrelas (0 a 3) com flags para fases marcadas como `IsBossStage`.

### 3. Parâmetros da Meta no Loadout da Partida (`LastStandBarracks.luac`, `LastStandHospital.luac`)
- Parâmetros da meta (níveis de construções, boosts de inventário e mutações ativas) são lidos e compilados antes da fase iniciar (`ParkourFightStartCheckMessage.luac`), gerando a entrada da corrida.
- No Horde Runner: `RunConfigBuilder` compila `MetaState` em um `RunConfig` imutável, mantendo `Game.Gameplay` completamente agnóstico sobre regras de persistência e economia da meta.

## 3. Armadilhas do Repositório

| Armadilha | Onde morde (arquivo/passo) | Regra a respeitar |
|---|---|---|
| Pureza C# em `Game.Meta` | `Assets/_Game/Scripts/Meta/Game.Meta.asmdef` | `Game.Meta` DEVE ter `"noEngineReferences": true`. Nenhuma classe da meta pode importar `UnityEngine` ou usar APIs como `Mathf`, `PlayerPrefs`, `Time.time` ou `MonoBehaviour`. Usar `System.Math`, `System.DateTime`, `System.Text.Json` ou serializador C# puro. |
| Invariante de Direção de Asmdefs | `Game.Gameplay.asmdef` | `Game.Gameplay` **NUNCA** referencia `Game.Meta`. A comunicação ocorre unicamente por `RunConfig` (entrada), sinais em `IEventBus` (durante a fase) e `RunResult` (saída). |
| Idempotência de Concessão de Recompensas | `RewardBundle.cs` / `IRewardSink.cs` — Marco 1 | Se o mesmo `grantId` for submetido mais de uma vez a um `IRewardSink`, a concessão deve retornar sucesso sem duplicar saldo de moedas, itens ou direitos no estado persistido. |
| Guarda Monotônica do Relógio | `TimeProvider.cs` — Marco 4 | Se o relógio do sistema retroceder mais de 10 minutos em relação a `lastSeenUtc`, `TimeProvider` deve ativar o estado de congelamento ou trava monotônica (`IsClockManipulated = true`), impedindo exploração de timers de obra e calendário offline. |
| Versionamento e Integridade do Save | `SaveStore.cs` — Marco 4 | Todo save serializado possui `saveVersion` e `integrityHash` (HMAC/SHA256). O carregamento deve aplicar migrações sequenciais registradas (`ISaveMigration`) antes de desserializar o estado na versão corrente. |
| Multiplicador de Recompensas e Mutações | `RewardCalculator.cs` — Marco 5 | Moedas ganhas seguem a fórmula: `coins = (int)Math.Round(runResult.CoinsEarned * runResult.MultiplierReached * (1f + sum(mutationBonuses)))`. Não truncar precipitadamente para evitar discrepâncias de arredondamento. |

## 4. Árvore de Arquivos Planejada

```
Assets/_Game/Scripts/Meta/
├── Game.Meta.asmdef
├── Wallet/
│   ├── CurrencyKind.cs
│   ├── IWallet.cs
│   ├── Wallet.cs
│   └── WalletTransaction.cs
├── Rewards/
│   ├── IReward.cs
│   ├── IRewardSink.cs
│   ├── RewardBundle.cs
│   ├── CurrencyReward.cs
│   ├── ItemReward.cs
│   ├── UnlockReward.cs
│   └── EntitlementReward.cs
├── Objectives/
│   ├── ObjectiveScope.cs
│   ├── ObjectiveDefinition.cs
│   ├── ObjectiveProgress.cs
│   ├── IObjectiveTracker.cs
│   └── ObjectiveTracker.cs
├── Tracks/
│   ├── MilestoneDefinition.cs
│   ├── ProgressTrackDefinition.cs
│   ├── ProgressTrackState.cs
│   ├── IProgressTrack.cs
│   ├── ProgressTrack.cs
│   ├── IEntitlements.cs
│   └── Entitlements.cs
├── Time/
│   ├── ITimeProvider.cs
│   └── MonotonicTimeProvider.cs
├── Save/
│   ├── MetaState.cs
│   ├── ISaveStore.cs
│   ├── ISaveMigration.cs
│   ├── SaveStore.cs
│   └── Migrations/
│       └── SaveMigrationV0ToV1.cs
├── Stages/
│   ├── StageProgress.cs
│   ├── IStageMapService.cs
│   └── StageMapService.cs
└── Run/
    ├── IRunConfigBuilder.cs
    ├── RunConfigBuilder.cs
    ├── IRewardCalculator.cs
    └── RewardCalculator.cs

Assets/_Game/Scripts/Tests/EditMode/Meta/
├── MetaScenariosTests.cs
├── WalletTests.cs
├── RewardTests.cs
├── ObjectiveTrackerTests.cs
├── ProgressTrackTests.cs
├── TimeProviderTests.cs
├── SaveStoreTests.cs
├── StageMapTests.cs
└── MetaGameplayIntegrationTests.cs
```

## 5. Contratos e Interfaces Principais

```csharp
namespace Game.Meta.Wallet
{
    public enum CurrencyKind
    {
        Coins = 0,
        Recruits = 1,
        Medals = 2,
        EventToken = 3
    }

    public interface IWallet
    {
        int GetBalance(CurrencyKind currency, string tokenId = null);
        void Add(CurrencyKind currency, int amount, string source, string tokenId = null);
        bool TrySpend(CurrencyKind currency, int amount, string source, string tokenId = null);
        IReadOnlyList<WalletTransaction> GetHistory();
    }
}

namespace Game.Meta.Rewards
{
    public interface IReward
    {
        string RewardType { get; }
        void Apply(IRewardSink sink);
    }

    public interface IRewardSink
    {
        IWallet Wallet { get; }
        IEntitlements Entitlements { get; }
        bool HasGranted(string grantId);
        void RecordGrant(string grantId);
        void AddItem(string itemId, int quantity);
        void UnlockContent(string contentId);
    }

    public class RewardBundle
    {
        public string GrantId { get; }
        public IReadOnlyList<IReward> Rewards { get; }

        public RewardBundle(string grantId, IReadOnlyList<IReward> rewards)
        {
            GrantId = grantId;
            Rewards = rewards ?? Array.Empty<IReward>();
        }

        public bool Grant(IRewardSink sink)
        {
            if (sink.HasGranted(GrantId)) return false;
            foreach (var reward in Rewards)
            {
                reward.Apply(sink);
            }
            sink.RecordGrant(GrantId);
            return true;
        }
    }
}

namespace Game.Meta.Objectives
{
    public enum ObjectiveScope
    {
        Run = 0,
        Cumulative = 1
    }

    public interface IObjectiveTracker
    {
        void RegisterObjective(ObjectiveDefinition definition);
        ObjectiveProgress GetProgress(string objectiveId);
        void IngestEvent<TEvent>(TEvent evt) where TEvent : class;
        void IngestRunResult(Game.Core.State.RunResult result);
        IReadOnlyList<string> CheckCompletedObjectives();
    }
}

namespace Game.Meta.Tracks
{
    public interface IProgressTrack
    {
        string TrackId { get; }
        int CurrentPoints { get; }
        void AddPoints(int points);
        bool CanClaimMilestone(int milestoneIndex, bool isPremium);
        RewardBundle ClaimMilestone(int milestoneIndex, bool isPremium, IRewardSink sink);
    }

    public interface IEntitlements
    {
        bool HasEntitlement(string entitlementId);
        void GrantEntitlement(string entitlementId);
        void RevokeEntitlement(string entitlementId);
        IReadOnlySet<string> GetAllEntitlements();
    }
}

namespace Game.Meta.Time
{
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
        long LastSeenUtcTicks { get; }
        bool IsClockManipulated { get; }
        void RecordCurrentTime();
    }
}

namespace Game.Meta.Save
{
    public interface ISaveStore
    {
        int CurrentVersion { get; }
        void Save(MetaState state, string filePath);
        MetaState Load(string filePath);
        bool ValidateIntegrity(string filePath);
    }

    public interface ISaveMigration
    {
        int SourceVersion { get; }
        int TargetVersion { get; }
        string MigrateJson(string sourceJson);
    }
}

namespace Game.Meta.Stages
{
    public interface IStageMapService
    {
        bool IsStageUnlocked(string stageId);
        int GetStars(string stageId);
        void RecordStageResult(string stageId, int stars, bool isBoss);
        string GetHighestUnlockedStage();
        IReadOnlyList<string> GetAllBossStages();
    }
}

namespace Game.Meta.Run
{
    public interface IRunConfigBuilder
    {
        Game.Core.State.RunConfig Build(string stageId, IReadOnlyList<string> selectedMutations, IReadOnlyList<string> activeBoosts);
    }

    public interface IRewardCalculator
    {
        RewardBundle Calculate(Game.Core.State.RunResult result, Game.Core.State.RunConfig config, bool doubleReward = false);
    }
}
```

## 6. Mapa de Execução & Marcos

| Marco | Tarefa | AC Cobertos | Depende de | Escopo & Entregáveis |
|---|---|---|---|---|
| **Marco 0** | **NEX-989** | AC-1, AC-2, AC-3, AC-4, AC-5, AC-6, AC-7 | — | **Suíte Red & Fundação Arquitetural**: criação do asmdef `Game.Meta` (`noEngineReferences: true`, referenciando `Game.Core`), fiação em `Game.Tests.EditMode.asmdef`, contratos e stubs puros em `Game.Meta.*`, suíte `MetaScenariosTests.cs` cobrindo cenários ponta a ponta. |
| **Marco 1** | **NEX-990** | AC-1, AC-2 | Marco 0 | **Carteira & Recompensas Idempotentes**: `Wallet`, transações com `source`, `WalletChangedEvent`, tipos de `IReward`, `RewardBundle` com `grantId` idempotente e testes unitários em `WalletTests` e `RewardTests`. |
| **Marco 2** | **NEX-991** | AC-3 | Marco 0 | **Objetivos & Tracker de Sinais**: `ObjectiveDefinition`, escopos `Run` e `Cumulative`, rastreamento por sinais do `IEventBus` e `RunResult`, eventos de conclusão e testes em `ObjectiveTrackerTests`. |
| **Marco 3** | **NEX-992** | AC-4 | Marco 0 | **Trilhas de Progresso & Entitlements**: `ProgressTrack`, marcos com divisão Grátis/Premium, resgate de recompensas, `Entitlements` com verificação estrita e testes em `ProgressTrackTests` e `EntitlementsTests`. |
| **Marco 4** | **NEX-993** | AC-5 | Marco 1, Marco 3 | **Tempo Monotônico & Save Versionado**: `MonotonicTimeProvider` com guarda monotônica (10 min), `MetaState` agregado, `ISaveStore` com migrações encadeadas (`ISaveMigration`), hash de integridade e testes em `TimeProviderTests` e `SaveStoreTests`. |
| **Marco 5** | **NEX-994** | AC-6, AC-7 | Marco 1, Marco 2, Marco 4 | **Fronteira Gameplay ↔ Meta & Mapa de Fases**: `RunConfigBuilder`, `RewardCalculator` com multiplicadores e mutações, `StageMapService` sequencial com Boss gates e testes em `MetaGameplayIntegrationTests` e `StageMapTests`. |

## 7. Critérios de Aceite (ACs)

- **AC-1 (Pureza & Carteira)**: `Wallet` opera com 4 moedas (`Coins`, `Recruits`, `Medals`, `EventToken`), exige `source` em cada movimentação, bloqueia saldo negativo em `TrySpend` e publica `WalletChangedEvent`.
- **AC-2 (Idempotência de Recompensas)**: `RewardBundle` executado repetidamente com o mesmo `grantId` sobre um `IRewardSink` concede recompensas exatamente 1 vez; execuções subsequentes retornam `false` sem alterar estados.
- **AC-3 (Rastreamento de Objetivos)**: `ObjectiveTracker` computa progresso de objetivos `Run` e `Cumulative` com base em sinais recebidos e no `RunResult`, marcando completude ao atingir a meta.
- **AC-4 (Trilhas & Entitlements)**: `ProgressTrack` desbloqueia marcos proporcionais aos pontos; marcos premium só podem ser resgatados se o jogador possuir o `Entitlement` correspondente.
- **AC-5 (Guarda Monotônica & Save)**: `MonotonicTimeProvider` sinaliza `IsClockManipulated = true` se o relógio retroceder > 10 minutos; `SaveStore` salva e carrega `MetaState`, migra versões anteriores e detecta adulteração de integridade.
- **AC-6 (Liquidação da Corrida)**: `RewardCalculator` calcula moedas baseadas em `runResult.CoinsEarned * multiplierReached * (1 + sum(mutationBonuses))`, gerando `RewardBundle` com `grantId` determinístico.
- **AC-7 (Mapa de Fases)**: `StageMapService` só desbloqueia a fase N se a fase N-1 foi vencida; armazena estrelas conquistadas (0 a 3) e identifica fases de Chefe (`IsBossStage`).

## 8. Verificação do Harness

A verificação de cada tarefa seguirá a ordem canônica:
1. `tools/unity compile` -> 0 erros.
2. `tools/unity test-edit` -> 100% verde (sem quebra dos 792 testes existentes).
3. `git status` -> Somente arquivos previstos na tarefa.

## 9. Fatiamento em Tarefas & Checklist de Execução

### `Marco 0: Suíte de Cenários, Contratos de Game.Meta e Stubs [NEX-517]` (`NEX-989`)
```dispatch
effort: medium
```
```track
level: tarefa
plan: docs/planos/2026-10-15-NEX-517-nucleo-de-meta-mapa-de-fases-e-save.md
marco: 0
steps: Passo 0 (Marco 0)
needs: cenarios
```
**Objetivo:** Criar o assembly C# puro `Game.Meta` (`noEngineReferences: true`), integrá-lo em `Game.Tests.EditMode.asmdef`, definir contratos/stubs puros das interfaces de meta e estabelecer a suíte comportamental Red em `MetaScenariosTests.cs`.
**Cobre:** AC-1 a AC-7 (História NEX-517)
**Seam:** `Game.Meta.Wallet.IWallet`, `Game.Meta.Rewards.IRewardSink`, `Game.Meta.Objectives.IObjectiveTracker`, `Game.Meta.Tracks.IProgressTrack`, `Game.Meta.Time.ITimeProvider`, `Game.Meta.Save.ISaveStore`, `Game.Meta.Run.IRunConfigBuilder`, `Game.Meta.Stages.IStageMapService`.
**Pronto quando:**
- [x] Assembly `Game.Meta` criado com `noEngineReferences: true` e referenciando apenas `Game.Core`
- [x] Stubs dos contratos compilam sem quebrar os 792 testes existentes
- [x] Suíte Red implementada em `MetaScenariosTests.cs` cobrindo cenários ponta a ponta
- [x] `tools/unity compile` e `tools/unity test-edit` executam sem erros de compilação e com Red comprovado
**Não fazer:** Não implementar a lógica concreta dos serviços de meta nesta etapa nem criar dependências com `UnityEngine`.
**Contexto:** História NEX-517 · Plano Passo 0

### `Marco 1: Carteira, Moedas e Sistema de Recompensas Idempotentes [NEX-517]` (`NEX-990`)
```dispatch
effort: medium
```
```track
level: tarefa
plan: docs/planos/2026-10-15-NEX-517-nucleo-de-meta-mapa-de-fases-e-save.md
marco: 1
steps: Passo 1 (Marco 1)
needs: carteira-recompensas
```
**Objetivo:** Implementar o sistema de carteira multimoedas (`Wallet`) com registro obrigatório de `source`, emissão de `WalletChangedEvent`, e o pipeline de concessão idempotente de recompensas (`RewardBundle`, `IReward`, `IRewardSink`) por `grantId` determinístico.
**Cobre:** AC-1, AC-2 (História NEX-517)
**Seam:** `Game.Meta.Wallet.Wallet`, `Game.Meta.Rewards.RewardBundle`, `Game.Meta.Rewards.IRewardSink`
**Pronto quando:**
- [x] CT-1 e CT-2 passam 100% verde
- [x] Suíte unitária em `WalletTests.cs` e `RewardTests.cs` aprovada
- [x] Concessão idempotente verificada com reentrância
- [x] `tools/unity compile` e `tools/unity test-edit` executam sem erros
**Não fazer:** Não implementar trackers de objetivos nem salvamento em disco.
**Contexto:** História NEX-517 · Plano Passo 1

### `Marco 2: Objetivos e Rastreador de Sinais do Gameplay [NEX-517]` (`NEX-991`)
```dispatch
effort: medium
```
```track
level: tarefa
plan: docs/planos/2026-10-15-NEX-517-nucleo-de-meta-mapa-de-fases-e-save.md
marco: 2
steps: Passo 2 (Marco 2)
needs: objetivos-tracker
```
**Objetivo:** Implementar o subsistema de objetivos e missões (`ObjectiveTracker`, `ObjectiveDefinition`) com suporte a escopos `Run` e `Cumulative`, ingestão desacoplada de sinais de gameplay (`IEventBus`) e consolidação via `RunResult`.
**Cobre:** AC-3 (História NEX-517)
**Seam:** `Game.Meta.Objectives.ObjectiveTracker`
**Pronto quando:**
- [x] CT-3 passa 100% verde
- [x] Suíte unitária em `ObjectiveTrackerTests.cs` aprovada
- [x] Suporte a escopo `Run` e `Cumulative` verificado
- [x] `tools/unity compile` e `tools/unity test-edit` executam sem erros
**Não fazer:** Não integrar com a carteira ou UI de missões.
**Contexto:** História NEX-517 · Plano Passo 2

### `Marco 3: Trilhas de Progresso e Entitlements [NEX-517]` (`NEX-992`)
```dispatch
effort: medium
```
```track
level: tarefa
plan: docs/planos/2026-10-15-NEX-517-nucleo-de-meta-mapa-de-fases-e-save.md
marco: 3
steps: Passo 3 (Marco 3)
needs: trilhas-entitlements
```
**Objetivo:** Implementar o sistema de trilhas de progressão (`ProgressTrack`) com suporte a marcos, ramificação de recompensas Grátis / Premium, e o catálogo central de direitos (`Entitlements`) do jogador.
**Cobre:** AC-4 (História NEX-517)
**Seam:** `Game.Meta.Tracks.ProgressTrack`, `Game.Meta.Tracks.Entitlements`
**Pronto quando:**
- [ ] CT-4 passa 100% verde
- [ ] Suíte unitária em `ProgressTrackTests.cs` e `EntitlementsTests.cs` aprovada
- [ ] Bloqueio estrito de marcos premium sem entitlement verificado
- [ ] `tools/unity compile` e `tools/unity test-edit` executam sem erros
**Não fazer:** Não criar serialização persistente de estado.
**Contexto:** História NEX-517 · Plano Passo 3

### `Marco 4: Tempo com Guarda Monotônica e Save Versionado com Migrações [NEX-517]` (`NEX-993`)
```dispatch
effort: high
```
```track
level: tarefa
plan: docs/planos/2026-10-15-NEX-517-nucleo-de-meta-mapa-de-fases-e-save.md
marco: 4
steps: Passo 4 (Marco 4)
needs: tempo-save
```
**Objetivo:** Implementar o provedor de tempo UTC desacoplado (`MonotonicTimeProvider`) com guarda monotônica (10 min), o agregado de estado `MetaState` e o armazenamento persistente `SaveStore` com versionamento, migrações encadeadas (`ISaveMigration`) e hash de integridade (HMAC).
**Cobre:** AC-5 (História NEX-517)
**Seam:** `Game.Meta.Time.MonotonicTimeProvider`, `Game.Meta.Save.SaveStore`
**Pronto quando:**
- [ ] CT-5 passa 100% verde
- [ ] Suíte unitária em `TimeProviderTests.cs` e `SaveStoreTests.cs` aprovada
- [ ] Detecção de fraude de relógio (> 10 min) e rejeição de save adulterado verificadas
- [ ] `tools/unity compile` e `tools/unity test-edit` executam sem erros
**Não fazer:** Não usar `PlayerPrefs` ou referências à `UnityEngine`.
**Contexto:** História NEX-517 · Plano Passo 4

### `Marco 5: Fronteira Gameplay-Meta e Mapa de Fases [NEX-517]` (`NEX-994`)
```dispatch
effort: high
```
```track
level: tarefa
plan: docs/planos/2026-10-15-NEX-517-nucleo-de-meta-mapa-de-fases-e-save.md
marco: 5
steps: Passo 5 (Marco 5)
needs: fronteira-mapa
```
**Objetivo:** Implementar os componentes de fronteira entre o gameplay e a meta (`RunConfigBuilder` e `RewardCalculator`), integrando multiplicadores de pista, mutações e recibos auditáveis, além do serviço de progressão de fases (`StageMapService`) com marcação de Boss gates.
**Cobre:** AC-6, AC-7 (História NEX-517)
**Seam:** `Game.Meta.Run.RunConfigBuilder`, `Game.Meta.Run.RewardCalculator`, `Game.Meta.Stages.StageMapService`
**Pronto quando:**
- [ ] CT-6 e CT-7 passam 100% verde
- [ ] Toda a suíte de cenários `MetaScenariosTests.cs` passa 100% verde (sem regressões)
- [ ] `tools/unity compile` e `tools/unity test-edit` executam sem erros
**Não fazer:** Não referenciar `Game.Meta` dentro de `Game.Gameplay`.
**Contexto:** História NEX-517 · Plano Passo 5
