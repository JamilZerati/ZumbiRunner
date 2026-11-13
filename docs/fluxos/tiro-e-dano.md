---
cobre:
  - "Assets/_Game/Scripts/Gameplay/Combat/WeaponController.cs"
  - "Assets/_Game/Scripts/Gameplay/Combat/Projectile.cs"
  - "Assets/_Game/Scripts/Data/WeaponCatalog.cs"
  - "Assets/_Game/Scripts/Data/WeaponDefinition.cs"
  - "Assets/_Game/Scripts/Presentation/ProjectileView.cs"
revisado: 2026-11-13
---
# Tiro e dano

A tropa atira sozinha. Para não instanciar um projétil por soldado, ela é agrupada em pelotões: cada pelotão é um emissor, e o dano do projétil dele é o dano da arma vezes os soldados do pelotão. O projétil voa em linha reta e fere o primeiro zumbi que tocar.

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Cadência | `Assets/_Game/Scripts/Gameplay/Combat/WeaponController.cs:Tick` | acumula tempo e dispara salvas pelo intervalo da arma |
| Salva | `Assets/_Game/Scripts/Gameplay/Combat/WeaponController.cs:FireVolley` | um disparo por emissor; cada projétil leva o dano do pelotão inteiro |
| Pelotões | `Assets/_Game/Scripts/Core/Combat/PlatoonSolver.cs:CalculatePlatoons` | um emissor a cada 5 soldados, até 40 emissores |
| Acerto com status | `Assets/_Game/Scripts/Gameplay/Combat/Projectile.cs:HandleTrigger` | alvo com `IStatusReceiver` recebe o golpe e os status do tiro |
| Acerto simples | `Assets/_Game/Scripts/Gameplay/Combat/Projectile.cs:HandleTrigger` | alvo só com `IDamageable` recebe `TakeDamage`, sem status |
| Troca de arma | `Assets/_Game/Scripts/Gameplay/Combat/WeaponController.cs:TryEquip` | por perk; busca a definição no catálogo e publica `WeaponEquippedEvent` |

## Invariantes

- Um projétil acerta uma vez e volta ao pool; sem acerto, volta ao esgotar o alcance.
- Os atributos da salva são um retrato resolvido de `StatCollection` no momento do disparo — ver [stats e modificadores](../regras/stats-e-modificadores.md).
- O conjunto de status do tiro é copiado para o projétil na hora do disparo: perk pego depois não muda projétil em voo.
- Com a tropa zerada a arma ainda tem um emissor de reserva de um soldado.

## Mexer junto

- Atributo de arma novo → `WeaponDefinition`, importador de armas, `WeaponProfile` e `WeaponStats.Resolve`.
- Forma nova de acertar (perfurante, em área) → `HandleTrigger` e a regra de reciclagem do projétil.

## Divergências

- Projéteis por disparo multiplica o dano total do pelotão, porque cada projétil sai com o dano cheio (`Damage × SoldierCount`) em vez de dividi-lo.
- O projétil anda sempre para a frente e ignora a lane: zumbi de outra lane só é atingido se o colisor alcançar.
- Dois caminhos de acerto com consequências diferentes. O do `IStatusReceiver` publica `EnemyKilledEvent` e recicla o zumbi; o do `IDamageable` puro (`HealthComponent`) publica `DamageTakenEvent` e `EntityDiedEvent` e não recicla — ver [hordas e inimigos](hordas-e-inimigos.md).
- O bônus de dano do Quartel não chega à arma em jogo — ver [stats e modificadores](../regras/stats-e-modificadores.md).
- A arma inicial e o catálogo são escolhidos pelo `SceneBuilder` por id; `RunConfig` não escolhe arma.
