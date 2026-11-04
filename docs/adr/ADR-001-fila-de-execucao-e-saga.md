# ADR-001: Fila de execução como NoSQL e papel na Saga

## Status
Aceito

## Contexto
O Execução Service representa a etapa final do fluxo de negócio: depois
que o orçamento é aprovado e pago, a OS entra numa fila de execução
mecânica. É o passo que, ao concluir, fecha a Saga orquestrada pelo OS
Service (sucesso) ou aciona a compensação (falha).

## Decisão
- **DynamoDB** (NoSQL) para o registro de execução — mesmo raciocínio do
  Billing Service (ADR-001 daquele repositório): agregado simples, sem
  necessidade de transações relacionais complexas.
- O serviço **não expõe** um endpoint para "entrar na fila" — isso só
  acontece pela chegada do evento `OrcamentoAprovadoEvent` via SQS,
  garantindo que a execução sempre reflita um orçamento realmente aprovado
  no OS Service (nunca criada diretamente via API, o que abriria uma
  inconsistência entre serviços).
- `POST /api/Execucoes/{id}/iniciar` e `.../finalizar` são os únicos
  endpoints síncronos, pensados para o mecânico/atendente operar a fila
  (um painel futuro consumiria esta API).
- Ao finalizar (sucesso ou falha), publica `ExecucaoFinalizadaEvent` — o
  OS Service é quem decide o que fazer com esse resultado (finalizar a OS
  ou compensar), mantendo a orquestração centralizada conforme
  [ADR-006 do OS Service](../../../oficina-os-service/docs/adr/ADR-006-decomposicao-em-microsservicos.md).

## Consequências
- Se a fila SQS não estiver configurada, nenhuma OS entra na fila de
  execução automaticamente — um gap conhecido até a etapa de provisionar
  a infraestrutura de mensageria via Terraform (Dias 10-11 do roadmap).
