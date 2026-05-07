// Represents a single Q&A exchange stored in history
export interface ChatEntry {
  id: string;
  question: string;
  answer: string;
  timestamp: Date;
}

// Shape of the response from the C# API
export interface ApiResponse {
  response: string;
}

// Error structure returned by the API (if any)
export interface ApiError {
  message: string;
  statusCode?: number;
}
