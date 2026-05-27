# Blackjack TUI

A terminal-based blackjack game written in F#. The player starts with $100 and tries to reach $1,000 by playing rounds against a dealer.

## Game Rules

- **Starting money:** $100
- **Goal:** Reach $1,000 to win; lose all your money and the game ends in defeat.
- **Deck:** Standard 52 cards, freshly shuffled each round.
- **Card values:**
  - Number cards (2–10): face value
  - Jack, Queen, King: 10
  - Ace: 11 if the hand stays at or below 21, otherwise 1
- **Player turn:** Repeatedly choose `hit` or `stand`. Going over 21 is a bust and an immediate loss.
- **Dealer turn:** Reveals both cards, then hits while the hand value is below 17, stands at 17 or higher.
- **Round result:**
  - Higher hand wins
  - Equal hand value is a push (no money change)
  - Win adds the bet to your money; loss subtracts it.

## Project Layout

```
Blackjack-TUI/
├── BlackjackTUI.slnx
├── docs/
│   └── SPEC1.pdf                    # original specification
├── src/BlackjackTUI/
│   ├── BlackjackTUI.fsproj
│   ├── Domain.fs                    # core types (Card, Suit, Rank, Hand, ...)
│   ├── Deck.fs                      # 52-card deck, Fisher–Yates shuffle, draw
│   ├── Game.fs                      # hand value, dealer AI, outcome, formatting
│   ├── Stats.fs                     # SQLite repository + summary aggregations
│   ├── View.fs                      # Spectre.Console card boxes and panels
│   ├── Screens.fs                   # Home, Tutorial, Stats screens
│   └── Program.fs                   # TUI entry point and screen-transition loop
└── tests/BlackjackTUI.Tests/
    ├── BlackjackTUI.Tests.fsproj
    └── Tests.fs                     # xUnit unit tests
```

## Requirements

- .NET SDK 10.0 or later (the project targets `net10.0`)

Check your installation:

```bash
dotnet --version
```

## Build, Run, Test

From the repository root:

```bash
# Build everything
dotnet build BlackjackTUI.slnx

# Run the game
dotnet run --project src/BlackjackTUI/BlackjackTUI.fsproj

# Run the unit tests
dotnet test BlackjackTUI.slnx
```

## Example Session

```
=== Blackjack TUI ===

Money: $100

Enter your bet:

20

Player cards: [10♠, 7♥] (17)
Dealer shows: [K♦]

Action (hit/stand):

stand

Dealer cards: [K♦, 6♣] (16)
Dealer hits…
Dealer cards: [K♦, 6♣, 5♠] (21)

Dealer wins.

Money: $80

────

Enter your bet:
```

## Design Notes

- **Functional style:** Game state is modeled with immutable records and discriminated unions; each action returns a new state.
- **Ace handling:** All aces start as 11. While the total exceeds 21 and an ace remains, one ace is downgraded to 1. This handles multi-ace hands correctly.
- **Deck per round:** Each round draws from a freshly shuffled 52-card deck, so a round never runs out of cards.
- **Stats storage:** SQLite database at `~/.blackjack-tui/stats.db`. Only completed games (won/lost) are included in aggregates; abandoned games stay in the table with `result = NULL`.
- **Compilation order (F#):** `Domain.fs` → `Deck.fs` → `Game.fs` → `Stats.fs` → `View.fs` → `Screens.fs` → `Program.fs`, declared in `BlackjackTUI.fsproj`.

## Versions

This project evolves in clearly tagged steps. **`v1.0.0` is the reference release that satisfies `docs/SPEC1.pdf` exactly as written**; every later release builds on it without breaking the original specification.

| Tag | Theme | What it adds |
|-----|-------|--------------|
| [`v1.0.0`](https://github.com/ltgiant/Blackjack-TUI/releases/tag/v1.0.0) | **SPEC baseline** | First working implementation that meets every requirement in `docs/SPEC1.pdf`: $100→$1000, standard 52-card deck, Ace 11/1 handling, dealer hits below 17, push on tie, plain-text I/O exactly like the spec's example session. 31 unit tests. |
| [`v2.0.0`](https://github.com/ltgiant/Blackjack-TUI/releases/tag/v2.0.0) | **Full TUI rendering** | Replaces line-based output with Spectre.Console: unicode card boxes, suit-colored markup (red ♥♦ / white ♠♣), Figlet header, rounded Money panel, hidden dealer card pattern, interactive bet prompt and arrow-key hit/stand selection, paced dealer turn, victory/game-over screens. Domain logic and the 31 unit tests are unchanged. |
| [`v2.1.0`](https://github.com/ltgiant/Blackjack-TUI/releases/tag/v2.1.0) | **Home screen + tutorial** | Adds a navigable home menu (Start Game / How to Play / Quit) and a 3-page in-app tutorial (Basics, Card Values, Rules & Controls) with Prev/Next/Back navigation. Finished games auto-return to the home menu so multiple sessions can be played without restarting. |
| [`v2.2.0`](https://github.com/ltgiant/Blackjack-TUI/releases/tag/v2.2.0) | **Persistent stats** | Records every round and game in a local SQLite database (`~/.blackjack-tui/stats.db`). Adds a Stats screen with two panels — game aggregates (wins, losses, win rate, best/worst run, longest streak) and round aggregates (average bet, bust rate, hit/stand rate). Includes a Reset action with confirmation. 6 new integration tests against an in-memory SQLite database; total now 37 tests. |

In short: **`v1.0.0` = spec-faithful baseline**, and **`v2.x` = incremental UX and feature improvements** on top of that baseline.
