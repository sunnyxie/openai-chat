# OpenAI Chat — ASP.NET Core 8 Backend

A clean, production-ready ASP.NET Core 8 Web API that proxies chat messages
to the OpenAI API. Includes Polly resilience policies, Swagger UI, structured
logging, global error handling, Docker support, and an xUnit test suite.

---

## Project Structure

```
openai-api/
├── OpenAiChat.sln
├── Dockerfile
├── docker-compose.yml
├── .dockerignore
├── .github/
│   └── workflows/
│       └── ci.yml                  ← GitHub Actions: build + test + docker
└── src/
    ├── OpenAiChat.Api/             ← Main Web API project
    │   ├── Configuration/
    │   │   └── OpenAiOptions.cs    ← Strongly-typed settings (model, tokens, prompts)
    │   ├── Controllers/
    │   │   └── ChatController.cs   ← POST /api/chat
    │   ├── Middleware/
    │   │   └── GlobalExceptionMiddleware.cs   ← Unified error responses
    │   ├── Models/
    │   │   └── ChatModels.cs       ← Request / Response / Error DTOs
    │   ├── Services/
    │   │   ├── IChatService.cs
    │   │   ├── OpenAiChatService.cs  ← OpenAI SDK wrapper
    │   │   ├── ChatServiceException.cs
    │   │   └── ResiliencePolicies.cs ← Polly retry + timeout registry
    │   ├── appsettings.json
    │   ├── appsettings.Development.json
    │   └── Program.cs              ← DI composition root
    └── OpenAiChat.Tests/           ← xUnit test project
        ├── Controllers/
        │   └── ChatControllerTests.cs
        ├── Integration/
        │   └── ChatEndpointTests.cs
        └── Services/
            └── ResiliencePoliciesTests.cs
```

---

## Prerequisites

| Tool        | Minimum version |
|-------------|----------------|
| .NET SDK    | 8.0            |
| Docker      | 24.x (optional) |
| OpenAI key  | Any valid key  |

---

## Step-by-Step Setup (Local)

### Step 1 — Clone / download the project

```bash
cd openai-api
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

```json
"OpenAI": {
  "ModelName": "gpt-4o",
  "MaxTokens": 2048
}
```

Or override without touching code using an environment variable:

```bash
export OpenAI__ModelName=gpt-4o
```

### Step 4 — Restore and run

```bash
dotnet restore OpenAiChat.sln

dotnet run --project src/OpenAiChat.Api/OpenAiChat.Api.csproj
```

The API starts on **http://localhost:5000** by default.

Swagger UI is available at the root: **http://localhost:5000/**

### Step 5 — Test the endpoint

```bash
curl -X POST http://localhost:5000/api/chat \
     -H "Content-Type: application/json" \
     -d '{"message": "What is an API?"}'
```

Expected response:
```json
{
  "response": "An API (Application Programming Interface) is..."
}
```

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

## Docker

### Build the image

```bash
docker build -t openai-chat-api:latest .
```

### Run the container

```bash
docker run -d \
  --name openai-chat-api \
  -p 5000:8080 \
  -e OPENAI_API_KEY="sk-proj-..." \
  openai-chat-api:latest
```

Override the model at runtime without rebuilding:

```bash
docker run -d \
  -p 5000:8080 \
  -e OPENAI_API_KEY="sk-proj-..." \
  -e OpenAI__ModelName=gpt-4o \
  openai-chat-api:latest
```

### Docker Compose (recommended for local dev)

```bash
# Export the key first
export OPENAI_API_KEY="sk-proj-..."

docker compose up --build
```

---

## GitHub Actions & Secrets

The CI pipeline in `.github/workflows/ci.yml`:

1. Restores, builds, and runs tests on every push / PR.
2. Builds the Docker image to verify it compiles cleanly.

**To add the API key as a GitHub secret:**

1. Go to your repo → **Settings → Secrets and variables → Actions**
2. Click **New repository secret**
3. Name: `OPENAI_API_KEY`, Value: your key
4. In the workflow file, surface it as an environment variable:

```yaml
env:
  OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
```

> The unit tests intentionally do NOT require `OPENAI_API_KEY` — they mock
> the service layer. Only the integration smoke-test step (if you add one)
> would need the real key.

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
