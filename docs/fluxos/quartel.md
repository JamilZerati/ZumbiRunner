---
cobre:
  - "Assets/_Game/Scripts/Meta/Barracks/**"
  - "Assets/_Game/Scripts/Presentation/UI/Barracks/**"
revisado: 2026-11-13
---
# Quartel

Meta idle e única fonte de poder permanente. Cinco construções com nível: Recrutamento (tropa inicial), Arsenal (dano), Cantina (moedas offline), Enfermaria (soldados recuperados) e Munição (perks de munição). Subir de nível custa moedas e Recrutas e leva tempo de parede.

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Iniciar obra | `Assets/_Game/Scripts/Meta/Barracks/BarracksService.cs:TryStartUpgrade` | debita moedas e Recrutas, com devolução se o segundo débito falhar |
| Concluir | `Assets/_Game/Scripts/Meta/Barracks/BarracksService.cs:TryCompleteUpgrade` | só depois do prazo; publica `BuildingUpgradeCompletedEvent` |
| Acelerar com anúncio | `Assets/_Game/Scripts/Meta/Barracks/BarracksService.cs:TrySpeedupWithAd` | tira 30 minutos do prazo, até 3 vezes por dia |
| Concluir com Medalhas | `Assets/_Game/Scripts/Meta/Barracks/BarracksService.cs:TryInstantCompleteWithMedals` | paga em Medalhas para concluir na hora |
| Moedas offline | `Assets/_Game/Scripts/Meta/Barracks/BarracksService.cs:TryCollectOfflineCoins` | coleta da Cantina, em dobro com anúncio |
| Poder para a fase | `Assets/_Game/Scripts/Meta/Run/RunConfigBuilder.cs:Build` | lê tropa, dano, munição e recuperação do serviço |

## Invariantes

- Uma obra por vez.
- Débito é tudo ou nada: sem Recrutas suficientes, as moedas voltam.
- O prazo usa `ITimeProvider`; com relógio manipulado, acelerar e concluir são recusados.

## Mexer junto

- Construção nova → `BarracksBuildingType`, `BarracksCatalog.CreateDefault`, os getters de efeito, `RunConfigBuilder` e o cartão da tela.
- Limite de aceleração → o serviço e `AdPolicyDefinition`, enquanto houver os dois.

## Divergências

- **O poder do Quartel não chega à fase.** `GetInitialSquadBonus`, `GetBaseDamageMultiplier`, `GetUnlockedAmmoPerks` e `CalculateRecoveredSoldiers` só são lidos pelo `RunConfigBuilder`, que nenhum código de produção instancia. Subir construção gasta moedas e não muda o jogo — ver [início e fim de fase](inicio-e-fim-de-fase.md).
- O Quartel não persiste: o serviço é criado pelo composer com estado novo a cada carga — ver [save](../regras/save-e-integridade.md).
- Não há como ganhar Recrutas em jogo: eles vêm da recompensa de fim de fase, que nunca é concedida.
- O catálogo é código (`Assets/_Game/Scripts/Meta/Barracks/BarracksCatalog.cs:CreateDefault`), fora de `Content/Source`.
- Fórmulas de efeito em três lugares: os níveis do catálogo, o padrão dos getters quando falta definição (`2 × nível`, `0,05 × nível`) e um caminho do `RunConfigBuilder` que lê `MetaState.Barracks` por nome com as mesmas fórmulas escritas de novo.
- O limite de 3 acelerações por dia é literal no serviço e também `DailyBarracksSpeedupLimit` na política de anúncios; a tela consulta os dois, cada um com o seu contador.
- O serviço do Quartel usa um relógio próprio, diferente do da política de anúncios — ver [tempo](../regras/tempo.md).
