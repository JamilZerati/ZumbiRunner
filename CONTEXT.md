# Horde Runner

Runner mobile (retrato) em Unity: o General lidera uma tropa por lanes discretas contra hordas de zumbis, escolhendo portões de perk.

## Linguagem

**General**:
Avatar do jogador; tem vida própria, que só recebe dano com a tropa zerada. A morte do General é a única derrota da fase.

**Tropa**:
Soldados que seguem o General em formação; a contagem é o HP e o poder de fogo.
_Avoid_: vida, esquadrão

**Lane**:
Faixa discreta da pista; a quantidade vem de `LaneLayout`.

**Portão**:
Par de escolhas, uma por lane, que aplica um Perk ao ser atravessado.

**Perk**:
Composição de `IPerkEffect` (tropa, arma, aliado, stat, clima).
_Avoid_: power-up

**Efeito de status**:
Modificador ou condição temporal/instantânea aplicada a entidades (`StatusKind`).
_Avoid_: debuff

**Sinergia**:
Interação combinada entre status e dano resolvida pela `EffectInteractionTable`.

**Estilhaço**:
Sinergia que consome o status Congelado sob golpe pesado, multiplicando o dano final.

**Fase Boss**:
Fase de dificuldade alta que, vencida, desbloqueia conteúdo.

**Pelotão**:
Grupo de até ~5 soldados representado por um único emissor de tiro; dano do emissor = dano da arma × soldados do pelotão.
_Avoid_: esquadra, fireteam

**Engajamento**:
Zumbi que alcança a tropa gruda na vanguarda e causa dano contínuo até morrer; soldados caem quando o buffer de vida deles esgota.
_Avoid_: contato, atropelamento

**Escape**:
Zumbi que passa pelo General sem engajar; não fere ninguém, mas custa as moedas do abate e pesa nas estrelas.

**Estrelas**:
Nota da fase vencida (1 a 3) pela fração de zumbis eliminados e pelas barricadas mantidas.

**Barricada**:
Trecho em que a corrida para e a tropa defende uma barricada com sobreviventes atrás; zumbis das outras lanes ferem a barricada.
_Avoid_: holdout (no texto de produto)

**Poder do General**:
Habilidade ativa carregada por abates (ex.: Granada); disparo automático ou manual por opção.
_Avoid_: ultimate

**Portão vivo**:
Portão com variante: evolui com tiros, amaldiçoado (bônus + desvantagem) ou guardado por horda.

**Jaula**:
Obstáculo com HP que, aberto a tiros, libera sobreviventes como soldados; ignorado, eles voltam como zumbis.

**Recruta**:
Sobrevivente resgatado que vira recurso da meta, gasto no Quartel.

**Mutação**:
Modificador opcional (ou forçado por evento) que muda as regras da fase em troca de bônus de recompensa.

**Pista de multiplicador**:
Trecho final da fase em que a tropa restante paga marcos `x1`–`x5` que multiplicam as moedas.

**Quartel**:
Meta idle: construções com nível e tempo de obra; única fonte de poder permanente.

**Moedas / Medalhas**:
Moeda soft (ganha jogando) e moeda hard (compra, passe, eventos).

**Trilha**:
Sequência de marcos com recompensa grátis e premium; base do passe, dos eventos e do login diário.

**Temporada**:
Janela de 28 dias com passe próprio.

**Evento**:
Janela curta com regras próprias (mutações forçadas, inimigos em destaque), fichas e trilha.

## Mapa

| Pasta de topo | Papel |
|---|---|
| `docs/` | `LINEAR.md` (IDs do overlay) e `planos/` |
