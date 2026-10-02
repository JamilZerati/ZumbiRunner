# Editor vivo (`unity` CLI + Pipeline)

Para simular o jogo de verdade, o CLI `unity` dirige um Editor residente pelo pacote `com.unity.pipeline`: entra em Play, roda C# dentro do processo, lê estado e memória em runtime, simula input e captura a tela. A skill `unity-cli` documenta o CLI inteiro; aqui ficam as regras deste repo e o que foi verificado em 6000.6.3f1 com Pipeline 0.8.0-exp.1.

## Quando usar

- **Editor vivo** para observar o jogo rodando: bug que só aparece jogando, conferir spawn, colisão e portões numa cena real, medir memória e frame time, capturar a tela como evidência, iterar com `eval` sem recompilar.
- **Harness `tools/unity`** para o portão de verificação. Observação no Editor vivo é diagnóstico e evidência complementar; tarefa pronta continua sendo o `verify` verde.
- **Fontes** para autoria: cena vem do `SceneBuilder`, números de `Content/Source/`. Mudança persistida pelo Editor vivo (`save_scene`, `create_gameobject` fora do Play) some na próxima regeneração; leve o achado para a fonte. O Play mode descarta o que mudou ao sair.

## Subir, dirigir, fechar

```bash
# Editor headless residente: sem -quit ele fica servindo o Pipeline. Rode com run_in_background.
# Caminho do Unity.exe: campo location de `unity editors --installed --json`, na versão de ProjectVersion.txt.
"<Unity.exe>" -batchmode -projectPath "<pasta>" -logFile .artifacts/editor-vivo.log

# Pronto quando lista comandos. Editor batchmode não aparece em `unity status`.
unity command --project-path "<pasta>" --tags

unity close "<pasta>"   # encerra sem salvar
```

Um Editor GUI aberto pelo humano (`unity open <pasta>`) serve igual e aparece em `unity status` com estado `ready`.

## Simular uma run

```bash
U="unity command --project-path <pasta> --result-only"
$U eval 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Game/Scenes/M6_Greybox.unity"); return "ok";'
$U editor_play --timeout 120
$U eval 'return UnityEngine.Object.FindObjectsByType<Game.Gameplay.EnemyController>(UnityEngine.FindObjectsSortMode.None).Length;'
$U get_performance_stats                      # memória (alocado, reservado, mono) e frame time
$U capture_game_view --source camera --width 540 --height 960 --save_path .artifacts/run.png
$U editor_stop
```

Abra a cena pelo caminho em `Assets/_Game/Scenes/`. Para descobrir parâmetros: `unity command --query <nome> --json`. Tags úteis aqui: `editor/playmode`, `scripts/eval`, `runtime` (`set_timescale`, `simulate_key`, `simulate_pointer`), `observability/performance`, `observability/console`, `capture`, `wait` (`wait_for` espera uma condição no servidor), `tests`.

## Armadilhas verificadas

- **Um Unity por pasta.** O Editor residente trava o projeto: `tools/unity` na mesma pasta falha enquanto ele estiver aberto. Feche com `unity close` antes do portão, ou suba o Editor vivo num worktree. Com mais de um Editor no ar, passe sempre `--project-path`.
- **Headless não desenha.** `get_performance_stats` traz memória e frame time reais, mas draw calls e triângulos ficam 0, e os frames correm sem teto (milhares por segundo). Para medir render ou ter cadência de aparelho, use `set_target_framerate 60` ou um Editor GUI.
- **Captura.** Sem janela Game, `capture_game_view` precisa de `--source camera`. O `save_path` é relativo a `Assets/`: `.artifacts/run.png` grava em `Assets/.artifacts/`, pasta que o Unity não importa mas o git enxerga. Mova o PNG para o `.artifacts/` da raiz e apague `Assets/.artifacts/`.
- **Memória** sai de `get_performance_stats`; o `editor_status --includeMemory` que a skill cita não existe nesta versão do Pipeline.
- **Safe Mode.** Com erro de compilação o Pipeline não sobe e todo `unity command` falha. `unity pipeline list` confirma; corrija pelo `tools/unity compile`.
- **Licença.** O Editor residente ocupa um assento até fechar. Feche ao terminar.
