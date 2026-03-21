import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import './App.css'

type ChatMessage = {
  id: string
  role: 'assistant' | 'user'
  text: string
}

type ChatResponse = {
  id: string
  role: 'assistant' | 'user'
  message: string
  timestamp: string
  tags: string[]
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

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    await sendMessage(draft)
  }

  return (
    <main className="app-shell">
      <section className="hero-panel">
        <p className="eyebrow">Azure AI Loan Copilot</p>
        <h1>Mock Chat API setup is ready for the next integration step.</h1>
        <p className="hero-copy">
          Step 2 wires the React app to the local backend so the interface is
          now exercising the real chat contract.
        </p>

        <div className="hero-card">
          <span className="hero-card-label">What happens next</span>
          <ol>
            <li>Replace local replies with a POST to the Chat API.</li>
            <li>Keep the same message contract when Azure OpenAI is added.</li>
            <li>Layer retrieval on top once chat is stable end to end.</li>
          </ol>
        </div>
      </section>

      <section className="chat-panel">
        <div className="chat-header">
          <div>
            <p className="chat-kicker">Step 2</p>
            <h2>React connected to local API</h2>
          </div>
          <span className="status-pill">
            {isLoading ? 'Waiting on API' : 'API connected'}
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
            <p>Requests are sent to `POST /api/chat` through the Vite dev proxy.</p>
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
