---
cobre:
  - "Assets/_Game/Scripts/Gameplay/LevelDirector.cs"
  - "Assets/_Game/Scripts/Gameplay/TrackScroller.cs"
  - "Assets/_Game/Scripts/Gameplay/MultiplierLane.cs"
  - "Assets/_Game/Scripts/Gameplay/Combat/CombatDirector.cs"
  - "Assets/_Game/Scripts/Gameplay/Combat/CollisionLayers.cs"
  - "Assets/_Game/Scripts/Data/LevelDefinition.cs"
  - "Assets/_Game/Scripts/Data/SegmentDefinition.cs"
  - "Assets/_Game/Scripts/Data/LevelValidator.cs"
revisado: 2026-11-13
---
# Laço da fase

A fase avança por distância. A `LevelDefinition` lista segmentos (aquecimento, portão, horda, jaula, barricada, boss, pista de multiplicador) e o `LevelDirector` dispara cada um quando a pista chega nele, conta escapes e estrelas e encerra a fase. Existem hoje dois diretores, e o que está em cena não é esse.

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Distância | `Assets/_Game/Scripts/Gameplay/TrackScroller.cs:Step` | única origem de progressão; para quando `IsPaused` |
| Diretor de combate, o que roda | `Assets/_Game/Scripts/Gameplay/Combat/CombatDirector.cs:Tick` | vitória por distância fixa, derrota por contato; não conhece segmentos |
| Diretor de fase | `Assets/_Game/Scripts/Gameplay/LevelDirector.cs:Tick` | recebe a distância absoluta e dispara segmentos |
| Agenda de segmentos | `Assets/_Game/Scripts/Gameplay/LevelDirector.cs:CheckSpawns` | horda com antecedência; barricada e boss na distância de início |
| Escape | `Assets/_Game/Scripts/Gameplay/LevelDirector.cs:CanEnemyEscape` | zumbi 5 unidades atrás do General, exceto boss em luta |
| Pista de multiplicador | `Assets/_Game/Scripts/Gameplay/MultiplierLane.cs:TryAdvance` | gasta soldados para subir de marco |
| Validação de fase | `Assets/_Game/Scripts/Data/LevelValidator.cs:Validate` | sobreposição de segmentos, lanes de horda, specs de jaula, barricada e boss |

## Invariantes

- Cada segmento dispara uma vez por fase.
- Estrelas: 3 com tropa viva; no máximo 2 se alguma barricada tentada caiu; 0 na derrota — `Assets/_Game/Scripts/Gameplay/LevelDirector.cs:EndRun`.
- Boss em luta segura o fim da fase mesmo depois da distância total.
- Camadas de colisão são as de `Assets/_Game/Scripts/Gameplay/Combat/CollisionLayers.cs:CollisionLayers`; cada tipo colide na sua.

## Mexer junto

- Tipo de segmento novo → `SegmentType`, `CheckSpawns`, `LevelValidator`, importador de fases e o simulador.
- Regra de fim de fase → os dois diretores, enquanto existirem os dois.

## Divergências

- **O diretor de fase não roda.** `LevelDirector` não está em nenhuma cena gerada e `Tick(distance)` não tem chamador de produção. Segmentos, escape, estrelas, revive, boss, clima e zumbis bônus de sobreviventes perdidos só executam em teste e no simulador.
- A fase em jogo não lê `LevelDefinition`: distância de vitória e ondas são fixas na cena — ver [harness e cenas](../arquitetura/harness-e-cenas.md).
- `CheckSpawns` só trata horda, barricada e boss. Portão, jaula, pista de multiplicador e aquecimento existem no enum e na validação e não têm gatilho; jaula depende de `TrackCage` chamado por fora, que ninguém chama.
- O `LevelDirector` guarda uma cópia da contagem da tropa (`_currentSquad`) atualizada por evento; o revive escreve nessa cópia e publica o evento sem alterar o `SquadController`.
- `MultiplierLane` não está em cena e nenhum código de produção chama `TryAdvance`: o multiplicador da recompensa é sempre 1.
- Fim de fase decidido em dois lugares com regras diferentes — ver [início e fim de fase](inicio-e-fim-de-fase.md).
