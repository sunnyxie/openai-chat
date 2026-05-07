import React, { useState, useEffect, useRef } from 'react';
import { InputTextarea } from 'primereact/inputtextarea';
import { Button } from 'primereact/button';
import { ProgressSpinner } from 'primereact/progressspinner';
import { Message } from 'primereact/message';
import { Divider } from 'primereact/divider';
import { Badge } from 'primereact/badge';
import { sendMessage } from '../services/chatService';
import HistoryItem from '../components/HistoryItem';
import type { ChatEntry } from '../types/chat';
import './ChatPage.css';

const ChatPage: React.FC = () => {
  // ----- State -----
  const [inputText, setInputText] = useState<string>('');
  const [history, setHistory] = useState<ChatEntry[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Ref used to auto-scroll to the latest answer
  const bottomRef = useRef<HTMLDivElement>(null);

  // ----- Effects -----

  // On mount: restore any previous history from sessionStorage
  useEffect(() => {
    const saved = sessionStorage.getItem('chat-history');
    if (saved) {
      try {
        const parsed: ChatEntry[] = JSON.parse(saved);
        // Restore Date objects (JSON.parse gives strings)
        setHistory(
          parsed.map((e) => ({ ...e, timestamp: new Date(e.timestamp) }))
        );
      } catch {
        sessionStorage.removeItem('chat-history');
      }
    }
  }, []);

  // Whenever history changes: persist to sessionStorage & scroll to bottom
  useEffect(() => {
    if (history.length > 0) {
      sessionStorage.setItem('chat-history', JSON.stringify(history));
      bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
    }
  }, [history]);

  // ----- Handlers -----

  const handleSubmit = async (): Promise<void> => {
    const trimmed = inputText.trim();
    if (!trimmed || isLoading) return;

    setErrorMessage(null);
    setIsLoading(true);

    try {
      const response = await sendMessage(trimmed);
      // const response = JSON.parse(response_raw)

      // console.log("response: ", response.response, response)
      const newEntry: ChatEntry = {
        id: crypto.randomUUID(),
        question: trimmed,
        answer: response.response,
        timestamp: new Date(),
      };

      setHistory((prev) => [...prev, newEntry]);
      setInputText('');
    } catch (error: unknown) {
      const msg =
        error instanceof Error
          ? error.message
          : 'An unexpected error occurred. Please try again.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  };

  // Allow Ctrl+Enter to submit
  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>): void => {
    if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
      void handleSubmit();
    }
  };

  const handleClearHistory = (): void => {
    setHistory([]);
    setErrorMessage(null);
    sessionStorage.removeItem('chat-history');
  };

  // ----- Render -----
  return (
    <div className="chat-page">
      {/* ── Header ── */}
      <header className="chat-header">
        <div className="header-inner">
          <div className="logo-area">
            <span className="logo-icon">
              <i className="pi pi-comments" />
            </span>
            <div>
              <h1 className="app-title">OpenAI Chat</h1>
              <p className="app-subtitle">Powered by WCD </p>
            </div>
          </div>
          {history.length > 0 && (
            <div className="header-actions">
              <Badge value={history.length} severity="info" className="history-badge" />
              <Button
                label="Clear"
                icon="pi pi-trash"
                className="p-button-outlined p-button-sm clear-btn"
                onClick={handleClearHistory}
                disabled={isLoading}
              />
            </div>
          )}
        </div>
      </header>

      {/* ── Main layout ── */}
      <main className="chat-main">
        {/* History panel */}
        <section className="history-panel">
          {history.length === 0 && !isLoading && (
            <div className="empty-state">
              <i className="pi pi-send empty-icon" />
              <p className="empty-title">No messages yet</p>
              <p className="empty-sub">
                Type your first question below and press <kbd>Submit</kbd>
              </p>
            </div>
          )}

          {history.map((entry, idx) => (
            <HistoryItem key={entry.id} entry={entry} index={idx} />
          ))}

          {/* Loading skeleton while waiting */}
          {isLoading && (
            <div className="loading-entry">
              <div className="loading-question-placeholder" />
              <div className="loading-answer-card">
                <ProgressSpinner
                  style={{ width: '36px', height: '36px' }}
                  strokeWidth="4"
                  animationDuration=".8s"
                />
                <span className="loading-label">Thinking…</span>
              </div>
            </div>
          )}

          <div ref={bottomRef} />
        </section>

        {/* Error banner */}
        {errorMessage && (
          <div className="error-banner">
            <Message
              severity="error"
              text={errorMessage}
              className="error-message"
            />
          </div>
        )}

        <Divider className="input-divider" />

        {/* Input area */}
        <section className="input-section">
          <div className="textarea-wrapper">
            <InputTextarea
              value={inputText}
              onChange={(e) => setInputText(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="Ask anything… (Ctrl + Enter to submit)"
              rows={3}
              autoResize
              disabled={isLoading}
              className="chat-textarea"
              aria-label="Message input"
            />
          </div>

          <div className="submit-row">
            <span className="hint-text">Ctrl + Enter to send</span>
            <Button
              label={isLoading ? 'Waiting…' : 'Submit'}
              icon={isLoading ? 'pi pi-spin pi-spinner' : 'pi pi-send'}
              iconPos="right"
              onClick={() => void handleSubmit()}
              disabled={isLoading || inputText.trim().length === 0}
              className="submit-btn"
            />
          </div>
        </section>
      </main>
    </div>
  );
};

export default ChatPage;
