---
cobre:
  - "Assets/_Game/Scripts/Core/Time/**"
  - "Assets/_Game/Scripts/Meta/Time/**"
revisado: 2026-11-13
---
# Tempo

Dois relógios com papéis diferentes. O de jogo (`IGameTimeService`) escala o delta de cada frame por alvo, para câmera lenta que afeta zumbis e poupa a tropa. O de meta (`ITimeProvider`) dá a hora de parede para obras do Quartel e limites de anúncio, e detecta relógio do aparelho atrasado.

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Delta escalado por alvo | `Assets/_Game/Scripts/Core/Time/GameTimeService.cs:GetDeltaTime` | global × escala do alvo (aliado ou inimigo) |
| Câmera lenta | `Assets/_Game/Scripts/Core/Time/GameTimeService.cs:ActivateBulletTime` | guarda as escalas base e as restaura ao expirar |
| Hora de parede | `Assets/_Game/Scripts/Meta/Time/MonotonicTimeProvider.cs:RecordCurrentTime` | marca manipulação quando a hora recua mais de dez minutos |
| Pausa | `Assets/_Game/Scripts/Presentation/UI/PauseController.cs:PauseGame` | zera `Time.timeScale` direto, fora do serviço de tempo |
| Delta cru | `Assets/_Game/Scripts/Gameplay/Enemies/EnemyController.cs:Update` | usa `Time.deltaTime` quando não há serviço injetado |

## Invariantes

- A pausa só zera o tempo depois de a máquina de estados aceitar `Paused`, e restaura a escala em todo caminho de saída.
- A câmera lenta decai em tempo não escalado: pausar não a prolonga.

## Mexer junto

- Componente novo que anda por tempo → recebe `IGameTimeService` e cai em `Time.deltaTime` só sem ele.
- Regra nova com prazo em hora de parede → `ITimeProvider`, nunca `DateTime.UtcNow`.

## Divergências

- **O serviço de tempo de jogo não está ligado.** `GameTimeService` não é instanciado em produção e `SetTimeService` do `EnemyController` e do `SquadController` não tem chamador: todo movimento usa `Time.deltaTime`, e a câmera lenta só existe em teste.
- Três instâncias de `MonotonicTimeProvider`: a do composer para anúncios, outra criada no bloco do Quartel e a registrada no `GameLifetimeScope`. Cada uma tem a sua última hora vista, então a detecção de relógio manipulado não é compartilhada.
- A última hora vista não é persistida (não há save): reabrir o app zera a detecção.
- A carteira carimba transações com `DateTime.UtcNow` direto — `Assets/_Game/Scripts/Meta/Wallet/Wallet.cs:Add`.
