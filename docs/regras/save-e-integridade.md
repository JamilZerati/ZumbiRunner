---
cobre:
  - "Assets/_Game/Scripts/Meta/Save/**"
revisado: 2026-11-13
---
# Save e integridade

`MetaState` é o estado permanente do jogador: moedas, fases liberadas, estrelas, Quartel. `SaveStore` grava esse estado em JSON com versão e hash, e aplica migrações na leitura.

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Gravação | `Assets/_Game/Scripts/Meta/Save/SaveStore.cs:Save` | carimba a versão, calcula o hash com sal e escreve o arquivo |
| Leitura | `Assets/_Game/Scripts/Meta/Save/SaveStore.cs:Load` | confere a integridade, migra versão a versão e desserializa |
| Migrações | `Assets/_Game/Scripts/Meta/Save/Migrations/SaveMigrationV2ToV3.cs:SaveMigrationV2ToV3` | cada uma recalcula o hash com o mesmo sal |

## Invariantes

- Arquivo com hash inválido não carrega: `Load` lança exceção.
- Serviços de meta aceitam o estado por construtor e passam a escrever nas coleções dele: `Assets/_Game/Scripts/Meta/Stages/StageMapService.cs:StageMapService` e `Assets/_Game/Scripts/Meta/Progression/ProgressionService.cs:ProgressionService`.

## Mexer junto

- Campo novo em `MetaState` → versão corrente, migração e o serviço dono do campo.
- Serviço novo de meta → recebe `MetaState` e é salvo depois de cada operação que muda o estado.

## Divergências

- **O jogo não salva nem carrega.** Nenhum código de produção instancia `SaveStore` nem cria `MetaState`; o composer monta carteira, mapa de fases, progressão e Quartel sem estado — ver [composição e boot](../arquitetura/composicao-e-boot.md). Tudo o que o jogador ganha some ao fechar o app.
- A versão corrente padrão do construtor é 1, embora existam migrações até a 3: criado sem argumento, `SaveStore` grava versão 1 e nunca migra.
- A escrita não é atômica nem tem cópia de segurança: `File.WriteAllText` direto no arquivo final.
- O sal do hash é constante pública no código (`DefaultSalt`): o hash barra arquivo corrompido e edição casual, não quem lê o binário.
- Duas cópias de cada estado quando houver save: `MetaState.Currencies` e o saldo interno de `Assets/_Game/Scripts/Meta/Wallet/Wallet.cs:Wallet`; `MetaState.Barracks` e o `BarracksState` do serviço. Nada sincroniza um com o outro.
- Itens e desbloqueios concedidos ficam em coleções de memória do `RewardSink`, fora do `MetaState`.
