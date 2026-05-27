module BlackjackTUI.Screens

open Spectre.Console
open BlackjackTUI.Domain
open BlackjackTUI.View

type Screen =
    | Home
    | Tutorial
    | Game
    | Quit

[<Literal>]
let Version = "v2.1.0"

let private drawHomeIntro () =
    AnsiConsole.Clear()
    writeHeader ()
    let intro =
        Markup(
            sprintf "[white]Beat the dealer and grow your bankroll from[/] [bold green]$%d[/] [white]to[/] [bold yellow]$%d[/][white].[/]"
                StartingMoney TargetMoney)
    AnsiConsole.Write(Panel(intro).Header("[bold]Welcome[/]").RoundedBorder())
    AnsiConsole.WriteLine()

let private drawFooter () =
    AnsiConsole.WriteLine()
    AnsiConsole.Write(Rule(sprintf "[grey]%s[/]" Version).RightJustified())

let home () : Screen =
    drawHomeIntro ()
    let menu =
        SelectionPrompt<string>()
            .Title("[bold]What would you like to do?[/]")
            .AddChoices([| "Start Game"; "How to Play"; "Quit" |])
    let choice = AnsiConsole.Prompt(menu)
    drawFooter ()
    match choice with
    | "Start Game" -> Game
    | "How to Play" -> Tutorial
    | _ -> Quit

type private NavChoice =
    | Prev
    | Next
    | BackHome

let private navPrompt (isFirst: bool) (isLast: bool) : NavChoice =
    let labels =
        [ if not isFirst then yield "← Prev", Prev
          if not isLast then yield "Next →", Next
          yield (if isLast then "Back to Home ⏎" else "Back to Home"), BackHome ]
    let prompt = SelectionPrompt<string>().Title("[grey]Navigate:[/]")
    for label, _ in labels do prompt.AddChoice(label) |> ignore
    let picked = AnsiConsole.Prompt(prompt)
    labels |> List.find (fun (l, _) -> l = picked) |> snd

let private pageHeader (n: int) (total: int) (title: string) =
    AnsiConsole.Clear()
    writeHeader ()
    AnsiConsole.Write(
        Rule(sprintf "[bold yellow]Page %d / %d  -  %s[/]" n total title)
            .LeftJustified())
    AnsiConsole.WriteLine()

let private tutorialBasics () =
    pageHeader 1 3 "Basics"
    let body =
        Markup(
            "[bold]Goal[/]\n" +
            sprintf "  Reach [bold yellow]$%d[/] from your starting [bold green]$%d[/].\n" TargetMoney StartingMoney +
            "  Lose all your money and the game ends.\n\n" +
            "[bold]One round in five steps[/]\n" +
            "  [cyan]1.[/] Place a bet\n" +
            "  [cyan]2.[/] You and the dealer each get two cards\n" +
            "      (one of the dealer's cards is hidden)\n" +
            "  [cyan]3.[/] You choose [yellow]hit[/] or [yellow]stand[/]\n" +
            "  [cyan]4.[/] Dealer reveals and plays automatically\n" +
            "  [cyan]5.[/] Higher hand (closer to 21) wins")
    AnsiConsole.Write(Panel(body).Header("[bold]Blackjack basics[/]").RoundedBorder())

let private tutorialCardValues () =
    pageHeader 2 3 "Card Values"
    let table = Table()
    table.AddColumn("[bold]Card[/]") |> ignore
    table.AddColumn("[bold]Value[/]") |> ignore
    table.AddRow("2 - 10", "face value (the number on the card)") |> ignore
    table.AddRow("J, Q, K", "10") |> ignore
    table.AddRow("A (Ace)", "[green]11[/] if your hand stays at or below 21, otherwise [yellow]1[/]") |> ignore
    table.Border <- TableBorder.Rounded
    AnsiConsole.Write(table)
    AnsiConsole.WriteLine()
    AnsiConsole.MarkupLine("[bold]Example[/]")
    let example =
        renderHand [ { Suit = Spades; Rank = Ten }; { Suit = Hearts; Rank = Seven } ]
    AnsiConsole.Markup(example)
    AnsiConsole.WriteLine()
    AnsiConsole.MarkupLine("  → hand value is [bold cyan]17[/]")
    AnsiConsole.WriteLine()
    AnsiConsole.MarkupLine(
        "[grey italic]Aces auto-adjust: an Ace counts as 11 until that would bust you, then it drops to 1.[/]")

let private tutorialRulesAndControls () =
    pageHeader 3 3 "Rules & Controls"
    let rules =
        Markup(
            "[bold]Player turn[/]\n" +
            "  [yellow]hit[/]   — take one more card. Going over 21 is a [red]bust[/] (instant loss).\n" +
            "  [yellow]stand[/] — keep your hand and end your turn.\n\n" +
            "[bold]Dealer rules (automatic)[/]\n" +
            "  Hits while hand value is [red]below 17[/].\n" +
            "  Stands at [green]17 or higher[/].\n\n" +
            "[bold]Result[/]\n" +
            "  • Higher hand wins the bet.\n" +
            "  • Equal hand value is a [yellow]push[/] (no money change).\n" +
            "  • Win: bet is added to your money.\n" +
            "  • Loss: bet is subtracted.")
    AnsiConsole.Write(Panel(rules).Header("[bold]Rules[/]").RoundedBorder())
    AnsiConsole.WriteLine()
    let controls = Table()
    controls.AddColumn("[bold]Action[/]") |> ignore
    controls.AddColumn("[bold]Keys[/]") |> ignore
    controls.AddRow("Move menu cursor", "[cyan]↑[/] / [cyan]↓[/]") |> ignore
    controls.AddRow("Select", "[cyan]Enter[/]") |> ignore
    controls.AddRow("Enter a bet", "type a number, then [cyan]Enter[/]") |> ignore
    controls.Border <- TableBorder.Rounded
    AnsiConsole.Write(Panel(controls).Header("[bold]Controls[/]").RoundedBorder())

let private renderPage (page: int) =
    match page with
    | 1 -> tutorialBasics ()
    | 2 -> tutorialCardValues ()
    | _ -> tutorialRulesAndControls ()

let tutorial () =
    let total = 3
    let rec loop page =
        renderPage page
        AnsiConsole.WriteLine()
        let isFirst = page = 1
        let isLast = page = total
        match navPrompt isFirst isLast with
        | Prev -> loop (max 1 (page - 1))
        | Next -> loop (min total (page + 1))
        | BackHome -> ()
    loop 1
