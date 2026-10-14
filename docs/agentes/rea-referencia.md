# Engenharia Reversa (REA) como Referência de Implementação

Neste repositório, o desenvolvimento e a modelagem das mecânicas do **Horde Runner** baseiam-se fortemente em engenharia reversa das referências de mercado (como Top Lords, Last War v1.0.39 e equivalentes), utilizando as ferramentas e técnicas de REA (Reverse Engineer Anything).

## Princípio Central

> **Sempre decompilar e localizar a parte/código de referência correspondente à mecânica que precisamos implementar para se basear fortemente nela.**

Antes de especular ou reinventar algoritmos de gameplay (combate de atrito, movimentação de tropa, comportamento de portões, holdout/barricadas, cálculo de status, sinergias, etc.), qualquer LLM ou agente deve consultar e extrair a lógica diretamente do código decompilado da referência.

## O que pode mudar vs. O que deve ser preservado

| Elemento | Regra | Justificativa |
|---|---|---|
| **Nomes de variáveis, campos e parâmetros** | **Pode alterar** | Devem seguir o padrão C# descritivo do projeto (ex.: camelCase com `_` para campos privados, nomes autoexplicativos sem abreviações crípticas da referência). |
| **Nomes de métodos, funções e classes** | **Pode alterar** | Devem aderir à arquitetura e ao vocabulário do repositório (`CONTEXT.md`, assemblies `Game.Core`, `Game.Gameplay`, etc.). |
| **Nomes de arquivos e estrutura de pastas** | **Pode alterar** | Devem respeitar a separação por asmdefs (`Game.Core` C# puro sem UnityEngine, `Game.Data`, etc.). |
| **Lógica de negócio e fluxo de estados** | **PRESERVAR fielmente** | A sequência de checagens, transições de estado e condições de parada devem reproduzir exatamente o comportamento de referência. |
| **Fórmulas e cálculos matemáticos** | **PRESERVAR fielmente** | Multiplicadores, taxas de decaimento, fórmulas de propagação, curvas de atrito e números base devem seguir a referência decompilada. |
| **Gatilhos de eventos e sinergias** | **PRESERVAR fielmente** | A ordem e as condições de disparo de efeitos (ex.: consumo de status em impacto de choque, divisão/multiplicação de soldados) devem espelhar a referência. |

## Fluxo de Trabalho para Agentes e LLMs

1. **Localizar o alvo de referência:**
   - Identificar no acervo de scripts decompilados (ex.: `.luac`, `.lua`, classes decompiladas Java/C# via REA/JADX/JEB) os arquivos que tratam da mecânica solicitada.
   - Exemplos documentados em planos: `TeamStateDefense.luac`, `LastStandGate.luac`, controladores de formação e combat state machines.
2. **Decompilar e inspecionar:**
   - Analisar o trecho de código decompilado responsável pela rotina (o loop principal, a função de cálculo de dano, o resolvedor de colisão, etc.).
3. **Mapear a lógica essencial:**
   - Listar as variáveis invariantes, as etapas do algoritmo e as regras de borda da referência.
4. **Transpor para o Horde Runner:**
   - Implementar a mecânica em C# puro (`Game.Core`), aplicando injeção de dependência e tipos estritos.
   - Isolar números de balanceamento em `Content/Source/*.json` quando aplicável.
5. **Garantir em testes EditMode:**
   - Criar cenários de teste automatizados que comprovem matematicamente que o comportamento e as bordas seguem a lógica da referência.
