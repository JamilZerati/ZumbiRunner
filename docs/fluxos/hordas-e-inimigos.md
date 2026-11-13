---
cobre:
  - "Assets/_Game/Scripts/Gameplay/Enemies/EnemyController.cs"
  - "Assets/_Game/Scripts/Gameplay/Enemies/HordeSpawner.cs"
  - "Assets/_Game/Scripts/Data/Enemies/**"
  - "Assets/_Game/Scripts/Presentation/EnemyView.cs"
revisado: 2026-11-13
---
# Hordas e inimigos

Zumbis vêm de um pool, nascem em ondas numa lane e andam contra a tropa. O desenho prevê arquétipos por dados: cada `EnemyDefinition` compõe comportamentos (`IEnemyBehavior`) como investir, explodir, cuspir, curar, erguer escudo. Em jogo existe um zumbi só.

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Onda | `Assets/_Game/Scripts/Gameplay/Enemies/HordeSpawner.cs:SpawnWave` | aluga do pool, posiciona na lane e inicializa com vida e velocidade efetivas |
| Movimento | `Assets/_Game/Scripts/Gameplay/Enemies/EnemyController.cs:Tick` | anda para trás na pista pela velocidade × multiplicador de status |
| Golpe recebido | `Assets/_Game/Scripts/Gameplay/Enemies/EnemyController.cs:ReceiveHit` | passa pelo controlador de status, que resolve sinergia e dano |
| Morte | `Assets/_Game/Scripts/Gameplay/Enemies/EnemyController.cs:Die` | publica `EnemyKilledEvent` uma vez, recicla e devolve ao pool |
| Arquétipo por dados | `Assets/_Game/Scripts/Data/Enemies/EnemyDefinition.cs:EnemyDefinition` | vida, velocidade, dano e lista de comportamentos por `[SerializeReference]` |
| Comportamento | `Assets/_Game/Scripts/Data/Enemies/IEnemyBehavior.cs:IEnemyBehavior` | doze implementações em `Behaviors/`, inclusive as três do boss |

## Invariantes

- `SpawnWave` em lane inválida lança exceção; sem pool, não cria nada.
- Sobreviventes perdidos voltam como zumbis a mais só na próxima onda da mesma lane, para não quebrar o ritmo da pista.
- O abate é publicado uma vez por morte, com os status ativos e se foi causado pelo poder do General.

## Mexer junto

- Atributo novo de inimigo → `EnemyDefinition`, importador, `EnemyController.Initialize` e o simulador.
- Comportamento novo → classe em `Behaviors/`, ramo no `EnemyImporter` e, quando houver, o laço que executa comportamentos.

## Divergências

- **Arquétipos e comportamentos não chegam ao jogo.** Nenhum código de produção lê `EnemyCatalog`, `EnemyDefinition` ou `IEnemyBehavior`: o `EnemyController` não executa comportamentos. Os oito inimigos de `Content/Source/Enemies` (corredor, bruto, explosivo, xamã, escudo, cuspidor, boss) só existem no simulador e em teste. Todo zumbi da cena é o `walker` padrão.
- **Ninguém cria ondas em jogo.** `SpawnWave` só é chamado pelo `SceneBuilder`, em tempo de edição — ver [harness e cenas](../arquitetura/harness-e-cenas.md).
- Velocidade, custo de contato e dano por segundo do zumbi são valores padrão em C# no `EnemyController` (2, 1 e 5).
- Os comportamentos escrevem estado por fora dos caminhos do jogo: `ExplodeOnContactBehavior` e `RangedSpitBehavior` tiram soldados direto da tropa; `ExplodeOnDeathBehavior` e `HealAuraBehavior` escrevem a vida de outros zumbis no contexto, sem `ReceiveHit`, sem evento e sem morte. Ligados como estão, seriam um segundo caminho de dano.
- Um zumbi tem duas mortes possíveis: pelo `ReceiveHit` (evento de abate e reciclagem) e pelo `HealthComponent` direto (evento de entidade morta, sem reciclagem) — ver [tiro e dano](tiro-e-dano.md).
- Os multiplicadores de mutação para zumbi só são lidos no nascimento da onda — ver [modificadores de fase](../regras/modificadores-de-fase-e-mutacoes.md).
