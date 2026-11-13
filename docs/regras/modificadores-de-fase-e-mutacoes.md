---
cobre:
  - "Assets/_Game/Scripts/Core/Gates/StageModifierController.cs"
  - "Assets/_Game/Scripts/Core/Gates/IStageModifier.cs"
  - "Assets/_Game/Scripts/Core/Gates/StageModifierSpec.cs"
  - "Assets/_Game/Scripts/Core/Modifiers/**"
  - "Assets/_Game/Scripts/Core/Weather/**"
  - "Assets/_Game/Scripts/Data/Mutations/**"
  - "Assets/_Game/Scripts/Data/Weather/**"
  - "Assets/_Game/Scripts/Presentation/WeatherVisualController.cs"
  - "Assets/_Game/Scripts/Presentation/UI/MutationSelectionWidget.cs"
  - "Assets/_Game/Scripts/Presentation/UI/MutationToggleItem.cs"
revisado: 2026-11-13
---
# Modificadores de fase e mutações

Um `IStageModifier` muda as regras da fase por um trecho de distância: multiplica vida, velocidade e dano de contato dos zumbis, o alcance da tropa, força a tropa inicial ou troca a operação dos portões. Mutação, portão amaldiçoado e clima são três origens do mesmo mecanismo, somadas no `StageModifierController`.

## Caminhos

| Origem | Âncora | O que difere |
|---|---|---|
| Mutação escolhida no Hub | `Assets/_Game/Scripts/Composition/GreyboxRunComposer.cs:OnRunStarted` | ativa cada modificador da mutação da distância 0 até o fim |
| Portão amaldiçoado | `Assets/_Game/Scripts/Core/Gates/Variants/CursedGateVariant.cs:ActivateModifier` | dura a distância configurada na variante |
| Clima | `Assets/_Game/Scripts/Core/Weather/WeatherSystem.cs:SetWeather` | cria um `WeatherStageModifier` por tipo de clima |
| Agregação | `Assets/_Game/Scripts/Core/Gates/StageModifierController.cs:RecalculateMultipliers` | multiplicadores multiplicam a partir de 1; tropa inicial fica com o último; trocas de operação são OU lógico |

## Invariantes

- `Activate` ignora modificador nulo ou duração menor ou igual a zero, sem erro.
- Modificador expira por distância em `TickDistance`.
- Bônus de recompensa da mutação é somado, não multiplicado entre si — `Assets/_Game/Scripts/Meta/Run/RewardCalculator.cs:Calculate`.

## Mexer junto

- Propriedade nova no controlador → um leitor de produção no mesmo PR, e a tabela de leitores abaixo.
- Mutação nova → `Content/Source/Mutations/mutations.json`, importador e `Assets/_Game/Scripts/Data/Mutations/MutationCatalog.cs:GetRewardBonuses`.

## Divergências

Quem lê cada valor corrente em jogo:

- `CurrentSquadRangeMultiplier` → `CombatDirector`, a cada frame. Funciona.
- `CurrentInitialSquadOverride` → o composer, ao iniciar a fase. Funciona.
- `RemapGateMultiplication` e `RemapGateDivision` → `GatePair`, que procura o controlador no `LevelDirector` e não recebe o do composer: em jogo a troca de operação não acontece — ver [portões e perks](../fluxos/portoes-e-perks.md).
- `CurrentEnemyHealthMultiplier` e `CurrentEnemySpeedMultiplier` → só `HordeSpawner.SpawnWave`, que em jogo ninguém chama: as hordas já estão na cena. Mutação que deixa zumbi mais forte ou mais rápido não muda nada.
- `CurrentEnemyContactDamageMultiplier` → nenhum leitor de produção.

Outros:

- Ninguém chama `TickDistance` com a distância da pista fora do `LevelDirector`, que não está em cena: modificador de portão amaldiçoado não expira.
- O controlador é criado em quatro pontos (composer duas vezes, `CombatDirector` e `LevelDirector`); o composer entrega o mesmo a diretor e spawner, e o `GatePair` fica de fora.
- O clima nunca muda em jogo: `SetWeather` só é chamado pelo `LevelDirector`. `WeatherVisualController` não está em nenhuma cena.
- `appliedStatus` da definição de clima não tem leitor — ver [status e sinergias](status-e-sinergias.md).
- Os multiplicadores de cada clima são literais em `Assets/_Game/Scripts/Core/Weather/WeatherStageModifier.cs:WeatherStageModifier`, ao lado dos valores do JSON em `Content/Source/Weather`.
