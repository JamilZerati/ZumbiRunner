# Plano de Implementação · NEX-506
Data: 2026-09-26
Issue: NEX-506 (M1 · Corredor greybox com lanes discretas)

---

## 1. Contexto & Decisões de Produto

O núcleo da jogabilidade do Horde Runner é a movimentação discreta entre faixas (lanes) sob avanço contínuo por distância. Esta história (M1) valida essa interação essencial usando geometria greybox (cubos e pista) antes da adição de assets finais ou combate.

### Decisões Fundamentais:
1. **Contagem Dinâmica de Lanes:** O número de lanes começa em 2 (padrão), mas o sistema é flexível a qualquer $N \ge 1$, suportando $N=3$ sem alterações de código.
2. **Cálculo Centrado no Eixo X:** A pista fica centrada em $X = 0$. As posições de centro de cada lane são calculadas deterministicamente:
   $$\text{LaneCenterX}(i) = \left(i - \frac{N - 1}{2}\right) \times \text{LaneWidth}$$
3. **Movimentação Discreta com Interpolação:** A troca de lane é uma decisão lógica instantânea (`TargetLaneIndex`), interpolada suavemente no transform para manter feedback visual contínuo.
4. **Avanço por Distância:** O corredor avança no eixo $Z$ a uma velocidade constante. A distância percorrida é acumulada em metros (`DistanceTraveled`).
5. **Entrada Dual:** Suporte a swipe horizontal em touch screens (mobile) e teclas `A`/`D` ou setas direcionais no Editor/Desktop via Novo Input System.

---

## 2. Arquitetura & Fluxo de Componentes

```mermaid
flowchart TD
    Input[ILaneInput\nStandaloneLaneInput] -->|OnMoveRequested -1 / +1| Mover[LaneMover]
    Layout[LaneLayoutDefinition SO\nLaneLayout Core] -.->|GetLaneCenterX| Mover
    Mover -->|Atualiza X| Transform[General Transform]
    Scroller[TrackScroller] -->|Atualiza Z e DistanceTraveled| Transform
    Bus[IEventBus] <--|Dispara LaneChangedEvent| Mover
    Camera[Presentation Camera] -->|Acompanha Z| Transform
```

---

## 3. Sub-Tarefas & Marcos de Execução (Linear Tasks)

### Marco 1 · [NEX-526] `LaneLayout` & `LaneLayoutDefinition` (ScriptableObject + Cálculo de Centro)
```dispatch
needs: executar
faixa: forte
runtime: low
verify: tools/unity compile && tools/unity test-edit
```
- **Contratos:**
  - `Game.Core.LaneLayout`: Expansão da struct em `Game.Core` com método puro `float GetLaneCenterX(int laneIndex, float laneWidth)`. Validação de índice e contagem.
  - `Game.Data.LaneLayoutDefinition`: `ScriptableObject` em `Game.Data` expondo `laneCount` (default 2) e `laneWidth` (default 2.0f), retornando a struct `LaneLayout`.
- **Testes:**
  - `Game.Tests.EditMode.LaneLayoutTests`: Validação para $N=2$ (centros em -1.0f e +1.0f), $N=3$ (centros em -2.0f, 0.0f, +2.0f), limites e exceções.

---

### Marco 2 · [NEX-527] `ILaneInput` com Suporte a Swipe Mobile e Teclado
```dispatch
needs: executar
faixa: forte
runtime: medium
verify: tools/unity compile && tools/unity test-edit
```
- **Contratos:**
  - `Game.Core.ILaneInput`: Interface em `Game.Core` expondo `event Action<int> MoveRequested` (onde -1 = esquerda, +1 = direita) e método de polling/leitura.
  - `Game.Infrastructure.StandaloneLaneInput`: Implementação em `Game.Infrastructure` escutando Input System (`actions.FindAction("Move")`) e detecção de swipe horizontal por delta de touch/cursor.
- **Testes:**
  - `Game.Tests.EditMode.LaneInputTests`: Validação lógica do filtro de swipe (threshold mínimo, rejeição de swipe vertical/ruído).

---

### Marco 3 · [NEX-528] `LaneMover` e `TrackScroller` (Avanço e Troca Suave de Lane)
```dispatch
needs: executar
faixa: forte
runtime: medium
verify: tools/unity compile && tools/unity test-edit
```
- **Contratos:**
  - `Game.Gameplay.LaneMover`: MonoBehaviour em `Game.Gameplay` que recebe `ILaneInput` e `LaneLayout`, gerencia `CurrentLane` e `TargetLane`, e interpola a posição $X$.
  - `Game.Gameplay.TrackScroller`: MonoBehaviour em `Game.Gameplay` que move o objeto no eixo $Z$ a `forwardSpeed` m/s e acumula `DistanceTraveled`.
  - `Game.Core.LaneChangedEvent`: Struct em `Game.Core` disparada via `IEventBus` ao mudar de faixa.
- **Testes:**
  - `Game.Tests.EditMode.LaneMoverTests`: Validação lógica de troca de faixa (clamp de limites, não sair da lane 0 nem da lane max).
  - `Game.Tests.EditMode.TrackScrollerTests`: Validação de acúmulo de distância percorrida por delta time.

---

### Marco 4 · [NEX-529] Câmera Retrato e Cena Greybox via `SceneBuilder`
```dispatch
needs: executar
faixa: mecânico
runtime: low
verify: tools/unity compile && tools/unity test-edit
```
- **Contratos:**
  - `Game.Presentation.FollowCamera`: MonoBehaviour que trava offsets em $X, Y, Z$ acompanhando o General suavemente no avanço de fase.
  - `Game.Editor.SceneBuilder.BuildM1GreyboxScene()`: Método CLI que gera `Assets/_Game/Scenes/M1_Greybox.unity` montando pista com linhas divisórias de lane, cubo do General, `LaneMover`, `TrackScroller`, `LaneLayoutDefinition` e câmera retrato.

---

## 4. Portões de Aceite da História

1. Todos os PRs das tarefas integrados na branch `jamilzerati/nex-506-m1-corredor-greybox-com-lanes-discretas`.
2. `tools/unity compile` sem erros nem warnings de compilação.
3. `tools/unity test-edit` executando todos os testes automatizados com 100% de sucesso.
4. Cena `M1_Greybox.unity` gerável via CLI e pronta para execução.
