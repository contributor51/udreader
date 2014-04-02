// --------------------------------------------------------------------------------------
// Common type definitions and defaults for all readers
//    reference: "MAT-File Format" available at mathworks.com
// --------------------------------------------------------------------------------------

module internal MTech.UdReader.matFileTypes

// The type of data contained in a mat file element
type DataType = 
    | MiINT8 = 1u //8 bit, signed
    | MiUINT8 = 2u //8 bit, unsigned
    | MiINT16 = 3u //16-bit, signed
    | MiUINT16 = 4u //16-bit, unsigned
    | MiINT32 = 5u //32-bit, signed
    | MiUINT32 = 6u //32-bit, unsigned
    | MiSINGLE = 7u //IEEE® 754 single format
    //|        = 8u //Reserved
    | MiDOUBLE = 9u //IEEE 754 double format
    //|        = 10u //Reserved
    //|        = 11u //Reserved
    | MiINT64 = 12u //64-bit, signed
    | MiUINT64 = 13u //64-bit, unsigned
    | MiMATRIX = 14u //MATLAB array
    | MiCOMPRESSED = 15u //Compressed Data
    | MiUTF8 = 16u //Unicode UTF-8 Encoded Character Data
    | MiUTF16 = 17u //Unicode UTF-16 Encoded Character Data
    | MiUTF32 = 18u //Unicode UTF-32 Encoded Character Data

// The type of array if DataType is MiMATRIX
type ArrayType = 
    | MxCELL_CLASS = 1 //Cell array
    | MxSTRUCT_CLASS = 2 //Structure 
    | MxOBJECT_CLASS = 3 //Object
    | MxCHAR_CLASS = 4 //Character array
    | MxSPARSE_CLASS = 5 //Sparse array
    | MxDOUBLE_CLASS = 6 //Double precision array
    | MxSINGLE_CLASS = 7 //Single precision array
    | MxINT8_CLASS = 8 //8bit signed integer array
    | MxUINT8_CLASS = 9 //8bit unsigned integer array
    | MxINT16_CLASS = 10 //16bit signed integer array
    | MxUINT16_CLASS = 11 //16bit unsigned integer array
    | MxINT32_CLASS = 12 //32bit signed integer array
    | MxUINT32_CLASS = 13 //32bit unsigned integer array
    | MxINT64_CLASS = 14 //64bit signed integer array
    | MxUINT64_CLASS = 15 //64bit unsigned integer array
