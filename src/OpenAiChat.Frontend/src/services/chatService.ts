import type { ApiResponse } from '../types/chat';

// Base URL — Vite proxies /api/* to http://localhost:5000 in dev
const API_BASE_URL = '/api';

// Timeout for all requests (10 seconds)
const REQUEST_TIMEOUT_MS = 10_000;

/**
 * Sends the user's message to the C# backend which forwards it to OpenAI.
 * Throws a descriptive Error on network failure, timeout, or non-2xx response.
 */
export async function sendMessage(message: string): Promise<ApiResponse> {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);

  try {
    const response = await fetch(`${API_BASE_URL}/chat`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ message }),
      signal: controller.signal,
    });

    if (!response.ok) {
      // Try to pull a meaningful message from the API body
      let serverMessage = `Server error: ${response.status} ${response.statusText}`;
      try {
        const errorBody = await response.json();
        if (errorBody?.message) serverMessage = errorBody.message;
      } catch {
        // Ignore JSON parse errors on error bodies
      }
      throw new Error(serverMessage);
    }

    const data: ApiResponse = await response.json();
    return data;
  } catch (error: unknown) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw new Error(
        'Request timed out after 10 seconds. Please try again.'
      );
    }
    // Re-throw all other errors (network failures, our own throws above, etc.)
    throw error;
  } finally {
    clearTimeout(timeoutId);
  }
}
