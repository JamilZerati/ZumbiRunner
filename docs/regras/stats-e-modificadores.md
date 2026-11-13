---
cobre:
  - "Assets/_Game/Scripts/Core/Stats/**"
revisado: 2026-11-13
---
# Stats e modificadores

Atributo de arma é base mais modificadores com fonte. Perk, Quartel e troca de arma nunca escrevem o valor final: acrescentam ou retiram modificadores, e o valor é resolvido na leitura. Hoje `StatId` só tem atributos de arma (dano, cadência, velocidade do projétil, alcance, projéteis por disparo).

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Leitura resolvida | `Assets/_Game/Scripts/Core/Stats/StatCollection.cs:GetValue` | `(base + Σflat) × (1 + ΣpercentAdd) × Π(1 + percentMultiply)` |
| Retrato da arma | `Assets/_Game/Scripts/Core/Stats/WeaponStats.cs:Resolve` | aplica pisos e teto sobre o valor resolvido |
| Perk de stat | `Assets/_Game/Scripts/Core/Perks/Effects/ModifyStatEffect.cs:Apply` | acrescenta modificador com a fonte do perk |
| Bônus do Quartel | `Assets/_Game/Scripts/Gameplay/Combat/WeaponController.cs:ApplyBarracksDamageBonus` | retira os modificadores da fonte do Quartel e recoloca um |
| Alcance por modificador de fase | `Assets/_Game/Scripts/Gameplay/Combat/CombatDirector.cs:UpdateWeaponRangeMultiplier` | não passa pela coleção: escreve `RangeMultiplier` na arma a cada frame |

## Invariantes

- Modificador sem fonte é recusado — `Assets/_Game/Scripts/Core/Stats/StatCollection.cs:AddModifier` e o construtor de `StatModifier`.
- `PercentMultiply` com valor menor ou igual a -1 lança exceção, porque zeraria ou inverteria o atributo.
- `Resolve` garante dano mínimo 1 (arredondado para longe do zero), projéteis entre 1 e `MaxProjectilesPerShot`, cadência não negativa e alcance e velocidade mínimos de 1.
- Remoção é por fonte: `RemoveAllFromSource` desfaz tudo o que um perk ou sistema aplicou.

## Mexer junto

- Atributo novo → `StatId`, `WeaponStats.Resolve`, `Assets/_Game/Scripts/Core/Stats/WeaponProfile.cs:WeaponProfile` e o importador de armas.
- Fonte nova de bônus → remover pela mesma fonte no fim da fase ou da troca de arma.

## Divergências

- O alcance tem dois multiplicadores: o da coleção e o `RangeMultiplier` da arma, multiplicado depois em `Assets/_Game/Scripts/Gameplay/Combat/WeaponController.cs:FireVolley`. Mutação e clima que mexem em alcance não aparecem em `GetModifiers` nem no overlay de debug.
- Atributos de inimigo e de tropa não usam a coleção: vida, velocidade e dano de contato são propriedades simples do `EnemyController`, multiplicadas por conta própria — ver [modificadores de fase](modificadores-de-fase-e-mutacoes.md).
- O bônus de dano do Quartel só entra pelo `RunConfig` passado a `Assets/_Game/Scripts/Gameplay/Combat/WeaponController.cs:Initialize`, e o único chamador é o `SceneBuilder`, sem `RunConfig`: em jogo o multiplicador é sempre 1 — ver [Quartel](../fluxos/quartel.md).
