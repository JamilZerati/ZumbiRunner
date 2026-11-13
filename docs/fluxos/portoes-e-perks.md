---
cobre:
  - "Assets/_Game/Scripts/Gameplay/Gate.cs"
  - "Assets/_Game/Scripts/Gameplay/GatePair.cs"
  - "Assets/_Game/Scripts/Core/Perks/**"
  - "Assets/_Game/Scripts/Core/Gates/Variants/**"
  - "Assets/_Game/Scripts/Core/Gates/GateRuntimeState.cs"
  - "Assets/_Game/Scripts/Core/Gates/GateVariantType.cs"
  - "Assets/_Game/Scripts/Core/Gates/IGateVariant.cs"
  - "Assets/_Game/Scripts/Data/Gates/**"
  - "Assets/_Game/Scripts/Data/PerkDefinition.cs"
  - "Assets/_Game/Scripts/Presentation/GateView.cs"
revisado: 2026-11-13
---
# Portões e perks

Um Portão é um par de escolhas, uma por lane. Atravessar aplica o Perk da lane escolhida: uma composição de `IPerkEffect` (somar, multiplicar ou dividir soldados, mudar atributo da arma, trocar de arma, aplicar status no tiro, recrutar aliado). Portão vivo é o portão com variante: evolui com tiros, amaldiçoado ou guardado.

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Travessia | `Assets/_Game/Scripts/Gameplay/Gate.cs:OnTriggerEnter` | o colisor da tropa aciona o par com a lane do portão tocado |
| Consumo do par | `Assets/_Game/Scripts/Gameplay/GatePair.cs:TryTrigger` | marca o par como consumido antes de aplicar; publica `GatePassedEvent` e `GateTriggeredEvent` |
| Perk | `Assets/_Game/Scripts/Gameplay/GatePair.cs:ApplyPerk` | único ponto de produção que chama `IPerkEffect.Apply` |
| Evolui com tiros | `Assets/_Game/Scripts/Core/Gates/Variants/ShootToUpgradeGateVariant.cs:ApplyToSquad` | não passa por perk: aplica o valor acumulado direto na tropa |
| Amaldiçoado | `Assets/_Game/Scripts/Core/Gates/Variants/CursedGateVariant.cs:ActivateModifier` | aplica o perk de bônus e ativa um modificador de fase |
| Operação trocada por mutação | `Assets/_Game/Scripts/Gameplay/GatePair.cs:ApplyPerk` | multiplicar vira somar o fator; dividir vira subtrair o divisor, sem zerar a tropa |

## Invariantes

- Um par só é consumido uma vez, qualquer que seja a lane.
- Divisor menor ou igual a zero lança exceção — `Assets/_Game/Scripts/Core/Perks/Effects/DivideSoldiersEffect.cs:Apply`.
- Soma negativa remove soldados; a subtração por troca de operação deixa pelo menos um.
- Modificador de atributo aplicado por perk leva o id do perk como fonte.

## Mexer junto

- Efeito de perk novo → classe em `Effects/`, ramo no `PerkImporter`, e o tratamento de troca de operação em `ApplyPerk` se mexer na contagem.
- Variante nova → `GateVariantType`, a classe, `GateImporter`, `Assets/_Game/Scripts/Presentation/GateView.cs:GateView` e o simulador.

## Divergências

- **Portões vivos não estão em jogo.** Os portões da cena são criados pelo `SceneBuilder` só com um perk por lane; `Gate.SetVariant` não tem chamador de produção e nada lê `GateCatalog` ou `GateDefinition`. Evolução por tiros, maldição e guarda só rodam em teste e no simulador.
- **O par não recebe o controlador de modificadores.** `TryTrigger` é chamado pela colisão sem barramento, controlador nem distância; o par procura o controlador no `LevelDirector`, que não está em cena, e o composer não entrega o dele. A mutação que troca a operação dos portões e o modificador do portão amaldiçoado não têm efeito.
- `GatePassedEvent.AppliedValue` sai zero para todo portão de perk; só a variante que evolui com tiros preenche o valor. O único assinante seria o `ObjectiveTracker`.
- A variante amaldiçoada declara um `BonusPerkId`, e o perk aplicado vem de `Gate.BonusPerk`: dois campos para o mesmo dado.
- Sem distância informada, a duração da maldição conta a partir da posição Z do portão, não da distância percorrida pela pista.
