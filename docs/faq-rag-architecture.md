# FAQ: RAG Architecture — Retrieval, Embeddings, and Search

Covers key concepts and decisions for Phase 4 (Retrieval / RAG) of the Azure AI Loan Copilot.

For a full visual progression of RAG architectures from simple to production, see
[rag-architecture-progression.md](rag-architecture-progression.md).

---

## 1. What is the Phase 4 RAG plan?

Two sub-phases behind a single `IRetrievalService` abstraction:

- **Phase 4A** — local file keyword search over `data/loan-kb/*.md`. Zero infrastructure. Proves the prompt composition pattern.
- **Phase 4B** — Azure AI Search with hybrid (BM25 + vector) retrieval. Production-grade semantic search.

The abstraction means `ChatApi` never changes when the backend is upgraded.

---

## 2. What is the maximum context size and what happens when search returns too much?

The model's context window is shared by everything:

```
Context window (e.g., 128K tokens)
├── System prompt          ~200–400 tokens
├── Retrieved snippets     ← controlled by MaxRetrievalTokens
├── User question          ~20–100 tokens
├── Conversation history   grows over time
└── Model output reserve   (MaxOutputTokens = 400)
```

**Recommended `MaxRetrievalTokens`: 1,500–2,500 tokens.**

When search returns more than the budget allows, snippets are ranked by relevance score and included highest-first until the budget is full. Lower-scoring chunks are dropped. The model answers better from 2 tight snippets than 8 loosely related ones.

---

## 3. How is Azure AI Search different from local file search?

| | Local File Search (4A) | Azure AI Search (4B) |
|---|---|---|
| Match method | Keyword overlap (syntactic) | BM25 + vector similarity (semantic) |
| Query: "how much cash do I need upfront?" | Misses "Closing Cost Breakdown" — no shared keywords | Finds it — vectors capture meaning, not just words |
| Infrastructure | None | Azure resource + embedding model |
| Document updates | Edit `.md` file in repo | Indexing pipeline re-embeds changed docs |

The core difference: local search is *syntactic* (does the word appear?), Azure AI Search is *semantic* (does the meaning match?). Users rarely use the exact technical terms from loan documents — semantic search bridges that gap.

---

## 4. How does the model know where the Azure AI indexed data is?

It doesn't — and that is the point.

The model has no connection to Azure AI Search. The flow is:

```
User question
  → IRetrievalService retrieves relevant text snippets
  → Prompt composer pastes snippets into the system prompt
  → Azure OpenAI receives plain text (context + question)
  → Model reads the text and answers as if it "knew"
```

The model is not connected to any index. Your code retrieves the text and puts it in the prompt. If the retrieval returns the wrong snippet, the model gives a confident but wrong answer. Retrieval quality determines answer quality.

---

## 5. When should you use an indexing pipeline — and when not?

**Core rule:** use a pipeline when data changes independently of code deploys.

| Situation | Pipeline? |
|---|---|
| Small corpus, changes rarely, engineers own updates | No — files in repo is fine |
| Docs change weekly/monthly, non-technical owners update | Yes |
| Real-time data (live rate feeds, pricing APIs) | Yes + streaming |
| Large corpus (thousands of documents) | Yes |
| Multiple source systems (CRM, policy docs, rate sheets) | Yes |
| Regulated content that must always be current | Yes |

**For this project:**
- Phase 4A: no pipeline — loan KB lives in the repo, engineers commit changes
- Phase 4B: CI-triggered re-index on file changes (GitHub Actions, git diff to detect only changed files)
- Full pipeline only needed if loan officers or compliance teams must push updates without engineering involvement

---

## 6. What is the difference between chunking, embedding, and indexing?

Three sequential steps — each feeds the next:

```
Document → Chunking → Embedding → Indexing
```

| Step | What it does | Phase 4A | Phase 4B |
|---|---|---|---|
| **Chunking** | Splits document into retrievable pieces | Split `.md` at `##` section boundaries | Same strategy |
| **Embedding** | Converts each chunk to a vector (numbers that capture meaning) | Not needed — keyword search | `text-embedding-3-small` per chunk |
| **Indexing** | Stores vectors for fast similarity search at query time | In-memory at request time | Azure AI Search persistent index |

**Critical constraint:** chunking strategy, embedding model, and index must all stay in sync. If you change the chunking strategy or the embedding model, you must re-embed and re-index everything.

---

## 7. What are the main chunking strategies?

| Strategy | Quality | Complexity | Best For |
|---|---|---|---|
| **Fixed size** (every N tokens) | Low | Low | Unstructured docs, uniform sizing |
| **Sentence / paragraph boundaries** | Medium | Low | Well-written prose |
| **Section / header boundaries** (`##`) | High | Low | Structured markdown, policy docs |
| **Semantic chunking** (split where meaning shifts) | Highest | High | Dense unstructured content |
| **Recursive / hierarchical** (try large then split) | High | Medium | Mixed structure, size enforcement |

**For this project:** section/header boundaries (`##` in markdown). Loan KB documents are structured — each section is a coherent topic, chunks are human-auditable, and no embedding cost is needed to detect boundaries.

**Key risk:** overlapping chunk content across sections. Fixed overlap (repeat last 100 tokens in next chunk) reduces the risk of splitting a concept across a boundary.

---

## 8. What are the main embedding models?

### Azure OpenAI (recommended for this project)

| Model | Dimensions | Cost per 1M tokens | Notes |
|---|---|---|---|
| `text-embedding-ada-002` | 1,536 | ~$0.10 | Widely used, solid baseline |
| `text-embedding-3-small` | 512–1,536 | ~$0.02 | Better quality than ada-002, 5× cheaper — recommended default |
| `text-embedding-3-large` | 256–3,072 | ~$0.13 | Highest OpenAI quality |

**For Phase 4B:** `text-embedding-3-small` at 1,536 dimensions. Better than ada-002 at lower cost, stays within the Azure platform boundary established by ADR-0001.

### Open Source / Self-Hosted

| Model | Dimensions | Notes |
|---|---|---|
| `bge-large-en-v1.5` | 1,024 | Strong English retrieval quality |
| `all-MiniLM-L6-v2` | 384 | Fast and lightweight for low-latency use |
| `multilingual-e5-large` | 1,024 | 100+ languages, good for multilingual RAG |

**Hard constraint:** the same model must embed documents at index time and questions at query time. Mixing models produces incompatible vector spaces and silently wrong results.

---

## 9. How do you handle multilingual projects?

Four strategies, each suited to different scenarios:

| Strategy | How | Best For |
|---|---|---|
| **Multilingual embedding model** | One model maps all languages to a shared vector space | Corpus and users in different languages, one index |
| **Separate index per language** | One index per language, route query to matching index | Regulated content reviewed per locale |
| **Translate everything to one language** | Machine-translate docs to English before indexing | Speed of delivery, translation fidelity is acceptable |
| **Store original + translated** | Translate for search, return original text to model | Search quality of translation + authenticity of source |

**Azure AI Search multilingual support:**
- Per-field language analysers for BM25 (stemming, stop words per language)
- Semantic ranker supports 50+ languages
- Hybrid search (BM25 language-aware + multilingual vectors) = best cross-language quality without separate indexes

**Recommended model for multilingual:** Cohere Embed v3 Multilingual (available on Azure AI Foundry) or `multilingual-e5-large` (self-hosted).

**For this project:** currently English-only. If Spanish support is added (realistic for a US loan product), switch to a multilingual embedding model, add a `language` metadata field to each chunk, detect query language, and pass it to the system prompt so the model responds in kind. The `IRetrievalService` interface and `ChatApi` do not change.

---

## Key Decisions for This Project (Summary)

| Decision | Choice | Reason |
|---|---|---|
| Retrieval backend (4A) | Local file keyword search | Zero infrastructure, proves the pattern |
| Retrieval backend (4B) | Azure AI Search hybrid | Production quality, Azure-native (ADR-0001) |
| Chunking strategy | Section/header boundaries (`##`) | Structured markdown, human-auditable |
| Embedding model (4B) | `text-embedding-3-small` | Best cost/quality on Azure OpenAI |
| Context injection | System prompt enrichment | Simple, sufficient for 4A chunk volumes |
| Citation source | Structured metadata from retrieval layer | Reliable — not LLM-generated |
| Max retrieval budget | 1,500–2,500 tokens | Leaves room for output, history, system prompt |
| Indexing pipeline (4B) | CI-triggered on file changes | No infra overhead, change detection via git diff |
| Multilingual | Not in scope — design accommodates it | Interface and prompt pattern are language-agnostic |
