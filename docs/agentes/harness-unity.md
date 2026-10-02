# Harness Unity (`tools/unity`)

Como rodar e ler o harness CLI. O portão de verificação em si está no `AGENTS.md`.

## Qual script

| Entrada | Encaminha para | Comandos |
|---|---|---|
| `tools/unity.ps1` (PowerShell) ou `tools/unity.cmd` | — | todos: `setup`, `compile`, `test-edit`, `test-play`, `import-content`, `validate`, `simulate`, `build-android` |
| `./tools/unity` ou `tools/unity.sh` (Bash) | `unity.sh` | só `setup`, `compile`, `test-edit`, `test-play`; os outros caem no `Usage` com exit 1 (https://github.com/JamilZerati/ZumbiRunner/issues/33) |

No Windows, `import-content`, `validate`, `simulate` e `build-android` rodam pelo `.ps1`: `powershell -File tools/unity.ps1 import-content`.

O editor é resolvido por `UNITY_EDITOR` (caminho exato do `Unity.exe`) ou pelos caminhos padrão do Hub em `C:` e `D:`, sempre na versão de `ProjectSettings/ProjectVersion.txt`. Sem editor encontrado, o harness sai com erro e pede `UNITY_EDITOR`.

## O que cada comando prova

| Comando | Prova | Onde ler |
|---|---|---|
| `compile` | Scripts compilam para Android | `.artifacts/compile.log` (erros `error CS`) |
| `test-edit` | Suíte EditMode | resumo `SUCCESS n/n`; falhas em `.artifacts/editmode.xml` |
| `test-play` | Suíte PlayMode (física real, ciclo de vida) | resumo; falhas em `.artifacts/playmode.xml` |
| `import-content` | JSON válido virou SO; exit 1 com erros de importação | `.artifacts/import-content.log` |
| `validate` | **Nada ainda**: stub com exit 0 até a M7 | — |
| `simulate` | **Nada ainda**: stub com exit 0 até a M7 | — |
| `build-android` | APK de desenvolvimento em `Builds/` (fora do git) | `.artifacts/build-android.log` |

Falhas de teste, com mensagem e stack trace:

```bash
python - <<'EOF'
import xml.etree.ElementTree as ET
for f in [".artifacts/editmode.xml", ".artifacts/playmode.xml"]:
    try: root = ET.parse(f)
    except FileNotFoundError: continue
    for tc in root.iter("test-case"):
        if tc.get("result") == "Failed":
            print(f, tc.get("name"), (tc.findtext("failure/message") or "").strip())
EOF
```

## Tempo e concorrência

- Worktree novo não tem `Library/`: a primeira execução reimporta o projeto inteiro e leva minutos. Rode em background e espere o exit code.
- Comandos Unity na mesma pasta rodam um de cada vez (o editor trava o projeto). Encadeie na mesma chamada: `compile; test-play; test-edit`.
- `".artifacts is not a valid directory name"` no log é ruído do Unity e não indica falha.

## CI

O workflow GameCI (NEX-524) ainda não roda nos PRs. Até ele entrar, o portão local é o único: o PR lista os comandos rodados e os resultados (`test-edit 210/210`, `test-play 1/1`).

## Build Android e aparelho

`build-android` fecha a história, antes do `/homologar`. Instalar e jogar no aparelho é do humano; o agente entrega o caminho do APK e o que conferir.
