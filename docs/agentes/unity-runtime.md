# Padrões de runtime Unity

Armadilhas do motor que já custaram bugs aqui, e o padrão que as evita.

## Física e colisão

- Colliders filhos de um objeto com Rigidbody formam **um corpo composto** com ele, e `OnTrigger*` dispara também nos scripts do dono do Rigidbody. Foi a causa de https://github.com/JamilZerati/ZumbiRunner/issues/47: projéteis filhos do General faziam cada acerto contar como contato com a tropa.
- Trigger só detecta trigger se **um dos lados tiver Rigidbody**. Projéteis e zumbis são triggers; cada um tem Rigidbody cinemático próprio, sem gravidade.
- Padrão: toda entidade que colide (projétil, zumbi, jaula, portão) tem camada própria e Rigidbody cinemático, e o pool dela fica na raiz da cena. Sob o General ficam só o próprio General e os soldados (camada `SquadBody`).
- Camadas e matriz de colisão: spec `docs/planos/2026-09-29-horde-runner-gameplay-meta-liveops.md`, seção 2.3.

## Pooling

- `ObjectPool<T>` (`Game.Infrastructure`, em `Assets/_Game/Scripts/Infrastructure/Pooling/`): `Rent` → `Initialize(...)` → uso → `Recycle`.
- `Initialize` restaura **todo** o estado: vida, flags de acerto, colliders habilitados, Rigidbody configurado, status limpos. Objeto reciclado volta como novo.
- `Recycle` é idempotente (guarda `IsActiveInPool`) e devolve ao pool uma vez só.

## Ciclo de vida e testes

- Em EditMode, `Awake`, `Start`, `Update` e callbacks de física não rodam sozinhos. Por isso a lógica fica em métodos públicos (`Initialize`, `Tick(float deltaTime)`, `HandleTrigger`) e os testes EditMode chamam esses métodos direto.
- Comportamento que depende de física real (contato, trigger, camadas) tem teste PlayMode em `Game.Tests.PlayMode`: monte os objetos no teste, avance frames com `yield return null` / `WaitForFixedUpdate` e destrua tudo no `TearDown`, incluindo pools criados na raiz.
- Regra pura (`Game.Core`, `Game.Meta`) não precisa de cena: teste NUnit simples.

## `[SerializeReference]`

O asset guarda o tipo concreto por nome (`type: {class: AddSoldiersEffect, ns: Game.Core.Perks.Effects, asm: Game.Core}`). Renomear classe, namespace ou assembly de um efeito, comportamento ou recompensa quebra os assets existentes. Ao renomear: rode `import-content` para regenerar, ou anote o tipo com `[UnityEngine.Scripting.APIUpdating.MovedFrom]`.

## Fiação pelo `SceneBuilder`

Cenas são montadas por código e referências são ligadas via `SerializedObject.FindProperty("nomeDoCampo")`. Nome de campo errado ou renomeado é ignorado em silêncio (detalhes em `guardrails-build.md`, "Cena acompanha o código"). Asset carregado só depois de `EditorSceneManager.NewScene`: abrir cena em modo `Single` descarrega assets sem referência.

## Comunicação entre sistemas

Sistemas conversam por eventos no `IEventBus` (`Game.Core`, em `Assets/_Game/Scripts/Core/Events/`) e por interfaces de `Game.Core`. O gameplay publica sinais e não conhece a meta; valores de jogo chegam por dados (`RunConfig`, definições importadas).
