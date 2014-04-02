// --------------------------------------------------------------------------------------
// Extensions to List module
// --------------------------------------------------------------------------------------

module List 

// NOTE: List.filterByIndex was too slow so I implemented it in the ArrayExtensions module


/// Returns all indices in the list that satisfies the given predicate. Return None if no such element exists.
let internal tryFindAllIndices f list = 
    let rec loop n list acc = 
        match list with
        | [] -> List.rev acc
        | h :: t -> 
            let acc' = if f h then n::acc else acc
            loop (n+1) t acc'
    let indices = loop 0 list []
    if List.isEmpty indices then None else Some indices 
  