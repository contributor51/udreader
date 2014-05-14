function y = jfind( jsisstruct, findstr )
%JFIND Finds channels within a JSIS data structure
%   y = jfind(jsisstruct, findstr) finds the channel number(s)
%   associated with case-insensitive findstr within a jsisstruct where 
%   jsisstruct is a JSIS data structure. findstr is a regular expression.
%   Regular expressions form a powerful search syntax, but you don't need
%   to know the full syntax to make it work for you. Just use a simple 
%   search string such as 'North Bus' to get a match. More information
%   on regular expressions can be found at <a href="matlab:web('http://en.wikipedia.org/wiki/Regular_expression#Examples')">Regex Wiki</a>.
%
%   jfind searches both jsisstruct.Name and jsisstruct.Description for
%   matches.
%
%   y is a vector of channel numbers. y is empty if no matches were found.
%

y = [];

% Check for valid jsisstruct
if ~all(isfield(jsisstruct, {'Name' 'Data'})), warning('Invalid jsis structure.'), return, end

% Search the Name field for the search string
namechans = find(~cellfun(@isempty, regexpi(jsisstruct.Name, findstr)));
% Search the Description field for the search string
if isfield(jsisstruct, 'Description')
    descchans = find(~cellfun(@isempty, ...
        regexpi(jsisstruct.Description, findstr)));
else descchans = [];
end

% Remove duplicates (found both in Name and Description)
% This will also sort the results -- a side effect you may not want.
y = unique([namechans descchans]);

end % fun jfind

