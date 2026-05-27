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
│   └── Program.fs                   # TUI entry point and main loop
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
- **Compilation order (F#):** `Domain.fs` → `Deck.fs` → `Game.fs` → `Program.fs`, declared in `BlackjackTUI.fsproj`.
