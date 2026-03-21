# Azure AI Loan Copilot Plan

Status legend: `todo`, `in progress`, `done`, `blocked`

## 1. Mock Chat Backend

Status: `done`

Goal:
Build a local mock chat experience with a stable API contract before adding external AI dependencies.

Deliverables:
- `POST /api/chat` returns a mock assistant response
- `GET /api/health` confirms the API is alive
- `GET /api/chat/prompts` returns starter prompts
- React chat shell exists for message flow and layout

Files involved:
- `src/ChatApi/Program.cs`
- `src/frontend-react/src/App.tsx`
- `src/frontend-react/src/App.css`
- `src/frontend-react/src/index.css`
- `docs/phase-1-mock-chat-backend.md`

Notes:
- Frontend production build passed
- Backend endpoints were added, but runtime verification is still pending in this environment
- React UI shell for chat is in place
- Architecture and tradeoffs are documented in `docs/phase-1-mock-chat-backend.md`

## 2. Connect React UI To API

Status: `done`

Goal:
Replace local mock replies in React with real HTTP calls to the local Chat API.

Deliverables:
- React submits messages to `POST /api/chat`
- Loading state while waiting for API response
- Error state for failed requests
- Shared request/response shape matches backend contract
- Local dev config supports frontend-to-backend calls

Files involved:
- `src/frontend-react/src/App.tsx`
- `src/frontend-react/vite.config.ts`
- `src/ChatApi/Program.cs`
- `docs/phase-2-connect-react-ui-to-api.md`

Notes:
- Keep the UI shape stable so Azure OpenAI can slot in later without a redesign
- Vite dev proxy is being used for local `/api` calls to `http://localhost:5216`
- Architecture and tradeoffs are documented in `docs/phase-2-connect-react-ui-to-api.md`

## 3. Plug In Azure OpenAI

Status: `in progress`

Goal:
Swap the mock backend response generator for Azure OpenAI chat completion calls.

Deliverables:
- Azure OpenAI settings added to configuration
- Backend chat endpoint calls Azure OpenAI
- Secrets stay in development/user-secret or environment configuration
- Mock fallback can be disabled or removed once integration is stable

Files involved:
- `src/ChatApi/Program.cs`
- `src/ChatApi/Configuration/AzureOpenAiOptions.cs`
- `src/ChatApi/Services/AzureOpenAiChatResponder.cs`
- `src/ChatApi/Services/IChatResponder.cs`
- `src/ChatApi/Services/ChatResult.cs`
- `src/ChatApi/ChatApi.csproj`
- `src/ChatApi/appsettings.json`
- `src/ChatApi/appsettings.Development.json`
- Additional service files if we split API and AI client logic
- `docs/phase-3-plug-in-azure-openai.md`

Notes:
- Prefer isolating Azure client code behind a small service abstraction
- Azure OpenAI integration compiles successfully with the `Azure.AI.OpenAI` SDK
- `POST /api/chat` now routes through a responder service so the frontend contract stays unchanged
- A mock fallback remains available when Azure settings are missing or requests fail
- Runtime verification against a real Azure endpoint is still pending

## 4. Add Retrieval / RAG

Status: `todo`

Goal:
Ground chat responses in loan-specific knowledge instead of general model behavior.

Deliverables:
- Retrieval pipeline for relevant content
- Prompt composition that includes retrieved context
- Citations or source hints in responses
- Basic evaluation path for relevance and answer quality

Files involved:
- `src/RetrievalService/*`
- `src/ChatApi/*`
- Shared models if response payload grows
- `docs/phase-4-add-retrieval-rag.md`

Notes:
- Only start this after chat is stable end to end with Azure OpenAI
- Architecture and tradeoffs should be updated once the retrieval design is chosen
