# Horde Runner — Gameplay, meta, monetização e live-ops (design)

Complementa o plano-mãe (`2026-09-26-horde-runner-gdd-arquitetura.md`). O plano-mãe continua valendo para visão, arquitetura base e harness CLI; este documento fixa as regras do combate, os ganchos de gameplay, a meta, a monetização e a camada de live-ops, e redefine o roadmap a partir da M7. Cada história derivada ganha seu próprio plano em `docs/planos/` via `/planejar`; os números aqui são **valores iniciais** que o `simulate` (M7) ajusta.

## 0. Decisões e critério de sucesso (2026-09-29)

| # | Decisão |
|---|---|
| 7 | Monetização **hybrid-casual**: rewarded ads como motor, interstitial com política, IAP leve (remover anúncios, starter pack, passe, medalhas) |
| 8 | Live-ops **offline-first**: calendário em JSON embutido no build atrás de `IContentCatalog` + `ITimeProvider`; Remote Config entra depois atrás das mesmas interfaces |
| 9 | Pago ou por anúncio só compra **poder leve e temporário** (boost de uma fase, revive, aceleração de tempo); nada exclusivo pago que afete combate |
| 10 | **Cada soldado atira**: dano escala com a tropa, agrupado em pelotões para performance |
| 11 | Meta por **núcleo genérico**: sinal → objetivo → trilha → recompensa, reaproveitado por passe, evento, login diário e missões |
| 12 | Ganchos de gameplay: **portões vivos**, **resgate de sobreviventes**, **mutações** e **quartel** |
| 13 | Sem energia/vidas: o jogador joga quantas fases quiser (retenção > fricção no gênero) |

**Critério de sucesso da arquitetura:**

1. Uma temporada de passe, um evento ou um inimigo temático novos são criados editando só `Content/Source/` e passam em `tools/unity validate`.
2. `Game.Gameplay` não referencia `Game.Meta` (garantido por asmdef).
3. Todo número de combate vive em dados; nenhum valor de balanceamento fica fixo em `SceneBuilder` ou MonoBehaviour.

## 1. Módulos e assemblies

Gameplay e meta se tocam em dois pontos, e só neles:

```
            ┌──────────── RunConfig (entrada da fase) ────────────┐
            │ tropa inicial, boosts, mutações, regras de evento,   │
            ▼ multiplicador de recompensa                          │
  Game.Gameplay ──publica──▶ sinais no IEventBus ──▶ Game.Meta ─────┘
  (não conhece meta)          + RunResult no fim     (objetivos, trilhas,
                                                      carteira, calendário,
                                                      quartel)
```

| Assembly | Recebe | Depende de |
|---|---|---|
| `Game.Core` | Sinais novos, `RunConfig`, `RunResult`, `IStageModifier`, `PlatoonSolver`, `IEnemyBehavior` | nada |
| `Game.Meta` (novo, `noEngineReferences: true`) | `Wallet`, `Inventory`, `IReward`/`RewardBundle`, `ObjectiveTracker`, `ProgressTrack`, `Entitlements`, `LiveOpsCalendar`, `Barracks`, `RunConfigBuilder`, `RewardCalculator`, `AdPolicy`, interfaces `ITimeProvider`, `ISaveStore`, `IAdService`, `IStoreService`, `IAnalyticsService`, `IContentCatalog` | `Game.Core` |
| `Game.Data` | SOs: `EnemyDefinition`, `GateDefinition`, `MutationDefinition`, `ObjectiveDefinition`, `TrackDefinition`, `LiveOpsEntryDefinition`, `OfferDefinition`, `CurrencyDefinition`, `CosmeticDefinition`, `BarracksBuildingDefinition`, `AdPolicyDefinition` | `Game.Core`, `Game.Meta` |
| `Game.Gameplay` | Lê `RunConfig`, aplica `IStageModifier`, publica sinais e `RunResult` | como hoje; **nunca** `Game.Meta` |
| `Game.Infrastructure` | Save JSON local, relógio do sistema, mocks de ads/IAP/analytics; SDKs reais depois | ganha `Game.Meta` |
| `Game.Presentation` | Hub, resultado, passe, evento, quartel, loja, cosméticos | ganha `Game.Meta` |
| `Game.Editor` | Importadores JSON dos novos SOs; regras novas em `validate` e `simulate` | como hoje |

## 2. Combate

### 2.1 Tropa é HP e poder de fogo

- `PlatoonSolver` (Core, puro) recebe a contagem da tropa e devolve os **pelotões**: `emissores = min(ceil(tropa / 5), 40)`; cada emissor representa `tropa / emissores` soldados (resto distribuído nos primeiros).
- Cada emissor dispara a arma equipada; `dano por projétil = dano da arma × soldados representados`. O dano total escala linearmente com a tropa; o custo de renderização para em 40 emissores.
- Emissores ficam nas posições do `FormationSolver`; o volume visual de tiro cresce com a tropa.
- Munição de status (M6) segue por projétil; um projétil de pelotão aplica o status uma vez.
- Consequência de balanceamento: portões `x2`/`÷2` passam a dobrar/cortar o DPS. As armas da M5 são reinterpretadas como **dano por soldado**.

| Arma (por soldado) | Dano | Tiros/s | Alcance | Projéteis | DPS por soldado |
|---|---|---|---|---|---|
| Pistola (base) | 2 | 2 | 40 m | 1 | 4 |
| SMG | 1 | 6 | 35 m | 1 | 6 |
| Shotgun | 2 | 1,2 | 22 m | 3 em leque | 7,2 (curto alcance) |

Tropa inicial base: **10** (quartel aumenta). Com pistola, 10 soldados = 40 DPS.

### 2.2 Contato e derrota

- Zumbi que toca `SquadBody` (General ou soldado) remove `contactCost` soldados e é **consumido** (sinal `EnemyConsumedEvent`, não conta como abate).
- Tropa zerada: o General fica sozinho; o próximo contato ou acerto encerra a fase em derrota (regra do plano-mãe mantida).
- Dano recebido tem **pisca de 0,15 s** por soldado perdido (game feel na M14).

### 2.3 Física por camadas

Camadas `SquadBody`, `PlayerProjectile`, `Enemy`, `EnemyProjectile`, `Pickup` (portões, jaulas). Matriz: `PlayerProjectile × Enemy/Pickup`, `SquadBody × Enemy/EnemyProjectile/Pickup`; o resto não colide. Projéteis e inimigos têm Rigidbody cinemático próprio e nunca são filhos do General (lição do GH #47).

### 2.4 Inimigos como dados

`EnemyDefinition` (JSON → SO): `id`, `hp`, `speed`, `contactCost`, `coinValue`, `behaviors: IEnemyBehavior[]` via `[SerializeReference]`. Comportamentos: `MoveStraight`, `ChaseLane(delay)`, `StopAt(distance)`, `RangedSpit`, `HealAura`, `Resurrect`, `FrontShield`, `ExplodeOnContact`, `ExplodeOnDeath`.

| Arquétipo | HP | Vel. (m/s) | Contato | Comportamentos | Entra em |
|---|---|---|---|---|---|
| Andarilho | 20 | 2 | 1 | `MoveStraight` | fase 1 |
| Corredor | 12 | 5 | 1 | `MoveStraight` | fase 3 |
| Brutamontes | 300 | 1,2 | 5 | `MoveStraight` | fase 5 |
| Explosivo | 30 | 2,5 | 3 | `ExplodeOnContact(raio 2 m)`, `ExplodeOnDeath(50 dano em zumbis, raio 3 m)` | fase 7 |
| Cuspidor | 40 | 1 | 1 | `StopAt(25 m)`, `RangedSpit(a cada 3 s, aviso 1,0 s, mata 3)` | fase 9 |
| Escudeiro | 80 | 1,5 | 2 | `FrontShield(bloqueia projétil reto até receber qualquer status ou dano de área)` | fase 11 |
| Xamã | 60 | 1 | 1 | `StopAt(8 m atrás da horda)`, `HealAura(5 HP/s, raio 4 m)`, `Resurrect(1 Andarilho a cada 6 s)` | fase 13 |
| Chefe | M11 | — | — | padrões por lane | fase Boss |

Escala por fase: `hp × (1 + 0,08 × (fase − 1))`, definida no `LevelDefinition`.

**Cuspe (ataque à distância):** marca a lane alvo no chão 1,0 s antes do impacto; trocar de lane a tempo anula. Dá ao swipe uso fora dos portões. Projétil inimigo em `EnemyProjectile`.

### 2.5 Poder do General

- `GeneralAbilityDefinition`: `chargeKills` (25), `effect` (`IAbilityEffect`). Primeira: **Granada**, 150 de dano em raio 4 m na lane atual.
- Disparo auto/manual pelo `IHeroAbilityTriggerPolicy` do plano-mãe (toggle nas opções).
- Monetização: no máximo **uma carga extra por fase via rewarded**; nunca vendida.

### 2.6 Estrutura de fase e pista de multiplicador

Fase de 60–120 s a 8 m/s (480–960 m), autorada em `LevelDefinition` → `SegmentDefinition` (M7):

aquecimento (0–60 m) → portões → horda → portões/jaula → evento de clima → horda grande → portões finais → chefe (fase Boss) → **pista de multiplicador**.

**Pista de multiplicador:** marcos `x1`…`x5` a cada 15 m; cada marco exige N soldados (a tropa "paga" para avançar); o marco alcançado multiplica as moedas da fase. Transforma a tropa restante em recompensa e é a âncora do "dobrar recompensa".

### 2.7 Sinais publicados pelo gameplay

`RunStartedEvent(levelId, runConfig)`, `EnemyKilledEvent(archetypeId, lane, statusesAtDeath, byAbility)`, `EnemyConsumedEvent(archetypeId, soldiersLost)`, `SoldiersLostEvent(count, cause)`, `GatePassedEvent(gateId, perkId, variant)`, `SurvivorsRescuedEvent(count)`, `SurvivorsLostEvent(count)`, `AbilityUsedEvent(abilityId)`, `SynergyTriggeredEvent` (já existe), `RunEndedEvent(RunResult)`.

`RunResult`: `levelId`, `victory`, `distance`, `squadAtEnd`, `multiplierReached`, `killsByArchetype`, `rescued`, `coinsEarned`, `mutationIds`, `durationSeconds`.

## 3. Ganchos de gameplay

### 3.1 Portões vivos

`GateDefinition` = perk + variante (`IGateVariant`):

| Variante | Regra | Decisão que cria |
|---|---|---|
| `Static` | Como hoje | — |
| `ShootToUpgrade(step, hitsPerStep, cap)` | Projéteis que acertam o portão sobem o valor (`-5` → `+15`) | Gastar tiros no portão ou nos zumbis |
| `Cursed(bonusPerk, stageModifier, duration)` | Bônus forte + `IStageModifier` negativo (ex.: `x3` tropa, zumbis +30% velocidade por 150 m) | Ganância × risco |
| `Guarded(enemyWaveId)` | Horda posicionada na frente do portão | Limpar a guarda ou trocar de lane |

Portão com `ShootToUpgrade` fica na camada `Pickup` e recebe projéteis.

### 3.2 Resgate de sobreviventes

- **Jaula** na lane com HP (ex.: 60). Atirar até abrir → `+N` soldados (`SurvivorsRescuedEvent`).
- Passar sem abrir → os sobreviventes viram `N` Andarilhos na próxima horda daquela lane (`SurvivorsLostEvent`).
- Resgatados ao fim da fase viram **Recrutas** (moeda da meta usada pelo quartel).
- Eventos usam resgate como objetivo ("resgate 500 sobreviventes").

### 3.3 Mutações

- `MutationDefinition`: `id`, `nome`, `modifiers: IStageModifier[]`, `rewardBonus` (+0,25 a +1,0).
- Exemplos: *Blindados* (HP ×1,5, +0,5), *Frenesi* (velocidade ×1,3, +0,5), *Neblina* (alcance −40%, +0,25), *Último Soldado* (tropa inicial 1, +1,0), *Sem Multiplicação* (portões `x`/`÷` viram `+`/`−`, +0,5).
- O jogador ativa até 3 antes da fase (desbloqueio após o primeiro Boss). Bônus somam no `RewardCalculator`.
- Eventos podem **forçar** mutações (seção 6).
- `IStageModifier` aplica no início da fase via `RunConfig`; modificadores de stat usam `Source = mutação:<id>` para remoção limpa.

### 3.4 Quartel

Meta idle diegética que substitui a "tela de upgrades" da M12:

| Construção | Efeito por nível | Custo |
|---|---|---|
| Recrutamento | +2 tropa inicial | moedas + recrutas |
| Arsenal | +5% dano base | moedas |
| Refeitório | gera moedas/h offline (teto 8 h) | moedas + recrutas |
| Enfermaria | +1 soldado recuperado a cada 10 perdidos na fase (teto) | moedas + recrutas |
| Oficina de munição | desbloqueia munição de status como portão inicial opcional | recrutas |

- Construções são o único poder permanente do jogo e custam só recursos ganhos jogando (moedas e recrutas); dinheiro real acelera, nunca compra nível direto.
- Construções têm tempo de obra (minutos a horas). Rewarded acelera 30 min (limite diário); medalhas pulam.
- Coleta offline pode ser dobrada por rewarded.

## 4. Núcleo de meta (`Game.Meta`)

### 4.1 Moedas e carteira

| Moeda | Tipo | Fonte | Uso |
|---|---|---|---|
| Moedas | soft | fases, refeitório, trilhas | quartel, cosméticos baratos |
| Recrutas | soft | resgate | quartel |
| Medalhas | hard | IAP, passe, eventos, conquistas | pular obra, revive sem anúncio, cosméticos, ofertas |
| Fichas de evento (`evt_<id>`) | temporária | objetivos do evento | trilha e loja do evento; convertidas em moedas no fim |

`Wallet.Add(currency, amount, source)` / `TrySpend(...)`; todo movimento registra `source` (analytics e auditoria) e publica `WalletChangedEvent`.

### 4.2 Recompensas

`RewardBundle` = lista de `IReward` via `[SerializeReference]`: `CurrencyReward`, `ItemReward` (boosts), `CosmeticReward`, `UnlockReward` (conteúdo), `EntitlementReward`. `bundle.Grant(IRewardSink)`; conceder é idempotente por `grantId` (evita duplicar em crash/reentrada).

### 4.3 Objetivos

```json
{
  "id": "kill_frozen_spitters_50",
  "signal": "EnemyKilled",
  "filters": { "archetypeId": "spitter", "statusAtDeath": "frozen" },
  "target": 50,
  "scope": "cumulative"
}
```

- `ObjectiveTracker` assina os sinais durante a fase e aplica o `RunResult` no fim; `scope`: `run` (numa fase) ou `cumulative`.
- Sinais suportados = seção 2.7. Filtros tipados por sinal; `validate` rejeita filtro desconhecido.

### 4.4 Trilhas

```json
{
  "id": "season_01_track",
  "pointsCurrency": "season_01_xp",
  "premiumEntitlement": "season_01_premium",
  "milestones": [
    { "points": 100, "free": [{ "type": "currency", "id": "coins", "amount": 200 }],
      "premium": [{ "type": "cosmetic", "id": "general_skin_frost" }] }
  ]
}
```

`ProgressTrack` serve passe (XP), evento (fichas), login diário (dias) e jornada de estreia. Estado: pontos + marcos resgatados por lado (grátis/premium).

### 4.5 Entrada e saída da fase

- `RunConfigBuilder` monta `RunConfig` = base (quartel) + boosts consumidos + mutações escolhidas + regras do evento ativo.
- `RewardCalculator(RunResult, RunConfig)` = `coinsEarned × multiplicador da pista × (1 + Σ bônus de mutação)`; o resultado vai à tela de resultado, que oferece o "dobrar" (rewarded) antes de creditar.

### 4.6 Persistência e tempo

- `MetaState` é o agregado salvo: carteira, inventário, trilhas, objetivos, quartel, entitlements, estado do calendário, opções.
- `ISaveStore` JSON local com `version` e migrações testadas; hash de integridade contra edição casual (não é segurança).
- `ITimeProvider.UtcNow`; offline usa o relógio do aparelho com guarda monotônica: se o relógio voltar mais de 10 min, timers congelam até alcançar o último valor visto. Servidor de tempo entra com o Remote Config.

## 5. Monetização

### 5.1 Anúncios (`IAdService`, SDK real atrás da interface)

| Placement | Tipo | Regra |
|---|---|---|
| Dobrar recompensa | rewarded | tela de resultado; recompensa base nunca se perde (M13 original) |
| Revive | rewarded | 1× por fase: continua com 10 soldados |
| Boost pré-fase | rewarded | 1× por fase: +10 tropa inicial |
| Carga do poder | rewarded | 1× por fase |
| Quartel | rewarded | dobrar coleta offline; acelerar obra 30 min (limite diário) |
| Entre fases | interstitial | só depois da fase 5 do jogador; no máximo 1 a cada 3 fases **e** 90 s entre anúncios; nunca até 120 s após rewarded ou compra; nunca na primeira sessão do dia antes da 2ª fase |

- Todos os números em `AdPolicyDefinition` (JSON). Teto diário de rewarded configurável.
- Sem banner.
- `AdPolicy` (Meta, puro) decide; `IAdService` só exibe. Testável em EditMode com relógio falso.

### 5.2 Compras (`IStoreService`, Unity IAP / Play Billing)

| Produto | Tipo | Conteúdo (sugestão; preço é decisão humana) |
|---|---|---|
| Remover anúncios | não consumível | remove interstitial; rewarded seguem opcionais |
| Starter pack | oferta única | moedas + medalhas + skin + 3 boosts; aparece após a 1ª derrota em Boss ou fase 3 |
| Passe premium | por temporada | libera a trilha premium da temporada |
| Medalhas | consumível | 3 tamanhos |
| Oferta de evento | temporária | pacote temático do evento ativo |

`OfferDefinition` em JSON (gatilho, janela, conteúdo, limite por jogador). Recibos validados localmente agora; validação em servidor vem com o backend.

### 5.3 Cosméticos

Skins do General, uniforme da tropa, rastro de projétil, bandeira do quartel. `CosmeticDefinition` é só apresentação; `validate` garante que nenhum cosmético referencia stat.

### 5.4 Regra de ouro (decisão 9)

| Permitido | Proibido |
|---|---|
| Boost de uma fase, revive, carga extra do poder | Arma, herói ou pet exclusivos pagos |
| Acelerar/pular tempo de obra | Stat permanente vendido direto |
| Moedas via medalhas (acelera upgrades que qualquer um alcança) | Trilha premium com poder de combate |
| Cosméticos | Energia/vidas |

### 5.5 Analytics e consentimento

- `IAnalyticsService` com eventos: `run_start`, `run_end`, `ftue_step`, `ad_offer`/`ad_shown`/`ad_reward`, `iap_purchase`, `objective_complete`, `track_claim`, `economy_source`/`economy_sink`.
- Consentimento (UMP do Google) antes de inicializar ads/analytics; sem consentimento, anúncios não personalizados.
- Faixa etária do público-alvo é decisão humana (afeta políticas da Play Store e dos SDKs).

## 6. Live-ops (offline-first)

### 6.1 Calendário

```json
{
  "id": "evt_2026_10_spitter_night",
  "type": "event",
  "startUtc": "2026-10-24T00:00:00Z",
  "endUtc": "2026-10-31T00:00:00Z",
  "currency": "evt_spitter_night_tokens",
  "trackId": "evt_spitter_night_track",
  "objectiveIds": ["evt_sn_kill_spitters_200", "evt_sn_clear_fog_10"],
  "forcedMutationIds": ["fog"],
  "enemyOverrides": { "spitter": { "weight": 3 } },
  "offerIds": ["offer_spitter_night_bundle"],
  "theme": { "bannerKey": "spitter_night", "accent": "#7FFF4F" }
}
```

- Tipos: `season`, `event`, `daily_login`, `offer`, `mission_rotation`.
- `LiveOpsCalendar(ITimeProvider, IContentCatalog)` devolve entradas ativas; no fim de um evento, fichas viram moedas a taxa fixa e recompensas já alcançadas e não resgatadas são concedidas automaticamente.
- `IContentCatalog`: `BuiltInCatalog` (SOs importados) agora; `RemoteCatalog` depois (mesmo schema, validado no cliente, fallback para o embutido).

### 6.2 Produtos de live-ops

| Produto | Forma | Montado com |
|---|---|---|
| Temporada (passe) | 28 dias, 30 níveis, trilha grátis + premium | `season` + `TrackDefinition` + XP de missões e de fases |
| Missões | 3 diárias + 5 semanais, sorteio determinístico por data (`seed = dia`) | `mission_rotation` + pool de `ObjectiveDefinition` |
| Login diário | trilha de 7 dias em ciclo | `daily_login` + `TrackDefinition` |
| Evento | 5–7 dias, regras próprias | `event` + mutações forçadas + inimigos em destaque + fichas + trilha + oferta |
| Jornada de estreia | trilha dos primeiros 7 dias | `TrackDefinition` fixa, fora do calendário |

Ideias de evento que só usam dados: *Noite dos Cuspidores* (neblina forçada, cuspidores ×3), *Resgate em massa* (jaulas dobradas, ficha = sobrevivente), *Era do Gelo* (nevasca, objetivos de Estilhaço), *Horda Blindada* (Blindados forçada, recompensa ×1,5).

### 6.3 Validação

`tools/unity validate` passa a checar: IDs únicos e referências (objetivos, trilhas, recompensas, moedas, mutações, ofertas); datas coerentes e sem sobreposição de temporadas; trilhas com pontos crescentes; nenhum cosmético com stat; nenhum item premium com poder permanente. `simulate` aceita `-mutations` e `-event` e reporta taxa de vitória com as regras do evento.

## 7. Roadmap (histórias)

Histórias existentes reescopadas (**R**) ou novas (**N**). A ordem é o caminho crítico; o detalhamento em tarefas acontece em `/planejar` na vez de cada uma.

| Ordem | História | Épico | | Pronto quando (resumo) |
|---|---|---|---|---|
| 1 | `NEX-650` M15 · Tropa como poder de fogo e física por camadas | E2 | N | Dano escala com a tropa via pelotões; camadas e matriz de colisão; armas reinterpretadas por soldado; tropa inicial 10 |
| 2 | `NEX-512` M7 · Level Director, simulador e pista de multiplicador | E2 | R | Fase só por dados; `simulate`; pista `x1`–`x5`; `RunConfig`/`RunResult` e sinais da seção 2.7 |
| 3 | `NEX-515` M10 · Inimigos por dados | E3→E2 | R | `EnemyDefinition` + `IEnemyBehavior`; 7 arquétipos; cuspe com aviso na lane; Xamã |
| 4 | `NEX-651` M16 · Poder do General | E2 | N | Granada carregada por abates; auto/manual |
| 5 | `NEX-652` M17 · Portões vivos | E2 | N | `ShootToUpgrade`, `Cursed`, `Guarded` por dados |
| 6 | `NEX-653` M18 · Resgate de sobreviventes | E2 | N | Jaulas com HP; perda vira Andarilhos; recrutas no `RunResult` |
| 7 | `NEX-513` M8 · Heróis e pets | E2 | R | Como hoje; heróis e pets só por portão, nunca vendidos |
| 8 | `NEX-514` M9 · Clima e tempo | E2 | — | Sem mudança |
| 9 | `NEX-517` M12 · Núcleo de meta e save | E3 | R | `Game.Meta`: carteira, recompensas, objetivos, trilhas, entitlements, `RunConfigBuilder`, `RewardCalculator`, save versionado, `ITimeProvider`; mapa de fases |
| 10 | `NEX-654` M19 · Fluxo de telas e estreia | E3 | N | Hub, resultado, pausa, opções (toggle do poder), tutorial das 3 primeiras fases, jornada de estreia |
| 11 | `NEX-516` M11 · Fases Boss e desbloqueios | E3 | — | Sem mudança |
| 12 | `NEX-655` M20 · Mutações | E3 | N | `MutationDefinition`; escolha pré-fase; bônus de recompensa; `simulate -mutations` |
| 13 | `NEX-656` M21 · Quartel | E3 | N | 5 construções, obras com tempo, coleta offline, recrutas |
| 14 | `NEX-518` M13 · Anúncios: rewarded e interstitial com política | E4 | R | Placements da seção 5.1; `AdPolicy` puro com testes; mock + SDK real |
| 15 | `NEX-657` M22 · Analytics e consentimento | E4 | N | `IAnalyticsService` com eventos da seção 5.5; UMP antes de ads/analytics |
| 16 | `NEX-658` M23 · Compras: remover anúncios, starter pack e medalhas | E4 | N | `IStoreService` com Unity IAP; ofertas por dados; restauração de compras |
| 17 | `NEX-659` M24 · Cosméticos e loja | E4 | N | Skins/uniformes/rastros; loja por moedas e medalhas |
| 18 | `NEX-660` M25 · Calendário live-ops | E5 | N | `LiveOpsCalendar`, `IContentCatalog` embutido, validação de datas |
| 19 | `NEX-661` M26 · Missões e login diário | E5 | N | Rotação determinística; trilha de 7 dias |
| 20 | `NEX-662` M27 · Passe de temporada | E5 | N | Temporada de 28 dias, trilha grátis + premium, compra do premium |
| 21 | `NEX-663` M28 · Eventos temporários | E5 | N | Evento completo só por JSON (mutações forçadas, fichas, trilha, oferta) |
| 22 | `NEX-519` M14 · Performance, game feel e áudio | E4 | R | Como hoje + SFX/música e mixagem |
| 23 | `NEX-664` M29 · Conteúdo remoto | E5 | N | `RemoteCatalog` (Remote Config) com fallback; tempo de servidor |

Novo épico: **E5 · Live-ops e economia** (Later). Dependências registradas como `blocked by` no Linear.

**O que é humano:** contas Play Console, AdMob/LevelPlay e provedor de analytics; configuração do UMP; preços e ofertas; faixa etária; direção de arte, áudio e sensação de jogo no aparelho.

## 8. Testes

- `Game.Meta` é C# puro: carteira, recompensas idempotentes, objetivos, trilhas, `AdPolicy`, calendário e migrações de save cobertos em EditMode com `ITimeProvider` falso.
- Combate: `PlatoonSolver` e comportamentos de inimigo puros em EditMode; colisão por camadas em PlayMode (padrão do teste do GH #47).
- Conteúdo: `validate` com as regras da seção 6.3 em todo PR.
- Balanceamento: `simulate` com seeds fixas; metas do plano-mãe (~70% em fase comum, ~30% em Boss) e, para eventos, ~60% com as mutações forçadas.

## 9. Fora de escopo e riscos

- **Fora:** multiplayer, ranking online, gacha, energia/vidas, backend próprio antes da M29.
- **Relógio offline manipulável:** aceito até a M29; guarda monotônica limita o abuso de timers.
- **Números do combate:** a troca para dano por soldado muda todo o balanceamento de M4–M6; a M15 recalibra as cenas greybox e a M7 fecha com o `simulate`.
- **Escopo total grande:** E5 inteiro depende de M12; nada de live-ops começa antes do núcleo de meta estar pronto e testado.
