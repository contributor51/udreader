// --------------------------------------------------------------------------------------
// Type definitions and defaults for the various file formats.
// --------------------------------------------------------------------------------------

module SpecificTypes 
open System
open BaseTypes

// --------------------------------------------------------------------------------------
// Comtrade
// --------------------------------------------------------------------------------------
// Enumeration for data file formats
type ComtradeDatFileFmt = 
    | Ascii = 0
    | Binary = 1
    | Binary32 = 2
    | Float32 = 3
    | Unspecified = 99

// The analog channel information from a comtrade file
type ComtradeChannelDefA = 
    {Idx : uint32
     Chid : string
     Ph : string
     Ccbm : string
     Uu : string
     A : float //float is System.Double in .NET
     B : float
     Skew : float
     Min : float
     Max : float
     Prim : float
     Sec : float
     PorS : char }

// The digital channel information from a comtrade file
type ComtradeChannelDefD = 
    {Idx : uint32
     Chid : string
     Ph : string
     Ccbm : string
     Y : int }

// The file information, including channels, from a comtrade file
type ComtradeFileInfo = 
    {Version : uint32
     StationName : string
     RecorderName : string
     NumAnalogs : uint32
     Analogs : ComtradeChannelDefA list
     NumDigitals : uint32
     Digitals : ComtradeChannelDefD list
     NominalFreq : float
     Rates : (float * float * uint32) list // rate * endtime for this group * samplenum
     StartTime : DateTime
     TrigTime : DateTime
     FileType : ComtradeDatFileFmt
     TimeMult : float option 
     TimeCode : (string * string) option
     TimeQual : (char * char) option 
     DataPtr : (string * uint32 * DateTime) list } // datfname * firstPoint * firstPointTime




// --------------------------------------------------------------------------------------
// GE CHF (PSLF)
// --------------------------------------------------------------------------------------
// The channel information from a chf file
type GeChfChannelDef = 
    {FmBus : uint32 * string * single
     ToBus : uint32 * string * single
     ModelName : string
     TypeName : string
     DeviceId : string
     CktNum : string
     SecNum : uint32
     AreaNum : uint32
     ZoneNum : uint32
     Min : single
     Max : single
     PlotSelect : uint32 }

// The file information, including channels, from a chf file
type GeChfFileInfo = 
    {Version : uint32
     Subversion : uint32
     Channels : GeChfChannelDef list
     Tmin4plot : float32
     Tmax4plot : float32 
     PlotXLabel : string 
     BytePointer : uint32 } 


