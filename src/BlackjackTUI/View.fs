module BlackjackTUI.View

open Spectre.Console
open BlackjackTUI.Domain
open BlackjackTUI.Game

let private suitColor (suit: Suit) =
    match suit with
    | Hearts | Diamonds -> "red"
    | Spades | Clubs -> "white"

let private suitMarkup (suit: Suit) =
    sprintf "[%s]%s[/]" (suitColor suit) (suitSymbol suit)

let cardLines (card: Card) : string list =
    let r = rankSymbol card.Rank
    let s = suitMarkup card.Suit
    let rTop = r.PadRight(3)
    let rBottom = r.PadLeft(3)
    [ "┌─────┐"
      sprintf "│%s  │" rTop
      sprintf "│  %s  │" s
      sprintf "│  %s│" rBottom
      "└─────┘" ]

let hiddenCardLines () : string list =
    [ "┌─────┐"
      "│░░░░░│"
      "│░ ? ░│"
      "│░░░░░│"
      "└─────┘" ]

let private joinSideBySide (blocks: string list list) : string =
    let height = blocks |> List.head |> List.length
    [ for row in 0 .. height - 1 ->
          blocks
          |> List.map (fun b -> b.[row])
          |> String.concat " " ]
    |> String.concat "\n"

let renderHand (hand: Hand) : string =
    hand |> List.map cardLines |> joinSideBySide

let renderHandHidden (visible: Card) : string =
    joinSideBySide [ cardLines visible; hiddenCardLines () ]

let writeHeader () =
    AnsiConsole.Clear()
    let header = FigletText("Blackjack").LeftJustified().Color(Color.Gold1)
    AnsiConsole.Write(header)
    AnsiConsole.Write(Rule("[yellow]F# TUI Edition[/]").LeftJustified())
    AnsiConsole.WriteLine()

let writeMoney (money: int) =
    let panel =
        Panel(Markup(sprintf "[bold green]$%d[/]  [grey](goal: $%d)[/]" money TargetMoney))
            .Header("[bold]Money[/]")
            .RoundedBorder()
    AnsiConsole.Write(panel)

let writeRoundSeparator () =
    AnsiConsole.WriteLine()
    AnsiConsole.Write(Rule().DoubleBorder())
    AnsiConsole.WriteLine()

let writePlayerHand (hand: Hand) =
    AnsiConsole.MarkupLine(sprintf "[bold cyan]Player[/]  [grey](%d)[/]" (handValue hand))
    AnsiConsole.Markup(renderHand hand)
    AnsiConsole.WriteLine()

let writeDealerHandHidden (dealer: Hand) =
    AnsiConsole.MarkupLine("[bold magenta]Dealer[/]  [grey](?)[/]")
    AnsiConsole.Markup(renderHandHidden dealer.[0])
    AnsiConsole.WriteLine()

let writeDealerHand (dealer: Hand) =
    AnsiConsole.MarkupLine(sprintf "[bold magenta]Dealer[/]  [grey](%d)[/]" (handValue dealer))
    AnsiConsole.Markup(renderHand dealer)
    AnsiConsole.WriteLine()

let writeOutcome (outcome: RoundOutcome) =
    AnsiConsole.WriteLine()
    let text =
        match outcome with
        | PlayerBust -> "[red bold]Bust! You lose this round.[/]"
        | DealerBust -> "[green bold]Dealer busts. You win![/]"
        | PlayerWins -> "[green bold]You win![/]"
        | DealerWins -> "[red bold]Dealer wins.[/]"
        | Push -> "[yellow bold]Push.[/]"
    AnsiConsole.Write(Panel(Markup(text)).RoundedBorder())

let writeDealerHits () =
    AnsiConsole.MarkupLine("[grey italic]Dealer hits…[/]")

let writeGameWon (money: int) =
    AnsiConsole.WriteLine()
    let panel =
        Panel(Markup(sprintf "[bold green]You reached $%d.[/]\n[green]You win the game![/]" money))
            .Header("[bold green]:trophy: VICTORY[/]")
            .DoubleBorder()
    AnsiConsole.Write(panel)

let writeGameLost () =
    AnsiConsole.WriteLine()
    let panel =
        Panel(Markup("[bold red]You ran out of money.[/]\n[red]Game over.[/]"))
            .Header("[bold red]:skull: GAME OVER[/]")
            .DoubleBorder()
    AnsiConsole.Write(panel)

let promptBet (money: int) : int =
    let prompt =
        TextPrompt<int>(sprintf "Enter your bet [grey](1-%d)[/]:" money)
            .PromptStyle("cyan")
            .ValidationErrorMessage("[red]Invalid bet. Must be > 0 and <= your money.[/]")
            .Validate(fun bet ->
                if isValidBet money bet then ValidationResult.Success()
                else ValidationResult.Error("[red]Bet must be > 0 and <= your money.[/]"))
    AnsiConsole.Prompt(prompt)

let promptAction () : string =
    let prompt =
        SelectionPrompt<string>()
            .Title("[bold]Your action:[/]")
            .AddChoices([| "hit"; "stand" |])
    AnsiConsole.Prompt(prompt)
