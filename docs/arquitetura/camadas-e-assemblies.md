---
cobre:
  - "Assets/_Game/Scripts/**/*.asmdef"
revisado: 2026-11-13
---
# Camadas e assemblies

O código de produção vive em sete assemblies. A referência entre eles só aponta para dentro: `Game.Core` não vê ninguém, e só `Game.Composition` (e o `Game.Editor`) vê todos. É o que mantém a regra de negócio testável em EditMode sem cena.

## Caminhos

| Assembly | Âncora | Referencia |
|---|---|---|
| `Game.Core` | `Assets/_Game/Scripts/Core/Game.Core.asmdef:noEngineReferences` | nada; C# puro |
| `Game.Meta` | `Assets/_Game/Scripts/Meta/Game.Meta.asmdef:noEngineReferences` | Core; C# puro |
| `Game.Data` | `Assets/_Game/Scripts/Data/Game.Data.asmdef:references` | Core |
| `Game.Infrastructure` | `Assets/_Game/Scripts/Infrastructure/Game.Infrastructure.asmdef:references` | Core, Data, Input System |
| `Game.Gameplay` | `Assets/_Game/Scripts/Gameplay/Game.Gameplay.asmdef:references` | Core, Data, Infrastructure |
| `Game.Presentation` | `Assets/_Game/Scripts/Presentation/Game.Presentation.asmdef:references` | Core, Data, Gameplay, Infrastructure, Meta |
| `Game.Composition` | `Assets/_Game/Scripts/Composition/Game.Composition.asmdef:references` | todos acima e VContainer |
| `Game.Editor` | `Assets/_Game/Scripts/Editor/Game.Editor.asmdef:references` | todos, só em Editor |

## Invariantes

- `Game.Core` e `Game.Meta` compilam com `noEngineReferences: true`: nenhum tipo de `UnityEngine` entra neles.
- `Game.Gameplay` não referencia `Game.Meta` nem `Game.Presentation`. O que a meta sabe chega ao gameplay por `RunConfig` e sai por `RunResult` e eventos do `IEventBus`.
- Tipo novo usado por dois assemblies mora no mais interno dos dois.

## Mexer junto

- Assembly novo ou referência nova → a seção Arquitetura do `AGENTS.md` e [composição e boot](composicao-e-boot.md).
- Contrato que o gameplay precisa da meta → campo em `RunConfig` ou evento, nunca referência de assembly.

## Divergências

- O `AGENTS.md` diz que `Game.Meta` serve Data e Infrastructure; nenhum dos dois o referencia. Quem usa a meta é Presentation, Composition e Editor.
- Serviços de mentira moram em `Game.Core` e são os usados em jogo: `Assets/_Game/Scripts/Core/Ads/MockAdService.cs:MockAdService` e `Assets/_Game/Scripts/Core/Analytics/MockAnalyticsService.cs:MockAnalyticsService` — ver [anúncios](../fluxos/anuncios.md).
- O canal `RunConfig` existe, mas na cena do jogo quase nada o lê — ver [início e fim de fase](../fluxos/inicio-e-fim-de-fase.md).
