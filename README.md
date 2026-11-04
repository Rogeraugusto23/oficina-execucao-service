# oficina-execucao-service

Microsserviço de **Execução Mecânica** da Oficina Mecânica Wagyu — Tech
Challenge Fase 4. Último elo da Saga: recebe a OS já com orçamento
aprovado, gerencia a fila/execução, e sinaliza o resultado de volta para o
OS Service.

Ver [ADR-001](docs/adr/ADR-001-fila-de-execucao-e-saga.md).

## Responsabilidades

- Consumir `OrcamentoAprovadoEvent` (publicado pelo OS Service) e enfileirar
  a OS para execução
- Expor `iniciar`/`finalizar` para o mecânico operar a fila
- Publicar `ExecucaoFinalizadaEvent` (sucesso ou falha) — passo final da
  Saga, consumido pelo OS Service para finalizar a OS ou compensar
  (cancelar OS + estornar pagamento no Billing Service)

## Arquitetura

```
src/
  ExecucaoService.Domain/          Execucao, StatusExecucao, eventos de integração
  ExecucaoService.Application/     Use Cases, IEventPublisher
  ExecucaoService.Infrastructure/  DynamoDB, SNS (publica), SQS (consome)
  ExecucaoService.API/             Controllers, Program.cs
tests/
  ExecucaoService.Tests/           xUnit + FluentAssertions, com dublês
```

## Banco de dados

**DynamoDB** (NoSQL) — banco próprio, não compartilhado com o OS Service
nem com o Billing Service.

## Fluxo

```
OS Service: cliente aprova orçamento → OrcamentoAprovadoEvent ──(SNS/SQS)──┐
                                                                            ▼
                                                 Execução: EnfileirarExecucaoUseCase
                                                 (status NaFila)
                                                            │
                                     mecânico: POST /iniciar → EmAndamento
                                                            │
                                   mecânico: POST /finalizar → Concluida | Falhou
                                                            │
                                                            ▼
                                          ExecucaoFinalizadaEvent ──(SNS)──► OS Service
                                          (finaliza a OS, ou aciona compensação)
```

## Rodando localmente

```bash
cd src/ExecucaoService.API
dotnet run
```

Sem `Messaging:SqsQueueUrl`, o consumidor de eventos fica desligado — use
o Swagger para testar `iniciar`/`finalizar` manualmente contra um registro
criado direto (via testes ou chamando o Use Case).

## Testes

```bash
dotnet test
```

## Deploy

CI/CD em `.github/workflows/ci-cd.yml`, mesmo padrão SSH-para-EC2/K3s dos
outros serviços, namespace Kubernetes próprio (`oficina-execucao-service`).

## Status

🚧 Scaffolding completo (Dias 7-9 do roadmap da Fase 4). Pendente:
provisionar a tabela DynamoDB e a fila SQS reais via Terraform, e validar
o fluxo de ponta a ponta junto com o OS Service e o Billing Service
(Dias 10-11: orquestração completa da Saga e testes de compensação).
