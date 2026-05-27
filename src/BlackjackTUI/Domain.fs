module BlackjackTUI.Domain

type Suit =
    | Spades
    | Hearts
    | Diamonds
    | Clubs

type Rank =
    | Two
    | Three
    | Four
    | Five
    | Six
    | Seven
    | Eight
    | Nine
    | Ten
    | Jack
    | Queen
    | King
    | Ace

type Card = { Suit: Suit; Rank: Rank }

type Hand = Card list

type RoundOutcome =
    | PlayerBust
    | DealerBust
    | PlayerWins
    | DealerWins
    | Push

type GameStatus =
    | Ongoing
    | Won
    | Lost

[<Literal>]
let StartingMoney = 100

[<Literal>]
let TargetMoney = 1000
