import { useState } from 'react'
import type { FormEvent } from 'react'
import './App.css'

type ChatMessage = {
  id: string
  role: 'assistant' | 'user'
  text: string
}

const cannedReplies = [
  'For step 1, this response is generated in the React app so we can shape the UI before wiring the real API.',
  'This mock conversation helps us validate layout, message rendering, and basic input handling.',
  'Once step 2 starts, we will replace this local reply with a fetch call to the ChatApi backend.',
]

const starterQuestions = [
  'What do I need for pre-approval?',
  'How much should I save for closing costs?',
  'How does my credit score affect rates?',
]

function App() {
  const [draft, setDraft] = useState('')
  const [messages, setMessages] = useState<ChatMessage[]>([
    {
      id: 'welcome',
      role: 'assistant',
      text: 'Welcome to Loan Copilot. This is the step 1 UI shell with local mock replies so we can finalize the experience before connecting the real API.',
    },
  ])

  const sendMessage = (text: string) => {
    const trimmed = text.trim()

    if (!trimmed) {
      return
    }

    const userMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'user',
      text: trimmed,
    }

    const assistantMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'assistant',
      text: cannedReplies[messages.length % cannedReplies.length],
    }

    setMessages((current) => [...current, userMessage, assistantMessage])
    setDraft('')
  }

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    sendMessage(draft)
  }

  return (
    <main className="app-shell">
      <section className="hero-panel">
        <p className="eyebrow">Azure AI Loan Copilot</p>
        <h1>Mock Chat API setup is ready for the next integration step.</h1>
        <p className="hero-copy">
          Step 1 gives us a stable contract and a realistic chat screen. The UI
          below is still using local mock responses on purpose.
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
            <p className="chat-kicker">Step 1</p>
            <h2>Frontend chat shell</h2>
          </div>
          <span className="status-pill">Local mock mode</span>
        </div>

        <div className="starter-row" aria-label="Starter questions">
          {starterQuestions.map((question) => (
            <button
              key={question}
              type="button"
              className="starter-chip"
              onClick={() => sendMessage(question)}
            >
              {question}
            </button>
          ))}
        </div>

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
            placeholder="Ask a mock loan question..."
          />
          <div className="composer-footer">
            <p>Step 2 will point this form at `POST /api/chat`.</p>
            <button type="submit">Send</button>
          </div>
        </form>
      </section>
    </main>
  )
}

export default App
