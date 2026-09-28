# Plano de Implementação · NEX-508
> Data: 2026-09-27
> Issue: NEX-508 (M3 · Portões de perk aritméticos)
> Status: Pronto para Execução via /executar

---

## 1. Contexto & Decisões de Produto

A decisão de lane só tem relevância e impacto estratégico para o jogador se cada lane oferecer recompensas, penalidades ou riscos distintos. No *Horde Runner*, os **portões de perk** aparecem em pares ou conjuntos correspondentes ao layout de pistas discretas (`LaneLayout.LaneCount`). Ao guiar o General para uma lane e cruzar o portal, o perk correspondente é ativado instantaneamente, alterando o tamanho da tropa (`SquadController`) ou aplicando modificações aritméticas (`+N`, `xN`, `-N`, `÷N`).

### Decisões Fundamentais:
1. **Composição via `[SerializeReference]` sem Herança Rígida:**
   - Efeitos de perks implementam `IPerkEffect` em `Game.Core` (C# puro).
   - O ScriptableObject `PerkDefinition` em `Game.Data` armazena uma lista polimórfica de efeitos anotada com `[SerializeReference]`.
   - Permite combinar múltiplos efeitos no mesmo perk futuramente (ex.: `+5 soldados E +10% de velocidade de tiro`).
2. **Desacoplamento via Interface `ISquad`:**
   - `PerkContext` vive em `Game.Core` e opera sobre a abstração `ISquad`.
   - `SquadController` em `Game.Gameplay` implementa `ISquad`, preservando a invariante de arquitetura unidirecional: `Game.Core` nunca referencia MonoBehaviour nem UnityEngine.
3. **Pares de Portões e Exclusão Mútua (`GatePair`):**
   - Um `GatePair` (ou conjunto por lane) agrupa os portões distribuídos nas lanes em uma coordenada Z do percurso.
   - Cruzar qualquer portão consome imediatamente o par (`IsConsumed = true`), impedindo que o jogador ative múltiplos portões do mesmo par ou reative ao retroceder.
   - Emite `GateTriggeredEvent` via `IEventBus` para desacoplamento de áudio, VFX e telemetria.
4. **Conteúdo como Dados Legíveis (JSON → SO):**
   - Dados de perks são definidos em arquivos JSON sob `Content/Source/Perks/`.
   - O importador de Editor `PerkImporter` gera/atualiza os ScriptableObjects `PerkDefinition` em `Assets/_Game/Data/Perks/`.
   - Executável de forma headless via CLI através de `tools/unity import-content` (`Game.Editor.Cli.ImportContent`).
5. **Apresentação Visual e Montagem de Cena Greybox (`M3_Greybox`):**
   - `GateView` renderiza arcos/painéis greybox com identificação cromática (verde/azul para positivo, vermelho/laranja para penalidades) e rótulo numérico claro (`+5`, `x2`, `-3`, `÷2`).
   - `SceneBuilder.BuildM3GreyboxScene()` monta a cena jogável completa com pista, General, tropa de soldados, HUD e pares de portões ao longo da distância.

---

## 2. Armadilhas do Repositório

| Armadilha | Onde morde (arquivo/passo) | Regra a respeitar |
|---|---|---|
| `Game.Core` sem UnityEngine | `IPerkEffect.cs`, `PerkContext.cs` — Marco 1 | Não importar `UnityEngine` em `Game.Core`. O `PerkContext` deve referenciar `ISquad` e tipos primitivos/puros. |
| Divisão inteira truncada e divisão por zero | `DivideSoldiersEffect.cs`, `SquadController.cs` — Marco 1 | Divisor deve ser estritamente $> 0$. Divisão inteira trunca para baixo (`Mathf.FloorToInt` ou divisão inteira C#). Se resultado for zero, clamp em 0. |
| Re-gatilho de portões no mesmo par | `GatePair.cs` — Marco 2 | Ao disparar um portão, o `GatePair` inteiro deve marcar `IsConsumed = true` atomicamente antes de aplicar o efeito, prevenindo loops de colisão no mesmo frame. |
| Importador sem `.meta` consistente | `PerkImporter.cs` — Marco 3 | Assets gerados pelo importador devem usar caminhos canônicos e chamar `AssetDatabase.SaveAssets()` / `AssetDatabase.Refresh()` garantindo serialização `Force Text`. |
| SceneBuilder em Batchmode | `SceneBuilder.cs` — Marco 4 | A montagem de cena M3 deve rodar headless sem erros de GUI e salvar a cena com `.meta` válido em `Assets/_Game/Scenes/M3_Greybox.unity`. |

---

## 3. Árvore de Arquivos

- [NOVO] `Assets/_Game/Scripts/Core/ISquad.cs`
- [NOVO] `Assets/_Game/Scripts/Core/Perks/IPerkEffect.cs`
- [NOVO] `Assets/_Game/Scripts/Core/Perks/PerkContext.cs`
- [NOVO] `Assets/_Game/Scripts/Core/Perks/Effects/AddSoldiersEffect.cs`
- [NOVO] `Assets/_Game/Scripts/Core/Perks/Effects/MultiplySoldiersEffect.cs`
- [NOVO] `Assets/_Game/Scripts/Core/Perks/Effects/DivideSoldiersEffect.cs`
- [NOVO] `Assets/_Game/Scripts/Core/Events/GateTriggeredEvent.cs`
- [NOVO] `Assets/_Game/Scripts/Data/PerkDefinition.cs`
- [MODIFICAÇÃO] `Assets/_Game/Scripts/Gameplay/SquadController.cs`
- [NOVO] `Assets/_Game/Scripts/Gameplay/Gate.cs`
- [NOVO] `Assets/_Game/Scripts/Gameplay/GatePair.cs`
- [NOVO] `Content/Source/Perks/add_5.json`
- [NOVO] `Content/Source/Perks/add_10.json`
- [NOVO] `Content/Source/Perks/multiply_2.json`
- [NOVO] `Content/Source/Perks/subtract_3.json`
- [NOVO] `Content/Source/Perks/divide_2.json`
- [NOVO] `Assets/_Game/Scripts/Editor/PerkImporter.cs`
- [MODIFICAÇÃO] `Assets/_Game/Scripts/Editor/Cli.cs`
- [NOVO] `Assets/_Game/Scripts/Presentation/GateView.cs`
- [MODIFICAÇÃO] `Assets/_Game/Scripts/Editor/SceneBuilder.cs`
- [NOVO] `Assets/_Game/Scripts/Tests/EditMode/PerkEffectTests.cs`
- [NOVO] `Assets/_Game/Scripts/Tests/EditMode/GatePairTests.cs`
- [NOVO] `Assets/_Game/Scripts/Tests/EditMode/PerkImporterTests.cs`
- [NOVO] `Assets/_Game/Scripts/Tests/EditMode/GateViewTests.cs`
- [NOVO] `Assets/_Game/Scripts/Tests/EditMode/SceneBuilderM3Tests.cs`

---

## 4. Contratos e Interfaces

```csharp
// Game.Core
namespace Game.Core
{
    public interface ISquad
    {
        int SquadCount { get; }
        bool Add(int amount);
        bool Remove(int amount);
        bool Multiply(int factor);
        bool Divide(int divisor);
        bool SetCount(int newCount);
    }
}

namespace Game.Core.Perks
{
    public readonly struct PerkContext
    {
        public ISquad Squad { get; }
        public int LaneIndex { get; }
        public PerkContext(ISquad squad, int laneIndex = 0);
    }

    public interface IPerkEffect
    {
        string Description { get; }
        void Apply(PerkContext context);
    }
}

namespace Game.Core.Perks.Effects
{
    [System.Serializable]
    public class AddSoldiersEffect : IPerkEffect
    {
        public int Amount;
        public string Description => Amount >= 0 ? $"+{Amount}" : $"{Amount}";
        public void Apply(PerkContext context);
    }

    [System.Serializable]
    public class MultiplySoldiersEffect : IPerkEffect
    {
        public int Factor;
        public string Description => $"x{Factor}";
        public void Apply(PerkContext context);
    }

    [System.Serializable]
    public class DivideSoldiersEffect : IPerkEffect
    {
        public int Divisor;
        public string Description => $"÷{Divisor}";
        public void Apply(PerkContext context);
    }
}

namespace Game.Core.Events
{
    public readonly struct GateTriggeredEvent
    {
        public int LaneIndex { get; }
        public string PerkId { get; }
        public GateTriggeredEvent(int laneIndex, string perkId);
    }
}

// Game.Data
namespace Game.Data
{
    [CreateAssetMenu(fileName = "PerkDefinition", menuName = "Horde Runner/Data/Perk Definition")]
    public class PerkDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [SerializeReference] public List<IPerkEffect> Effects = new();
        public void Apply(PerkContext context);
    }
}

// Game.Gameplay
namespace Game.Gameplay
{
    public class Gate : MonoBehaviour
    {
        public int LaneIndex { get; set; }
        public PerkDefinition Perk { get; set; }
        public GatePair ParentPair { get; set; }
    }

    public class GatePair : MonoBehaviour
    {
        public bool IsConsumed { get; }
        public bool TryTrigger(int laneIndex, ISquad squad, IEventBus eventBus = null);
    }
}

// Game.Presentation
namespace Game.Presentation
{
    public class GateView : MonoBehaviour
    {
        public void SetPerk(PerkDefinition perk);
        public void SetConsumed(bool consumed);
    }
}
```

---

## 4.5 Linear Overlay

- **História (issue-pai):** `NEX-508` (M3 · Portões de perk aritméticos)
- **Sub-issues deste plano:**
  1. `NEX-549`: IPerkEffect, PerkContext, PerkDefinition com SerializeReference e efeitos aritméticos [Priority: High]
  2. `NEX-550`: Gate, GatePair e deteccao de passagem por lane com eventos [Priority: High]
  3. `NEX-551`: Importador de conteudo JSON para ScriptableObjects de Perks via CLI [Priority: High]
  4. `NEX-552`: GateView e montagem de cena M3 Greybox via SceneBuilder [Priority: High]
- **Modelo Git:**
  - Branch da história: `jamilzerati/nex-508-m3-portoes-de-perk-aritmeticos` (baseada em `main`)
  - PR de cada tarefa: `branch da tarefa` → `branch da história` (`jamilzerati/nex-508-...`)

---

## 5. Divisão de Execução por Passo

| Faixa | Passos | Por quê |
|---|---|---|
| **Modelo forte, obrigatório** | Marco 1, Marco 2 | Operações aritméticas de tropa (`+`, `x`, `-`, `÷`), serialização polimórfica com `[SerializeReference]` e consumo atômico de pares de portões possuem modos de falha silenciosa (arredondamento indevido, mutação duplicada, re-gatilho de portões adjacentes). |
| **Mecânico, qualquer modelo** | Marco 3, Marco 4 | Parser JSON para SO e montagem estática de cena greybox via SceneBuilder; qualquer erro de schema ou referência nula falha de forma visível e imediata. |

---

## 6. Checklist de Execução

### Marco 1 · [NEX-549] `IPerkEffect`, `PerkContext`, `PerkDefinition` com `[SerializeReference]` e efeitos aritméticos
```dispatch
needs: executar
faixa: forte
runtime: medium
verify: tools/unity compile && tools/unity test-edit
```
- **Ação**: Criar `ISquad.cs`, `IPerkEffect.cs`, `PerkContext.cs`, `AddSoldiersEffect.cs`, `MultiplySoldiersEffect.cs`, `DivideSoldiersEffect.cs` em `Game.Core`, atualizar `SquadController.cs` em `Game.Gameplay` para implementar `ISquad`, criar `PerkDefinition.cs` em `Game.Data`, e `PerkEffectTests.cs` em `Game.Tests.EditMode`.
- **Lógica de Negócios / Responsabilidade**:
  - `ISquad`: interface em `Game.Core` com `SquadCount`, `Add`, `Remove`, `Multiply`, `Divide`, `SetCount`.
  - `SquadController`: implementa `ISquad` sem quebrar compatibilidade existente.
  - `PerkContext`: struct pura contendo `ISquad Squad` e `int LaneIndex`.
  - `IPerkEffect`: interface pura com `string Description { get; }` e `void Apply(PerkContext context)`.
  - `AddSoldiersEffect`: adiciona $N$ se $N > 0$, ou remove $|N|$ se $N < 0$.
  - `MultiplySoldiersEffect`: multiplica tropa por $M$ ($M \ge 0$).
  - `DivideSoldiersEffect`: divide tropa por $D$ ($D > 0$), com truncamento e clamp em 0.
  - `PerkDefinition`: ScriptableObject com `Id`, `DisplayName`, `List<IPerkEffect>` com `[SerializeReference]`, e método `Apply(PerkContext)`.
- **Seam Público**: `IPerkEffect.Apply`, `PerkDefinition.Apply`, `ISquad`
- **Marco de PR**: Marco 1 + `NEX-549`
- **Runtime**: medium

---

### Marco 2 · [NEX-550] `Gate`, `GatePair` e detecção de passagem por lane com eventos
```dispatch
needs: executar
faixa: forte
runtime: medium
verify: tools/unity compile && tools/unity test-edit
```
- **Ação**: Criar `GateTriggeredEvent.cs` em `Game.Core.Events`, `Gate.cs` e `GatePair.cs` em `Game.Gameplay`, e `GatePairTests.cs` em `Game.Tests.EditMode`.
- **Lógica de Negócios / Responsabilidade**:
  - `GateTriggeredEvent`: struct imutável com `LaneIndex` e `PerkId`.
  - `Gate`: componente de portão individual associado a `LaneIndex`, referência para `PerkDefinition` e referência para `GatePair`. Possui `Collider` trigger para detecção em PlayMode.
  - `GatePair`: gerencia conjunto de portões (um por lane). Método `TryTrigger(int laneIndex, ISquad squad, IEventBus eventBus = null)`:
    - Se já consumido (`IsConsumed == true`), retorna `false`.
    - Localiza o portão da `laneIndex`. Se existir e tiver perk, executa `Perk.Apply(context)`.
    - Marca `IsConsumed = true`.
    - Dispara `GateTriggeredEvent`.
    - Retorna `true`.
- **Seam Público**: `GatePair.TryTrigger`, `GatePair.IsConsumed`
- **Marco de PR**: Marco 2 + `NEX-550`
- **Runtime**: medium

---

### Marco 3 · [NEX-551] Importador de conteúdo JSON para ScriptableObjects de Perks via CLI
```dispatch
needs: executar
faixa: mecânico
runtime: low
verify: tools/unity compile && tools/unity test-edit && tools/unity import-content
```
- **Ação**: Criar arquivos JSON em `Content/Source/Perks/`, criar `PerkImporter.cs` em `Game.Editor`, atualizar `Game.Editor.Cli.ImportContent()`, e criar `PerkImporterTests.cs` em `Game.Tests.EditMode`.
- **Lógica de Negócios / Responsabilidade**:
  - Arquivos JSON: `add_5.json`, `add_10.json`, `multiply_2.json`, `subtract_3.json`, `divide_2.json` definindo id, displayName e efeitos.
  - `PerkImporter.ImportAll()`: lê arquivos JSON em `Content/Source/Perks/`, instancia ou atualiza `PerkDefinition` em `Assets/_Game/Data/Perks/`, atribui os efeitos polimórficos serializados, e salva os assets via `AssetDatabase`.
  - Conectar no CLI: `Cli.ImportContent()` invoca `PerkImporter.ImportAll()` e sai com código 0.
- **Seam Público**: `PerkImporter.ImportAll`, `Game.Editor.Cli.ImportContent`
- **Marco de PR**: Marco 3 + `NEX-551`
- **Runtime**: low

---

### Marco 4 · [NEX-552] `GateView` e montagem de cena M3 Greybox via `SceneBuilder`
```dispatch
needs: executar
faixa: mecânico
runtime: low
verify: tools/unity compile && tools/unity test-edit
```
- **Ação**: Criar `GateView.cs` em `Game.Presentation`, atualizar `SceneBuilder.cs` adicionando `BuildM3GreyboxScene()`, e criar testes em `Game.Tests.EditMode/GateViewTests.cs` e `SceneBuilderM3Tests.cs`.
- **Lógica de Negócios / Responsabilidade**:
  - `GateView`: componente MonoBehaviour de apresentação visual do portão. Configura texto formatado (`+5`, `x2`, etc.) e cor do material (verde/azul para ganhos de tropa, vermelho/laranja para perdas). Reage a `SetConsumed(true)` escurecendo ou desativando visuais.
  - `SceneBuilder.BuildM3GreyboxScene()`: monta `Assets/_Game/Scenes/M3_Greybox.unity` com pista, General, tropa com soldados e HUD de contagem (M2), e posiciona 3 pares de portões (`GatePair` com `GateView`) em distâncias estratégicas (ex.: Z=25, Z=60, Z=100) utilizando os perks importados.
- **Seam Público**: `GateView.SetPerk`, `SceneBuilder.BuildM3GreyboxScene`
- **Marco de PR**: Marco 4 + `NEX-552`
- **Runtime**: low

---

## 7. Portões de Aceite da História

1. Todos os 4 PRs de tarefas integrados na branch `jamilzerati/nex-508-m3-portoes-de-perk-aritmeticos`.
2. `tools/unity compile` sem erros de compilação.
3. `tools/unity test-edit` executando todos os testes automatizados com 100% de sucesso.
4. `tools/unity import-content` importando perks JSON para ScriptableObjects com integridade.
5. Cena `M3_Greybox.unity` gerável via CLI e validada.
