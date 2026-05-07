import React from 'react';
import { Card } from 'primereact/card';
import type { ChatEntry } from '../types/chat';

interface HistoryItemProps {
  entry: ChatEntry;
  index: number;
}

/**
 * Renders a single Q&A card in the history list.
 */
const HistoryItem: React.FC<HistoryItemProps> = ({ entry, index }) => {
  const formattedTime = entry.timestamp.toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
  });

  return (
    <div className="history-item" style={{ animationDelay: `${index * 0.05}s` }}>
      {/* Question bubble */}
      <div className="question-bubble">
        <span className="bubble-label">You</span>
        <p className="bubble-text">{entry.question}</p>
        <span className="bubble-time">{formattedTime}</span>
      </div>

      {/* Answer card */}
      <Card className="answer-card">
        <div className="answer-header">
          <span className="answer-badge">OpenAI</span>
        </div>
        <p className="answer-text">{entry.answer}</p>
      </Card>
    </div>
  );
};

export default HistoryItem;
