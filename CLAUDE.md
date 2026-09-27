# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

As regras do repositório valem para todos os agentes e vivem em `AGENTS.md`:

@AGENTS.md

## Específico do Claude Code

- Os comandos `/atuar`, `/planejar`, `/executar`, `/review`, `/conduzir` e `/homologar` seguem o fluxo descrito em `AGENTS.md` (Fluxo Linear + Git). Agentes sem slash commands seguem o mesmo fluxo lendo `AGENTS.md` e `docs/LINEAR.md`.
- `.scratch/NOW.md` (gitignored) guarda o ponteiro da sessão (história, plano, passo, runtime). Se divergir do Linear ou do plano, Linear e plano ganham.
- Rode Unity em background quando a importação for longa e espere pelo exit code; não use `sleep` para aguardar.
- Outra sessão pode estar trabalhando no mesmo checkout: para uma tarefa paralela, use `git worktree` em vez de trocar a branch da pasta principal.
