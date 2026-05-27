module BlackjackTUI.Deck

open System
open BlackjackTUI.Domain

let allSuits = [ Spades; Hearts; Diamonds; Clubs ]

let allRanks =
    [ Two; Three; Four; Five; Six; Seven; Eight; Nine; Ten; Jack; Queen; King; Ace ]

let createDeck () : Card list =
    [ for s in allSuits do
          for r in allRanks do
              yield { Suit = s; Rank = r } ]

let shuffle (rng: Random) (deck: Card list) : Card list =
    let arr = List.toArray deck
    let n = arr.Length
    for i in n - 1 .. -1 .. 1 do
        let j = rng.Next(i + 1)
        let tmp = arr.[i]
        arr.[i] <- arr.[j]
        arr.[j] <- tmp
    Array.toList arr

let drawCard (deck: Card list) : Card * Card list =
    match deck with
    | [] -> failwith "Deck is empty"
    | top :: rest -> top, rest

let drawN (n: int) (deck: Card list) : Card list * Card list =
    let rec loop acc remaining count =
        if count = 0 then List.rev acc, remaining
        else
            let c, rest = drawCard remaining
            loop (c :: acc) rest (count - 1)
    loop [] deck n
