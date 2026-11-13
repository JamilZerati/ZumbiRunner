---
cobre:
  - "Assets/_Game/Scripts/Composition/**"
  - "Assets/_Game/Scripts/Core/State/**"
revisado: 2026-11-13
---
# Composição e boot

Quem cria os serviços, o barramento e a máquina de estados quando a cena carrega. O jogo que vai para o aparelho é a cena `M6_Greybox`, e nela tudo é montado à mão por um `MonoBehaviour`; o contêiner VContainer existe em paralelo e não participa.

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Composição manual, a que roda | `Assets/_Game/Scripts/Composition/GreyboxRunComposer.cs:Compose` | chamada no `Awake` com ordem -1000; cria `EventBus`, `GameStateMachine` em `Hub` e os serviços de meta com `new` |
| Contêiner VContainer | `Assets/_Game/Scripts/Composition/GameLifetimeScope.cs:Configure` | registra os mesmos serviços como singletons; o componente só existe na cena `Bootstrap`, fora do build |
| Escopo de corrida | `Assets/_Game/Scripts/Composition/RunLifetimeScope.cs:Configure` | vazio e em nenhuma cena |
| Fiação em tempo de edição | `Assets/_Game/Scripts/Editor/SceneBuilder.cs:BuildCombatGreyboxScene` | cria outro `EventBus` e outra máquina (Boot→Run) que não são serializados; o composer os substitui ao carregar |
| Fallback do diretor de combate | `Assets/_Game/Scripts/Gameplay/Combat/CombatDirector.cs:Start` | sem máquina injetada, cria uma própria já em `Run` |

## Invariantes

- Transições de estado válidas ficam em `Assets/_Game/Scripts/Core/State/GameStateMachine.cs:IsValidTransition`; transição para o mesmo estado é recusada.
- A cena do build Android é `Assets/_Game/Scripts/Editor/Cli.cs:DefaultAndroidScenePath`.
- O composer entrega o barramento a todo `EnemyController` já existente na cena, inclusive os inativos do pool.

## Mexer junto

- Serviço novo → `Compose` e, enquanto os dois existirem, `GameLifetimeScope.Configure`.
- Estado novo → `IsValidTransition`, `Assets/_Game/Scripts/Presentation/UI/UIManager.cs:OnGameStateChanged` e `Assets/_Game/Scripts/Composition/GreyboxRunComposer.cs:OnGameStateChanged`.
- Componente novo na cena → o `SceneBuilder` precisa ligá-lo ao composer; ver [harness e cenas](harness-e-cenas.md).

## Divergências

- Duas raízes de composição. `GameLifetimeScope` registra `Wallet`, `StageMapService`, `AdPolicy`, `BarracksService` e os demais, e nada no jogo resolve por ele.
- Nada é carregado nem salvo: `Compose` não instancia `SaveStore`. A carteira nasce com 100 moedas a cada carga (`"initial"`), e mapa de fases, progressão, Quartel e jornada de estreia nascem vazios — ver [save](../regras/save-e-integridade.md).
- O analytics de jogo é `MockAnalyticsService`, criado dentro de `Compose`.
- Sem a referência serializada, o catálogo de mutações só é carregado por `AssetDatabase` sob `#if UNITY_EDITOR`: no aparelho as mutações escolhidas não aplicam nada.
- `GameState.Boot` nunca é estado em jogo, e nenhum código transiciona para `GameState.Result` — ver [início e fim de fase](../fluxos/inicio-e-fim-de-fase.md).
- `ProjectSettings/EditorBuildSettings.asset` lista só `Assets/Scenes/SampleScene.unity`: o build pelo menu do Editor não leva o jogo; só o comando do harness leva.
- O composer acha dependências por `FindFirstObjectByType` em oito pontos quando a referência falta, e cada serviço é opcional (`if (x != null)`): componente ausente da cena desliga a funcionalidade sem erro.
- Três instâncias de relógio — ver [tempo](../regras/tempo.md).
