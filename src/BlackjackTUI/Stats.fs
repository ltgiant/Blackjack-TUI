module BlackjackTUI.Stats

open System
open System.IO
open Microsoft.Data.Sqlite
open BlackjackTUI.Domain

type Db =
    { Connection: SqliteConnection }
    interface IDisposable with
        member this.Dispose() = this.Connection.Dispose()

type GameId = int64

type RoundRecord =
    { RoundNo: int
      Bet: int
      PlayerValue: int
      DealerValue: int
      Outcome: RoundOutcome
      MoneyBefore: int
      MoneyAfter: int
      Hits: int
      Stood: bool }

type SummaryStats =
    { TotalGames: int
      Wins: int
      Losses: int
      WinRate: float
      AvgFinalMoney: float
      MaxFinalMoney: int
      MinFinalMoney: int
      LongestWinStreak: int
      TotalRounds: int
      AvgBet: float
      BustRate: float
      HitRate: float }

let emptySummary =
    { TotalGames = 0
      Wins = 0
      Losses = 0
      WinRate = 0.0
      AvgFinalMoney = 0.0
      MaxFinalMoney = 0
      MinFinalMoney = 0
      LongestWinStreak = 0
      TotalRounds = 0
      AvgBet = 0.0
      BustRate = 0.0
      HitRate = 0.0 }

let defaultPath () : string =
    let home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
    let dir = Path.Combine(home, ".blackjack-tui")
    if not (Directory.Exists dir) then Directory.CreateDirectory(dir) |> ignore
    Path.Combine(dir, "stats.db")

let private outcomeText (o: RoundOutcome) =
    match o with
    | PlayerBust -> "PlayerBust"
    | DealerBust -> "DealerBust"
    | PlayerWins -> "PlayerWins"
    | DealerWins -> "DealerWins"
    | Push -> "Push"

let private statusText (s: GameStatus) =
    match s with
    | Won -> "won"
    | Lost -> "lost"
    | Ongoing -> "ongoing"

let private nowIso () =
    DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")

let private exec (db: Db) (sql: string) =
    use cmd = db.Connection.CreateCommand()
    cmd.CommandText <- sql
    cmd.ExecuteNonQuery() |> ignore

let ensureSchema (db: Db) =
    exec db """
        CREATE TABLE IF NOT EXISTS games (
            id            INTEGER PRIMARY KEY AUTOINCREMENT,
            started_at    TEXT NOT NULL,
            ended_at      TEXT,
            result        TEXT,
            final_money   INTEGER,
            rounds_played INTEGER NOT NULL DEFAULT 0
        )
    """
    exec db """
        CREATE TABLE IF NOT EXISTS rounds (
            id            INTEGER PRIMARY KEY AUTOINCREMENT,
            game_id       INTEGER NOT NULL REFERENCES games(id),
            round_no      INTEGER NOT NULL,
            bet           INTEGER NOT NULL,
            player_value  INTEGER NOT NULL,
            dealer_value  INTEGER NOT NULL,
            outcome       TEXT NOT NULL,
            money_before  INTEGER NOT NULL,
            money_after   INTEGER NOT NULL,
            hits          INTEGER NOT NULL,
            stood         INTEGER NOT NULL,
            ended_at      TEXT NOT NULL
        )
    """
    exec db "CREATE INDEX IF NOT EXISTS idx_rounds_game ON rounds(game_id)"

let openDb (path: string) : Db =
    let connStr = SqliteConnectionStringBuilder()
    connStr.DataSource <- path
    let conn = new SqliteConnection(connStr.ToString())
    conn.Open()
    let db = { Connection = conn }
    ensureSchema db
    db

let close (db: Db) =
    db.Connection.Close()
    db.Connection.Dispose()

let beginGame (db: Db) : GameId =
    use cmd = db.Connection.CreateCommand()
    cmd.CommandText <- "INSERT INTO games (started_at) VALUES ($started); SELECT last_insert_rowid();"
    cmd.Parameters.AddWithValue("$started", nowIso ()) |> ignore
    let id = cmd.ExecuteScalar() :?> int64
    id

let recordRound (db: Db) (gameId: GameId) (r: RoundRecord) =
    use tx = db.Connection.BeginTransaction()
    use insert = db.Connection.CreateCommand()
    insert.Transaction <- tx
    insert.CommandText <- """
        INSERT INTO rounds
            (game_id, round_no, bet, player_value, dealer_value, outcome,
             money_before, money_after, hits, stood, ended_at)
        VALUES
            ($g, $n, $bet, $pv, $dv, $o, $mb, $ma, $h, $s, $t)
    """
    insert.Parameters.AddWithValue("$g", gameId) |> ignore
    insert.Parameters.AddWithValue("$n", r.RoundNo) |> ignore
    insert.Parameters.AddWithValue("$bet", r.Bet) |> ignore
    insert.Parameters.AddWithValue("$pv", r.PlayerValue) |> ignore
    insert.Parameters.AddWithValue("$dv", r.DealerValue) |> ignore
    insert.Parameters.AddWithValue("$o", outcomeText r.Outcome) |> ignore
    insert.Parameters.AddWithValue("$mb", r.MoneyBefore) |> ignore
    insert.Parameters.AddWithValue("$ma", r.MoneyAfter) |> ignore
    insert.Parameters.AddWithValue("$h", r.Hits) |> ignore
    insert.Parameters.AddWithValue("$s", (if r.Stood then 1 else 0)) |> ignore
    insert.Parameters.AddWithValue("$t", nowIso ()) |> ignore
    insert.ExecuteNonQuery() |> ignore

    use bump = db.Connection.CreateCommand()
    bump.Transaction <- tx
    bump.CommandText <- "UPDATE games SET rounds_played = rounds_played + 1 WHERE id = $g"
    bump.Parameters.AddWithValue("$g", gameId) |> ignore
    bump.ExecuteNonQuery() |> ignore

    tx.Commit()

let endGame (db: Db) (gameId: GameId) (status: GameStatus) (finalMoney: int) =
    use cmd = db.Connection.CreateCommand()
    cmd.CommandText <- """
        UPDATE games
        SET ended_at = $t, result = $r, final_money = $m
        WHERE id = $g
    """
    cmd.Parameters.AddWithValue("$t", nowIso ()) |> ignore
    cmd.Parameters.AddWithValue("$r", statusText status) |> ignore
    cmd.Parameters.AddWithValue("$m", finalMoney) |> ignore
    cmd.Parameters.AddWithValue("$g", gameId) |> ignore
    cmd.ExecuteNonQuery() |> ignore

let private scalar (db: Db) (sql: string) : obj =
    use cmd = db.Connection.CreateCommand()
    cmd.CommandText <- sql
    cmd.ExecuteScalar()

let private asInt (v: obj) =
    if isNull v || v = box DBNull.Value then 0
    else Convert.ToInt32(v)

let private asFloat (v: obj) =
    if isNull v || v = box DBNull.Value then 0.0
    else Convert.ToDouble(v)

let private longestWinStreak (db: Db) : int =
    use cmd = db.Connection.CreateCommand()
    cmd.CommandText <- """
        SELECT outcome FROM rounds
        WHERE game_id IN (SELECT id FROM games WHERE result IN ('won','lost'))
        ORDER BY game_id, round_no
    """
    use reader = cmd.ExecuteReader()
    let mutable best = 0
    let mutable cur = 0
    while reader.Read() do
        let o = reader.GetString(0)
        if o = "PlayerWins" || o = "DealerBust" then
            cur <- cur + 1
            if cur > best then best <- cur
        else
            cur <- 0
    best

let getSummary (db: Db) : SummaryStats =
    let totalGames =
        scalar db "SELECT COUNT(*) FROM games WHERE result IN ('won','lost')" |> asInt
    if totalGames = 0 then
        emptySummary
    else
        let wins = scalar db "SELECT COUNT(*) FROM games WHERE result = 'won'" |> asInt
        let losses = scalar db "SELECT COUNT(*) FROM games WHERE result = 'lost'" |> asInt
        let avgFinal =
            scalar db "SELECT AVG(final_money) FROM games WHERE result IN ('won','lost')"
            |> asFloat
        let maxFinal =
            scalar db "SELECT MAX(final_money) FROM games WHERE result IN ('won','lost')"
            |> asInt
        let minFinal =
            scalar db "SELECT MIN(final_money) FROM games WHERE result IN ('won','lost')"
            |> asInt
        let totalRounds =
            scalar db """
                SELECT COUNT(*) FROM rounds
                WHERE game_id IN (SELECT id FROM games WHERE result IN ('won','lost'))
            """ |> asInt
        let avgBet =
            scalar db """
                SELECT AVG(bet) FROM rounds
                WHERE game_id IN (SELECT id FROM games WHERE result IN ('won','lost'))
            """ |> asFloat
        let bustRounds =
            scalar db """
                SELECT COUNT(*) FROM rounds
                WHERE outcome = 'PlayerBust'
                  AND game_id IN (SELECT id FROM games WHERE result IN ('won','lost'))
            """ |> asInt
        let totalHits =
            scalar db """
                SELECT COALESCE(SUM(hits), 0) FROM rounds
                WHERE game_id IN (SELECT id FROM games WHERE result IN ('won','lost'))
            """ |> asInt
        let totalStands =
            scalar db """
                SELECT COALESCE(SUM(stood), 0) FROM rounds
                WHERE game_id IN (SELECT id FROM games WHERE result IN ('won','lost'))
            """ |> asInt
        let denomActions = totalHits + totalStands
        let streak = longestWinStreak db
        let winRate =
            let denom = wins + losses
            if denom = 0 then 0.0 else float wins / float denom
        let bustRate =
            if totalRounds = 0 then 0.0
            else float bustRounds / float totalRounds
        let hitRate =
            if denomActions = 0 then 0.0
            else float totalHits / float denomActions
        { TotalGames = totalGames
          Wins = wins
          Losses = losses
          WinRate = winRate
          AvgFinalMoney = avgFinal
          MaxFinalMoney = maxFinal
          MinFinalMoney = minFinal
          LongestWinStreak = streak
          TotalRounds = totalRounds
          AvgBet = avgBet
          BustRate = bustRate
          HitRate = hitRate }

let resetStats (db: Db) =
    exec db "DELETE FROM rounds"
    exec db "DELETE FROM games"
    exec db "DELETE FROM sqlite_sequence WHERE name IN ('games','rounds')"
