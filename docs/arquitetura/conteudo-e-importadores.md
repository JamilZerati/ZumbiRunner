---
cobre:
  - "Assets/_Game/Scripts/Editor/*Importer.cs"
  - "Assets/_Game/Scripts/Editor/JsonEnumNames.cs"
  - "Assets/_Game/Scripts/Editor/SpriteImportRules.cs"
revisado: 2026-11-13
---
# Conteúdo e importadores

Número de balanceamento nasce em `Content/Source/*.json` e vira asset em `Assets/_Game/Data/` pelos importadores de Editor. Os catálogos (`ScriptableObject`) guardam composições por `[SerializeReference]`: perks, efeitos de status, variantes de portão, comportamentos de inimigo e modificadores de mutação são instâncias criadas aqui e lidas em runtime sem que nenhum código de produção faça `new` delas.

## Caminhos

| Caminho | Âncora | O que difere |
|---|---|---|
| Importação completa | `Assets/_Game/Scripts/Editor/Cli.cs:ImportContent` | encadeia os importadores em ordem fixa (armas, status e interações, aliados, perks, inimigos, portões, jaulas, barricadas, fases, habilidades, clima, mutações); sai com 1 se qualquer um reportar erro |
| Status e sinergias | `Assets/_Game/Scripts/Editor/StatusContentImporter.cs:StatusContentImporter` | um importador para duas pastas (`Statuses` e `Interactions`) |
| Demais tipos | `Assets/_Game/Scripts/Editor/PerkImporter.cs:ImportAll` | um importador por pasta de `Content/Source`, todos com `ImportAll(errors)` |

## Invariantes

- A ordem importa: perk referencia arma e status; portão referencia perk; fase referencia inimigo, portão, jaula e barricada.
- Asset de `Assets/_Game/Data/` é gerado: muda-se o JSON e reimporta-se.
- Tipo novo de efeito, variante ou comportamento precisa de ramo no importador correspondente, senão o JSON não tem como criá-lo.

## Mexer junto

- Campo novo numa `*Definition` → o importador do tipo, o JSON de exemplo e `Assets/_Game/Scripts/Editor/Tools/ValidateCommand.cs:Run`.
- Pasta nova em `Content/Source` → importador, entrada em `ImportContent` na posição certa e o guia `docs/agentes/guardrails-build.md`.

## Divergências

- Nem todo conteúdo importado chega ao jogo. `appliedStatus` do clima é lido do JSON e nenhum código de produção o consulta — ver [modificadores de fase](../regras/modificadores-de-fase-e-mutacoes.md). `AllyCatalog` não tem leitor em produção — ver [aliados](../fluxos/aliados.md).
- Conteúdo que não passa por aqui: o catálogo do Quartel é código (`BarracksCatalog.CreateDefault`), os arquétipos de aliado são literais em `AllyRoster` e a granada tem raio e dano em constantes — ver as docs de cada fluxo.
- `Content/Source/ad_policy.json` existe e não há importador para ele; os limites valem pelos padrões de `AdPolicyDefinition` — ver [anúncios](../fluxos/anuncios.md).
- A validação de conteúdo cobre cinco pastas (fases, habilidades, portões, jaulas, barricadas); armas, status, perks, inimigos, clima e mutações só são validados pelo que o próprio importador recusa.
