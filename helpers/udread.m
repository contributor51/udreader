function y = udread( fname, channels, path2dll )
%UDREAD Reads time series data using UdReader
%   Y = UDREAD('filename') reads file anc channel information from filename
%   where filename is in one of several formats commonly used in the
%   electric power utility industry. The function utilizes the .NET library
%   UdReader.dll. The dll must be in the matlab path to use this form of
%   the function.
%
%   Y = UDREAD('filename', []) reads all data from filename.
%
%   Y = UDREAD('filename', [1 3 5 7]) reads selected channels from filename.
%
%   Use the form Y = UDREAD('fname', 'c:\path2UdReader') or Y = 
%   UDREAD('fname', [], 'c:\path2UdReader') when the file UdReader.dll is
%   not on the matlab path. 'filename' can be an absolute or relative path.
%   'path2UdReader' must be an absolute path.
%   
%   Y is a matlab struct in WECC JSIS format.
%

%% Parse the input arguments
switch nargin
    case 1
        isDataRequested = false;
        path2dll = which('UdReader.dll'); 
    case 2
        if isnumeric(channels)
		    isDataRequested = true;
            path2dll = which('UdReader.dll'); 
        else
            path2dll = channels;
            isDataRequested = false;
        end
    case 3
	    isDataRequested = true;
    otherwise
end
if ~exist(path2dll, 'file') 
    error('Invalid path2dll. Must be full absolute path to UdReader.dll')
end

%% Add the UdReader library to the Matlab workspace 
NET.addAssembly(char(path2dll));

%% Instantiate the reader and read the file information
rdr = MTech.UdReader.UdReader(fname);
rdrout = rdr.FullFileInfo;

%% Read the file data
if isDataRequested
    if isempty(channels), rdrout = rdr.GetData();
    else rdrout = rdr.GetData(channels);
    end
end

%% Format the output to JSIS
y.StartTime = double(rdrout.StartTime.Ticks) * 1e-7/86400 + 367;
y.TimeUnits = rdrout.TimeUnits;
y.Name = cell(rdrout.Name);
y.Description = cell(rdrout.Description);
y.Type = cell(rdrout.Type);
y.Units = cell(rdrout.Units);
y.Title = char(rdrout.Title);
y.Data = double(rdrout.Data);

end

