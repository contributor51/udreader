// Learn more about F# at http://fsharp.net. See the 'F# Tutorial' project
// for more guidance on F# programming.

#r @"D:\Users\mdonnelly\Documents\projects\udreader\packages\FSharp.Data.2.0.5\lib\net40\FSharp.Data.dll"
//#r @"D:\Users\mdonnelly\Documents\projects\udreader\packages\MathNet.Numerics.2.6.0\lib\net40\MathNet.Numerics.dll"
#load "ArrayModuleExtensions.fs"
#load "ListModuleExtensions.fs"
#load "BaseTypes.fs"
#load "FileSpecificTypes.fs"
#load "AggregateTypes.fs"
#load "UtilityAndIOFuncs.fs"
#load "parsers/GeChf.fs"
#load "parsers/Comtrade.fs"
#load "UdReader.fs"
open MTech.UdReader

// Define your library scripting code here
//let u = UdReader(@"D:\Users\mdonnelly\Documents\projects\chf\0\C37118-CJB_0-20130619000000.cfg") // PSLF_cases\Wscc9busGenLoss.chf  PSLF_cases\09hsp1a1\chans\case1.chf
//let u = UdReader(@"D:\Users\mdonnelly\Documents\data\PSLF_cases\09hsp1a1\chans\case1.chf") // PSLF_cases\Wscc9busGenLoss.chf  PSLF_cases\09hsp1a1\chans\case1.chf
let u = UdReader(@"D:\Users\mdonnelly\Documents\data\comtrade\testdata\testasciicomtrade.cfg") //testasciicomtrade testbincomtrade
//let u = UdReader(@"D:\Users\mdonnelly\Documents\data\comtrade\testdata\0\C37118-CJB_0-20130619000000.cfg")
//let u = UdReader(@"D:\Users\mdonnelly\Documents\data\comtrade\testdata\00\C37118-CJB-20130619000000.cfg")
//let u = UdReader(@"D:\Users\mdonnelly\Documents\data\comtrade\testdata\01\C37118-CJB-20130619000000.cfg")
//let u = UdReader(@"D:\Users\mdonnelly\Documents\data\PSLF_cases\2mach\chans\case1.chf")
//printfn "%A" u.Config


