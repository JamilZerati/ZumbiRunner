---
cobre:
  - "Assets/_Game/Scripts/Core/Status/**"
  - "Assets/_Game/Scripts/Gameplay/Enemies/StatusEffectDirector.cs"
  - "Assets/_Game/Scripts/Data/StatusCatalog.cs"
  - "Assets/_Game/Scripts/Data/EffectInteractionTable.cs"
  - "Assets/_Game/Scripts/Presentation/EnemyStatusTint.cs"
revisado: 2026-11-13
---
# Status e sinergias

Efeito de status é uma instância de `IStatusEffect` (Queima, Veneno, Choque, Congelamento, Congelado, Lentidão, Molhado) aplicada a um hospedeiro. Sinergia é a linha da `EffectInteractionTable` que troca um status presente por dano multiplicado quando chega o golpe certo (Estilhaço, Eletrocussão).

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Golpe que carrega status | `Assets/_Game/Scripts/Core/Status/StatusEffectController.cs:ResolveHit` | avalia sinergia, aplica dano e só então os status do golpe |
| Tique | `Assets/_Game/Scripts/Core/Status/StatusEffectController.cs:Tick` | cada efeito persistente recebe `OnTick`; dano sai por `DealDamage` |
| Vizinhança | `Assets/_Game/Scripts/Gameplay/Enemies/StatusEffectDirector.cs:FindNearby` | usada por Choque (corrente) e Veneno (explosão na morte) |
| Origem no tiro | `Assets/_Game/Scripts/Core/Perks/Effects/ApplyStatusOnHitEffect.cs:Apply` | o perk grava no `OnHitStatusSet` da arma; o projétil leva a cópia |
| Catálogo do asset | `Assets/_Game/Scripts/Data/StatusCatalog.cs:SetEffects` | preenchido pelo importador a partir de `Content/Source/Statuses` |
| Catálogo reserva | `Assets/_Game/Scripts/Gameplay/Enemies/EnemyController.cs:DefaultFallbackCatalog` | números em C#, usado quando o inimigo não tem `StatusEffectDirector` |

## Invariantes

- A sinergia é avaliada antes de aplicar os status do próprio golpe, e a primeira linha da tabela que casar encerra a busca: o status que o projétil carrega não completa uma sinergia no mesmo golpe.
- Congelamento soma cargas até o limiar e então vira Congelado; aplicar Congelamento em alvo Congelado não faz nada.
- Choque não é persistente: age no `OnApply` e não fica em `ActiveStatuses`.
- Dano por tique não reavalia sinergia; corrente do Choque e explosão do Veneno passam pelo `ReceiveHit` do vizinho e reavaliam.

## Mexer junto

- Status novo → `StatusKind`, a classe em `Effects/`, `StatusContentImporter`, `DefaultFallbackCatalog` e `Assets/_Game/Scripts/Presentation/EnemyStatusTint.cs:EnemyStatusTint`.
- Sinergia nova → `Content/Source/Interactions` e a ordem das linhas, que decide o desempate.

## Divergências

- **Molhado não tem fonte em jogo.** Nenhum perk, arma ou aliado de `Content/Source` aplica Molhado; o clima declara `appliedStatus: wet` e ninguém lê o campo; o catálogo reserva não tem `WetStatus`. A Eletrocussão só acontece em teste.
- Três fontes para os números de status: o JSON, o asset importado e o catálogo reserva em C#. O `StatusEffectDirector` só está na cena M6; em M4 e M5 vale o reserva.
- Um Molhado consumido conta duas vezes: `ResolveHit` multiplica o dano do golpe pela sinergia e marca `WasWetConsumedThisHit`, que `Assets/_Game/Scripts/Core/Status/Effects/ShockStatus.cs:OnApply` lê para dobrar também a corrente.
- Lentidão de status e lentidão de modificador de fase não se compõem no mesmo ponto — ver [modificadores de fase](modificadores-de-fase-e-mutacoes.md).
- Status aplicado por aliado (`HeroUnit.FireAbility`) não roda em jogo — ver [aliados](../fluxos/aliados.md).
