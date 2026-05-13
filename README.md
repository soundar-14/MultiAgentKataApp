# MultiAgentKataApp

## Use Case 11: Bug Analysis & Fix Recommendation — Multi-Agent System

A .NET 10 ASP.NET Core Web API designed as the **sample application** for a 5-agent bug analysis pipeline built in [EPAM CodeMie](https://www.codemie.ai) (no-code platform).

---

## 🏗️ Multi-Agent Architecture (CodeMie Workflow)

```
[Jira Ticket Input]
  → Agent 1: Bug Reader Agent      (Reads Jira ticket via Jira Tool)
  → Agent 2: Log Analysis Agent    (Reads GitHub repo code via GitHub Tool)
  → Agent 3: Root Cause Analysis   (LLM reasoning — no external tools)
  → Agent 4: Solution Recommender  (Reads source code, suggests fixes)
  → Agent 5: Summary Agent         (Posts analysis report back to Jira)
  → Human Developer Review
```

---

## 🐛 Bug Scenarios in This App (for agent testing)

| Endpoint | Bug Scenario | Exception |
|---|---|---|
| `GET /api/bug/order/2` | NullReferenceException — Customer is null | `NullReferenceException` |
| `GET /api/bug/orders` | DB connection pool exhaustion under load | `TimeoutException` |
| `POST /api/bug/payment` | Missing config key `PaymentGateway:Url` | `InvalidOperationException` |
| `GET /api/bug/report/13` | No input validation (month > 12) | `ArgumentOutOfRangeException` |
| `POST /api/bug/inventory/update` | Race condition — no concurrency control | Data inconsistency |

---

## 🚀 Getting Started

```bash
cd MultiAgentKataApp
dotnet run
```

API available at: `https://localhost:7xxx/api/bug`

---

## 📁 CodeMie Agent Configuration

All agent prompts and setup instructions are in:
- `codemie-agents/agent-prompts.txt` — System prompts for all 5 agents
- `codemie-agents/codemie-setup-steps.txt` — Step-by-step CodeMie workflow setup guide

---

## 🔗 Links

- **Jira Project**: [EPMCDMETST](https://jiraeu.epam.com/secure/RapidBoard.jspa?rapidView=328303&projectKey=EPMCDMETST)
- **CodeMie Platform**: https://www.codemie.ai
- **Tech Stack**: .NET 10, ASP.NET Core Web API, C#
