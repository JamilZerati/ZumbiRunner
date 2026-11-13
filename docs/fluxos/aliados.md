---
cobre:
  - "Assets/_Game/Scripts/Core/Allies/**"
  - "Assets/_Game/Scripts/Gameplay/Allies/**"
  - "Assets/_Game/Scripts/Data/Allies/**"
  - "Assets/_Game/Scripts/Presentation/AllyHudView.cs"
revisado: 2026-11-13
---
# Aliados

Heróis e pets que um perk de portão recruta para lutar ao lado da tropa. O herói tem vida própria e uma habilidade com recarga que aplica dano e status.

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Recrutamento por perk | `Assets/_Game/Scripts/Core/Perks/Effects/RecruitAllyPerkEffect.cs:Apply` | pede o aliado ao elenco pelo id |
| Elenco | `Assets/_Game/Scripts/Core/Allies/AllyRoster.cs:TryRecruit` | cria a instância e publica `AllyRecruitedEvent`; recrutar de novo só restaura a vida |
| Recarga | `Assets/_Game/Scripts/Core/Allies/AbilityRunner.cs:TryTrigger` | exige recarga completa e política de disparo |
| Habilidade do herói | `Assets/_Game/Scripts/Gameplay/Allies/HeroUnit.cs:FireAbility` | golpe com dano e duas cargas de status; publica `AllyAbilityFiredEvent` |
| Botão do herói | `Assets/_Game/Scripts/Presentation/AllyHudView.cs:AllyHudView` | disparo manual pelo HUD |

## Invariantes

- Um id de aliado existe uma vez no elenco.
- Herói com vida zero publica `AllyKnockedOutEvent` e deixa de recarregar a habilidade.
- A política de disparo é a mesma do poder do General (automática ou manual).

## Mexer junto

- Aliado novo → `Content/Source/Allies`, `AllyDefinition`, `AllyImporter` e a criação da instância no elenco.
- Mudança na habilidade → `HeroUnit`, `AbilityRunner` e o HUD.

## Divergências

- **Aliado recrutado não aparece.** `HeroUnit`, `PetUnit` e `AllyHudView` não estão em nenhuma cena e nenhum código de produção os instancia. O perk acrescenta a instância ao elenco, publica um evento sem assinante, e nada entra em campo.
- Os arquétipos são literais em `Assets/_Game/Scripts/Core/Allies/AllyRoster.cs:CreateDefaultInstance` (Piromante 150 de vida e recarga 12 s; Criomante 120 e 14 s; Cão de Guarda 100). `AllyCatalog` e os JSON de `Content/Source/Allies` não têm leitor em produção.
- Id desconhecido não é recusado: vira um herói genérico de 100 de vida.
- O herói recebe dano por um `TakeDamage(int)` próprio, fora de `IDamageable`.
- O elenco é guardado pelo `SquadController` — ver [lanes, input e tropa](lanes-input-e-tropa.md).
