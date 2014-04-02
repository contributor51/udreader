// --------------------------------------------------------------------------------------
// Common functions for UdReader
// --------------------------------------------------------------------------------------

module UtilityAndIOFuncs 
open System 
open System.IO
open System.Text.RegularExpressions
open BaseTypes
open AggregateTypes

// Checks a file's validity and returns a full path
let checkvalidfile fname = 
    let fi = FileInfo(fname)
    if fi.Exists then fi.FullName
    else
        let errmsg = sprintf "%s not found" fname 
        raise (FileNotFoundException(errmsg))

// Checks a file's type
let checkfiletype (fname) = 
    match (IO.Path.GetExtension fname).ToLower() with
    | ".cfg" | ".cff" -> {dfltfileinfo with FileName = fname; FileType = FileType.Comtrade}
    | ".chf" -> {dfltfileinfo with FileName = fname; FileType = FileType.GeChf}
    | _ -> dfltfileinfo
    
// A function for matching strings. 
let findbyname (srchArr: string array) (pattArr:string array) (casesensitive:bool) = 
    // Treat all search strings as regular expressions. If it is a simple
    // search term regex will find it OK.
    let regexmatch str ptrn = Regex.Match(str, ptrn) 
    let regexmatchi str ptrn = Regex.Match(str, ptrn, RegexOptions.IgnoreCase) 
    let regexmatch'  = if casesensitive then regexmatch else regexmatchi
    let matchmultiple str ptrn = 
        ptrn |> Array.exists (fun el -> (regexmatch' str el).Success)
    let indices = 
        srchArr
        |> List.ofArray 
        |> List.tryFindAllIndices (fun el -> matchmultiple el pattArr)
    match indices with
    | None -> [||]
    | Some i -> List.toArray i

// Converts a byte array to a string replacing NULL (ASCII 0) with space (ASCII 32)
let getStringFromByteArr bytearr = 
    bytearr 
    |> Array.map (fun b -> if b = 0uy then 32uy else b) 
    |> System.Text.Encoding.ASCII.GetString 

// Match the pattern using a cached compiled Regex
let (|CompiledMatch|_|) pattern input =
    if input = null then None
    else
        let m = Regex.Match(input, pattern, RegexOptions.Compiled)
        if m.Success then Some(m.Value) else None
// This active pattern appears to be very slow. Use it cautiously. Here's how
// match str with
// | CompiledMatch @"m\s*$" x -> x


// Maps a file on storage media to a memory-mapped file
let getMMView startbyte fname = 
    // Establish the memory-mapped file
    use mm = 
        MemoryMappedFiles.MemoryMappedFile.CreateFromFile(fname, FileMode.Open,
            FileInfo(fname).Name, 0L, MemoryMappedFiles.MemoryMappedFileAccess.Read) 
    // Establish and return a view into the mm file starting at the offset
    mm.CreateViewStream(startbyte, 0L, MemoryMappedFiles.MemoryMappedFileAccess.Read) 
