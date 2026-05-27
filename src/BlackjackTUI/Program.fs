module BlackjackTUI.Program

open System
open System.Threading
open Spectre.Console
open BlackjackTUI.Domain
open BlackjackTUI.Deck
open BlackjackTUI.Game
open BlackjackTUI.View

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

let rec private playerTurn (money: int) (bet: int) (player: Hand) (dealer: Hand) (deck: Card list)
    : Hand * Card list * bool =
    redrawTable money bet player dealer true
    if isBust player then
        player, deck, true
    else
        let action = promptAction ()
        if action = "hit" then
            let card, rest = drawCard deck
            playerTurn money bet (player @ [ card ]) dealer rest
        else
            player, deck, false

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

let private playRound (rng: Random) (money: int) : int =
    AnsiConsole.Clear()
    writeHeader ()
    writeMoney money
    AnsiConsole.WriteLine()

    let bet = promptBet money

    let deck = createDeck () |> shuffle rng
    let player, deck = drawN 2 deck
    let dealer, deck = drawN 2 deck

    let player, deck, busted = playerTurn money bet player dealer deck

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
    AnsiConsole.WriteLine()
    AnsiConsole.MarkupLine("[grey]Press [bold]Enter[/] to continue…[/]")
    Console.ReadLine() |> ignore
    newMoney

[<EntryPoint>]
let main _ =
    Console.OutputEncoding <- Text.Encoding.UTF8
    let rng = Random()

    let rec loop money =
        match gameStatus money with
        | Won ->
            AnsiConsole.Clear()
            writeHeader ()
            writeGameWon money
        | Lost ->
            AnsiConsole.Clear()
            writeHeader ()
            writeGameLost ()
        | Ongoing ->
            let next = playRound rng money
            loop next

    loop StartingMoney
    0
