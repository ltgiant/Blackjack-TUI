module BlackjackTUI.Program

open System
open System.Threading
open Spectre.Console
open BlackjackTUI.Domain
open BlackjackTUI.Deck
open BlackjackTUI.Game
open BlackjackTUI.Stats
open BlackjackTUI.View
open BlackjackTUI.Screens

let private pause (ms: int) =
    if not (Console.IsInputRedirected) then Thread.Sleep(ms)

let private redrawTable (money: int) (bet: int) (player: Hand) (dealer: Hand) (hideDealer: bool) =
    AnsiConsole.Clear()
    writeHeader ()
    writeMoney money
    AnsiConsole.MarkupLine(sprintf "[bold]Bet:[/] [yellow]$%d[/]" bet)
    AnsiConsole.WriteLine()
    if hideDealer then writeDealerHandHidden dealer
    else writeDealerHand dealer
    AnsiConsole.WriteLine()
    writePlayerHand player

// player turn: tracks hit count and whether the player ended on stand
let rec private playerTurn
    (money: int) (bet: int) (player: Hand) (dealer: Hand) (deck: Card list) (hits: int)
    : Hand * Card list * int * bool * bool =
    redrawTable money bet player dealer true
    if isBust player then
        player, deck, hits, false, true   // hits, stood=false, busted=true
    else
        let action = promptAction ()
        if action = "hit" then
            let card, rest = drawCard deck
            playerTurn money bet (player @ [ card ]) dealer rest (hits + 1)
        else
            player, deck, hits, true, false   // stood=true, busted=false

let private dealerTurn (money: int) (bet: int) (player: Hand) (dealer: Hand) (deck: Card list)
    : Hand * Card list =
    let rec loop dealer deck =
        redrawTable money bet player dealer false
        pause 700
        if handValue dealer < 17 then
            writeDealerHits ()
            pause 500
            let card, rest = drawCard deck
            loop (dealer @ [ card ]) rest
        else
            dealer, deck
    loop dealer deck

let private playRound (db: Db) (gameId: GameId) (roundNo: int) (rng: Random) (money: int) : int =
    AnsiConsole.Clear()
    writeHeader ()
    writeMoney money
    AnsiConsole.WriteLine()

    let bet = promptBet money

    let deck = createDeck () |> shuffle rng
    let player, deck = drawN 2 deck
    let dealer, deck = drawN 2 deck

    let player, deck, hits, stood, busted = playerTurn money bet player dealer deck 0

    let finalDealer =
        if busted then dealer
        else
            let d, _ = dealerTurn money bet player dealer deck
            d

    redrawTable money bet player finalDealer false

    let outcome = determineOutcome player finalDealer
    writeOutcome outcome
    let newMoney = applyOutcome money bet outcome
    AnsiConsole.WriteLine()
    AnsiConsole.MarkupLine(
        sprintf "[bold]Money:[/] [green]$%d[/]  →  [bold]$%d[/]" money newMoney)

    recordRound db gameId
        { RoundNo = roundNo
          Bet = bet
          PlayerValue = handValue player
          DealerValue = handValue finalDealer
          Outcome = outcome
          MoneyBefore = money
          MoneyAfter = newMoney
          Hits = hits
          Stood = stood }

    AnsiConsole.WriteLine()
    AnsiConsole.MarkupLine("[grey]Press [bold]Enter[/] to continue…[/]")
    Console.ReadLine() |> ignore
    newMoney

let private playFullGame (db: Db) (rng: Random) : unit =
    let gameId = beginGame db
    let rec rounds roundNo money =
        match gameStatus money with
        | Won ->
            endGame db gameId Won money
            AnsiConsole.Clear()
            writeHeader ()
            writeGameWon money
            AnsiConsole.WriteLine()
            AnsiConsole.MarkupLine("[grey]Press [bold]Enter[/] to return to home…[/]")
            Console.ReadLine() |> ignore
        | Lost ->
            endGame db gameId Lost money
            AnsiConsole.Clear()
            writeHeader ()
            writeGameLost ()
            AnsiConsole.WriteLine()
            AnsiConsole.MarkupLine("[grey]Press [bold]Enter[/] to return to home…[/]")
            Console.ReadLine() |> ignore
        | Ongoing ->
            let next = playRound db gameId roundNo rng money
            rounds (roundNo + 1) next
    rounds 1 StartingMoney

[<EntryPoint>]
let main _ =
    Console.OutputEncoding <- Text.Encoding.UTF8
    let rng = Random()
    let db = openDb (defaultPath ())

    let rec loop screen =
        match screen with
        | Home ->
            let next = home ()
            loop next
        | Tutorial ->
            tutorial ()
            loop Home
        | Game ->
            playFullGame db rng
            loop Home
        | Stats ->
            stats db
            loop Home
        | Quit ->
            AnsiConsole.Clear()
            AnsiConsole.MarkupLine("[grey]Thanks for playing. See you next time![/]")

    try loop Home
    finally close db
    0
