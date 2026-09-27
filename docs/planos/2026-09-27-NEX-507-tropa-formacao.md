# Plano de Implementação · NEX-507
> Data: 2026-09-27
> Issue: NEX-507 (M2 · Tropa do General (contagem = HP) e formação)
> Status: Pronto para Execução via /executar

---

## 1. Contexto & Decisões de Produto

O crescimento e encolhimento visível da tropa é o feedback primário de progresso da fase no *Horde Runner*. O número de soldados na tropa funciona como a "barra de vida" dinâmica do General: cada soldado absorve impacto contra zumbis/obstáculos, e o General só é derrotado quando a contagem chega a zero e o próprio líder é atingido.

### Decisões Fundamentais:
1. **Contagem = Vida (HP):** A tropa começa com uma contagem base configurável (ex.: 1 soldado ou valor do level). Alterações na tropa (`+N`, `-N`, `xN`, `÷N`) acontecem imediatamente no estado lógico.
2. **Formação Pura e Determinística (`FormationSolver`):**
   - O cálculo das posições relativas dos soldados é uma função pura em `Game.Core` (sem UnityEngine), baseada em slots ordenados atrás do General.
   - Os soldados distribuem-se em linhas horizontais compactas ou em cunha simétrica, respeitando a largura da pista (`LaneWidth`) para não vazar para fora do espaço das lanes.
   - Posição relativa: $X$ centrado no líder, $Z$ negativo (atrás do líder).
3. **Desacoplamento por Eventos (`SquadSizeChangedEvent`):**
   - Qualquer mutação na contagem da tropa emite `SquadSizeChangedEvent(previousCount, newCount)` via `IEventBus`.
   - O HUD e o sistema visual escutam esse evento sem acoplamento direto com o controlador de gameplay.
4. **Pooling de Soldados (`SoldierView`):**
   - Como portões multiplicadores e hordas adicionam e removem dezenas de soldados rapidamente, a instanciação direta causaria picos de Garbage Collection (GC) e engasgos de frame rate no Android.
   - Soldados visuais usam `IObjectPool<SoldierView>` com reutilização ativa/inativa.
5. **Câmera e HUD Greybox:**
   - A câmera de retrato (M1) continua acompanhando o General no avanço por distância.
   - Um contador visual numérico no HUD no topo ou sobre o General exibe o tamanho da tropa atualizado em tempo real.

---

## 2. Armadilhas do Repositório

| Armadilha | Onde morde (arquivo/passo) | Regra a respeitar |
|---|---|---|
| `Game.Core` sem UnityEngine | `FormationSolver.cs` — Marco 1 | Não usar `Vector2`/`Vector3` do Unity em `Game.Core`. Definir `FormationPosition(float X, float Z)` puro. |
| Invariante de contagem não-negativa | `SquadController.cs` — Marco 2 | `SquadCount` nunca pode ser inferior a 0. `Remove` ou divisão com resultado fracionário deve fazer clamp e truncamento determinístico (inteiro). |
| Vazamento de instâncias de pool | `SquadVisualController.cs` — Marco 3 | Ao reduzir tropa ou reiniciar cena, todas as instâncias ativas de `SoldierView` devem ser devidamente devolvidas ao pool (`Return()`). |
| SceneBuilder em Batchmode | `SceneBuilder.cs` — Marco 4 | Todo método de montagem CLI deve rodar headless sem lançar exceções de GUI e salvar a cena no disco com `.meta` válido. |

---

## 3. Árvore de Arquivos

- [NOVO] `Assets/_Game/Scripts/Core/FormationPosition.cs`
- [NOVO] `Assets/_Game/Scripts/Core/FormationSolver.cs`
- [NOVO] `Assets/_Game/Scripts/Core/Events/SquadSizeChangedEvent.cs`
- [NOVO] `Assets/_Game/Scripts/Gameplay/SquadController.cs`
- [NOVO] `Assets/_Game/Scripts/Presentation/SoldierView.cs`
- [NOVO] `Assets/_Game/Scripts/Presentation/SquadVisualController.cs`
- [NOVO] `Assets/_Game/Scripts/Presentation/SquadCountHud.cs`
- [MODIFICAÇÃO] `Assets/_Game/Scripts/Editor/SceneBuilder.cs`
- [NOVO] `Assets/_Game/Scripts/Tests/EditMode/FormationSolverTests.cs`
- [NOVO] `Assets/_Game/Scripts/Tests/EditMode/SquadControllerTests.cs`
- [NOVO] `Assets/_Game/Scripts/Tests/EditMode/SoldierViewTests.cs`
- [NOVO] `Assets/_Game/Scripts/Tests/EditMode/SquadCountHudTests.cs`
- [NOVO] `Assets/_Game/Scripts/Tests/EditMode/SceneBuilderM2Tests.cs`

---

## 4. Contratos e Interfaces

```csharp
// Game.Core
public readonly struct FormationPosition
{
    public float X { get; }
    public float Z { get; }
    public FormationPosition(float x, float z);
}

public static class FormationSolver
{
    public static FormationPosition[] CalculatePositions(int count, float spacing = 0.5f, int maxPerRow = 5);
}

namespace Game.Core.Events
{
    public readonly struct SquadSizeChangedEvent
    {
        public int PreviousCount { get; }
        public int NewCount { get; }
        public SquadSizeChangedEvent(int previousCount, int newCount);
    }
}

// Game.Gameplay
public class SquadController : MonoBehaviour
{
    public int SquadCount { get; private set; }
    public void Initialize(int initialCount, IEventBus eventBus = null);
    public void Add(int amount);
    public void Remove(int amount);
    public void Multiply(int factor);
    public void SetCount(int count);
}

// Game.Presentation
public class SoldierView : MonoBehaviour
{
    public Vector3 TargetOffset { get; set; }
    public void SetTargetOffset(Vector3 offset);
    public void UpdatePosition(Vector3 leaderPosition, float deltaTime, float followSpeed = 15f);
}

public class SquadVisualController : MonoBehaviour
{
    public void Initialize(SquadController controller, IObjectPool<SoldierView> pool, IEventBus eventBus = null);
}

public class SquadCountHud : MonoBehaviour
{
    public void Initialize(IEventBus eventBus);
    public void SetCount(int count);
}
```

---

## 4.5 Linear Overlay

- **História (issue-pai):** `NEX-507` (M2 · Tropa do General (contagem = HP) e formação)
- **Sub-issues deste plano:**
  1. `NEX-531`: FormationSolver puro e cálculo de posições em formação [Priority: High]
  2. `NEX-532`: SquadController e SquadSizeChangedEvent (operações e contagem da tropa) [Priority: High]
  3. `NEX-533`: SoldierView e apresentação visual da tropa com pooling [Priority: High]
  4. `NEX-534`: SquadCountHud e montagem de cena M2 Greybox via SceneBuilder [Priority: High]
- **Modelo Git:**
  - Branch da história: `jamilzerati/nex-507-m2-tropa-do-general-contagem-hp-e-formacao` (baseada em PR 12 `jamilzerati/nex-506-m1-corredor-greybox-com-lanes-discretas`)
  - PR de cada tarefa: `branch da tarefa` → `branch da história` (`jamilzerati/nex-507-...`)

---

## 5. Divisão de Execução por Passo

| Faixa | Passos | Por quê |
|---|---|---|
| **Modelo forte, obrigatório** | Marco 1, Marco 2, Marco 3 | Cálculos de offset relativo da tropa e operações matemáticas de contagem têm modos de falha silenciosa (soldados sobrepostos, índices fora dos limites de lane, underflow em remoção de tropa, vazamento de instâncias no pool). |
| **Mecânico, qualquer modelo** | Marco 4 | Bind de texto no HUD e montagem de cena estática greybox via API de Editor e GameObject primitivo; qualquer erro quebra a montagem imediatamente. |

---

## 6. Checklist de Execução

### Marco 1 · [NEX-531] `FormationSolver` puro e cálculo de posições em formação
```dispatch
needs: executar
faixa: forte
runtime: low
verify: tools/unity compile && tools/unity test-edit
```
- **Ação**: Criar `FormationPosition.cs` e `FormationSolver.cs` em `Game.Core`, e `FormationSolverTests.cs` em `Game.Tests.EditMode`.
- **Lógica de Negócios / Responsabilidade**:
  - `FormationPosition`: struct imutável pura com coordenadas $(X, Z)$.
  - `FormationSolver.CalculatePositions(int count, float spacing = 0.5f, int maxPerRow = 5)`: retorna array de posições ordenadas simetricamente em linhas atrás do líder $(Z < 0)$. Centrado em $X=0$.
  - Se $count \le 0$, retorna array vazio.
- **Seam Público**: `FormationSolver.CalculatePositions`
- **Marco de PR**: Marco 1 + `NEX-531`
- **Runtime**: low

---

### Marco 2 · [NEX-532] `SquadController` e `SquadSizeChangedEvent` (operações e contagem da tropa)
```dispatch
needs: executar
faixa: forte
runtime: medium
verify: tools/unity compile && tools/unity test-edit
```
- **Ação**: Criar `SquadSizeChangedEvent.cs` em `Game.Core.Events`, `SquadController.cs` em `Game.Gameplay`, e `SquadControllerTests.cs` em `Game.Tests.EditMode`.
- **Lógica de Negócios / Responsabilidade**:
  - `SquadSizeChangedEvent`: struct imutável `(int PreviousCount, int NewCount)`.
  - `SquadController`: gerencia contagem inteira $\ge 0$. Métodos `Add`, `Remove`, `Multiply`, `SetCount`.
  - Clamp estrito em 0 (nunca negativo). Dispara `SquadSizeChangedEvent` via `IEventBus` somente se o valor efetivamente mudar.
- **Seam Público**: `SquadController.Add`, `SquadController.Remove`, `SquadController.Multiply`, `SquadController.SquadCount`
- **Marco de PR**: Marco 2 + `NEX-532`
- **Runtime**: medium

---

### Marco 3 · [NEX-533] `SoldierView` e apresentação visual da tropa com pooling
```dispatch
needs: executar
faixa: forte
runtime: medium
verify: tools/unity compile && tools/unity test-edit
```
- **Ação**: Criar `SoldierView.cs` e `SquadVisualController.cs` em `Game.Presentation`, e `SoldierViewTests.cs` / `SquadVisualTests.cs` em `Game.Tests.EditMode`.
- **Lógica de Negócios / Responsabilidade**:
  - `SoldierView`: MonoBehaviour simples representando um soldado; interpola posição local em direção ao `TargetOffset` com suavização.
  - `SquadVisualController`: gerencia a lista de `SoldierView` instanciados via `IObjectPool<SoldierView>`.
  - Ao receber alteração de contagem ou atualização de formação, aluga instâncias adicionais do pool ou devolve instâncias excedentes.
  - Atualiza o `TargetOffset` de cada soldado de acordo com o `FormationSolver.CalculatePositions`.
- **Seam Público**: `SquadVisualController.SynchronizeSquad`, `SoldierView.UpdatePosition`
- **Marco de PR**: Marco 3 + `NEX-533`
- **Runtime**: medium

---

### Marco 4 · [NEX-534] `SquadCountHud` e montagem de cena M2 Greybox via `SceneBuilder`
```dispatch
needs: executar
faixa: mecânico
runtime: low
verify: tools/unity compile && tools/unity test-edit
```
- **Ação**: Criar `SquadCountHud.cs` em `Game.Presentation`, adicionar `SceneBuilder.BuildM2GreyboxScene()` em `Game.Editor.SceneBuilder`, e testes em `Game.Tests.EditMode`.
- **Lógica de Negócios / Responsabilidade**:
  - `SquadCountHud`: exibe contador numérico da tropa. Atualiza dinamicamente via `SquadSizeChangedEvent` ou chamada direta.
  - `SceneBuilder.BuildM2GreyboxScene()`: gera a cena `Assets/_Game/Scenes/M2_Greybox.unity` montando o corredor com General, `LaneMover`, `TrackScroller`, `SquadController`, pool visual de soldados (`SoldierView`) e HUD de contagem.
- **Seam Público**: `SquadCountHud.SetCount`, `SceneBuilder.BuildM2GreyboxScene`
- **Marco de PR**: Marco 4 + `NEX-534`
- **Runtime**: low

---

## 7. Portões de Aceite da História

1. Todos os PRs integrados na branch `jamilzerati/nex-507-m2-tropa-do-general-contagem-hp-e-formacao`.
2. `tools/unity compile` sem erros de compilação.
3. `tools/unity test-edit` executando todos os testes automatizados com 100% de sucesso.
4. Cena `M2_Greybox.unity` gerável via CLI e validada.
