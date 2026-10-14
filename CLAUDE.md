# CLAUDE.md

@AGENTS.md

## Específico do Claude Code

- **Fluxo por slash commands**: `/atuar` é a porta de entrada (resolve o card e roteia); `/planejar`, `/cenarios`, `/executar`, `/review`, `/conduzir`, `/homologar` e `/bug` seguem o Fluxo Linear + Git do `AGENTS.md`.
- **Ponteiro da sessão**: `.scratch/NOW.md` (fora do git) guarda história, plano, passo e runtime. Em divergência, Linear e plano ganham.
- **Unity em background**: rode `tools/unity` com `run_in_background` e espere a notificação de término; o exit code e o resumo final são o resultado.
- **Worktrees**: tarefa paralela em `D:/Projects/worktrees/ZumbiRunner/nex-<n>` (`git worktree add -b <gitBranchName da tarefa> <pasta> origin/<branch da história>`).
- **Git Bash**: `git show "<ref>:<caminho>"` precisa de `MSYS_NO_PATHCONV=1`, senão o caminho é convertido e o comando falha.
- **Referência por REA**: antes de planejar ou implementar mecânicas, decompilar e buscar a lógica no código de referência; adaptar nomes para o padrão do projeto, mantendo fidelidade estrita à lógica e fórmulas (`docs/agentes/rea-referencia.md`).
