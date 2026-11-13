---
cobre:
  - "Assets/_Game/Scripts/Meta/Ads/**"
  - "Assets/_Game/Scripts/Core/Ads/**"
  - "Assets/_Game/Scripts/Infrastructure/Ads/**"
  - "Assets/_Game/Scripts/Core/Analytics/**"
revisado: 2026-11-13
---
# Anúncios

Intersticial entre fases e recompensado por escolha do jogador. A `AdPolicy` decide se pode mostrar; o `IAdService` mostra. Toda tela que oferece anúncio pergunta à política antes e registra depois.

## Caminhos

| Ponto de anúncio | Âncora | O que difere |
|---|---|---|
| Intersticial ao voltar ao Hub | `Assets/_Game/Scripts/Composition/GreyboxRunComposer.cs:TryShowInterLevelInterstitial` | disparado pela mudança de estado para `Hub` |
| Reforço de tropa antes da fase | `Assets/_Game/Scripts/Presentation/UI/HubView.cs:RequestPreRunBoostWithAd` | recompensado |
| Acelerar obra | `Assets/_Game/Scripts/Presentation/UI/Barracks/BarracksBuildingCardWidget.cs:BarracksBuildingCardWidget` | recompensado; limite próprio do Quartel além do da política |
| Dobrar moedas offline | `Assets/_Game/Scripts/Presentation/UI/Barracks/CanteenWidget.cs:CanteenWidget` | recompensado |
| Dobrar recompensa da fase | `Assets/_Game/Scripts/Presentation/UI/ResultRewardPresenter.cs:DoubleReward` | recompensado |
| Reviver | `Assets/_Game/Scripts/Presentation/UI/RevivePromptWidget.cs:HandleWatchAdClicked` | recompensado |
| Carga do poder | `Assets/_Game/Scripts/Gameplay/Abilities/GeneralAbilityController.cs:TryRequestBonusChargeWithAd` | recompensado |

## Invariantes

- Quem tem o direito `no_ads` ou `remove_ads` não vê intersticial — `Assets/_Game/Scripts/Meta/Ads/AdPolicy.cs:CanShowInterstitial`.
- Intersticial só a partir da fase 5, a cada 3 fases, com 90 s entre anúncios e 120 s depois de recompensado ou compra — padrões de `Assets/_Game/Scripts/Meta/Ads/AdPolicyDefinition.cs:AdPolicyDefinition`.
- Recompensado tem teto de 20 por dia — `Assets/_Game/Scripts/Meta/Ads/AdPolicy.cs:CanShowRewarded`.
- Os contadores diários zeram na virada do dia pelo `ITimeProvider`.

## Mexer junto

- Ponto de anúncio novo → `AdPlacement`, a consulta à política, o registro `RecordAdShown` e o evento de analytics.
- Limite novo → `AdPolicyDefinition` e `Content/Source/ad_policy.json`.

## Divergências

- **Não há SDK de anúncio.** `Assets/_Game/Scripts/Infrastructure/Ads/LevelPlayAdService.cs:LevelPlayAdService` sempre delega ao `MockAdService`, que responde sucesso: todo anúncio é concedido na hora, sem vídeo. Analytics é `MockAnalyticsService`.
- `Content/Source/ad_policy.json` não é lido por ninguém; valem os padrões em C#.
- Ninguém concede os direitos `no_ads` e `remove_ads`: `Entitlements` nasce vazio no composer e não há compra.
- Três dos sete pontos não são alcançáveis: dobrar recompensa (a tela de resultado nunca abre), reviver (o widget não está em cena) e carga do poder (sem chamador).
- O reforço de tropa mostra o anúncio e não altera a tropa — ver [início e fim de fase](inicio-e-fim-de-fase.md).
- O intersticial conta a fase mais alta por `GetPlayerHighestStage`, que funde o mapa de fases (sempre na fase 1, porque nada registra resultado) com `PlayerHighestStage`, que ninguém escreve: o mínimo de fase 5 nunca é atingido.
- Os contadores da política vivem em memória e zeram a cada abertura do app.
