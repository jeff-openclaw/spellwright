# Spellwright 🧙‍♂️

A word-guessing roguelike with LLM-powered NPCs and Balatro-style modifier stacking.

**Core loop:** Enter encounter → LLM NPC presents clues for a hidden word → guess the word → earn rewards → collect Tomes (modifiers) that change the rules → survive the run.

## Repo Structure

```
spellwright/
├── docs/
│   ├── research/       # Technical research, game design, architecture
│   └── prepared/       # Pre-built assets (word lists, NPC prompts, C# code)
├── game/               # Unity project (2022.3 LTS, URP)
└── README.md
```

## Tech Stack

- **Engine:** Unity 2022.3 LTS (URP)
- **LLM:** Ollama (local inference) — Qwen 2.5 7B + Llama 3.2 3B fallback
- **Word Validation:** WeCantSpell.Hunspell
- **Dev Workflow:** Claude Code + Unity MCP
- **Dev Platform:** MacBook M1 Pro

## Getting Started

1. Clone this repo
2. Install Ollama: `brew install ollama`
3. Pull models: `ollama pull qwen2.5:7b && ollama pull llama3.2:3b`
4. Open `game/` in Unity 2022.3 LTS
5. Follow [Issue #1](../../issues/1) for full setup

## Sprint Board

See [Issues](../../issues) for the 12-card MVP sprint plan with step-by-step guides.

## Research

- [Technical Research](docs/research/01_technical.md)
- [Game Design Research](docs/research/02_game_design.md)
- [Game Design Document](docs/research/03_game_design_document.md)
- [Architecture](docs/research/04_architecture.md)
