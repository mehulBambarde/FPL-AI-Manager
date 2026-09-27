# FPL AI Manager

A side project where a handful of AI agents team up to help with Fantasy Premier League transfers.

You give it your team ID, the gameweek, how much money you've got in the bank and how many free transfers you have. Five agents then take turns on the problem: one looks at your squad and picks someone to sell, another works out the budget, a third shortlists replacements, a fourth checks their upcoming fixtures, and the last one makes the call. The whole thing takes about 15–30 seconds and you get back a plain-English "sell X, buy Y" with reasons.

There's also a player search that runs over a couple of seasons of historical gameweek data stored in a vector database, so you can ask things like *"Haaland home games"* or *"cheap midfielders scoring goals"* and get back the closest matching performances.

I built it mainly to get hands-on with the [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) and Azure AI Foundry, and to have something real to show for it.

## How it's put together

```
src/
├── FPL.AI.Manager.Agents     Agents, their tools, and the transfer workflow
├── FPL.AI.Manager.RAG        Embeddings, Qdrant, and CSV ingestion
├── FPL.AI.Manager.API        ASP.NET Core Web API (controllers + MediatR handlers)
├── FPL.AI.Manager.Frontend   Vue 3 single-page UI
└── FPL.AI.Manager.Console    The original console prototype I started with
```

**Agents** holds the five agents and the sequential workflow that chains them together. Each agent only gets the FPL API tools it actually needs. The tools talk to the public FPL API (`fantasy.premierleague.com/api`) and trim the responses down before the model sees them. More on why below.

**RAG** turns each row of the historical CSVs into a short sentence ("Player: Salah, Season: 2024-25, Gameweek: 5, Points: 6 …"), embeds it with `text-embedding-3-small`, and stores it in Qdrant. Searching does the same thing in reverse: embed the question, find the nearest rows.

**API** is deliberately thin. Controllers just hand requests to MediatR, and the handlers call into the Agents and RAG libraries.

**Frontend** is a small Vue 3 app with Tailwind (via CDN) and a dark theme. There's no router or state library, because it doesn't need one.

The **Console** app is where this all started. I've left it as it is for reference.

### The transfer workflow

```
SquadAnalyser → BudgetCalculator → ReplacementFinder → FixtureAnalyst → DecisionMaker
```

Each agent sees everything the previous ones said, so by the time the DecisionMaker runs it has the full picture.

## Running it locally

### What you'll need

- .NET 10 SDK
- Node.js 22.18+ (or 24.12+)
- Docker, for Qdrant
- The Azure CLI, logged in (`az login`)
- An Azure AI Foundry project with two deployments: `gpt-4.1-mini` for the agents and `text-embedding-3-small` for embeddings

There are no API keys anywhere in the code. Everything authenticates with `AzureCliCredential`, so whichever account you're logged into with `az login` needs access to the Foundry resource.

### 1. Start Qdrant

```bash
docker run -d -p 6333:6333 -p 6334:6334 qdrant/qdrant
```

The .NET client talks to Qdrant over gRPC, which runs on **6334**. Port 6333 is the REST API and dashboard (handy for poking around at `http://localhost:6333/dashboard`).

### 2. Add your settings

`appsettings.json` files aren't checked in, so create `src/FPL.AI.Manager.API/appsettings.json` yourself:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource>.services.ai.azure.com/api/projects/<your-project>",
    "BaseEndpoint": "https://<your-resource>.cognitiveservices.azure.com/",
    "DeploymentName": "gpt-4.1-mini",
    "EmbeddingDeploymentName": "text-embedding-3-small"
  },
  "Qdrant": {
    "Host": "localhost",
    "Port": 6334,
    "CollectionName": "fpl_historical",
    "VectorSize": 1536
  }
}
```

Yes, there are two endpoints, and it caught me out. The agents use the Foundry **project** endpoint, but the embeddings client needs the plain **resource** endpoint. If you point the embeddings at the project URL, you'll get a 401 because the token is for the wrong audience.

### 3. Run the API

```bash
dotnet run --project src/FPL.AI.Manager.API --launch-profile http
```

It listens on `http://localhost:5190`, and Swagger UI is at `http://localhost:5190/swagger`.

### 4. Load some historical data (for player search)

The CSVs live in `src/FPL.AI.Manager.RAG/Data/`. Kick off an ingest through the API, and note that the path is on the machine running the API:

```bash
curl -X POST http://localhost:5190/api/transfer/ingest \
  -H "Content-Type: application/json" \
  -d '{"csvFilePath": "/full/path/to/src/FPL.AI.Manager.RAG/Data/merged_gw_2024-25.csv", "season": "2024-25"}'
```

This embeds every row one at a time, so a full season takes a while. Go and make a cup of tea.

### 5. Run the frontend

```bash
cd src/FPL.AI.Manager.Frontend
npm install
npm run dev
```

Open `http://localhost:5173`. Vite proxies anything under `/api` to the API on port 5190, so there's no CORS fiddling needed in development.

## API

| Method | Route | What it does |
| ------ | ----- | ------------ |
| `POST` | `/api/transfer/recommend` | Runs the five-agent workflow and returns a recommendation |
| `POST` | `/api/transfer/ingest` | Loads a season's CSV into Qdrant |
| `GET`  | `/api/players/search?query=...&topN=5` | Semantic search over the historical data |

Example recommendation request:

```json
{
  "teamId": 6804083,
  "gameWeek": 6,
  "budget": 1.6,
  "transfersAvailable": 1,
  "willingToTakeHit": false
}
```

## Things I learned the hard way

- **Don't hand raw FPL data to a model.** The `bootstrap-static` endpoint is almost 2 MB of JSON and the fixtures list is around 230 KB. Passing either one straight to `gpt-4.1-mini` blows through the token-per-minute limit and you get a 429. Worse, the workflow swallows that error, so the later agents just quietly return nothing. The tools now filter and trim everything down to a few KB before the agent sees it.
- **Similarity scores are lower than you'd think.** With `text-embedding-3-small`, a really good match scores around 0.6, not 0.9. The search UI colours results with that in mind.
- **Qdrant has two ports**, and the .NET client wants the gRPC one (6334).

## Known limitations

- The squad analyser doesn't look at your *actual* team yet. `GetSquadAsync` builds a stand-in squad from the top scorers in each position. Switching it over to the `entry/{teamId}/event/{gw}/picks/` endpoint is next on the list.
- Ingestion embeds one row at a time, which is slow. Batching the embedding calls would speed it up a lot.
- The recommendation endpoint waits for the whole workflow to finish. Streaming each agent's output to the UI as it happens would make it feel much snappier.

## What's next

- An Azure Function App (`FPL.AI.Manager.Ingestion`) to run the RAG ingestion on a schedule instead of through the API
- Using the real squad from the FPL API
- Streaming agent responses to the frontend

## Tech

.NET 10 · ASP.NET Core · Microsoft Agent Framework · Azure AI Foundry (gpt-4.1-mini, text-embedding-3-small) · Qdrant · MediatR · CsvHelper · Vue 3 · Vite · Tailwind CSS
