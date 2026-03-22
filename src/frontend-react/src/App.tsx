import { useEffect, useState } from 'react'
import './App.css'

type Source = {
  sourceName: string
  snippet: string
  relevance: number
  fileUrl: string
}

type ChatMessage = {
  id: string
  role: 'assistant' | 'user'
  text: string
  sources?: Source[]
}

type ChatResponse = {
  id: string
  role: 'assistant' | 'user'
  message: string
  timestamp: string
  tags: string[]
  sources: Source[]
}

function App() {
  const [draft, setDraft] = useState('')
  const [starterQuestions, setStarterQuestions] = useState<string[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')
  const [messages, setMessages] = useState<ChatMessage[]>([
    {
      id: 'welcome',
      role: 'assistant',
      text: 'Welcome to Loan Copilot. The chat UI is now connected to the local backend, so the next message you send will come from `POST /api/chat`.',
    },
  ])

  useEffect(() => {
    const loadStarterQuestions = async () => {
      try {
        const response = await fetch('/api/chat/prompts')

        if (!response.ok) {
          throw new Error('Unable to load starter prompts.')
        }

        const prompts = (await response.json()) as string[]
        setStarterQuestions(prompts)
      } catch {
        setStarterQuestions([
          'What do I need for pre-approval?',
          'How much should I save for closing costs?',
          'How does my credit score affect rates?',
        ])
      }
    }

    void loadStarterQuestions()
  }, [])

  const sendMessage = async (text: string) => {
    const trimmed = text.trim()

    if (!trimmed || isLoading) {
      return
    }

    const userMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'user',
      text: trimmed,
    }

    setErrorMessage('')
    setMessages((current) => [...current, userMessage])
    setDraft('')

    try {
      setIsLoading(true)

      const response = await fetch('/api/chat', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ message: trimmed }),
      })

      if (!response.ok) {
        throw new Error('The chat API returned an error.')
      }

      const assistantReply = (await response.json()) as ChatResponse

      setMessages((current) => [
        ...current,
        {
          id: assistantReply.id,
          role: assistantReply.role,
          text: assistantReply.message,
          sources: assistantReply.sources,
        },
      ])
    } catch {
      setErrorMessage(
        'The app could not reach the local chat API. Make sure the backend is running on http://localhost:5216.',
      )
    } finally {
      setIsLoading(false)
    }
  }

  const handleSubmit = async (event: { preventDefault(): void }) => {
    event.preventDefault()
    await sendMessage(draft)
  }

  return (
    <main className="app-shell">
      <section className="hero-panel">
        <div className="hero-panel-inner">
          <div className="hero-copy-block">
            <h1 className="eyebrow">Azure AI Loan Copilot</h1>
            <h2>Trusted mortgage guidance with a modern conversational experience.</h2>
            <p className="hero-copy">
              This prototype demonstrates a professional borrower-facing assistant
              that can answer lending questions, explain pre-approval steps, and
              evolve into a retrieval-grounded advisory experience.
            </p>
          </div>

          <div className="hero-card">
            <span className="hero-card-label">Experience Highlights</span>
            <ol>
              <li>Answers grounded in real loan guidelines — not generic AI responses.</li>
              <li>Every answer cites the source document it was drawn from.</li>
              <li>Powered by Azure OpenAI with a curated mortgage knowledge base.</li>
            </ol>
          </div>

          <div className="hero-stats" aria-label="Product highlights">
            <article className="hero-stat">
              <span className="hero-stat-value">24/7</span>
              <p className="hero-stat-label">Always-on borrower guidance</p>
            </article>
            <article className="hero-stat">
              <span className="hero-stat-value">1 API</span>
              <p className="hero-stat-label">Stable contract across phases</p>
            </article>
            <article className="hero-stat">
              <span className="hero-stat-value">Next</span>
              <p className="hero-stat-label">Retrieval-backed domain answers</p>
            </article>
          </div>
        </div>
      </section>

      <section className="chat-panel">
        <div className="chat-header">
          <div>
            <p className="chat-kicker">Live Assistant</p>
            <h2>Mortgage conversation workspace</h2>
          </div>
          <span className="status-pill">
            {isLoading ? 'Responding' : 'Connected'}
          </span>
        </div>

        <div className="starter-row" aria-label="Starter questions">
          {starterQuestions.map((question) => (
            <button
              key={question}
              type="button"
              className="starter-chip"
              onClick={() => void sendMessage(question)}
              disabled={isLoading}
            >
              {question}
            </button>
          ))}
        </div>

        {errorMessage ? <p className="error-banner">{errorMessage}</p> : null}

        <div className="message-list" aria-live="polite">
          {messages.map((message) => (
            <article
              key={message.id}
              className={`message-bubble message-bubble--${message.role}`}
            >
              <span className="message-role">
                {message.role === 'assistant' ? 'Copilot' : 'You'}
              </span>
              <p>{message.text}</p>
              {message.sources && message.sources.length > 0 && (
                <div className="source-list">
                  <span className="source-label">Sources</span>
                  <div className="source-chips">
                    {message.sources.map((source) => (
                      <a
                        key={source.sourceName}
                        className="source-chip"
                        href={source.fileUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                      >
                        {source.sourceName}
                      </a>
                    ))}
                  </div>
                </div>
              )}
            </article>
          ))}

          {isLoading ? (
            <article className="message-bubble message-bubble--assistant message-bubble--pending">
              <span className="message-role">Copilot</span>
              <p>Thinking through your loan question...</p>
            </article>
          ) : null}
        </div>

        <form className="composer" onSubmit={handleSubmit}>
          <label className="sr-only" htmlFor="chat-input">
            Ask a loan question
          </label>
          <textarea
            id="chat-input"
            rows={3}
            value={draft}
            onChange={(event) => setDraft(event.target.value)}
            placeholder="Ask a loan question..."
            disabled={isLoading}
          />
          <div className="composer-footer">
            <p>Ask about pre-approval, closing costs, credit, or loan readiness.</p>
            <button type="submit" disabled={isLoading || !draft.trim()}>
              {isLoading ? 'Sending...' : 'Send'}
            </button>
          </div>
        </form>
      </section>
    </main>
  )
}

export default App
