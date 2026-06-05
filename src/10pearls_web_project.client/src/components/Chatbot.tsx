import { useState, useEffect, useRef } from 'react';
import { getAnswer, SUGGESTED_QUESTIONS } from '../data/chatData';
import './Chatbot.css';

interface Message {
  id: number;
  from: 'user' | 'bot';
  text: string;
}

const WELCOME: Message = {
  id: 0,
  from: 'bot',
  text: "Hi! I'm your TaskFlow assistant. What would you like to know?",
};

export function Chatbot() {
  const [open, setOpen]         = useState(false);
  const [messages, setMessages] = useState<Message[]>([WELCOME]);
  const [input, setInput]       = useState('');
  const bottomRef               = useRef<HTMLDivElement>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, open]);

  function send(text: string) {
    const trimmed = text.trim();
    if (!trimmed) return;
    const userMsg: Message = { id: Date.now(),     from: 'user', text: trimmed };
    const botMsg:  Message = { id: Date.now() + 1, from: 'bot',  text: getAnswer(trimmed) };
    setMessages(prev => [...prev, userMsg, botMsg]);
    setInput('');
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    send(input);
  }

  return (
    <>
      {/* ── Floating trigger ── */}
      <button
        className={`chat-fab ${open ? 'chat-fab-open' : ''}`}
        onClick={() => setOpen(o => !o)}
        aria-label={open ? 'Close assistant' : 'Open assistant'}
        title={open ? 'Close' : 'Ask assistant'}
      >
        {/* Pulse ring — only visible when closed */}
        {!open && <span className="chat-fab-ring" aria-hidden="true" />}

        {/* Icon morphs between chat and close */}
        <span className="chat-fab-icon">
          {open ? (
            /* X close icon */
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"
              strokeLinecap="round" strokeLinejoin="round" width="20" height="20">
              <line x1="18" y1="6" x2="6" y2="18"/>
              <line x1="6" y1="6" x2="18" y2="18"/>
            </svg>
          ) : (
            /* Sparkle-chat icon: speech bubble with a star spark */
            <svg viewBox="0 0 24 24" fill="none" width="22" height="22">
              <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"
                fill="rgba(255,255,255,0.18)" stroke="white" strokeWidth="1.8"
                strokeLinecap="round" strokeLinejoin="round"/>
              {/* Sparkle dots inside bubble */}
              <circle cx="9"  cy="11" r="1.1" fill="white"/>
              <circle cx="12" cy="11" r="1.1" fill="white"/>
              <circle cx="15" cy="11" r="1.1" fill="white"/>
            </svg>
          )}
        </span>

        {/* Tooltip label — appears on hover when closed */}
        {!open && <span className="chat-fab-label">Ask Assistant</span>}
      </button>

      {/* ── Chat window ── */}
      {open && (
        <div className="chat-window glass" role="dialog" aria-label="Task assistant chat">

          {/* Header */}
          <div className="chat-header">
            <div className="chat-header-avatar">
              <svg viewBox="0 0 24 24" fill="none" width="16" height="16">
                <path d="M12 2l2.4 4.8L20 8l-4 3.9.9 5.6L12 15l-4.9 2.5.9-5.6L4 8l5.6-.9z"
                  fill="white" stroke="none"/>
              </svg>
            </div>
            <div className="chat-header-info">
              <span className="chat-header-title">TaskFlow Assistant</span>
              <span className="chat-header-status">
                <span className="chat-header-dot" />
                Online
              </span>
            </div>
            <button className="chat-close" onClick={() => setOpen(false)} aria-label="Close chat">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"
                strokeLinecap="round" strokeLinejoin="round" width="14" height="14">
                <line x1="18" y1="6" x2="6" y2="18"/>
                <line x1="6" y1="6" x2="18" y2="18"/>
              </svg>
            </button>
          </div>

          {/* Messages */}
          <div className="chat-messages">
            {messages.map(m => (
              <div key={m.id} className={`chat-bubble-wrap chat-bubble-wrap-${m.from}`}>
                {m.from === 'bot' && (
                  <div className="chat-bot-avatar" aria-hidden="true">
                    <svg viewBox="0 0 24 24" fill="none" width="10" height="10">
                      <path d="M12 2l2.4 4.8L20 8l-4 3.9.9 5.6L12 15l-4.9 2.5.9-5.6L4 8l5.6-.9z"
                        fill="white"/>
                    </svg>
                  </div>
                )}
                <div className={`chat-bubble chat-bubble-${m.from}`}>
                  {m.text.split('\n').map((line, i) => (
                    <span key={i}>
                      {line.split(/\*\*(.+?)\*\*/g).map((part, j) =>
                        j % 2 === 1 ? <strong key={j}>{part}</strong> : part
                      )}
                      {i < m.text.split('\n').length - 1 && <br />}
                    </span>
                  ))}
                </div>
              </div>
            ))}
            <div ref={bottomRef} />
          </div>

          {/* Suggested questions */}
          {messages.length === 1 && (
            <div className="chat-suggestions">
              {SUGGESTED_QUESTIONS.map(q => (
                <button key={q} className="chat-suggestion-btn" onClick={() => send(q)}>
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"
                    strokeLinecap="round" strokeLinejoin="round" width="11" height="11"
                    style={{ flexShrink: 0 }}>
                    <circle cx="12" cy="12" r="10"/>
                    <line x1="12" y1="8" x2="12" y2="12"/>
                    <line x1="12" y1="16" x2="12.01" y2="16"/>
                  </svg>
                  {q}
                </button>
              ))}
            </div>
          )}

          {/* Input */}
          <form className="chat-input-row" onSubmit={handleSubmit}>
            <input
              className="chat-input"
              value={input}
              onChange={e => setInput(e.target.value)}
              placeholder="Ask a question…"
              autoFocus
              autoComplete="off"
            />
            <button className="chat-send" type="submit" disabled={!input.trim()} aria-label="Send">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"
                strokeLinecap="round" strokeLinejoin="round" width="15" height="15">
                <line x1="22" y1="2" x2="11" y2="13"/>
                <polygon points="22 2 15 22 11 13 2 9 22 2"/>
              </svg>
            </button>
          </form>

        </div>
      )}
    </>
  );
}
