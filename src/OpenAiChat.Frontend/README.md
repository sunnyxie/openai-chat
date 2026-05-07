# OpenAI Chat — React + Vite Frontend

A clean, TypeScript-based React (Vite) app that lets users send messages to a C# API
which forwards them to OpenAI, then displays a full history of Q&A pairs.

---

## Project Structure

```
openai-chat/
├── index.html
├── vite.config.ts          ← Dev proxy: /api/* → http://localhost:5000
├── tsconfig.json
├── package.json
└── src/
    ├── main.tsx            ← Entry point + PrimeReact theme imports
    ├── App.tsx
    ├── types/
    │   └── chat.ts         ← Shared TypeScript interfaces
    ├── services/
    │   └── chatService.ts  ← fetch() wrapper with timeout & error handling
    ├── components/
    │   └── HistoryItem.tsx ← Single Q&A card component
    └── pages/
        ├── ChatPage.tsx    ← Main page (useState + useEffect)
        └── ChatPage.css    ← Page-scoped styles
```

---

## Prerequisites

| Tool | Version |
|------|---------|
| Node.js | ≥ 18.x |
| npm | ≥ 9.x (bundled with Node) |
| Your C# API | Running on `http://localhost:5000` |

---

## Step-by-Step Install & Run

### Step 1 — Clone / download the project

```bash
cd openai.frontend
```

### Step 2 — Install dependencies

```bash
npm install
```

> This installs React, Vite, TypeScript, PrimeReact, PrimeFlex, and PrimeIcons.

### Step 3 — Start your C# API

Make sure your C# backend is running and listening on **port 5000**:

```bash
# From your C# project folder:
dotnet run
# or:
dotnet watch run
```

Your C# API must expose:

```
POST http://localhost:5000/api/chat
Content-Type: application/json

Body:  { "message": "Your question here" }
Response: { "answer": "OpenAI response here" }
```

### Step 4 — Start the React dev server

```bash
npm run dev
```

Open your browser at: **http://localhost:5173**

Vite automatically proxies all `/api/*` requests to `http://localhost:5000`,
so no CORS configuration is needed during development.

---

## Building for Production

```bash
# Step 1: Build the optimised bundle
npm run build

# Step 2: Preview the production build locally
npm run preview
```

Output files are placed in the `dist/` folder — deploy these to any static host
(Azure Static Web Apps, Nginx, IIS, etc.) and point your `/api` rewrite rule
at the C# API URL.

---

## Features

- **PrimeReact controls** — InputTextarea, Button, ProgressSpinner, Message, Card, Badge, Divider
- **History list** — every Q&A pair is shown in chronological order with timestamps
- **useEffect hooks** — session persistence (saves/restores history via `sessionStorage`) and auto-scroll
- **Loading state** — spinner + skeleton placeholder while waiting for the API
- **Error handling** — 10-second timeout, non-2xx HTTP errors, network failures — all shown as a dismissible error banner
- **Keyboard shortcut** — `Ctrl + Enter` (or `⌘ + Enter` on Mac) submits the message
- **Dark theme** — PrimeReact `lara-dark-indigo` with custom CSS variables

---

## Troubleshooting

| Problem | Fix |
|---------|-----|
| `ECONNREFUSED` / network error | Make sure the C# API is running on port 5000 |
| CORS errors in browser | Use the Vite dev proxy (already configured); for production add CORS headers to your C# API |
| `npm install` fails | Ensure Node ≥ 18 — run `node -v` to check |
| Blank page | Check the browser console for errors; confirm `index.html` has `<div id="root"></div>` |
