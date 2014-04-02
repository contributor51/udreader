// --------------------------------------------------------------------------------------
// Common type definitions and defaults for all readers
// --------------------------------------------------------------------------------------

module AggregateTypes 
open System
open BaseTypes
open SpecificTypes

// --COMPOSITE DU------------------------------------------------------------------------
// A discrimated union that will hold format-specific info from all of these
// different file types.
type FormatSpecificFileInfo = 
    | NoInfo 
    | Comtrade of ComtradeFileInfo
    | GeChf of GeChfFileInfo 

// Information from the file, including a list of channel definitions
type UdFileInformation =  
    {FileName : string
     FileType : FileType 
     StartTime : DateTime
     // The time elapsed in one unit of the time vector
     TimeUnits : float 
     // The channel names
     Names : string array 
     // Description, if any, for each channel
     Descriptions : string array 
     // The channel types
     Types : string array 
     // The axis in the complex plane for the channel
     Axes : string array
     // The channel units
     Units : string array 
     // The title assigned to the data file
     Title : string 
     NumAnalogs : uint32
     NumDigitals : uint32
     FormatSpecificFileInfo : FormatSpecificFileInfo }
let dfltfileinfo = 
    {FileName = ""
     FileType = FileType.Unknown 
     StartTime = DateTime(1970,1,1) // Default to unix base time
     TimeUnits = 1.0 // Default to 1 second
     Names = [||]
     Descriptions = [||]
     Types = [||]
     Axes = [||]
     Units = [||] 
     Title = ""
     NumAnalogs = 0u
     NumDigitals = 0u
     FormatSpecificFileInfo = NoInfo }


