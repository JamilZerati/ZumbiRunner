---
cobre:
  - "Assets/_Game/Scripts/Gameplay/Combat/MeleeEngagementManager.cs"
  - "Assets/_Game/Scripts/Gameplay/HealthComponent.cs"
  - "Assets/_Game/Scripts/Core/Combat/**"
  - "Assets/_Game/Scripts/Core/Damage/**"
revisado: 2026-11-13
---
# Engajamento e perda de tropa

O que acontece quando um zumbi alcança a tropa. O desenho do jogo é o Engajamento: o zumbi gruda na vanguarda e causa dano contínuo, soldados caem quando o buffer de vida esgota, e o General só é ferido com a tropa zerada. O que a cena executa é uma regra mais simples, de custo por contato.

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Custo por contato, o que roda | `Assets/_Game/Scripts/Gameplay/Combat/CombatDirector.cs:ResolveEnemyContact` | tira `ContactCost` soldados de uma vez e recicla o zumbi; com a tropa zerada, derrota imediata |
| Engajamento | `Assets/_Game/Scripts/Gameplay/Combat/MeleeEngagementManager.cs:Engage` | o zumbi fica preso à tropa; publica `EnemyEngagedEvent` |
| Dano contínuo | `Assets/_Game/Scripts/Gameplay/Combat/MeleeEngagementManager.cs:Tick` | soma o dano por segundo dos engajados e aplica no buffer |
| Buffer de vida | `Assets/_Game/Scripts/Core/Combat/SquadHealthBuffer.cs:ApplyDamage` | cada estouro derruba um soldado; o que sobra do último vaza para a vida do General |
| Vida de entidade | `Assets/_Game/Scripts/Gameplay/HealthComponent.cs:TakeDamage` | publica `DamageTakenEvent` e, em zero, `EntityDiedEvent` |

## Invariantes

- O General só perde vida quando não há mais soldado para absorver.
- Zumbi Congelado não causa dano de engajamento.
- Dano entra por `DamageInfo` com valor, tipo e fonte; quem recebe implementa `IDamageable`.
- O contato com um zumbi é resolvido uma vez: zumbi já engajado ou fora do pool é ignorado.

## Mexer junto

- Mudança na regra de perda de soldado → os dois primeiros caminhos e o cálculo de horda do simulador.
- Tipo de dano novo → `DamageType` e a tabela de sinergias ([status](../regras/status-e-sinergias.md)).

## Divergências

- **O Engajamento não roda.** `MeleeEngagementManager` não está em nenhuma cena, então `CombatDirector` cai sempre no custo por contato. Buffer de vida do soldado, vida do General, dano contínuo e os eventos `EnemyEngagedEvent`, `EnemyDisengagedEvent` e `SoldierDamagedEvent` só existem em teste. O `CONTEXT.md` descreve o Engajamento como a regra do jogo.
- Em jogo o General não tem vida: a derrota é o primeiro contato com a tropa zerada.
- Vida fora do `HealthComponent`: barricada, jaula e portão guardam pontos de vida no próprio estado de runtime (`HoldoutRuntimeState`, `CageRuntimeState`, `GateRuntimeState`) e herói aliado tem `TakeDamage` próprio com assinatura diferente.
- Os comportamentos de inimigo em dados tiram soldados e escrevem vida direto no contexto, sem `IDamageable` e sem buffer — ver [hordas e inimigos](hordas-e-inimigos.md).
- `PlatoonSolver` e `PlatoonEmitter` moram nesta pasta e pertencem ao tiro — ver [tiro e dano](tiro-e-dano.md).
