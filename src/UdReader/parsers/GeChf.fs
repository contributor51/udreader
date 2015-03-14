// --------------------------------------------------------------------------------------
// Module to parse chf files from GE's PSLF load flow and simulation suite
// --------------------------------------------------------------------------------------

module GeChf

open System
open System.IO
open BaseTypes
open SpecificTypes
open AggregateTypes
open UtilityAndIOFuncs


// --------------------------------------------------------------------------------------
// Functions to translate GeChf info to FileContents object
// --------------------------------------------------------------------------------------

// Attempt to deduce channel type and units from PSLF model type.
let guessType (typestr:string) = 
    // Each pattern must be 4 characters.
    match typestr with
    | CompiledMatch @"^abu" _ -> 
        ChanType.V, ChanPhase.P, ChanComplexPart.Angle, ChanUnits.deg // Bus voltage angle
    | CompiledMatch @"^ibr" _ -> 
        ChanType.I, ChanPhase.P, ChanComplexPart.Mag, ChanUnits.None // p.u. branch current
    | CompiledMatch @"^v(t\s|bu)" _ -> 
        ChanType.V, ChanPhase.P, ChanComplexPart.Mag, ChanUnits.None // vt or vbu_
    | CompiledMatch @"^fbu" _ -> 
        ChanType.F, ChanPhase.NotApplicable, ChanComplexPart.NotApplicable, ChanUnits.Hz
    | CompiledMatch @"^t\s+" _ -> 
        ChanType.T, ChanPhase.NotApplicable, ChanComplexPart.NotApplicable, ChanUnits.sec
    | CompiledMatch @"^p[gm1234]" _ -> 
        ChanType.P, ChanPhase.P, ChanComplexPart.Real, ChanUnits.MW
    | CompiledMatch @"^q[g1234]" _ -> 
        ChanType.Q, ChanPhase.P, ChanComplexPart.Imag, ChanUnits.MVAR  
    | _ -> ChanType.Unknown, ChanPhase.Unknown, ChanComplexPart.Unknown, ChanUnits.Unknown 

// Fill FileInformation object from a GeChfFileInfo type
let translateChannelInfo (info:GeChfFileInfo) = 
    // Chf channel names will be fnum:fname:fkv:id:modelname:typename
    // Chf channel desc will be fnum:fname:fkv:tonum:toname:tokv:id:c##:s##:a##:z##:min##:max##:modelname:typename
    let builddesc (def:GeChfChannelDef) = 
        let buildstr busdef = match busdef with | num, nam, kv -> [string num; nam; string kv] 
        let n = List.append (buildstr def.FmBus) [def.DeviceId; def.ModelName; def.TypeName]
        let d = List.concat [buildstr def.FmBus; buildstr def.ToBus; 
                            [def.DeviceId; "c" + def.CktNum; "s" + string def.SecNum; "a" + string def.AreaNum; 
                             "z" + string def.ZoneNum; "min"+ string def.Min; "max" + string def.Max; 
                             def.ModelName; def.TypeName]] 
        let t, _, _, u = guessType def.TypeName 
        // Use special case for the time vector, which is the first column of the data.
        match t,n with
        | ChanType.T, ["0"; ""; "0"; _; "Time    "; _] -> "Time", "", (t.ToString(), u.ToString())
        | _ -> String.Join(":", n), String.Join(":", d), (t.ToString(), u.ToString())
    let names, descs, typtupl = List.map builddesc info.Channels
                                |> List.unzip3
    let typs, uns = List.unzip typtupl
    // Return a tuple of lists of names, descriptions, types and units
    names, descs, typs, uns
  



// --------------------------------------------------------------------------------------
// Parsing functions
// --------------------------------------------------------------------------------------

// Parses types within a given model. A type in a chf file equates to a channel. 
let parseModelTypes bytearr byteptr (proto:GeChfChannelDef) chans ntypes = 
    // proto is a prototype channel definition created in parseModel
    let rec parseModelType ptr acc n = 
        // If finished with types, return the accumulated list of all channels parsed so far
        if n <= 0 then acc, ptr
        else 
            let acc' = 
                {proto with
                     // Add the ToBus definition to the prototype
                     ToBus = BitConverter.ToUInt32(bytearr, ptr), 
                             bytearr.[ptr+4..ptr+11] |> getStringFromByteArr, 
                             BitConverter.ToSingle(bytearr, ptr+16) 
                     // Add the TypeName to the prototype
                     TypeName = bytearr.[ptr+20..ptr+23] |> getStringFromByteArr 
                     // Add the min and max to the prototype
                     Min = BitConverter.ToSingle(bytearr, ptr+24) |> single
                     Max = BitConverter.ToSingle(bytearr, ptr+28) |> single } :: acc
            // Recurse until all types for this model are finished
            parseModelType (ptr+32) acc' (n-1)
    // Start the recursion with an initial accumulator filled with all channels parsed so far
    parseModelType byteptr chans ntypes

// Parses a model. A model in a chf file defines part of the channel name. A
// model contains several types, which equate to  measurement channels. 
let parseModels bytearr byteptr (ver, subver) nmodels = 
    let rec parseModel ptr acc n = 
        // If finished with models, return the accumulated list of all channels parsed so far
        if n <= 0 then (List.rev acc), ptr
        else 
            // Create a channel prototype to be sent to parseModelTypes
            let channelprototype = {
                FmBus = BitConverter.ToUInt32(bytearr, ptr), 
                        bytearr.[ptr+4..ptr+11] |> getStringFromByteArr, 
                        BitConverter.ToSingle(bytearr, ptr+16) 
                // Leave the ToBus blank. It will be filled in parseModelTypes
                ToBus = 0u, "", 0.0f
                ModelName = bytearr.[ptr+40..ptr+47] |> getStringFromByteArr 
                // Leave the TypeName blank. It will be filled in parseModelTypes
                TypeName = ""
                DeviceId = bytearr.[ptr+20..ptr+21] |> getStringFromByteArr
                CktNum = bytearr.[ptr+22..ptr+23] |> getStringFromByteArr 
                SecNum = BitConverter.ToUInt32(bytearr, ptr+24) 
                AreaNum = BitConverter.ToUInt32(bytearr, ptr+28) 
                ZoneNum = BitConverter.ToUInt32(bytearr, ptr+32) 
                // Leave the min/max blank. They will be filled in parseModelTypes
                Min = 0.0f 
                Max = 0.0f 
                PlotSelect = BitConverter.ToUInt32(bytearr, ptr+36) } 
            let ntypes = BitConverter.ToUInt32(bytearr, ptr+48) |> int 
            // Now parse all of the types within this model
            let chans, newptr = 
//                if ver < 19u then
                    parseModelTypes bytearr (ptr+52) channelprototype acc ntypes
//                else
//                    parseModelTypes bytearr (ptr+92) channelprototype acc ntypes
            // Recurse until all models for this file are finished
            parseModel newptr chans (n-1)
    // Start the recursion with an empty accumulator
    parseModel byteptr [] nmodels

// Parses a chf file.
let parseChfInfo bytearr = 
    let ver = BitConverter.ToUInt32(bytearr, 0)
    let subver = BitConverter.ToUInt32(bytearr, 4)
    let nmodels = BitConverter.ToInt32(bytearr, 20)
    // Get the channel definitions
    let chans, dataptr = parseModels bytearr 65 (ver, subver) nmodels
    // Establish a channel definition for the time vector
    let tdef = 
        {FmBus = 0u, "", 0.0f; ToBus = 0u, "", 0.0f; ModelName = "Time    "; TypeName = "t   "
         DeviceId = ""; CktNum = ""; SecNum = 0u; AreaNum = 0u; ZoneNum = 0u; 
         Min = 0.0f; Max = 0.0f; PlotSelect = 0u } 
    // Create a GeChfFileInfo record
    let hdr = {
        Version = ver
        Subversion = subver
        Tmin4plot = BitConverter.ToSingle(bytearr, 24) 
        Tmax4plot = BitConverter.ToSingle(bytearr, 28) 
        PlotXLabel = bytearr.[32..41] |> getStringFromByteArr 
        // Add the tdef to the beginning of the channel list
        Channels = tdef :: chans 
        // Set pointer to the beginning of data section. Need to add 7240 bytes after
        // the end of the channel definitions. I don't know what's in this data block.
        BytePointer = (dataptr+7240) |> uint32 }
    if hdr.Version < 17u || hdr.Version > 20u then 
        let errmsg = "Invalid chf file. Incorrect version number."
        raise (System.IO.InvalidDataException(errmsg)) 
    // Return the header
    hdr

// Estimate the number of bytes in the header
let estimateHdrSize (mm:MemoryMappedFiles.MemoryMappedViewStream) = 
    // Get a small buffer -- enough only to read numchans from the chf header.
    let bufSize = 4096 // Use multiples of 4k to improve speed.
    let bytearr = Array.zeroCreate bufSize 
    // Read into the byte array
    mm.Read(bytearr, 0, bufSize) |> ignore 
    // Reset viewstream pointer
    mm.Position <- 0L
    // Get the number of channels at position 16
    let nchans = BitConverter.ToInt32(bytearr, 16) 
    // Approximate the size of the header, including channel definitions.
    let approxHdrSize = 65 + (90 * nchans)
    ((approxHdrSize / 4096) + 1) * 4096


// --------------------------------------------------------------------------------------
// File handling, i.e. reading, functions
// --------------------------------------------------------------------------------------
// Read header information from the chf file. 
let readGeChfHeader (finfo:UdFileInformation) = 
    // Get a view into a memory-mapped file
    use mm = getMMView 0L finfo.FileName 
    let hdrsize = estimateHdrSize mm
    // Create a byte array which will hold data from the memory-mapped file
    let bytearr = Array.zeroCreate hdrsize
    // Read into bytearr from the memory-mapped file
    if mm.Read(bytearr, 0, hdrsize) = hdrsize then 
    // Chf files have no start time. We can't set it to zero because chf files sometimes have
    // negative values of time and DateTime chokes on negative values. We have to choose an
    // arbitrary date. For now we will arbitrarily choose the default, 1/1/1970.
    // Chf files will always use the default, 1 second, as the time base.
    // Chf title is unknown for now.
        // Fill a GeChfFileInfo record from the chf file
        let chfinfo = parseChfInfo bytearr
        // Translate chf names into standard UdReader.FileInformation format
        let n, d, t, u = translateChannelInfo chfinfo
        // Return a FileInformation object to UdReader
        {finfo with Names = Array.ofList n
                    Descriptions = Array.ofList d
                    Types = Array.ofList t
                    Units = Array.ofList u
                    // NumAnalogs is length of all names minus the time vector
                    NumAnalogs = (List.length n) - 1 |> uint32
                    FormatSpecificFileInfo = GeChf(chfinfo) }
    else failwith "Error reading chf header."


// Read data from the chf file. This function uses a memory-mapped file. This method is really
// fast, even for very large files. Microsoft handles all page swaps and disc reads. This is
// much faster than reading the file byte-by-byte.
let readGeChfData (finfo:UdFileInformation) channels2read = 
    // Retrieve a pointer to the start of the data section of the chf file
    let ptr = match finfo.FormatSpecificFileInfo with
              | GeChf(info) -> int64 info.BytePointer 
              | _ -> failwith "Invalid file info for Ge Chf file"
    // Get a view into a memory-mapped file
    use mm = getMMView ptr finfo.FileName 
    // Compute the number of bytes in a row of the chf file. NumAnalogs does not include the 
    // time vector. Futhermore, each row of the chf file contains a leading int32 (numchans).
    // Therefore we need to read NumAnalogs + 2 entries each of 4 bytes. We'll discard the
    // first entry. The second entry will be time. The rest are the analogs.
    let num2read = finfo.NumAnalogs + 1u |> int
    let rowsize = (num2read + 1) * 4 
    // Create a blank row to read into.
    let row = Array.zeroCreate rowsize 
    // Create a sequence of rows of data. Each row is a float32 array. Then convert the
    // sequence to a 2D array of doubles.
    let data = 
        seq {
            while (mm.Read(row, 0, rowsize) >= rowsize) do //&& (BitConverter.ToUInt32(row,0) > 0u) do 
                let newrow = [|for i in 1 .. num2read -> BitConverter.ToSingle(row, i*4)|]
                printfn "%A" newrow//(BitConverter.ToUInt32(row,0))
                yield Array.filterByIndex channels2read newrow }
        |> array2D
        |> Array2D.map (fun x -> double x)
    data, channels2read



//version             0 (int32)
//subversion          4 (int32)
//nchans              16 (int32)
//nmodels             20 (int32)
//tmin4plotting       24 (single)
//tmax4plotting       28 (single)
//plotXLabel          32 (10 * char)
//models              65 (nummodels * ??)
//  fmBusNum          0 (uint32)
//  fmBusName         4 (8 * char)
//  fmBusKv           16 (single)
//  deviceId          20 (2 * char)
//  cktNum            22 (2 * char)
//  secNum            24 (uint32)
//  areaNum           28 (uint32)
//  zoneNum           32 (uint32)
//  plotSelect        36 (uint32)
//  modelName         40 (8 * char)
//  numTypes          48 (uint32)
//  types             52 (numTypes * 32) 
//    toBusNum        0 (uint32)
//    toBusName       4 (8 * char)
//    toBusKv         16 (single)
//    typeName        20 (4 *char)
//    cMin            24 (single)
//    cMax            28 (single)
//startOfData         advance 7240 from end of header
//numCols             0 (uint32)
//data                4 (numchans * single)

