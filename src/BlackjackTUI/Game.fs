module BlackjackTUI.Game

open BlackjackTUI.Domain
open BlackjackTUI.Deck

let rankBaseValue (rank: Rank) : int =
    match rank with
    | Two -> 2
    | Three -> 3
    | Four -> 4
    | Five -> 5
    | Six -> 6
    | Seven -> 7
    | Eight -> 8
    | Nine -> 9
    | Ten | Jack | Queen | King -> 10
    | Ace -> 11

let handValue (hand: Hand) : int =
    let total = hand |> List.sumBy (fun c -> rankBaseValue c.Rank)
    let aces = hand |> List.filter (fun c -> c.Rank = Ace) |> List.length
    let mutable v = total
    let mutable a = aces
    while v > 21 && a > 0 do
        v <- v - 10
        a <- a - 1
    v

let isBust (hand: Hand) : bool = handValue hand > 21

let rec dealerPlay (dealerHand: Hand) (deck: Card list) : Hand * Card list =
    if handValue dealerHand < 17 then
        let card, rest = drawCard deck
        dealerPlay (dealerHand @ [ card ]) rest
    else
        dealerHand, deck

let determineOutcome (playerHand: Hand) (dealerHand: Hand) : RoundOutcome =
    if isBust playerHand then PlayerBust
    elif isBust dealerHand then DealerBust
    else
        let p = handValue playerHand
        let d = handValue dealerHand
        if p > d then PlayerWins
        elif d > p then DealerWins
        else Push

let applyOutcome (money: int) (bet: int) (outcome: RoundOutcome) : int =
    match outcome with
    | PlayerBust | DealerWins -> money - bet
    | DealerBust | PlayerWins -> money + bet
    | Push -> money

let gameStatus (money: int) : GameStatus =
    if money >= TargetMoney then Won
    elif money <= 0 then Lost
    else Ongoing

let isValidBet (money: int) (bet: int) : bool =
    bet > 0 && bet <= money

let suitSymbol (suit: Suit) : string =
    match suit with
    | Spades -> "♠"
    | Hearts -> "♥"
    | Diamonds -> "♦"
    | Clubs -> "♣"

let rankSymbol (rank: Rank) : string =
    match rank with
    | Two -> "2"
    | Three -> "3"
    | Four -> "4"
    | Five -> "5"
    | Six -> "6"
    | Seven -> "7"
    | Eight -> "8"
    | Nine -> "9"
    | Ten -> "10"
    | Jack -> "J"
    | Queen -> "Q"
    | King -> "K"
    | Ace -> "A"

let formatCard (card: Card) : string =
    rankSymbol card.Rank + suitSymbol card.Suit

let formatHand (hand: Hand) : string =
    let cards = hand |> List.map formatCard |> String.concat ", "
    sprintf "[%s] (%d)" cards (handValue hand)
