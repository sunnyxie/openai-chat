# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution & project files first to leverage Docker layer caching
COPY OpenAiChat.sln .
COPY src/OpenAiChat.Api/OpenAiChat.Api.csproj       src/OpenAiChat.Api/
COPY src/OpenAiChat.Tests/OpenAiChat.Tests.csproj   src/OpenAiChat.Tests/

# Restore NuGet packages
RUN dotnet restore OpenAiChat.sln

# Copy the rest of the source
COPY . .

# Build and publish the API in Release configuration
RUN dotnet publish src/OpenAiChat.Api/OpenAiChat.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ── Stage 2: Runtime image ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create a non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish .

# The OPENAI_API_KEY must be supplied at 'docker run' time (see README).
# Do NOT bake the key into the image.
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

USER appuser

ENTRYPOINT ["dotnet", "OpenAiChat.Api.dll"]
