---
cobre:
  - "Assets/_Game/Scripts/Core/Abilities/**"
  - "Assets/_Game/Scripts/Gameplay/Abilities/**"
  - "Assets/_Game/Scripts/Data/Abilities/**"
  - "Assets/_Game/Scripts/Presentation/AbilityAimIndicator.cs"
  - "Assets/_Game/Scripts/Presentation/GeneralAbilityAudio.cs"
  - "Assets/_Game/Scripts/Presentation/GeneralAbilityExplosionVfx.cs"
  - "Assets/_Game/Scripts/Presentation/GeneralAbilityHud.cs"
  - "Assets/_Game/Scripts/Presentation/IAbilitySettings.cs"
  - "Assets/_Game/Scripts/Presentation/PlayerPrefsAbilitySettings.cs"
revisado: 2026-11-13
---
# Poder do General

Habilidade ativa carregada por abates. Hoje há uma, a Granada: dano em área à frente do General. O disparo é automático ou manual, conforme a opção do jogador. É o fluxo mais completo do jogo: da carga ao som, tudo está ligado na cena.

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Carga | `Assets/_Game/Scripts/Gameplay/Abilities/GeneralAbilityController.cs:OnEnemyKilled` | cada abate conta; abate causado pelo próprio poder não conta |
| Disparo automático | `Assets/_Game/Scripts/Core/Abilities/Policies/AutoHeroAbilityTriggerPolicy.cs:ShouldTrigger` | dispara ao ter carga e alvo |
| Disparo manual | `Assets/_Game/Scripts/Presentation/GeneralAbilityHud.cs:GeneralAbilityHud` | botão do HUD, com mira por arraste |
| Execução | `Assets/_Game/Scripts/Gameplay/Abilities/GeneralAbilityController.cs:TriggerAbility` | consome a carga, executa o efeito e publica `AbilityUsedEvent` |
| Dano em área | `Assets/_Game/Scripts/Gameplay/Abilities/GeneralAbilityController.cs:ApplyAreaDamage` | esfera de sobreposição na camada de inimigos; cada zumbi recebe um golpe |
| Carga por anúncio | `Assets/_Game/Scripts/Gameplay/Abilities/GeneralAbilityController.cs:TryRequestBonusChargeWithAd` | concede uma carga depois do anúncio recompensado |

## Invariantes

- Sem carga não dispara; a carga é consumida antes do efeito.
- Um zumbi recebe o dano da explosão uma vez, mesmo com vários colisores.
- Som, explosão e HUD reagem a `AbilityUsedEvent` e `AbilityChargeProgressEvent`; o controlador não conhece a apresentação.

## Mexer junto

- Poder novo → `IAbilityEffect`, `GeneralAbilityDefinition`, `AbilityImporter` e a validação de habilidades.
- Mudança no modo de disparo → as duas políticas, `IAbilitySettings` e a tela de opções.

## Divergências

- Raio e dano da Granada são constantes (`DefaultRadius` 4 e `DefaultDamage` 150) em `Assets/_Game/Scripts/Core/Abilities/Effects/GrenadeAbilityEffect.cs:GrenadeAbilityEffect`; a definição importada de `Content/Source/Abilities` só fornece os abates por carga.
- `TryRequestBonusChargeWithAd` não tem chamador: não há botão para a carga por anúncio.
- `AbilityCatalog` não tem leitor em produção; a definição chega ao controlador por referência serializada pelo `SceneBuilder`.
- O dano em área do poder e a explosão do zumbi explosivo são duas implementações de "dano em raio": uma por física e `ReceiveHit`, outra por escrita direta de vida no contexto — ver [hordas e inimigos](hordas-e-inimigos.md).
- `Initialize` aceita um `RunConfig` que ninguém passa.
