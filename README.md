# MortgageIQ — Azure AI Loan Copilot

A domain-grounded mortgage assistant powered by Azure OpenAI and Retrieval-Augmented Generation (RAG). Answers are drawn from a curated loan knowledge base and every response cites the source document it was drawn from.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![React](https://img.shields.io/badge/React-19-61DAFB?logo=react)
![TypeScript](https://img.shields.io/badge/TypeScript-5.9-3178C6?logo=typescript)
![Azure OpenAI](https://img.shields.io/badge/Azure-OpenAI-0078D4?logo=microsoftazure)

---

## Demo

![MortgageIQ — Home Screen](docs/basic/demo/Azure%20AI%20Loan%20Copilot%20Home%20Screen%20-%20Basic.png)

### Walkthrough Video

https://github.com/user-attachments/assets/3ef2e8b0-5188-4978-96c7-dba8d7c4fbbe

---

## What It Does

Mortgage borrowers face hundreds of questions before closing — credit scores, DTI limits, down payments, closing costs. Generic AI answers are unreliable because they are not grounded in actual guidelines.

**MortgageIQ** solves this by combining Azure OpenAI with a versioned loan knowledge base:

- Every answer is retrieved from real loan documents before the model generates a response
- Every response includes clickable citations linking back to the source document
- The knowledge base is auditable — it lives in the repo alongside the code

### Supported Questions (Phase 4A)

| Borrower asks | Source |
|---|---|
| What credit score do I need for an FHA loan? | FHA Loan Requirements |
| How much should I budget for closing costs? | Closing Cost Breakdown |
| What documents do I need for pre-approval? | Pre-Approval Process |
| What is the difference between pre-qual and pre-approval? | Pre-Approval Process |
| How does my credit score affect my interest rate? | Credit Score Guidelines |

---

## Architecture

```mermaid
flowchart TD
    User(["👤 Borrower"])
    UI["React UI\nVite · TypeScript"]
    API["ChatApi\nASP.NET Core · .NET 10"]
    Retriever["LocalFileRetriever\nKeyword search · Section chunking\nToken budget · Pre-computed chunks"]
    KB[("data/loan-kb/\nfha-loan-requirements.md\nconventional-loan-requirements.md\npre-approval-process.md\nclosing-costs.md\ncredit-score-guidelines.md")]
    Prompt["Prompt Composer\nBase system prompt\n+ retrieved context\n+ user question"]
    OAI["☁️ Azure OpenAI\nGPT-4o"]
    Response["Response\nmessage + sources[ ]"]

    User -->|asks a loan question| UI
    UI -->|POST /api/chat| API
    API -->|QueryAsync| Retriever
    Retriever -->|keyword match + score| KB
    KB -->|ranked snippets| Retriever
    Retriever -->|RetrievalResult[ ]| API
    API --> Prompt
    Prompt -->|grounded prompt| OAI
    OAI -->|completion| API
    API -->|answer + citations| Response
    Response --> UI
    UI -->|GET /api/docs/:file| KB
```

The retrieval backend sits behind an `IRetrievalService` interface — upgrading from local file search to Azure AI Search (Phase 4B) is a single line change in dependency injection.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React 19 · TypeScript 5.9 · Vite |
| Backend | ASP.NET Core · .NET 10 · C# 13 |
| AI | Azure OpenAI · GPT-4o · Azure.AI.OpenAI SDK 2.1.0 |
| Retrieval (Phase 4A) | Local file keyword search · Section chunking |
| Retrieval (Phase 4B) | Azure AI Search · Hybrid BM25 + Vector |
| Knowledge Base | Markdown files versioned in repo |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org)
- Azure OpenAI resource with a GPT-4o deployment

### 1. Clone

```bash
git clone https://github.com/your-org/azure-ai-loan-copilot.git
cd azure-ai-loan-copilot
```

### 2. Configure Azure OpenAI

```bash
cd src/ChatApi
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:Deployment" "gpt-4o"
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-api-key"
```

See [docs/SETUP_AZURE_OPENAI.md](docs/SETUP_AZURE_OPENAI.md) for full setup instructions.

### 3. Run the backend

```bash
cd src/ChatApi
dotnet run
# API running on https://localhost:5216
```

### 4. Run the frontend

```bash
cd src/frontend-react
npm install
npm run dev
# UI running on http://localhost:5173
```

---

## Project Structure

```
azure-ai-loan-copilot/
├── data/
│   └── loan-kb/                   # Knowledge base — 5 loan domain documents
├── docs/
│   ├── basic/                     # Phase 1–4A architecture, ADRs, tradeoffs
│   └── *.md                       # Cross-phase references and RAG guides
├── src/
│   ├── ChatApi/                   # ASP.NET Core Web API
│   │   ├── Program.cs             # Endpoints, DI, static file server
│   │   ├── Configuration/         # AzureOpenAiOptions
│   │   └── Services/              # IChatResponder, AzureOpenAiChatResponder
│   ├── RetrievalService/          # Retrieval library (swappable backend)
│   │   ├── IRetrievalService.cs
│   │   ├── LocalFileRetriever.cs
│   │   ├── RetrievalResult.cs
│   │   └── RetrievalOptions.cs
│   └── frontend-react/            # React + TypeScript UI
│       └── src/
│           ├── App.tsx            # Chat UI, message bubbles, source chips
│           └── App.css
└── PLAN.md                        # Phase roadmap and status
```

---

## Phases

| Phase | Status | Description |
|---|---|---|
| 1 — Mock Backend | ✅ Done | Stable API contract, mock responses |
| 2 — React UI | ✅ Done | Frontend connected to backend |
| 3 — Azure OpenAI | ✅ Done | Live model integration with fallback |
| 4A — Local RAG | ✅ Done | Keyword retrieval, citations, source chips |
| 4B — Azure AI Search | 🔜 Next | Hybrid BM25 + vector, semantic queries |
| Production | 📋 Planned | Cache, streaming, guardrails, evaluation |

See [PLAN.md](PLAN.md) for detailed roadmap and [docs/tradeoff-basic-intermediate-advanced.md](docs/tradeoff-basic-intermediate-advanced.md) for tradeoffs across phases.

---

## Documentation

| Document | Description |
|---|---|
| [Architecture Overview](docs/basic/architecture-overview.md) | System context, component diagram, sequence diagrams |
| [RAG Architecture Progression](docs/rag-architecture-progression.md) | RAG from Level 1 (naive) to Level 7 (agentic) |
| [Phase Tradeoffs](docs/tradeoff-basic-intermediate-advanced.md) | Deliberate tradeoffs across Basic → Intermediate → Advanced |
| [RAG FAQ](docs/faq-rag-architecture.md) | Chunking, embeddings, multilingual, pipelines |
| [Azure OpenAI Setup](docs/SETUP_AZURE_OPENAI.md) | Step-by-step Azure configuration |
| [ADR-0001 — Platform Decision](docs/adr-0001-foundry-platform-vs-azure-openai-inference.md) | Foundry vs Azure OpenAI inference choice |
| [ADR-0002 — Retrieval Strategy](docs/basic/adr-0002-retrieval-strategy-phase-4.md) | Local files → Azure AI Search decision |
