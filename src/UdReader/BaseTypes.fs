// --------------------------------------------------------------------------------------
// Common type definitions and defaults for all readers
// --------------------------------------------------------------------------------------

module BaseTypes 
open System

// Enumeration for file type
type FileType =
    | Unknown = 0
    | GeChf = 1
    | Comtrade = 5
    | BpaPdat = 10
    | BpaDst = 11
    | BpaScadaCsv = 12
    | JsisXml = 15
    | JsisMatlab = 16
    | CsvGeneric = 99

// Enumeration for phase
type ChanPhase = 
    | Unknown = 0
    | A = 1
    | B = 2
    | C = 3
    | P = 4
    | N = 5
    | Z = 6
    | NotApplicable = 99

// Enumeration for complex axis
type ChanComplexPart = 
    | Unknown = 0
    | Real = 1
    | Imag = 2
    | Mag = 3
    | Angle = 4
    | NotApplicable = 99

// Enumeration for channel type
type ChanType = 
    | Unknown = 0
    | T = 1 
    | V = 2 
    | I = 3 
    | F = 14 
    | P = 15 
    | Q = 16 
    | S = 17 

// Enumeration for channel units
type ChanUnits = 
    | Unknown = 0
    | V = 1 
    | kV = 2 
    | A = 3 
    | kA = 4 
    | Hz = 5 
    | real = 6
    | imag = 7
    | deg = 8 
    | rad = 9 
    | sec = 10 
    | msec = 11 
    | MW = 13 
    | MVAR = 14
    | MVA = 15 
    | None = 99 // includes pu quantities


        
