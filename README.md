# 1. OpenAI Chat — ASP.NET Core 8 Backend

A clean, production-ready ASP.NET Core 8 Web API that proxies chat messages
to the OpenAI API. Includes Polly resilience policies, Swagger UI, structured
logging, global error handling, CORS, Docker support, and an xUnit test suite.

# 2. OpenAI Chat — React + Vite Frontend 

A clean, TypeScript-based React (Vite) app that lets users send messages to a C# API
which forwards them to OpenAI, then displays a full history of Q&A pairs.
Located at: OpenAiChat.Frontend

## Prerequisites

| Tool        | Minimum version |
|-------------|----------------|
| .NET SDK    | 8.0            |
| Docker      | 24.x (optional) |
| OpenAI key  | Any valid key  |
| Node.js | ≥ 18.x |
| npm | ≥ 9.x (bundled with Node) |
---

## Step-by-Step Setup (Local)

### Step 1 — Clone / download the project

```bash
git clone https://github.com/sunnyxie/openai-chat.git

# cd to the root directory
cd openai-chat/
```

### Step 2 — Set your OpenAI API key

The API key is **never** stored in source control. Supply it as an environment variable:

**macOS / Linux**
```bash
export OPENAI_API_KEY="sk-proj-..."
```

**Windows (PowerShell)**
```powershell
$env:OPENAI_API_KEY = "sk-proj-..."
```

**Windows (Command Prompt)**
```cmd
set OPENAI_API_KEY=sk-proj-...
```

### Step 3 — (Optional) Change the model name

Edit `src/OpenAiChat.Api/appsettings.json`:
(if run on local environment, edit appsettings.Development.json instead.)

```json
"OpenAI": {
  "ModelName": "gpt-4o",
  "MaxTokens": 2048
}
```

### Step 4 — Restore and run

```bash

# downloads and resolves the NuGet packages of your project
dotnet restore OpenAiChat.sln

# run api endpoint on specified ports
dotnet run --project src/OpenAiChat.Api/OpenAiChat.Api.csproj --urls "http://localhost:5000;https://localhost:5001"
```

The API starts on **http://localhost:5000**, and the react front will communicate on this port.

Swagger UI is available at the root: **http://localhost:5000/**

### Step 5 — Test the endpoint

```bash
curl -X POST http://localhost:5000/api/chat \
     -H "Content-Type: application/json" \
     -d '{"message": "What is an API?"}'
```

### Step 6 — Run the react frontend (details on project's own README.md file)
```bash
# Navigate to the frontend project
cd src/OpenAiChat.Frontend

# Install dependencies
npm install

# Start the application
npm run dev
```

Expected response:
```
  react main page pops up, and you are able to ask questions and get responses from the backend API server.
```

### Other information
 Any assumptions made
  the docker image and its deployment part is not fully tested yet, 
  assume the application would run on local for testing/evaluation purpose!

 What you would improve if this were production-ready
  1. adding the authentication and authorization with Azure Entra or Google IAM (supports SSO, using OAUTH 2.0 and OIDC)
  2. using OpenAI agent mode, and chat in threads.
  3. adding the API keys on github secrets, make it secure and easy to share among team members.
  3. fully CI/CD deployment and docker support.
  5. UI update/optimization
---

## Running Unit Tests

```bash
dotnet test OpenAiChat.sln --verbosity normal
```

> Unit tests use Moq to fake `IChatService` — no real OpenAI calls are made.
> Integration tests use `WebApplicationFactory` with a mocked service.

To collect code coverage:

```bash
dotnet test OpenAiChat.sln --collect:"XPlat Code Coverage"
```

---

## Configuration Reference

### appsettings.json — OpenAI section

| Key | Default | Description |
|-----|---------|-------------|
| `OpenAI:ModelName` | `gpt-4o-mini` | Model to use. Override with `OpenAI__ModelName` env var. |
| `OpenAI:MaxTokens` | `1024` | Max tokens in the completion. |
| `OpenAI:SystemPrompt` | (see file) | System message prepended to every request. |

### appsettings.json — ResiliencePolicy section

| Key | Default | Description |
|-----|---------|-------------|
| `ResiliencePolicy:RetryCount` | `3` | Number of retries after the first attempt. |
| `ResiliencePolicy:RetryBaseDelaySeconds` | `1.0` | Base seconds for exponential back-off. |
| `ResiliencePolicy:TimeoutSeconds` | `30` | Per-attempt timeout in seconds. |

---

## API Reference

### `POST /api/chat`

**Request**
```json
{
  "message": "Explain what an API is in simple terms."
}
```

**Response 200**
```json
{
  "response": "An API is a way for software systems to communicate with each other."
}
```

**Response 400** — validation failed (empty message, too long, etc.)

**Response 502** — OpenAI call failed after all retries

**Response 500** — unexpected server error

---

## Troubleshooting

| Problem | Fix |
|---------|-----|
| `OPENAI_API_KEY environment variable is not set` | Export the variable before running (Step 2) |
| 502 from `/api/chat` | Check your API key is valid and has quota remaining |
| Model not found | Verify `OpenAI:ModelName` matches a model your key has access to |
| Port 5000 in use | Set `ASPNETCORE_URLS=http://+:5001` or change in `launchSettings.json` |
