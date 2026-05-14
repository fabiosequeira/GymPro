# GymPro — Sistema de Gestão de Ginásio

Projeto final desenvolvido para a UFCD 10794.  
Sistema completo de gestão de um ginásio com API REST em .NET 8, base de dados PostgreSQL, cache Redis, mock de serviços externos (Mountebank) e website de gestão.

---

## Como executar o projeto (Quick Start)

### Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado e a correr
- Git

### 1. Clonar o repositório

```bash
git clone https://github.com/<teu-username>/gympro.git
cd gympro
```

### 2. Iniciar todos os serviços

```bash
docker-compose up --build
```

> Na primeira execução demora alguns minutos enquanto faz download das imagens Docker e compila a API .NET.

### 3. Aceder à aplicação

Quando aparecer `Now listening on: http://[::]:8080` no terminal, está pronto.

| Serviço | URL | Descrição |
|---|---|---|
| **Website** | http://localhost:3000 | Interface de gestão |
| **API (Swagger)** | http://localhost:5000/swagger | Documentação interativa da API |
| **Mountebank** | http://localhost:2525 | Mock do serviço de pagamentos |

### 4. Credenciais de demonstração

| Perfil | Email | Password |
|---|---|---|
| Administrador | admin@gym.com | admin123 |
| Treinador | carlos@gym.com | trainer123 |
| Treinador | ana@gym.com | trainer123 |
| Membro | joao@email.com | member123 |
| Membro | maria@email.com | member123 |

> Os dados de demonstração são inseridos automaticamente na base de dados na primeira execução.

### 5. Parar os serviços

```bash
docker-compose down
```

Para apagar também os dados da base de dados:

```bash
docker-compose down -v
```

---

## Estrutura do projeto

```
gympro/
├── docker-compose.yml          # Orquestração de todos os serviços
├── README.md
│
├── GymAPI/                     # Backend — ASP.NET Core Web API (.NET 8)
│   ├── GymAPI.csproj           # Dependências (NuGet packages)
│   ├── Program.cs              # Configuração da aplicação (DI, middleware, JWT...)
│   ├── appsettings.json        # Connection strings e configurações
│   ├── Dockerfile              # Build multi-stage da API
│   │
│   ├── Models/
│   │   └── Models.cs           # Entidades: User, Member, Trainer, Plan, GymClass, etc.
│   │
│   ├── DTOs/
│   │   └── DTOs.cs             # Objetos de transferência de dados (request/response)
│   │
│   ├── Data/
│   │   └── GymDbContext.cs     # Entity Framework DbContext + dados de demonstração
│   │
│   ├── Migrations/             # Migrações da base de dados (geradas pelo EF Core)
│   │
│   ├── Services/
│   │   ├── AuthService.cs      # Autenticação JWT (login, registo, geração de tokens)
│   │   ├── CacheService.cs     # Abstração do Redis (get, set, invalidar)
│   │   └── Services.cs        # Lógica de negócio: Member, Trainer, Plan, Class, Payment
│   │
│   ├── Controllers/
│   │   └── Controllers.cs      # Endpoints REST: Auth, Members, Trainers, Plans, Classes, Payments
│   │
│   └── Middleware/
│       └── ExceptionMiddleware.cs  # Tratamento global de erros
│
├── mountebank/
│   └── imposters.json          # Configuração do mock do serviço de pagamentos
│
└── website/
    └── index.html              # Frontend — HTML/CSS/JS puro que consome a API
```

---

## Arquitetura

```
[ Browser / Website ]
        |  HTTPS + JSON
        v
[ ASP.NET Core Web API (.NET 8) ]  <-->  [ Redis (cache) ]
        |                    |
        v                    v
[ PostgreSQL (dados) ]   [ Mountebank (mock pagamentos) ]
```

**Fluxo típico de um pedido:**
1. O website envia um pedido HTTP com token JWT no header
2. A API valida o token e autoriza o acesso
3. A API verifica se existe resposta em cache no Redis
4. Se não houver, consulta o PostgreSQL via Entity Framework
5. Armazena o resultado no Redis e devolve ao cliente
6. Para pagamentos, a API chama o Mountebank com retry e circuit breaker (Polly)

---

## API — Endpoints

### Autenticação (público)

| Método | Endpoint | Descrição |
|---|---|---|
| POST | `/api/auth/login` | Login — devolve token JWT |
| POST | `/api/auth/register` | Criar nova conta de membro |

### Membros

| Método | Endpoint | Permissão | Descrição |
|---|---|---|---|
| GET | `/api/members` | Admin | Listar todos os membros (paginado) |
| GET | `/api/members/{id}` | Autenticado | Obter membro por ID |
| PUT | `/api/members/{id}` | Admin | Atualizar plano / telefone |
| DELETE | `/api/members/{id}` | Admin | Desativar membro |

### Treinadores

| Método | Endpoint | Permissão | Descrição |
|---|---|---|---|
| GET | `/api/trainers` | Público | Listar treinadores |
| GET | `/api/trainers/{id}` | Público | Obter treinador |
| POST | `/api/trainers` | Admin | Criar treinador |
| PUT | `/api/trainers/{id}` | Admin | Atualizar treinador |
| DELETE | `/api/trainers/{id}` | Admin | Desativar treinador |

### Planos

| Método | Endpoint | Permissão | Descrição |
|---|---|---|---|
| GET | `/api/plans` | Público | Listar planos disponíveis |
| POST | `/api/plans` | Admin | Criar novo plano |
| DELETE | `/api/plans/{id}` | Admin | Eliminar plano |

### Aulas

| Método | Endpoint | Permissão | Descrição |
|---|---|---|---|
| GET | `/api/classes` | Público | Listar aulas |
| GET | `/api/classes/{id}` | Público | Detalhes da aula |
| POST | `/api/classes` | Trainer/Admin | Criar aula |
| PUT | `/api/classes/{id}` | Trainer/Admin | Editar aula |
| DELETE | `/api/classes/{id}` | Admin | Eliminar aula |
| POST | `/api/classes/{id}/enroll` | Autenticado | Inscrever em aula |
| DELETE | `/api/classes/{id}/enroll/{memberId}` | Autenticado | Cancelar inscrição |
| GET | `/api/classes/{id}/enrollments` | Trainer/Admin | Ver lista de inscritos |

### Pagamentos

| Método | Endpoint | Permissão | Descrição |
|---|---|---|---|
| POST | `/api/payments` | Autenticado | Processar pagamento (via mock) |
| GET | `/api/payments/member/{id}` | Autenticado | Histórico de pagamentos |

---

## Tecnologias utilizadas

| Componente | Tecnologia | Versão |
|---|---|---|
| Backend | ASP.NET Core Web API | .NET 8 |
| Base de dados | PostgreSQL | 16 |
| ORM | Entity Framework Core + Npgsql | 8.0 |
| Cache distribuído | Redis + StackExchange.Redis | 7 |
| Resiliência HTTP | Polly (retry + circuit breaker) | 8.3 |
| Mock externo | Mountebank | latest |
| Autenticação | JWT (System.IdentityModel.Tokens.Jwt) | 7.5 |
| Hash de passwords | BCrypt.Net | 4.0 |
| Documentação API | Swagger / Swashbuckle | 6.5 |
| Containerização | Docker + Docker Compose | — |
| Frontend | HTML5 / CSS3 / JavaScript | — |

---

## Notas de desenvolvimento

- As migrações da base de dados são aplicadas automaticamente no arranque da API.
- O Redis cache tem TTL diferente por recurso: planos (30 min), treinadores (15 min), aulas (5 min), membros (5 min).
- O Polly está configurado com 3 retries exponenciais (2s, 4s, 8s) e circuit breaker que abre após 5 falhas consecutivas durante 30 segundos.
- O Mountebank simula dois cenários: pagamento bem-sucedido (POST /payment/process) e pagamento falhado (POST /payment/fail-test).
- A autenticação usa roles (Admin, Trainer, Member) com políticas de autorização definidas no Program.cs.
