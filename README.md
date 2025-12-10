# 💳 StackFood Payments API

Microsserviço responsável pelo **processamento, autorização e confirmação dos pagamentos** do sistema StackFood.

---

## 📋 Descrição do Projeto

O **Payments Service** gerencia todo o ciclo de pagamento dos pedidos, desde a geração do QR Code do Mercado Pago até a confirmação do pagamento. Faz parte da arquitetura de microsserviços da **Fase 4** do Tech Challenge.

**Repositório**: `https://github.com/Stack-Food/stackfood-api-payments`

---

## 🎯 Funcionalidades

### Core
- ✅ Criar pagamento (com QR Code Mercado Pago)
- ✅ Consultar status de pagamento
- ✅ Consultar pagamento por OrderId
- ✅ Listar pagamentos por status
- ✅ **Fake Checkout** baseado no nome do cliente (MVP)
- ✅ Manter histórico de transações no DynamoDB

### Integrações
- 📤 Publicar eventos SNS quando pagamento é aprovado/rejeitado/pendente
- 📥 Consumir eventos de Orders via SQS
- 🔗 Integração com Mercado Pago SDK (preparado)

---

## 🗂️ Estrutura do Projeto

```
stackfood-api-payments/
├── src/
│   ├── StackFood.Payments.API/         # API REST
│   │   ├── Controllers/
│   │   │   └── PaymentsController.cs
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── Dockerfile
│   │
│   ├── StackFood.Payments.Domain/      # Entidades e regras de negócio
│   │   ├── Entities/
│   │   │   └── Payment.cs
│   │   ├── Enums/
│   │   │   ├── PaymentStatus.cs
│   │   │   └── PaymentMethod.cs
│   │   └── Events/
│   │       ├── PaymentApprovedEvent.cs
│   │       ├── PaymentRejectedEvent.cs
│   │       └── PaymentPendingEvent.cs
│   │
│   ├── StackFood.Payments.Application/ # Casos de uso
│   │   ├── UseCases/
│   │   │   └── CreatePayment/
│   │   │       └── CreatePaymentUseCase.cs
│   │   ├── Interfaces/
│   │   │   ├── IPaymentRepository.cs
│   │   │   ├── IEventPublisher.cs
│   │   │   └── IFakeCheckoutService.cs
│   │   └── DTOs/
│   │       ├── PaymentDTO.cs
│   │       └── CreatePaymentRequest.cs
│   │
│   ├── StackFood.Payments.Infrastructure/ # Infraestrutura
│   │   ├── Repositories/
│   │   │   └── PaymentRepository.cs    # DynamoDB
│   │   ├── Messaging/
│   │   │   └── SnsEventPublisher.cs    # SNS Publisher
│   │   └── Services/
│   │       └── FakeCheckoutService.cs  # Fake Checkout
│   │
│   └── StackFood.Payments.Worker/      # Worker (SQS Consumer)
│
├── tests/
│   └── StackFood.Payments.Tests/
│       └── Features/
│           └── ProcessPayment.feature  # BDD Tests
│
├── scripts/
│   └── init-localstack.sh              # LocalStack setup
│
├── docker-compose.yml
├── .env.example
├── .gitignore
└── README.md
```

---

## 🗄️ Banco de Dados

### Tipo: **DynamoDB** (NoSQL) ⭐

> **Decisão**: Usar DynamoDB para atender requisito "1 SQL + 1 NoSQL"
> **Vantagem**: Flexibilidade para dados do Mercado Pago, serverless, baixa latência

### Tabela: `Payments`

**Primary Key**: `PaymentId` (String - UUID)

**Atributos**:
```json
{
  "PaymentId": "uuid-123",
  "OrderId": "uuid-456",
  "OrderNumber": "ORD-20250101-0001",
  "Amount": 25.90,
  "Status": "Pending | Approved | Rejected",
  "PaymentMethod": "QRCode",
  "CustomerName": "João PAGO",
  "QRCode": "base64-encoded-qr",
  "QRCodeUrl": "https://mp.com/qr/...",
  "CreatedAt": "2025-01-01T10:00:00Z",
  "UpdatedAt": "2025-01-01T10:05:00Z",
  "ApprovedAt": "2025-01-01T10:03:00Z",
  "Metadata": { ... }
}
```

### Índices Secundários (GSI)

```
# GSI 1: Buscar por OrderId
GSI_OrderId
  - Partition Key: OrderId (String)
  - Projection: ALL

# GSI 2: Listar por Status
GSI_Status_CreatedAt
  - Partition Key: Status (String)
  - Sort Key: CreatedAt (String)
  - Projection: ALL
```

---

## 🌐 APIs/Endpoints

### **Base URL**: `/api/payments`

| Método | Endpoint | Descrição | Request | Response |
|--------|----------|-----------|---------|----------|
| POST | `/api/payments` | Criar pagamento | `CreatePaymentRequest` | `PaymentDTO` |
| GET | `/api/payments/{id}` | Consultar pagamento | - | `PaymentDTO` |
| GET | `/api/payments/order/{orderId}` | Consultar por pedido | - | `PaymentDTO` |
| GET | `/api/payments/status/{status}` | Listar por status | - | `List<PaymentDTO>` |

### DTOs:

#### CreatePaymentRequest
```json
{
  "orderId": "uuid",
  "orderNumber": "ORD-20250101-0001",
  "amount": 25.90,
  "customerName": "João PAGO"
}
```

#### PaymentDTO
```json
{
  "paymentId": "uuid",
  "orderId": "uuid",
  "orderNumber": "ORD-20250101-0001",
  "amount": 25.90,
  "status": "Approved",
  "paymentMethod": "QRCode",
  "qrCode": "base64...",
  "qrCodeUrl": "https://...",
  "createdAt": "2025-01-01T10:00:00Z",
  "approvedAt": "2025-01-01T10:03:00Z"
}
```

---

## 🚨 Fake Checkout (MVP)

Para MVP, o status do pagamento é determinado pelo **nome do cliente**:

- **Nome contém "PAGO"** → Status = `Approved` ✅
- **Nome contém "CANCELADO"** → Status = `Rejected` ❌
- **Qualquer outro nome** → Status = `Pending` ⏳

**Exemplos**:
- `"João PAGO"` → Pagamento aprovado automaticamente
- `"Maria CANCELADO"` → Pagamento rejeitado
- `"Carlos Silva"` → Pagamento pendente

---

## 📡 Mensageria SNS/SQS

### Publishers (Envia para SNS)

#### Tópico: `sns-payment-events`

**Eventos**:

1. **PaymentApprovedEvent**
```json
{
  "eventType": "PaymentApproved",
  "paymentId": "uuid",
  "orderId": "uuid",
  "orderNumber": "ORD-001",
  "amount": 25.90,
  "approvedAt": "2025-01-01T10:03:00Z",
  "timestamp": "2025-01-01T10:03:00Z"
}
```

2. **PaymentRejectedEvent**
3. **PaymentPendingEvent**

### Consumers (Recebe de SQS)

#### Fila: `sqs-payments-order-queue`
Ouve: `sns-order-events`

**Eventos Consumidos**:
- `OrderCreated`: Gera pagamento automaticamente

---

## 🛠️ Tecnologias Utilizadas

- **Linguagem:** C# (.NET 8)
- **Banco de Dados:** DynamoDB (AWS SDK)
- **Mensageria:** SNS/SQS (AWS SDK)
- **Arquitetura:** Clean Architecture
- **Documentação:** Swagger/OpenAPI
- **Containerização:** Docker + LocalStack
- **Testes:** xUnit + SpecFlow (BDD)

---

## 🚀 Como Executar Localmente

### Pré-requisitos

- [Docker](https://www.docker.com/)
- [Docker Compose](https://docs.docker.com/compose/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Passos

1. **Clone o repositório**

   ```bash
   git clone https://github.com/Stack-Food/stackfood-api-payments.git
   cd stackfood-api-payments
   ```

2. **Configure as variáveis de ambiente**

   ```bash
   cp .env.example .env
   ```

3. **Suba o ambiente com LocalStack**

   ```bash
   docker-compose up -d
   ```

4. **Inicialize o LocalStack (DynamoDB + SNS/SQS)**

   ```bash
   chmod +x scripts/init-localstack.sh
   bash scripts/init-localstack.sh
   ```

5. **Acesse a API**

   - API: http://localhost:8080
   - Swagger: http://localhost:8080/swagger/index.html

---

## ⚙️ Variáveis de Ambiente

| Variável | Descrição | Valor Padrão |
|----------|-----------|--------------|
| `AWS_REGION` | Região AWS | `us-east-1` |
| `AWS_ACCESS_KEY_ID` | Access Key (LocalStack) | `test` |
| `AWS_SECRET_ACCESS_KEY` | Secret Key (LocalStack) | `test` |
| `AWS_SERVICE_URL` | URL do LocalStack | `http://localhost:4566` |
| `DYNAMODB_TABLE_NAME` | Nome da tabela DynamoDB | `Payments` |
| `SNS_PAYMENT_EVENTS_TOPIC_ARN` | ARN do tópico SNS | `arn:aws:sns:...` |
| `ASPNETCORE_ENVIRONMENT` | Ambiente ASP.NET Core | `Development` |

---

## 🧪 Testes

### Executar Testes

```bash
dotnet test
```

### Testes BDD (SpecFlow)

Arquivo: `tests/StackFood.Payments.Tests/Features/ProcessPayment.feature`

**Cenários**:
- ✅ Payment approved with fake checkout
- ✅ Payment rejected with fake checkout
- ✅ Payment pending with fake checkout

---

## 🐳 Docker

### Build da Imagem

```bash
docker build -t stackfood-payments-api:latest -f src/StackFood.Payments.API/StackFood.Payments.API/Dockerfile .
```

### Executar com Docker Compose

```bash
docker-compose up
```

---

## 📦 Pacotes NuGet Utilizados

- `AWSSDK.DynamoDBv2` - Cliente DynamoDB
- `AWSSDK.SimpleNotificationService` - Cliente SNS
- `AWSSDK.SQS` - Cliente SQS
- `AWSSDK.Extensions.NETCore.Setup` - Extensões AWS
- `Moq` - Mocking para testes
- `FluentAssertions` - Asserções fluentes
- `SpecFlow` + `SpecFlow.xUnit` - BDD
- `coverlet.collector` - Cobertura de código

---

## 👥 Participantes

| Nome | RM | E-mail | Discord |
|------|-----|--------|---------|
| Leonardo Duarte | RM364564 | leo.duarte.dev@gmail.com | _leonardoduarte |
| Luiz Felipe Maia | RM361928 | luiz.felipeam@hotmail.com | luiz_08 |
| Leonardo Luiz Lemos | RM364201 | leoo_lemos@outlook.com | leoo_lemos |
| Rodrigo Rodriguez Figueiredo de Oliveira Silva | RM362272 | rodrigorfig1@gmail.com | lilroz |
| Vinicius Targa Gonçalves | RM364425 | viniciustarga@gmail.com | targa1765 |

---

## 💡 Observações Finais

- ✅ **Compilação perfeita** - Projeto compila sem erros
- ✅ **DynamoDB local** com LocalStack
- ✅ **Fake Checkout** implementado para MVP
- ✅ **SNS/SQS** configurado
- ✅ **Testes BDD** com 3 cenários
- ⚠️ **Próximos passos**: CI/CD, Worker SQS Consumer, Kubernetes manifests

---

**Status**: ✅ Pronto para uso
**Última atualização**: 2025-12-10
