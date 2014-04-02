// --------------------------------------------------------------------------------------
// Extensions to Array module
// --------------------------------------------------------------------------------------

module Array 

/// Returns a list consisting of the elements of list at the specified indices. 
let internal filterByIndex indices arr = 
    if  ((List.max indices) > ((Array.length arr) - 1)) ||
        ((List.min indices) < 0) then invalidArg "indices" "Index out of bounds."
    List.map (fun idx -> arr.[idx]) indices
    |> Array.ofList 

 
  