module BlackjackTUI.Program

open System
open BlackjackTUI.Domain
open BlackjackTUI.Deck
open BlackjackTUI.Game

let rec readBet (money: int) : int =
    Console.WriteLine()
    Console.WriteLine("Enter your bet:")
    Console.WriteLine()
    let line = Console.ReadLine()
    match Int32.TryParse(if isNull line then "" else line.Trim()) with
    | true, bet when isValidBet money bet -> bet
    | _ ->
        Console.WriteLine()
        Console.WriteLine("Invalid bet. Must be > 0 and <= your money.")
        readBet money

let rec readAction () : string =
    Console.WriteLine()
    Console.WriteLine("Action (hit/stand):")
    Console.WriteLine()
    let line = Console.ReadLine()
    let s = if isNull line then "" else line.Trim().ToLowerInvariant()
    match s with
    | "hit" | "stand" -> s
    | _ ->
        Console.WriteLine()
        Console.WriteLine("Invalid action. Type 'hit' or 'stand'.")
        readAction ()

let rec playerTurn (playerHand: Hand) (deck: Card list) : Hand * Card list * bool =
    let action = readAction ()
    if action = "hit" then
        let card, rest = drawCard deck
        let newHand = playerHand @ [ card ]
        Console.WriteLine()
        Console.WriteLine(sprintf "Player cards: %s" (formatHand newHand))
        if isBust newHand then
            newHand, rest, true
        else
            playerTurn newHand rest
    else
        playerHand, deck, false

let runDealerTurn (dealerHand: Hand) (deck: Card list) : Hand * Card list =
    Console.WriteLine()
    Console.WriteLine(sprintf "Dealer cards: %s" (formatHand dealerHand))
    let rec loop hand d =
        if handValue hand < 17 then
            Console.WriteLine("Dealer hits…")
            let card, rest = drawCard d
            let next = hand @ [ card ]
            Console.WriteLine(sprintf "Dealer cards: %s" (formatHand next))
            loop next rest
        else
            hand, d
    loop dealerHand deck

let printOutcome (outcome: RoundOutcome) =
    Console.WriteLine()
    match outcome with
    | PlayerBust -> Console.WriteLine("Bust! You lose.")
    | DealerBust -> Console.WriteLine("Dealer busts. You win.")
    | PlayerWins -> Console.WriteLine("You win.")
    | DealerWins -> Console.WriteLine("Dealer wins.")
    | Push -> Console.WriteLine("Push.")

let playRound (rng: Random) (money: int) : int =
    Console.WriteLine()
    Console.WriteLine(sprintf "Money: $%d" money)

    let bet = readBet money

    let deck = createDeck () |> shuffle rng
    let playerCards, deck = drawN 2 deck
    let dealerCards, deck = drawN 2 deck

    Console.WriteLine()
    Console.WriteLine(sprintf "Player cards: %s" (formatHand playerCards))
    Console.WriteLine(sprintf "Dealer shows: [%s]" (formatCard dealerCards.[0]))

    let playerHand, deck, busted = playerTurn playerCards deck

    let dealerHand, _ =
        if busted then dealerCards, deck
        else runDealerTurn dealerCards deck

    let outcome = determineOutcome playerHand dealerHand
    printOutcome outcome

    let newMoney = applyOutcome money bet outcome
    Console.WriteLine()
    Console.WriteLine(sprintf "Money: $%d" newMoney)
    Console.WriteLine()
    Console.WriteLine("────")
    newMoney

[<EntryPoint>]
let main _ =
    Console.OutputEncoding <- Text.Encoding.UTF8
    Console.WriteLine("=== Blackjack TUI ===")

    let rng = Random()

    let rec loop money =
        match gameStatus money with
        | Won ->
            Console.WriteLine()
            Console.WriteLine(sprintf "You reached $%d. You win the game!" money)
        | Lost ->
            Console.WriteLine()
            Console.WriteLine("You ran out of money. Game over.")
        | Ongoing ->
            let next = playRound rng money
            loop next

    loop StartingMoney
    0
