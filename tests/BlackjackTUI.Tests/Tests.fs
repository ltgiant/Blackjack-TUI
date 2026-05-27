module BlackjackTUI.Tests

open Xunit
open BlackjackTUI.Domain
open BlackjackTUI.Deck
open BlackjackTUI.Game

let card s r = { Suit = s; Rank = r }

[<Fact>]
let ``handValue: number cards add up`` () =
    let hand = [ card Spades Five; card Hearts Seven ]
    Assert.Equal(12, handValue hand)

[<Fact>]
let ``handValue: face cards count as 10`` () =
    let hand = [ card Spades Jack; card Hearts Queen ]
    Assert.Equal(20, handValue hand)

[<Fact>]
let ``handValue: single ace counts as 11 when safe`` () =
    let hand = [ card Spades Ace; card Hearts Eight ]
    Assert.Equal(19, handValue hand)

[<Fact>]
let ``handValue: ace downgrades to 1 to avoid bust`` () =
    let hand = [ card Spades Ace; card Hearts Nine; card Clubs Five ]
    Assert.Equal(15, handValue hand)

[<Fact>]
let ``handValue: multiple aces - one stays 11, others 1`` () =
    let hand = [ card Spades Ace; card Hearts Ace; card Clubs Nine ]
    Assert.Equal(21, handValue hand)

[<Fact>]
let ``handValue: all aces downgrade when needed`` () =
    let hand = [ card Spades Ace; card Hearts Ace; card Clubs Ace; card Diamonds Nine ]
    Assert.Equal(12, handValue hand)

[<Fact>]
let ``isBust: hand over 21 is bust`` () =
    let hand = [ card Spades King; card Hearts Queen; card Clubs Five ]
    Assert.True(isBust hand)

[<Fact>]
let ``isBust: hand of 21 is not bust`` () =
    let hand = [ card Spades King; card Hearts Ace ]
    Assert.False(isBust hand)

[<Fact>]
let ``createDeck: produces 52 unique cards`` () =
    let deck = createDeck ()
    Assert.Equal(52, deck.Length)
    Assert.Equal(52, deck |> List.distinct |> List.length)

[<Fact>]
let ``shuffle: preserves all cards`` () =
    let original = createDeck ()
    let rng = System.Random(42)
    let shuffled = shuffle rng original
    Assert.Equal(52, shuffled.Length)
    let originalSorted: Card list = original |> List.sortBy (fun c -> c.Suit, c.Rank)
    let shuffledSorted: Card list = shuffled |> List.sortBy (fun c -> c.Suit, c.Rank)
    Assert.Equal<Card list>(originalSorted, shuffledSorted)

[<Fact>]
let ``drawN: removes N cards from top`` () =
    let deck = createDeck ()
    let drawn, rest = drawN 2 deck
    Assert.Equal(2, drawn.Length)
    Assert.Equal(50, rest.Length)

[<Fact>]
let ``dealerPlay: stands at 17`` () =
    let dealerHand = [ card Spades Ten; card Hearts Seven ]
    let deck = [ card Clubs Five ]
    let final, _ = dealerPlay dealerHand deck
    Assert.Equal(2, final.Length)
    Assert.Equal(17, handValue final)

[<Fact>]
let ``dealerPlay: hits while under 17`` () =
    let dealerHand = [ card Spades Ten; card Hearts Six ]
    let deck = [ card Clubs Five ]
    let final, _ = dealerPlay dealerHand deck
    Assert.Equal(3, final.Length)
    Assert.Equal(21, handValue final)

[<Fact>]
let ``determineOutcome: player bust`` () =
    let p = [ card Spades King; card Hearts King; card Clubs Five ]
    let d = [ card Diamonds Ten; card Hearts Seven ]
    Assert.Equal(PlayerBust, determineOutcome p d)

[<Fact>]
let ``determineOutcome: dealer bust`` () =
    let p = [ card Spades Ten; card Hearts Eight ]
    let d = [ card Diamonds King; card Hearts King; card Clubs Five ]
    Assert.Equal(DealerBust, determineOutcome p d)

[<Fact>]
let ``determineOutcome: higher hand wins`` () =
    let p = [ card Spades Ten; card Hearts Nine ]
    let d = [ card Diamonds Ten; card Hearts Seven ]
    Assert.Equal(PlayerWins, determineOutcome p d)

[<Fact>]
let ``determineOutcome: dealer higher wins`` () =
    let p = [ card Spades Ten; card Hearts Six ]
    let d = [ card Diamonds Ten; card Hearts Nine ]
    Assert.Equal(DealerWins, determineOutcome p d)

[<Fact>]
let ``determineOutcome: equal value is push`` () =
    let p = [ card Spades Ten; card Hearts Eight ]
    let d = [ card Diamonds King; card Hearts Eight ]
    Assert.Equal(Push, determineOutcome p d)

[<Fact>]
let ``applyOutcome: win adds bet`` () =
    Assert.Equal(120, applyOutcome 100 20 PlayerWins)
    Assert.Equal(120, applyOutcome 100 20 DealerBust)

[<Fact>]
let ``applyOutcome: lose subtracts bet`` () =
    Assert.Equal(80, applyOutcome 100 20 DealerWins)
    Assert.Equal(80, applyOutcome 100 20 PlayerBust)

[<Fact>]
let ``applyOutcome: push unchanged`` () =
    Assert.Equal(100, applyOutcome 100 20 Push)

[<Fact>]
let ``gameStatus: ongoing when between bounds`` () =
    Assert.Equal(Ongoing, gameStatus 500)
    Assert.Equal(Ongoing, gameStatus 1)
    Assert.Equal(Ongoing, gameStatus 999)

[<Fact>]
let ``gameStatus: win at 1000+`` () =
    Assert.Equal(Won, gameStatus 1000)
    Assert.Equal(Won, gameStatus 1500)

[<Fact>]
let ``gameStatus: loss at 0`` () =
    Assert.Equal(Lost, gameStatus 0)

[<Fact>]
let ``isValidBet: zero and negative invalid`` () =
    Assert.False(isValidBet 100 0)
    Assert.False(isValidBet 100 -5)

[<Fact>]
let ``isValidBet: over money invalid`` () =
    Assert.False(isValidBet 100 101)

[<Fact>]
let ``isValidBet: equal to money valid`` () =
    Assert.True(isValidBet 100 100)

[<Fact>]
let ``isValidBet: within range valid`` () =
    Assert.True(isValidBet 100 1)
    Assert.True(isValidBet 100 50)

[<Fact>]
let ``formatCard: face card with suit`` () =
    Assert.Equal("K♦", formatCard (card Diamonds King))

[<Fact>]
let ``formatCard: ten of spades`` () =
    Assert.Equal("10♠", formatCard (card Spades Ten))

[<Fact>]
let ``formatHand: shows cards and total`` () =
    let h = [ card Spades Ten; card Hearts Seven ]
    Assert.Equal("[10♠, 7♥] (17)", formatHand h)
