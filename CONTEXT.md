# Horde Runner

Runner mobile (retrato) em Unity: o General lidera uma tropa por lanes discretas contra hordas de zumbis, escolhendo portões de perk.

## Linguagem

**General**:
Avatar do jogador; ser atingido com a tropa zerada encerra a fase em derrota.

**Tropa**:
Soldados que seguem o General em formação; a contagem é o HP.
_Avoid_: vida, esquadrão

**Lane**:
Faixa discreta da pista; a quantidade vem de `LaneLayout`.

**Portão**:
Par de escolhas, uma por lane, que aplica um Perk ao ser atravessado.

**Perk**:
Composição de `IPerkEffect` (tropa, arma, aliado, stat, clima).
_Avoid_: power-up

**Fase Boss**:
Fase de dificuldade alta que, vencida, desbloqueia conteúdo.

## Mapa

| Pasta de topo | Papel |
|---|---|
| `docs/` | `LINEAR.md` (IDs do overlay) e `planos/` |
