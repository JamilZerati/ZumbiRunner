# Horde Runner — GDD e arquitetura

Projeto Linear: **Horde Runner** (time NexusLearning / `NEX`). Este documento é o plano-mãe; cada história ganha seu próprio `docs/planos/` quando for planejada (`/planejar`).

## 1. Decisões de produto (2026-09-26)

| # | Decisão |
|---|---|
| 1 | Mobile, orientação retrato, input por swipe (teclado A/D no editor) |
| 2 | Troca **discreta** de lane; nº de lanes é dado (`LaneLayout.LaneCount`), começa em 2 |
| 3 | Meio-termo: fases comuns curtas + **fases Boss** difíceis que desbloqueiam conteúdo |
| 4 | Habilidade de herói com **toggle** nas opções: automática ou manual (toque) |
| 5 | Rewarded ad **"dobrar recompensa"** no fim da fase |
| 6 | Desenvolvimento **CLI-first**, conduzido por Linear + LLMs (seção 5) |

## 2. Gameplay

**Pilares:** decisão constante e rápida · crescimento visível · tensão nas lanes · sinergias.

**Loop:** correr → ler as lanes → decidir → portão/horda → tropa muda → … → chefe → recompensa (x2 com ad) → meta.

- **General + tropa:** nº de soldados = HP. Zumbi que encosta mata soldado(s). Tropa atira sozinha para frente.
- **Lanes:** swipe troca de lane; reposicionamento da formação leva um instante (risco).
- **Portões (pares por lane):** aritméticos (`+10`, `x2`, `-5`, `÷2`), arma, aliado (herói/pet), stat, temporário. Variações: portão que evolui com tiros, portão guardado por horda, portão amaldiçoado (bônus + desvantagem).
- **Efeitos:** queimadura (DoT), congelamento (acúmulo → stun), slow, choque (cadeia), veneno (pilhas, explode ao morrer). **Sinergias** em tabela de dados: congelado + dano pesado = Estilhaço; Molhado + choque = cadeia dobrada.
- **Clima:** chuva, nevasca, onda de calor, neblina. **Tempo:** bullet time, congelar tempo (ultimate), eclipse.
- **Heróis:** HP próprio, passiva, ativa com cooldown (auto/manual por toggle). **Pets:** auras passivas.
- **Inimigos:** Andarilho, Corredor, Brutamontes, Cuspidor, Explosivo, Escudeiro, Chefe.
- **Estrutura de fase (60–120 s):** aquecimento → portões → horda → portões → evento de clima → horda grande → portões finais → chefe → multiplicador.
- **Progressão:** mapa de fases; a cada N fases uma **fase Boss** (dificuldade alta) que, vencida, desbloqueia arma/herói/mundo. Moedas → melhorias permanentes (tropa inicial, dano base, nível de herói). Rewarded ad dobra as moedas da fase.

## 3. Arquitetura Unity

Princípios: dados em ScriptableObjects · lógica em C# puro · apresentação em MonoBehaviour · composição via `[SerializeReference]` · eventos entre sistemas · pooling · progresso por **distância**.

**Assemblies:** `Game.Core` (sem UnityEngine) ← `Game.Data` ← `Game.Gameplay` ← `Game.Presentation`; `Game.Infrastructure` (input, save, áudio, ads); `Game.Composition` (VContainer); `Game.Editor` (tooling CLI); `Game.Tests.EditMode` / `Game.Tests.PlayMode`.

**Contratos-chave:** `StatCollection` + `StatModifier(Stat, Kind, Value, Source)`; `IPerkEffect.Apply(PerkContext)` e `PerkDefinition`; `DamageInfo` / `IDamageable`; `IStatusEffect` + `EffectInteractionTable`; `LaneLayout`; `LevelDefinition` → `SegmentDefinition` com eventos por distância; `IRewardedAdService`; `IHeroAbilityTriggerPolicy` (Auto/Manual).

**Sistemas:** GameStateMachine · LevelDirector · TrackScroller · LaneMover · SquadController · FormationSolver · WeaponController · ProjectileSystem · StatusEffectController · HordeSpawner · GateController · AllyRoster/AbilityRunner · WeatherSystem · GameTimeService (escalas separadas) · EventBus · ProgressionService · SaveService.

**Performance:** começar MonoBehaviour + pooling; lógica pura permite migrar movimento da horda para Jobs/Burst; GPU instancing + VAT no polimento; colisão por grade espacial por lane.

## 4. Roadmap (épicos → histórias)

| Épico | História | Critério de pronto |
|---|---|---|
| E1 Protótipo | M0 Fundação + harness CLI | Projeto abre, compila e roda testes **via CLI** |
| E1 | M1 Corredor greybox | Cubo corre e troca entre 2 lanes; `LaneCount=3` funciona |
| E1 | M2 Tropa | Formação cresce/encolhe; HUD de contagem |
| E1 | M3 Portões aritméticos | Escolha de lane altera a tropa |
| E1 | M4 Tiro e zumbis | Dá para perder e vencer uma horda |
| E2 Combate | M5 Stats e armas | Trocar arma muda comportamento |
| E2 | M6 Efeitos de status | Estilhaço funciona e tem teste EditMode |
| E2 | M7 Level Director | Fase de 90 s editável só por dados |
| E2 | M8 Heróis e pets | Herói recrutado; habilidade auto/manual por toggle |
| E2 | M9 Clima e tempo | Chuva + choque amplificado; bullet time |
| E3 Progressão | M10 Variedade de inimigos | Brutamontes, Cuspidor, Explosivo, Escudeiro |
| E3 | M11 Fases Boss e desbloqueios | Vencer Boss libera conteúdo |
| E3 | M12 Meta-progressão e save | Moedas, upgrades, mapa de fases persistem |
| E4 Polimento | M13 Rewarded ad "dobrar recompensa" | Fluxo com mock e SDK real atrás de interface |
| E4 | M14 Performance e game feel | 60 fps com 200+ unidades em aparelho alvo |

## 5. Desenvolvimento CLI-first conduzido por Linear + LLMs

Objetivo: um agente (LLM) consegue pegar uma tarefa no Linear, implementar, **compilar, testar, validar conteúdo e buildar sem abrir o Editor**, e abrir PR. O humano valida sensação de jogo no aparelho.

### 5.1 Regras de repositório
- Unity com **Asset Serialization = Force Text** e **Visible Meta Files**; `.gitattributes` com `UnityYAMLMerge` e Git LFS para binários (arte/áudio).
- Versão do Editor fixada em `ProjectSettings/ProjectVersion.txt`; caminho do executável em variável `UNITY_EDITOR` (script lê, nunca hardcode).
- **Conteúdo como dados legíveis:** o agente cria/edita conteúdo (armas, perks, fases) em JSON sob `Content/Source/`; um importador de Editor gera/atualiza os ScriptableObjects. SO gerado nunca é editado à mão pelo agente.
- Cenas montadas por código (`SceneBuilder`) sempre que possível; prefabs mínimos, compostos por dados.

### 5.2 Harness CLI (`tools/unity.ps1` + `tools/unity.sh`)
Todos rodam `Unity -batchmode -nographics -quit -projectPath . -logFile <arquivo>` e retornam exit code ≠ 0 em falha:

| Comando | Faz | Por baixo |
|---|---|---|
| `compile` | Importa e compila; falha em erro de compilação | `-executeMethod Game.Editor.Cli.Compile` |
| `test-edit` | Testes EditMode, resultado NUnit XML | `-runTests -testPlatform EditMode -testResults` |
| `test-play` | Testes PlayMode | `-runTests -testPlatform PlayMode` |
| `import-content` | JSON → ScriptableObjects | `-executeMethod Game.Editor.Cli.ImportContent` |
| `validate` | Valida referências, IDs únicos, fases jogáveis | `-executeMethod Game.Editor.Cli.ValidateContent` |
| `simulate` | Roda fase headless com bot de lanes e emite relatório JSON (vitória, tropa final, DPS) | `-executeMethod Game.Editor.Cli.SimulateLevel -level <id> -seed <n>` |
| `build-android` | APK/AAB de desenvolvimento | `-executeMethod Game.Editor.Cli.BuildAndroid` |

O resumo do log é extraído para texto curto (erros de compilação, testes falhos) para caber no contexto do agente.

### 5.3 Balanceamento assistido
`simulate` com seeds fixas gera métricas por fase; o agente compara com metas (ex.: taxa de vitória do bot guloso ~70% em fase comum, ~30% em fase Boss) e propõe ajustes nos JSON. Isso é o "teste de regressão de diversão".

### 5.4 CI
GitHub Actions com GameCI (`unity-test-runner`, `unity-builder`): `compile` + `test-edit` + `validate` em todo PR; `test-play` + `build-android` na branch da história. Licença Unity em secrets.

### 5.5 Fluxo por tarefa (Linear → LLM → PR)
1. Tarefa `NEX-n` com bloco `dispatch` (needs, faixa, runtime, comando de verificação).
2. `/atuar NEX-n` → `/executar`: agente implementa na branch da tarefa (a partir da branch da história).
3. Portões locais: `compile` → `test-edit` → `validate` (→ `simulate` quando mexer em conteúdo/balanceamento).
4. PR da tarefa → branch da história; CI repete os portões.
5. `/review` audita aceite; merge → Done. História completa → `/homologar` (inclui APK no aparelho).

### 5.6 O que continua humano
Sensação de controle e game feel no aparelho, direção de arte, aprovação de balanceamento final, contas de loja/ads, licença Unity.
