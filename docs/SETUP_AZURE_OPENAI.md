# Azure OpenAI Setup

This project expects Azure OpenAI settings under the `AzureOpenAI` section for `src/ChatApi`.

## Required Values

You need these three values:

- `Endpoint`
- `Deployment`
- `ApiKey`

## Where To Find Them

### Endpoint

In Azure Portal, open your Azure OpenAI resource and go to `Keys and Endpoint`.

Example:

```text
https://my-loan-openai.openai.azure.com/
```

### ApiKey

In the same `Keys and Endpoint` page, copy either `KEY 1` or `KEY 2`.

### Deployment

This is the Azure deployment name you created for the model.

Important:
This is not just the raw model name like `gpt-4o`. It must be the deployment name from your Azure OpenAI resource.

Example:

```text
loan-chat-gpt-4o
```

## Option 1: Put Values In appsettings.Development.json

Edit [appsettings.Development.json](/Users/so/code/azure-ai-loan-copilot/src/ChatApi/appsettings.Development.json#L1):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AzureOpenAI": {
    "Endpoint": "https://my-loan-openai.openai.azure.com/",
    "Deployment": "loan-chat-gpt-4o",
    "ApiKey": "your-real-key",
    "UseMockFallback": true
  }
}
```

## Option 2: Use dotnet User Secrets

This is the better local-development option because it keeps the key out of source-controlled config.

### Step 1: Initialize user secrets for ChatApi

From the repo root run:

```bash
dotnet user-secrets init --project src/ChatApi/ChatApi.csproj
```

This creates a `UserSecretsId` entry in [ChatApi.csproj](/Users/so/code/azure-ai-loan-copilot/src/ChatApi/ChatApi.csproj#L1).

### Step 2: Add the Azure OpenAI values

From the repo root run:

```bash
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://my-loan-openai.openai.azure.com/" --project src/ChatApi/ChatApi.csproj
dotnet user-secrets set "AzureOpenAI:Deployment" "loan-chat-gpt-4o" --project src/ChatApi/ChatApi.csproj
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-real-key" --project src/ChatApi/ChatApi.csproj
```

### Step 3: Keep a placeholder in appsettings.Development.json

Use the same configuration section in [appsettings.Development.json](/Users/so/code/azure-ai-loan-copilot/src/ChatApi/appsettings.Development.json#L1), but do not store the real key there:

```json
"AzureOpenAI": {
  "Endpoint": "https://my-loan-openai.openai.azure.com/",
  "Deployment": "loan-chat-gpt-4o",
  "ApiKey": "use-user-secrets-or-environment-variable",
  "UseMockFallback": true
}
```

### Step 4: Verify the secrets were stored

```bash
dotnet user-secrets list --project src/ChatApi/ChatApi.csproj
```

### Step 5: Update a value later if needed

```bash
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-new-key" --project src/ChatApi/ChatApi.csproj
```

### Step 6: Remove a secret if needed

```bash
dotnet user-secrets remove "AzureOpenAI:ApiKey" --project src/ChatApi/ChatApi.csproj
```

After moving the key to user secrets, set the `ApiKey` value in [appsettings.Development.json](/Users/so/code/azure-ai-loan-copilot/src/ChatApi/appsettings.Development.json#L1) to a placeholder such as:

```json
"ApiKey": "use-user-secrets-or-environment-variable"
```

Important:
You do not reference user secrets directly inside the json file. .NET automatically overlays the secret value at runtime when the key names match.

## Troubleshooting

### The app is still reading the placeholder value

Check these items:

1. Confirm the project was initialized for user secrets:

```bash
dotnet user-secrets list --project src/ChatApi/ChatApi.csproj
```

2. Confirm the exact key names exist:

```bash
dotnet user-secrets list --project src/ChatApi/ChatApi.csproj
```

You should see keys like:

```text
AzureOpenAI:Endpoint = ...
AzureOpenAI:Deployment = ...
AzureOpenAI:ApiKey = ...
```

3. Make sure the app is running in `Development`.

The current project launch settings use `ASPNETCORE_ENVIRONMENT=Development`, so `dotnet run --project src/ChatApi/ChatApi.csproj` should load user secrets automatically.

4. Make sure the key names match the configuration section exactly.

This works:

```text
AzureOpenAI:ApiKey
```

This does not:

```text
AzureOpenAi:ApiKey
```

5. Restart the API after changing secrets.

If the process was already running, it may still be using the old values in memory.

### I changed the secret, but the app still fails

Try resetting the secret explicitly:

```bash
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-new-key" --project src/ChatApi/ChatApi.csproj
```

Then restart the API and test:

```bash
dotnet run --project src/ChatApi/ChatApi.csproj
```

### How to confirm the app is using Azure instead of fallback

Run:

```bash
curl http://localhost:5216/api/health
```

Expected:

```json
{
  "mode": "azure-openai"
}
```

If it shows `mock-fallback`, the Azure settings were not loaded correctly or the request path is falling back after an error.

## Run The API

From the repo root:

```bash
dotnet run --project src/ChatApi/ChatApi.csproj
```

By default, local HTTP uses:

```text
http://localhost:5216
```

## Verify Configuration

Check health:

```bash
curl http://localhost:5216/api/health
```

If Azure settings are loaded, the response should include:

```json
{
  "mode": "azure-openai"
}
```

If settings are missing, the app stays in fallback mode:

```json
{
  "mode": "mock-fallback"
}
```

## Verify Chat

Send a test message:

```bash
curl -X POST http://localhost:5216/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message":"What documents do I need for pre-approval?"}'
```

## Current Code Paths

- Config binding: [AzureOpenAiOptions.cs](/Users/so/code/azure-ai-loan-copilot/src/ChatApi/Configuration/AzureOpenAiOptions.cs#L1)
- API wiring: [Program.cs](/Users/so/code/azure-ai-loan-copilot/src/ChatApi/Program.cs#L1)
- Azure responder: [AzureOpenAiChatResponder.cs](/Users/so/code/azure-ai-loan-copilot/src/ChatApi/Services/AzureOpenAiChatResponder.cs#L1)

## Notes

- `UseMockFallback: true` lets the app keep responding even when Azure settings are missing or a request fails.
- Once Azure is working reliably, you can decide whether to keep or remove the fallback path.

## Sources

- https://learn.microsoft.com/azure/ai-services/openai/chatgpt-quickstart
- https://learn.microsoft.com/azure/ai-foundry/openai/quickstart
