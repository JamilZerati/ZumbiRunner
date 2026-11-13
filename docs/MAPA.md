---
codigo:
  - "Assets/_Game/Scripts/**/*.cs"
  - "Assets/_Game/Scripts/**/*.asmdef"
  - "tools/**"
ignora:
  - "Assets/_Game/Scripts/Tests/**"
sem-dono:
---
# Mapa

Docs vivas do Horde Runner. Vocabulário em `CONTEXT.md`; operação do harness, guardrails e REA em `docs/agentes/`. Planos (`docs/planos/`, `docs/superpowers/`) e evidências (`docs/evidencias/`) são históricos e ficam fora do mapa. Testes não têm doc própria: acompanham a regra que blindam.

O jogo que vai para o aparelho é a cena `M6_Greybox`. Várias docs abaixo descrevem código que existe, é testado e não está nessa cena; a seção Divergências de cada uma diz o que roda e o que não roda.

## Arquitetura

| Doc | Leia quando |
|---|---|
| [Camadas e assemblies](arquitetura/camadas-e-assemblies.md) | criar asmdef, mudar referência entre assemblies ou decidir em que camada um código novo mora |
| [Composição e boot](arquitetura/composicao-e-boot.md) | criar serviço, mexer no `GreyboxRunComposer`, no VContainer, na máquina de estados ou na cena do build |
| [Barramento de eventos](arquitetura/barramento-de-eventos.md) | criar, publicar ou assinar evento do `IEventBus` |
| [Conteúdo e importadores](arquitetura/conteudo-e-importadores.md) | mexer em `Content/Source`, criar importador, campo de `Definition` ou tipo novo por `[SerializeReference]` |
| [Harness e cenas](arquitetura/harness-e-cenas.md) | mexer no `SceneBuilder`, no `Cli`, no simulador, na validação de conteúdo, no build ou colocar componente em cena |

## Regras

| Doc | Leia quando |
|---|---|
| [Stats e modificadores](regras/stats-e-modificadores.md) | ler ou escrever atributo de arma, criar `StatId`, modificador ou bônus com fonte |
| [Status e sinergias](regras/status-e-sinergias.md) | mexer em queima, veneno, choque, congelamento, lentidão, molhado, sinergia ou criar status |
| [Modificadores de fase e mutações](regras/modificadores-de-fase-e-mutacoes.md) | mexer em mutação, clima, `StageModifierController` ou em qualquer multiplicador de fase |
| [Save e integridade](regras/save-e-integridade.md) | mexer em `MetaState`, `SaveStore`, migração ou persistir qualquer estado do jogador |
| [Tempo](regras/tempo.md) | mexer em câmera lenta, pausa, `Time.deltaTime`, prazo de obra ou relógio do aparelho |

## Fluxos

| Doc | Leia quando |
|---|---|
| [Telas e estado de jogo](fluxos/telas-e-estado-de-jogo.md) | mexer em `UIManager`, camada, janela, pausa, opções, tutorial ou overlay de debug |
| [Início e fim de fase](fluxos/inicio-e-fim-de-fase.md) | mexer no Hub, seleção de fase, `RunConfig`, vitória, derrota, resultado, recompensa, carteira ou revive |
| [Laço da fase](fluxos/laco-da-fase.md) | mexer em `LevelDirector`, `CombatDirector`, segmentos, distância, escape, estrelas ou pista de multiplicador |
| [Lanes, input e tropa](fluxos/lanes-input-e-tropa.md) | mexer em troca de lane, deslize, contagem da tropa, formação, câmera ou pool |
| [Tiro e dano](fluxos/tiro-e-dano.md) | mexer em arma, cadência, pelotão, projétil ou troca de arma |
| [Engajamento e perda de tropa](fluxos/engajamento-e-perda-de-tropa.md) | mexer em contato com zumbi, perda de soldado, vida do General, `IDamageable` ou `HealthComponent` |
| [Hordas e inimigos](fluxos/hordas-e-inimigos.md) | mexer em onda, zumbi, arquétipo, comportamento de inimigo ou morte de inimigo |
| [Portões e perks](fluxos/portoes-e-perks.md) | mexer em portão, par de portões, efeito de perk ou portão vivo |
| [Poder do General](fluxos/poder-do-general.md) | mexer em granada, carga por abate, disparo automático ou manual, mira ou HUD do poder |
| [Aliados](fluxos/aliados.md) | mexer em herói, pet, elenco de aliados ou habilidade de aliado |
| [Barricada, Jaula e Boss](fluxos/barricada-jaula-e-boss.md) | mexer em barricada, sobreviventes, jaula ou luta de boss |
| [Quartel](fluxos/quartel.md) | mexer em construção, obra, aceleração, moedas offline ou no poder que o Quartel dá à fase |
| [Anúncios](fluxos/anuncios.md) | mexer em intersticial, recompensado, política de anúncio, serviço de anúncio ou analytics |
| [Progressão e trilhas](fluxos/progressao-e-trilhas.md) | mexer em mapa de fases, estrelas, desbloqueio por boss, objetivos, trilha ou jornada de estreia |
