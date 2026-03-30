# Budget Manager

A personal budget tracking web app with AI-powered natural language transaction entry. Describe a purchase in plain English (e.g. "spent $40 on groceries yesterday") and the app uses Claude to parse it into a structured transaction.

## Features

- **Natural language input** — type a description of a transaction and Claude extracts the amount, date, and category automatically
- **Manual entry** — traditional form with amount, date, and category fields
- **Clarification flow** — if a transaction is ambiguous, the AI asks follow-up questions before saving
- **Spending chart** — simple stacked bar chart showing monthly spending by category (Chart.js)
- **SQLite persistence** — transactions stored locally via Entity Framework Core

## Tech Stack

- **Backend:** ASP.NET Core Minimal API (.NET 10)
- **Database:** SQLite via EF Core
- **AI:** Claude API (claude-sonnet-4-5) with structured JSON output
- **Frontend:** Vanilla HTML/CSS/JS with Chart.js