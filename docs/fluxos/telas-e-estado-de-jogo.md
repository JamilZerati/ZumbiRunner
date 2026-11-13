---
cobre:
  - "Assets/_Game/Scripts/Presentation/UI/UIManager.cs"
  - "Assets/_Game/Scripts/Presentation/UI/UILayer.cs"
  - "Assets/_Game/Scripts/Presentation/UI/UILayerType.cs"
  - "Assets/_Game/Scripts/Presentation/UI/UIWindow.cs"
  - "Assets/_Game/Scripts/Presentation/UI/PauseController.cs"
  - "Assets/_Game/Scripts/Presentation/UI/PauseView.cs"
  - "Assets/_Game/Scripts/Presentation/UI/OptionsView.cs"
  - "Assets/_Game/Scripts/Presentation/UI/TutorialGuideView.cs"
  - "Assets/_Game/Scripts/Presentation/Settings/**"
  - "Assets/_Game/Scripts/Presentation/DebugTextOverlay.cs"
  - "Assets/_Game/Scripts/Gameplay/Tutorial/**"
  - "Assets/_Game/Scripts/Core/Diagnostics/**"
revisado: 2026-11-13
---
# Telas e estado de jogo

A interface é uma pilha de camadas (Hub, HUD de jogo, tutorial, modal) com janelas registradas no `UIManager`. Quem decide o que aparece é o estado de jogo: a UI reage a `GameStateChangedEvent`, e as telas pedem transição em vez de se abrirem umas às outras.

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Reação ao estado | `Assets/_Game/Scripts/Presentation/UI/UIManager.cs:OnGameStateChanged` | Hub mostra a camada do Hub; Run mostra o HUD; Paused e Result ligam a camada modal |
| Abrir janela | `Assets/_Game/Scripts/Presentation/UI/UIManager.cs:OpenWindow` | janela modal bloqueia a interação das outras camadas |
| Pausa | `Assets/_Game/Scripts/Presentation/UI/PauseController.cs:PauseGame` | transiciona para `Paused` e zera a escala de tempo |
| Sair para o Hub | `Assets/_Game/Scripts/Presentation/UI/PauseController.cs:QuitToHub` | restaura o tempo e transiciona para `Hub` |
| Opções | `Assets/_Game/Scripts/Presentation/UI/OptionsView.cs:Initialize` | áudio, idioma e modo de disparo do poder do General |
| Passo de tutorial | `Assets/_Game/Scripts/Gameplay/Tutorial/TutorialGuideController.cs:CheckTutorialStep` | escolhe a instrução pela fase |

## Invariantes

- Janela só abre se estiver registrada; o `UIManager` registra sozinho as que são filhas dele na cena.
- Com modal aberto, Hub, HUD e tutorial ficam sem interação.
- A pausa não acontece se a máquina de estados recusar a transição.

## Mexer junto

- Estado novo → `OnGameStateChanged` e [composição e boot](../arquitetura/composicao-e-boot.md).
- Janela nova → o `SceneBuilder` precisa criá-la sob o `UIManager` e o composer precisa inicializá-la.
- Opção nova → a interface em `Presentation/Settings` e quem lê a opção em jogo.

## Divergências

- `Victory` e `Defeat` não têm reação: `OnGameStateChanged` trata só Hub, Run, Paused e Result. Ao fim da fase o HUD continua na tela com a pista parada — ver [início e fim de fase](inicio-e-fim-de-fase.md).
- O tutorial nunca aparece: `CheckTutorialStep` e `CompleteTutorialStep` não têm chamador. A janela e o controlador são criados e ligados pelo composer, e ninguém pede um passo.
- As opções são gravadas em `PlayerPrefs` por classes próprias (`AudioSettings`, `LocalizationSettings`, `PlayerPrefsAbilitySettings`), fora do save da meta. É o único estado do jogador que persiste hoje.
- O composer cria `AudioSettings` e `LocalizationSettings` novos para a tela de opções; o `GameLifetimeScope` registra outros. Nenhum componente de jogo recebe essas instâncias para aplicar o volume ou o idioma escolhidos.
- `DebugTextOverlay` está nas cenas de combate, inclusive na M6 que vai para o build.
