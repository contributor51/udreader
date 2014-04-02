// --------------------------------------------------------------------------------------
// Module to parse comtrade files 
// --------------------------------------------------------------------------------------

module Comtrade

open System
open System.IO
open System.Text.RegularExpressions
open FSharp.Data 
open FSharp.Data.CsvExtensions
open BaseTypes
open SpecificTypes
open AggregateTypes
open UtilityAndIOFuncs


// --------------------------------------------------------------------------------------
// Utility functions
// --------------------------------------------------------------------------------------

// Split a comma-separated string into fields, each of which is a trimmed string
let splitstr (instr:string) = instr.Split(',') |> Array.map (fun el -> el.Trim()) 

// Turn a string into a float with a default value
let str2float def str = 
    if String.IsNullOrWhiteSpace str then def
    else str.AsFloat()


// --------------------------------------------------------------------------------------
// Functions to translate Comtrade info to FileContents object
// --------------------------------------------------------------------------------------

// Attempt to deduce channel type and units from Comtrade channel def.
let guessType (ph:string, uu:string) = 
    // Strings should already be trimmed. This pattern will be tricky
    // because, for example, phasor definitions use different
    // nomenclature than relays.
    let t, u = 
        match uu.ToLower() with
        | CompiledMatch @"^\s*v" _ -> ChanType.V, ChanUnits.V
        | CompiledMatch @"^\s*kv" _ -> ChanType.V, ChanUnits.kV
        | CompiledMatch @"^\s*a" _ -> ChanType.I, ChanUnits.A
        | CompiledMatch @"^\s*ka" _ -> ChanType.I, ChanUnits.kA
        | CompiledMatch @"hz$" _ -> ChanType.F, ChanUnits.Hz
        | CompiledMatch @"^\s*mw" _ -> ChanType.P, ChanUnits.MW  
        | CompiledMatch @"^\s*mvar" _ -> ChanType.Q, ChanUnits.MVAR  
        | CompiledMatch @"^\s*mva" _ -> ChanType.S, ChanUnits.MVA 
        | CompiledMatch @"^\s*sec" _ -> ChanType.T, ChanUnits.sec 
        | CompiledMatch @"^\s*msec" _ -> ChanType.T, ChanUnits.msec 
        | CompiledMatch @"^\s*[^\s]*sec" _ -> ChanType.T, ChanUnits.Unknown 
        | _ -> ChanType.Unknown, ChanUnits.Unknown 
    let p = 
        match ph.ToLower() with
        | CompiledMatch @"^\s*[ar1]" _ -> ChanPhase.A
        | CompiledMatch @"^\s*[bs2]" _ -> ChanPhase.B
        | CompiledMatch @"^\s*[ct3]" _ -> ChanPhase.C
        | CompiledMatch @"^\s*[p\+]" _ -> ChanPhase.P
        | CompiledMatch @"^\s*[n\-]" _ -> ChanPhase.N
        | CompiledMatch @"^\s*[z0]" _ -> ChanPhase.Z
        | _ -> ChanPhase.Unknown 
    let a = 
        if t = ChanType.V || t = ChanType.I then
            match ph.ToLower() with
            | CompiledMatch @"m\s*$" _ -> ChanComplexPart.Mag
            | CompiledMatch @"a\s*$" _ -> ChanComplexPart.Angle
            | CompiledMatch @"r\s*$" _ -> ChanComplexPart.Real
            | CompiledMatch @"i\s*$" _ -> ChanComplexPart.Imag
            | _ -> ChanComplexPart.Unknown 
        else ChanComplexPart.NotApplicable
    t, p, a, u
 

// Fill FileInformation object from a GeChfFileInfo type
let translateChannelInfo (info:ComtradeFileInfo) = 
    // Comtrade channel names will be ch_id
    // Comtrade channel desc will be ch_id:ph:uu:ccbm
    let builddescAsync (def:ComtradeChannelDefA) = 
        async {let n = def.Chid
               let d = [def.Chid; def.Ph; def.Uu; def.Ccbm] 
               let t, p, a, u = guessType (def.Ph, def.Uu) 
               // Use special case for the time vector, which is the first column of the data.
               return match t,n with
                      | ChanType.T, _ -> "Time", "", (t.ToString(), u.ToString())
                      | _ -> n, String.Join(":", d), (t.ToString(), u.ToString())}
    let tasks = seq { for a in info.Analogs do yield builddescAsync a} 
    let names, descs, typtupl = tasks |> Async.Parallel |> Async.RunSynchronously 
                                |> Array.unzip3
    let typs, uns = Array.unzip typtupl
    // Return a tuple of lists of names, descriptions, types and units
    names, descs, typs, uns
 


// --------------------------------------------------------------------------------------
// Parsing functions
// --------------------------------------------------------------------------------------

// Parse the first config row
let parseRevision ((finfo:ComtradeFileInfo), rows) = 
    let flds = List.head rows |> splitstr
    if Array.length flds < 2 then raise (InvalidDataException("Invalid first line of cfg file."))
    let station, recorder = flds.[0], flds.[1]
    let revyear = if Array.length flds < 3 then 1991u
                  else flds.[2].AsInteger() |> uint32
    {finfo with Version = revyear; StationName = station; RecorderName = recorder}, List.tail rows

// Parse the numchans config row
let parseNumChans ((finfo:ComtradeFileInfo), rows) = 
    let flds = List.head rows |> splitstr
    if Array.length flds < 3 then raise (InvalidDataException("Invalid second line of cfg file."))
    let numa, numd = flds.[1], flds.[2]
    // numanalogs has a trailing "A" so we need to remove it before casting to int
    let numanalogs = (numa.[..numa.Length-2]).AsInteger() 
    // numdigitals has a trailing "D" so we need to remove it before casting to int
    let numdigitals = (numd.[..numd.Length-2]).AsInteger()  
    if (flds.[0]).AsInteger() <> (numanalogs + numdigitals) then 
            raise (System.IO.InvalidDataException("Invalid NumChans line of cfg file.")) 
    {finfo with NumAnalogs = uint32 numanalogs; NumDigitals = uint32 numdigitals}, List.tail rows

// Parses the analog channel definitions. 
let parseAnalogs ((finfo:ComtradeFileInfo), rows) = 
    let rec parseAnalog (rowlist:string list) acc n = 
        // If finished with analogs, return the accumulated list
        if n > finfo.NumAnalogs then {finfo with Analogs = (List.rev acc)}, rowlist
        else 
            let newdef, newrows = 
                match rowlist with
                | [] -> failwith "Error parsing analog channels."
                | hd::tl -> 
                    let flds = splitstr hd 
                    try 
                        let idx = flds.[0].AsInteger() |> uint32
                        if idx <> n then raise (System.IO.InvalidDataException())
                        let chid, ph, ccbm, uu = flds.[1], flds.[2], 
                                                 flds.[3], flds.[4] 
                        let a, b, skew = 
                            str2float 1.0 flds.[5], 
                            str2float 0.0 flds.[6], 
                            str2float 0.0 flds.[7]
                        let min, max = str2float Double.NegativeInfinity flds.[8], 
                                       str2float Double.PositiveInfinity flds.[9]  
                        let prim, sec, pors = 
                            // These three fields were added to the channel def in 1999
                            if finfo.Version > 1998u then
                                str2float 1.0 flds.[10], 
                                str2float 1.0 flds.[11], flds.[12].Chars 0
                            else 1.0, 1.0, 'P' 
                        {Idx = idx; Chid = chid; Ph = ph; Ccbm = ccbm; Uu = uu
                         A = a; B = b; Skew = skew; Min = min; Max = max
                         Prim = prim; Sec = sec; PorS = pors}, tl
                    with | err -> 
                        let errmsg = 
                            sprintf "Invalid Comtrade configuration: invalid analog definition at %d" n
                        raise (System.IO.InvalidDataException(errmsg))
            parseAnalog newrows (newdef::acc) (n+1u)
                // Establish a channel definition for the time vector
    let tdef = 
        {Idx = 0u; Chid = "Time"; Ph = ""; Ccbm = ""; Uu = "sec"; A = 1.0; B = 0.0
         Skew = 0.0; Min = Double.NegativeInfinity; Max = Double.PositiveInfinity
         Prim = 1.0; Sec = 1.0; PorS = 'P' } 
    parseAnalog rows [tdef] 1u

// Parses the digital channel definitions. 
let parseDigitals ((finfo:ComtradeFileInfo), rows) = 
    let rec parseDigital (rowlist:string list) acc n = 
        // If finished with digitals, return the accumulated list
        if n > finfo.NumDigitals then {finfo with Digitals = (List.rev acc)}, rowlist
        else 
            let newdef, newrows = 
                match rowlist with
                | [] -> failwith "Error parsing digital channels."
                | hd::tl ->
                    let flds = splitstr hd 
                    try 
                        let idx = flds.[0].AsInteger() |> uint32
                        if idx <> n then raise (System.IO.InvalidDataException())
                        let chid = flds.[1]
                        let ph, ccbm, y = 
                            // Ph and Ccbm were added to the channel def in 1999
                            if finfo.Version > 1998u then
                                flds.[2], flds.[3], flds.[4].AsInteger()
                            else "", "", flds.[2].AsInteger()
                        {Idx = idx; Chid = chid; Ph = ph; Ccbm = ccbm; Y = y}, tl
                    with | err -> 
                        let errmsg = 
                            sprintf "Invalid Comtrade configuration: invalid digital definition at %d" n
                        raise (System.IO.InvalidDataException(errmsg))
            parseDigital newrows (newdef::acc) (n+1u)
    let tdef = 
        {Idx = 0u; Chid = "Time"; Ph = ""; Ccbm = ""; Y = 0 } 
    parseDigital rows [tdef] 1u

// Parses the line frequency row
let parseLf ((finfo:ComtradeFileInfo), rows) = 
    let lf = (rows |> List.head |> splitstr).[0] |>  str2float 60.0
    {finfo with NominalFreq = lf}, List.tail rows

// Parses the rates rows. 
let parseRates ((finfo:ComtradeFileInfo), rows) = 
    // This function will always read one row, even if nrates = 0. This is
    // desired behavior.
    let nrates = (List.head rows |> splitstr).[0].AsInteger()
    let rec parseRate (rowlist:string list) acc n = 
        let newrate, newsmpl, newrows = 
            match rowlist with
            | [] -> failwith "Error parsing rates."
            | hd::tl ->
                let flds = splitstr hd 
                try 
                    // newrate        ,    newsmpl                      , newrows
                    flds.[0].AsFloat(), flds.[1].AsInteger64() |> uint32, tl
                with | err -> 
                    raise (System.IO.InvalidDataException("Invalid rate definition"))
        // Accumulate the rate definitions
        let acc' = 
            let endtime = 
                match acc with
                | [] -> (float (newsmpl - 1u))/newrate // End time of first rate group
                | (r, e, s)::_ -> e + (float (newsmpl - s)/newrate) 
            (newrate, endtime, newsmpl)::acc
        // If finished with rates, return the accumulated list
        if n >= nrates then {finfo with Rates = (List.rev acc')}, newrows
        else parseRate newrows acc' (n+1)
    parseRate (List.tail rows) [] 1 

// Parses the date rows
let parseDates ((finfo:ComtradeFileInfo), rows) = 
    let sdstr = rows |> List.head |> splitstr 
    let trstr = rows |> List.tail |> List.head |> splitstr 
    try 
        let sddate = sdstr.[0].Split('/') |> Array.map (fun el -> int el)
        let trdate = trstr.[0].Split('/') |> Array.map (fun el -> int el)
        let sdtime = sdstr.[1].Split(':') 
        let trtime = trstr.[1].Split(':') 
        // We lose a little time resolution here because comtrade allows nanosecond accuracy
        // and DateTime allows only 10 nanosecond accuracy
        let sdticks = int64 ((float sdtime.[2]) * 10000000.0)
        let trticks = int64 ((float trtime.[2]) * 10000000.0)
        let sd = DateTime(sddate.[2], sddate.[1], sddate.[0], 
                          int sdtime.[0], int sdtime.[1], 0).AddTicks(sdticks)
        let tr = DateTime(trdate.[2], trdate.[1], trdate.[0], 
                          int trtime.[0], int trtime.[1], 0).AddTicks(trticks)
        {finfo with StartTime = sd; TrigTime = tr}, rows |> List.tail |> List.tail
    with
        | err -> 
            raise (System.IO.InvalidDataException("Invalid datetime definition"))

// Parses the datfiletype row
let parseFtype ((finfo:ComtradeFileInfo), rows) = 
    let typ = 
        match (rows |> List.head |> splitstr).[0].ToLower() with
        | "ascii" -> ComtradeDatFileFmt.Ascii
        | "binary" -> ComtradeDatFileFmt.Binary
        | "binary32" -> ComtradeDatFileFmt.Binary32
        | "float32" -> ComtradeDatFileFmt.Float32 
        | _ -> let errmsg = "Invalid data file type definition"
               raise (System.IO.InvalidDataException(errmsg)) 
    {finfo with FileType = typ}, rows |> List.tail 

// Parses the timemult row
let parseTimemult ((finfo:ComtradeFileInfo), rows) = 
    // Timemult was added in 1999
    let tm, newrows = 
        match rows, finfo.Version, finfo.Rates with
        | [], _, _ -> None, []
        // Case for early cfg file revisions
        | hd::_, v, _ when v < 1999u -> None, []
        // Case for valid sample rate, i.e. time vector of dat file is ignored
        | hd::tl, _, (r, _, _)::_ when r > 0.0 -> None, tl
        // Case for invalid sample rate where time vector of dat file must be used
        | hd::tl, _, _ -> (hd |> splitstr).[0] |> str2float 1.0 |> Some, tl
    {finfo with TimeMult = tm}, newrows

// Parses the time code row
let parseTimecode  ((finfo:ComtradeFileInfo), rows) = 
    // Timecode was added in 2013
    let tc, newrows = 
        match rows, finfo.Version with
        | [], _ -> None, []
        | hd::_, v when v < 2013u -> None, []
        | hd::tl, _ -> 
            let flds = hd |> splitstr
            Some(flds.[0], flds.[1]), tl
    {finfo with TimeCode = tc}, newrows

// Parses the time quality row
let parseTimequal  ((finfo:ComtradeFileInfo), rows) = 
    // Time quality was added in 2013
    let tq, newrows = 
        match rows, finfo.Version with
        | [], _ -> None, []
        | hd::_, v when v < 2013u -> None, []
        | hd::tl, _ -> 
            let flds = hd |> splitstr
            Some(flds.[0].ToCharArray().[0], flds.[1].ToCharArray().[0]), tl
    {finfo with TimeQual = tq}



// --------------------------------------------------------------------------------------
// Dat file utility functions
// --------------------------------------------------------------------------------------

// ---for handling dat files -----------------------------------------------------------
// Sometimes there are multiple dat files associated with a single config. In
// these cases the dats are numbered like basename.d01, basename.d02, etc. Care
// needs to be taken to sort these because a naive implementation will put d100
// ahead of d11, for example.
// Returns a sequence of all dat files having the specified base name
let getdatfnames cfgfname = 
    // If cff there cannot be multiple dat files, so just return the name
    if (IO.Path.GetExtension cfgfname).ToLower() = ".cff" then
        seq {yield cfgfname}
    else
        // Establish an integer sort key using the last part of the extension
        let sortkey (fi:FileInfo) = 
            let lasttxtofext = 
                (fi.Name).Split([|@".d"|], StringSplitOptions.None)
                |> Seq.last
            try int lasttxtofext
            with | _ -> if lasttxtofext = "at" then 0 else 999 
        // Get the directory of the cfg file and establish an info object on it
        let dirname = Path.GetDirectoryName(cfgfname)
        let di = DirectoryInfo(dirname)
        // Get all of the files with the basename and a dxxx extension, then sort
        // using the sort key. After sorting return the full file name.
        di.EnumerateFiles(Path.GetFileNameWithoutExtension(cfgfname) + @".d*")
        |> Seq.sortBy sortkey
        |> Seq.map (fun el -> el.FullName)

// Because there can be multiple sample rates or no sample rate, time, in seconds,
// is a function of the time elapsed in prior sample rate periods plus
// nsamples(this period) / rate(this period). If rate.[0] = 0.0 then we assume no
// sample rate was given and we use the time column rather than the row index column.
// Scales the time in a comtrade file
let scaletime (cfg:ComtradeFileInfo) idx t = 
    // A function to recurse through the rates
    let rec loop ratelist i indexedtime = 
        match ratelist with
        | [] -> indexedtime
        | (r, e, s)::tl -> if i > s then indexedtime
                           else loop tl i (e - (float (s - i))/r)
    // If rate is zero use the time field. If rate is non-zero
    // use the recursive function to sum the elapsed time.
    let rates = cfg.Rates
    let (r1, _, _) = List.head rates
    let timemult = match cfg.TimeMult with | Some(tm) -> tm | None -> 1.0
    if r1 = 0.0 then t / timemult 
    else loop rates idx 0.0              
 
// Scales the analogs in a comtrade dat file
let scaleanalogs (cfg:ComtradeFileInfo) (x:float []) = 
    let checkfornan el = 
        match cfg.FileType with
        | ComtradeDatFileFmt.Ascii -> el
        | ComtradeDatFileFmt.Binary -> if el = -32768.0 then Double.NaN else el
        | ComtradeDatFileFmt.Binary32 -> if el = -2147483648.0 then Double.NaN else el
        | ComtradeDatFileFmt.Float32 -> if el = 1.1755e-38 then Double.NaN else el
        | _ -> failwith "Unknown file format."
    let scaleanalog scale el = (checkfornan el) * (fst scale) + (snd scale) 
    // Ignore the first element, index = 0, of channelinfo. It is for time.
    let scalesforanalogs = 
        (Array.ofList cfg.Analogs).[1..(int cfg.NumAnalogs)]
        |> Array.map (fun el -> el.A, el.B) 
    Array.map2 scaleanalog scalesforanalogs x

// Coverts the integer booleans to boolean using the cfg normalstate field
let scaledigitals (cfg:ComtradeFileInfo) x = x

// Scales a row of data
let scaleAndJoinRow (cfg:ComtradeFileInfo) chlist (i, t, a, d) = 
    let tscaled = scaletime cfg i t
    let tanda = scaleanalogs cfg a
                  |> Array.append [|tscaled|]
                  |> Array.filterByIndex chlist
    let dscaled = scaledigitals cfg d |> Array.append [|tscaled|]
    tanda, dscaled

// Processes an ascii comtrade dat file
let processAsciiAsync (cfg:ComtradeFileInfo) channels2read fname = 
    // Use async workflow to speed up processing multiple dat files
    async {// Get a view into a memory-mapped file
           use mm = getMMView 0L fname 
           // Make a TextReader out of the mmview stream
           use rdr = new StreamReader(mm)
           // Use the FSharp csv data provider to parse rows 
           use csv = 
               // If not cff, then the whole file is parsed
               if (IO.Path.GetExtension fname).ToLower() <> ".cff" then 
                   CsvFile.Load(rdr, hasHeaders=false, ignoreErrors = true).Skip(0) 
               // If cff then we need to find the correct starting row and skip the others
               else
                   let regexstart = Text.RegularExpressions.Regex("file\s*type:\s*[Dd]")
                   // Skip while we haven't hit the separator row, then skip the separator row
                   CsvFile.Load(rdr, hasHeaders=false, ignoreErrors = true).SkipWhile(fun r -> not (regexstart.Match(r.Item(0)).Success)).Skip(1) 
           // Curry the scaler function so it doesn't calculate on each row
           let scale = scaleAndJoinRow cfg channels2read
           // Get the numanalogs
           let nanalogs = int cfg.NumAnalogs
           // Now parse each of the rows and convert to a 2D float
           let arr = 
               seq {for row in csv.Rows do 
                    // Each row is idx, time, analogs..., digitals...
                    let vals = row.Columns 
                    let a,d = scale (vals.[0].AsInteger64() |> uint32, 
                                     vals.[1].AsFloat(), 
                                     [|for i in 2..nanalogs+1 -> vals.[i].AsFloat(missingValues = [|""; " "|])|], 
                                     [||])
                    yield a }
           return array2D arr } 

// Processes a binary comtrade dat file
let processBinaryAsync (cfg:ComtradeFileInfo) channels2read fname = 
    // Use async workflow to speed execution for multiple dat files
    // Use a memory-mapped file to parse the binaries. This way all page swapping is handled
    // by the OS. This is fast for large files.
    async {// Establish the offset for the memory-mapped file view
           let startbyte = 
               // If not cff the offset is the start of the file
               if (IO.Path.GetExtension fname).ToLower() <> ".cff" then 0L 
               else //This is a cff file. Need to read by section
                   let regexignore = Text.RegularExpressions.Regex("(\s*\r\n)|(\s*)")
                   let regexstart = Text.RegularExpressions.Regex("file\s*type:\s*[Dd]")
                   use inStream = File.OpenText(fname) 
                   // When we find the start of the data section then we need to loop a
                   // few more rows looking for blank lines. Use a recursive function here
                   // and keep recursing after we've found the delimiter row to get rid
                   // of blank lines.
                   let rec loop isdatsectionfound = 
                       // Read file position before we read row
                       let byte = inStream.BaseStream.Position
                       let thisrow = inStream.ReadLine()
                       if inStream.EndOfStream then failwith "cff data section separator not found"
                       let foundstart = regexstart.Match(thisrow).Success
                       let foundignore = regexignore.Match(thisrow).Success
                       let newbool = foundstart || isdatsectionfound
                       match isdatsectionfound, foundignore with
                       | true, false -> byte
                       | _, _ -> loop newbool
                   loop false 
           // Get a view into a memory-mapped file
           use mm = getMMView startbyte fname 
           // Curry the scaler function so it doesn't calculate on each row
           let scale = scaleAndJoinRow cfg channels2read
           // Get the numanalogs
           let nanalogs = int cfg.NumAnalogs
           // Calculate the number of bytes in a comtrade data row. See the IEEE C37.111 standard 
           // for the equation.
           let nbytes = 
               let digbytes = match cfg.NumDigitals with
                              | 0u -> 0
                              | nd -> 2 * (1 + ((int nd)-1)/16)
               4 + 4 + digbytes + nanalogs * 
                   match cfg.FileType with 
                   | ComtradeDatFileFmt.Binary -> 2
                   | ComtradeDatFileFmt.Binary32 | ComtradeDatFileFmt.Float32 -> 4
                   | _ -> failwith "Could not correctly parse file type." 
           // Establish a blank row to hold the data.
           let row = Array.zeroCreate nbytes 
           // Read until the end
           let arr = 
               seq {while mm.Read(row, 0, nbytes) >= nbytes do 
                    let a,d = scale (BitConverter.ToUInt32(row, 0), 
                                     BitConverter.ToUInt32(row, 4) |> float, 
                                     [|for i in 1 .. nanalogs -> 
                                           match cfg.FileType with 
                                           | ComtradeDatFileFmt.Binary -> BitConverter.ToInt16(row, 8+(i-1)*2) |> float
                                           | ComtradeDatFileFmt.Binary32  -> BitConverter.ToInt32(row, (i+1)*4) |> float
                                           | ComtradeDatFileFmt.Float32  -> BitConverter.ToSingle(row, (i+1)*4) |> float
                                           | _ -> failwith "Could not correctly parse file type." |], 
                                     [||])
                    yield a }
           return array2D arr }



// --------------------------------------------------------------------------------------
// File handling, i.e. reading, functions
// --------------------------------------------------------------------------------------

// Read header information from the chf file. 
let readComtradeHeader (finfo:UdFileInformation) = 
    // Create a new Comtrade file info record
    let cinfo = {Version = 0u; StationName = ""; RecorderName = ""; NumAnalogs = 0u
                 Analogs = []; NumDigitals = 0u; Digitals = []; NominalFreq = 0.0
                 Rates = []; StartTime = DateTime(1970,1,1); TrigTime = DateTime(1970,1,1)
                 FileType = ComtradeDatFileFmt.Ascii; TimeMult = None; TimeCode = None
                 TimeQual = None; DataPtr = []}
    // Get a view into a memory-mapped file
    use mm = getMMView 0L finfo.FileName 
    // Make a TextReader out of the mmview stream
    use txtStream = new StreamReader(mm)
    // Turn the cfg file into a list of rows
    let rows =
        // Slurp and split at line breaks if not cff
        if (Path.GetExtension finfo.FileName).ToLower() <> ".cff" then
            txtStream.ReadToEnd().Split([|"\r\n"; "\n"|], StringSplitOptions.None)
        else      // Need to read by line because this is cff
            // Establish a regex for lines to ignore
            let regexignore = Text.RegularExpressions.Regex("(file\s*type:\s*CFG)|(\s*\r\n)|(\s*)")
            // Establish a regex for the end line
            let regexend = Text.RegularExpressions.Regex("file\s*type:\s*[^Cc]")
            // Read lines until we hit the end, ignoring the blank lines
            let rec loop acc = 
                let thisrow = txtStream.ReadLine()
                let acc' = 
                    if regexignore.Match(thisrow).Success then acc
                    else thisrow :: acc
                if txtStream.EndOfStream || regexend.Match(thisrow).Success then
                    acc' |> List.rev |> Array.ofList
                else loop acc
            loop []
        |> List.ofArray 
    // Parse the cfg. This composition reads each of the sections of a Comtrade cfg
    // file in order, passing a list of rows to the next parsing function as it goes.
    let parseCfg = 
        parseRevision >> parseNumChans >> parseAnalogs >> parseDigitals >> parseLf 
          >> parseRates >> parseDates >> parseFtype >> parseTimemult >> parseTimecode 
          >> parseTimequal 
    let cfginfo = parseCfg (cinfo, rows)
    // Translate cfg names into standard UdReader.FileInformation format
    let n, d, t, u = translateChannelInfo cfginfo
    // Return a FileInformation object to UdReader
    {finfo with 
         StartTime = cfginfo.StartTime
         TimeUnits = match cfginfo.TimeMult with
                     | None -> 1.0
                     | Some(tm) -> 1.0/tm 
         Names = n
         Descriptions = d
         Types = t
         Units = u
         Title = cfginfo.StationName + ":" + cfginfo.RecorderName
         NumAnalogs = cfginfo.NumAnalogs 
         NumDigitals = cfginfo.NumDigitals 
         FormatSpecificFileInfo = Comtrade(cfginfo)}

// A function to process comtrade data, whether it be ascii or binary
let readDataByFileType (cfginfo:ComtradeFileInfo) cfgfname channels2read = 
    // Get the datfilenames
    let datfilenames = getdatfnames cfgfname 
    // Curry and tuple the scaler functions
    let scalers = scaletime cfginfo, scaleanalogs cfginfo, scaledigitals cfginfo
    // Get a reader function that will either read ascii or binary
    let rdr = 
        if cfginfo.FileType = ComtradeDatFileFmt.Ascii then 
             processAsciiAsync cfginfo channels2read 
        else processBinaryAsync cfginfo channels2read 
    // Run the async workflow
    let tasks = seq { for f in datfilenames do yield rdr f }
    let alldata = Async.RunSynchronously (Async.Parallel tasks) 
    // Bundle all outputs from the various data files into a single output array
    let ttlrows = Array.map (fun el -> Array2D.length1 el) alldata 
    let ans = Array2D.create (Array.sum ttlrows) (List.length channels2read) 0.0
    Array.iteri2 (fun i (d:float [,]) r ->
                      let starti = if i = 0  then 0 
                                   else Array.sum (ttlrows.[0..i-1]) 
                      let endi = starti + r - 1 // r is ttlrows.[i]
                      ans.[starti..endi, *] <- d) alldata ttlrows 
    ans 

// Reads the data file(s) and returns the information
let readComtradeData (finfo:UdFileInformation) channels2read = 
    // If user asked for digital channels ignore them (for now). To do this we will
    // trim the chanellist.
    let cinfo = match finfo.FormatSpecificFileInfo with 
                | Comtrade(x) -> x
                | _ -> failwith "Missing Comtrade definition while reading data."
    let chanlist' = List.filter (fun ch -> ch <= int cinfo.NumAnalogs) channels2read
    (readDataByFileType cinfo (finfo.FileName) chanlist'), chanlist' 
