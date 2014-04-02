// --------------------------------------------------------------------------------------
// External interface to UdReader
// Extensions to a class providing read methods for utility data
// --------------------------------------------------------------------------------------

namespace MTech.UdReader

open System
open BaseTypes
open AggregateTypes
open UtilityAndIOFuncs

//
// This type is intended to replicate the JSIS Matlab format
// specification. It is relatively compact. Maybe we will need
// to present more information to the user at some point in
// the future.
//

/// A compact class representing file and channel information
type FileContents = { 
    /// The time corresponding to t = 0 in the data set
    StartTime : DateTime 
    /// The time elapsed in one unit of the time vector
    TimeUnits : float 
    /// The channel names
    Name : string array 
    /// Description, if any, for each channel
    Description : string array 
    /// The channel types
    Type : string array 
    /// The channel units
    Units : string array 
    /// The title assigned to the data file
    Title : string 
    /// The data contained in the data file(s)
    Data : float [,]
    }


/// A class for reading time series data in various utility formats
type UdReader(fileName : string) = 
    // Check for valid filename
    let m_fullfilename = checkvalidfile fileName
    // Get the file information, not the data 
    let m_fileinfo = 
        let finf = checkfiletype fileName
        match finf.FileType with
        | FileType.Comtrade -> Comtrade.readComtradeHeader finf 
        | FileType.GeChf -> GeChf.readGeChfHeader finf 
        | _ -> failwith "Unknown file format"


    /// The full list of channel metadata
    member udr.FullFileInfo = 
        {StartTime = m_fileinfo.StartTime 
         TimeUnits = m_fileinfo.TimeUnits  
         Name = m_fileinfo.Names  
         Description = m_fileinfo.Descriptions 
         Type = m_fileinfo.Types 
         Units = m_fileinfo.Units 
         Title = m_fileinfo.Title 
         Data = seq { yield Array.empty<float> } |> array2D } 

    /// Retrieve certain channels from the data file
    member udr.GetData(channels:int array) = 
        // First index in channels must be zero. Time vector must
        // be preserved.
        let chanlist = 
            if channels.[0] <> 0 then Array.append [|0|] channels else channels 
            |> List.ofArray 
            // Eliminate any entries out of index bounds
            |> List.filter (fun el -> el < Array.length udr.FullFileInfo.Name)
        // Get the data
        let fData, retrievedChanList = 
            match m_fileinfo.FileType with 
            | FileType.Comtrade -> Comtrade.readComtradeData m_fileinfo chanlist
            | FileType.GeChf -> GeChf.readGeChfData m_fileinfo chanlist
            | _ -> (seq { yield Array.empty<float> } |> array2D), [0]
        {udr.FullFileInfo with 
             Name = m_fileinfo.Names |> Array.filterByIndex retrievedChanList
             Description = m_fileinfo.Descriptions   
                           |> Array.filterByIndex retrievedChanList
             Type = m_fileinfo.Types |> Array.filterByIndex retrievedChanList
             Units = m_fileinfo.Units |> Array.filterByIndex retrievedChanList
             Data = fData } 

    /// Retrieve all channels from the data file
    member udr.GetData() = 
        // Do not include the time vector at the zero index. It will get included
        // in the parent method. Total length of ChInfo is nchans + time vector.
        udr.GetData( [|1 .. int (m_fileinfo.NumAnalogs)|] )

    /// Search the channel names for matching string fragments returning indices
    member udr.FindByName(srchStr) = findbyname m_fileinfo.Names srchStr true

    /// Search the channel names for a single matching string fragment returning indices
    member udr.FindByName(srchStr:string) = udr.FindByName([|srchStr|])

    /// Case-insensitive search the channel names for matching string fragments returning indices
    member udr.FindByNamei(srchStr) = findbyname m_fileinfo.Names srchStr false

    /// Case-insensitive search the channel names for a single matching string fragment returning indices
    member udr.FindByNamei(srchStr:string) = udr.FindByNamei([|srchStr|])
