# Documentation

This folder tracks architecture, decisions, and tradeoffs as the project evolves across phases.

## Folder Structure

```
docs/
  README.md                          ← this file
  SETUP_AZURE_OPENAI.md              ← Azure OpenAI setup guide
  adr-0001-...md                     ← foundational platform decision
  tradeoff-foundry-vs-azure-openai   ← foundational platform tradeoff
  tradeoff-basic-intermediate-advanced.md  ← cross-phase roadmap and tradeoffs
  rag-architecture-progression.md    ← RAG architecture from simple to production
  faq-rag-architecture.md            ← RAG Q&A reference
  basic/                             ← Phase 1 through 4A implementation docs
  images/                            ← diagrams and visuals
```

---

## Cross-Phase References

- [Phase Roadmap and Tradeoffs](./tradeoff-basic-intermediate-advanced.md)
- [RAG Architecture Progression](./rag-architecture-progression.md)
- [RAG FAQ](./faq-rag-architecture.md)
- [Azure OpenAI Setup](./SETUP_AZURE_OPENAI.md)

## Architecture Decision Records

- [ADR-0001 — Foundry Platform vs Azure OpenAI Inference](./adr-0001-foundry-platform-vs-azure-openai-inference.md)
- [ADR-0002 — Retrieval Strategy Phase 4](./basic/adr-0002-retrieval-strategy-phase-4.md)

## Tradeoff Notes

- [Foundry vs Azure OpenAI](./tradeoff-foundry-vs-azure-openai.md)
- [Retrieval Backends — Local Files vs Azure AI Search vs Vector DB](./basic/tradeoff-retrieval-backends.md)

---

## Basic Phase (Phases 1–4A)

- [Architecture Overview — Basic Phase](./basic/architecture-overview.md)
- [Phase 1 — Mock Chat Backend](./basic/phase-1-mock-chat-backend.md)
- [Phase 2 — Connect React UI To API](./basic/phase-2-connect-react-ui-to-api.md)
- [Phase 3 — Plug In Azure OpenAI](./basic/phase-3-plug-in-azure-openai.md)
- [Phase 4 — Add Retrieval / RAG](./basic/phase-4-add-retrieval-rag.md)
- [Retrieval: Chunking and Scoring (4A)](./basic/retrieval-chunking-and-scoring-for-local-files-4a.md)

---

## Convention

Each phase document includes:

- Scope
- Architecture diagram (Mermaid)
- Tradeoffs accepted
- Exit criteria
