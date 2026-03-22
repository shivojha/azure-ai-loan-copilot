# Architecture Overview — Basic Phase (1–4A)

---

## 1. System Context

Who uses the system and what external dependencies exist.

```mermaid
flowchart TD
    Borrower(["👤 Borrower"])
    Browser["React UI\nVite · TypeScript · CSS"]
    API["ChatApi\nASP.NET Core · .NET 10"]
    KB[("Loan Knowledge Base\nMarkdown files\ndata/loan-kb/")]
    AzureOAI["Azure OpenAI\nGPT-4o Deployment"]

    Borrower -->|asks a loan question| Browser
    Browser -->|POST /api/chat| API
    API -->|keyword search| KB
    KB -->|ranked snippets| API
    API -->|grounded prompt| AzureOAI
    AzureOAI -->|completion| API
    API -->|answer + sources| Browser
    Browser -->|GET /api/docs/:file| API
    API -->|serves .md file| Browser
```

---

## 2. Component Architecture

Internal structure of the backend and how the projects relate.

```mermaid
flowchart TB
    subgraph Frontend["Frontend"]
        direction TB
        UI["App.tsx\nChat UI · Message bubbles\nSource chips · Starter questions"]
    end

    subgraph ChatApi["ChatApi — ASP.NET Core"]
        direction TB
        EP1["POST /api/chat"]
        EP2["GET /api/health"]
        EP3["GET /api/chat/prompts"]
        EP4["GET /api/docs/:file\nStatic file server"]
        Responder["AzureOpenAiChatResponder\nimplements IChatResponder"]
        PromptBuilder["BuildSystemPrompt\nInjects retrieved context\ninto system message"]
        Fallback["MockLoanAssistant\nKeyword fallback\nwhen Azure not configured"]
        AzureClient["ChatClient (singleton)\nAzure.AI.OpenAI SDK"]

        EP1 --> Responder
        Responder --> PromptBuilder
        Responder --> Fallback
        Responder --> AzureClient
    end

    subgraph RetrievalSvc["RetrievalService — .NET Library"]
        direction TB
        Interface["IRetrievalService\nQueryAsync interface"]
        Retriever["LocalFileRetriever\nPre-computed chunks\nKeyword overlap scoring\nToken budget enforcement"]
        Models["RetrievalResult\nSourceName · Snippet\nRelevance · FileName"]
        Config["RetrievalOptions\nPath · MaxResults\nMaxTokens · MinScore"]

        Interface --> Retriever
        Retriever --> Models
        Retriever --> Config
    end

    subgraph KB["Knowledge Base"]
        direction LR
        F1["fha-loan-requirements.md"]
        F2["conventional-loan-requirements.md"]
        F3["pre-approval-process.md"]
        F4["closing-costs.md"]
        F5["credit-score-guidelines.md"]
    end

    AzureOpenAI["☁️ Azure OpenAI\nGPT-4o"]

    UI -->|fetch /api/chat| EP1
    UI -->|fetch /api/docs/:file| EP4
    EP4 --> KB
    Responder --> Interface
    Retriever --> KB
    AzureClient --> AzureOpenAI
```

---

## 3. Request Flow — Grounded Answer (Retrieval Hit)

The happy path when the knowledge base contains relevant content.

```mermaid
sequenceDiagram
    actor User
    participant UI as React UI
    participant API as ChatApi
    participant Ret as LocalFileRetriever
    participant KB as data/loan-kb/
    participant OAI as Azure OpenAI

    User->>UI: "What credit score do I need for an FHA loan?"
    UI->>API: POST /api/chat { message }

    API->>Ret: QueryAsync(message)
    Ret->>KB: Read pre-computed chunks (loaded at startup)
    Ret-->>API: [RetrievalResult x2, scores 0.80, 0.80]

    Note over API: BuildSystemPrompt()<br/>Appends 2 chunks to system prompt<br/>tag = "with-retrieval"

    API->>OAI: SystemMessage(base prompt + context)<br/>UserMessage(question)
    OAI-->>API: "For an FHA loan, the minimum credit score is 580..."

    API-->>UI: { message, tags: ["azure-openai","with-retrieval","step-4a"], sources: [...] }
    UI->>User: Renders answer + source chips
```

---

## 4. Request Flow — No Retrieval Match

When no knowledge base content matches the query.

```mermaid
sequenceDiagram
    actor User
    participant UI as React UI
    participant API as ChatApi
    participant Ret as LocalFileRetriever
    participant OAI as Azure OpenAI

    User->>UI: "What is the current Fed funds rate?"
    UI->>API: POST /api/chat { message }

    API->>Ret: QueryAsync(message)
    Ret-->>API: [] (score below MinRelevanceScore 0.1)

    Note over API: BuildSystemPrompt()<br/>Base prompt unchanged<br/>tag = "retrieval-miss"

    API->>OAI: SystemMessage(base prompt only)<br/>UserMessage(question)
    OAI-->>API: "The Fed funds rate changes frequently..."

    API-->>UI: { message, tags: ["azure-openai","retrieval-miss","step-4a"], sources: [] }
    UI->>User: Renders answer, no source chips shown
```

---

## 5. Request Flow — Source File Navigation

When the user clicks a source chip to read the original document.

```mermaid
sequenceDiagram
    actor User
    participant UI as React UI
    participant API as ChatApi
    participant FS as data/loan-kb/ (static files)

    User->>UI: Clicks source chip<br/>"fha loan requirements — Credit Score Requirements"
    UI->>API: GET /api/docs/fha-loan-requirements.md
    API->>FS: PhysicalFileProvider serves file
    FS-->>API: fha-loan-requirements.md content
    API-->>UI: Markdown file (text/plain)
    UI->>User: Opens raw .md file in new browser tab
```

---

## 6. Tech Stack

| Layer | Technology | Version | Role |
|---|---|---|---|
| Frontend | React | 19.2.4 | Chat UI, message rendering, source chips |
| Frontend | TypeScript | 5.9 | Type-safe API contract |
| Frontend | Vite | 8.x | Dev server, build, API proxy |
| Backend | ASP.NET Core | .NET 10 | REST API, static file server |
| Backend | Azure.AI.OpenAI | 2.1.0 | Azure OpenAI SDK |
| Backend | C# | 13 | Primary application language |
| AI | Azure OpenAI | GPT-4o | Chat completion |
| Retrieval | Local file search | — | Keyword overlap scoring, section chunking |
| Knowledge Base | Markdown files | — | 5 loan domain documents |
| Config | appsettings.json | — | Azure credentials, retrieval options |
| Secrets | .NET User Secrets | — | API keys in development |

---

## 7. Project Structure

```
azure-ai-loan-copilot/
├── PLAN.md                        ← phase roadmap and status
├── data/
│   └── loan-kb/                   ← knowledge base (5 .md files)
├── docs/
│   ├── basic/                     ← Phase 1–4A docs
│   └── *.md                       ← cross-phase references
├── src/
│   ├── ChatApi/                   ← ASP.NET Core Web API
│   │   ├── Program.cs             ← endpoints, DI, static files
│   │   ├── Configuration/         ← AzureOpenAiOptions
│   │   └── Services/              ← IChatResponder, AzureOpenAiChatResponder, ChatResult
│   ├── RetrievalService/          ← .NET class library
│   │   ├── IRetrievalService.cs
│   │   ├── LocalFileRetriever.cs
│   │   ├── RetrievalResult.cs
│   │   └── RetrievalOptions.cs
│   ├── frontend-react/            ← React + TypeScript
│   │   └── src/
│   │       ├── App.tsx            ← chat UI, message bubbles, source chips
│   │       └── App.css
│   ├── AgentService/              ← stub (Phase 5+)
│   └── SharedKernel/              ← stub (future shared models)
└── tests/
```

---

## 8. Key Design Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Retrieval abstraction | `IRetrievalService` interface | Swap local → Azure AI Search in Phase 4B without changing ChatApi |
| Context injection | System prompt enrichment | Simple, effective for small retrieved sets |
| Citation source | Retrieval layer metadata | Reliable — not LLM-generated, no hallucination risk |
| HTTP client | `Lazy<ChatClient>` singleton | Reuse connection pool across requests |
| Chunk pre-computation | At app startup | Avoid re-tokenizing files on every query |
| Token budget | `MaxRetrievalTokens = 2000` | Prevent context overflow; enforced before prompt assembly |
| Fallback | MockLoanAssistant | App stays usable when Azure not configured |
| Knowledge base | Versioned `.md` files in repo | Human-auditable, reviewed alongside code changes |
