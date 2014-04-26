function y = udfind( jsisstruct, findstr )
%UDFIND Finds channels within a JSIS data structure
%   y = udfind(jsisstruct, findstr) finds the channel number(s)
%   associated with findstr within a jsisstruct where jsisstruct is a JSIS
%   data structure. findstr is a regular expression. Regular expressions
%   form a powerful search syntax, but you don't need to know the full
%   syntax to make it work for you. Just use a simple search string such
%   as 'North Bus' to get a match. More information on regular expressions
%   can be found at <a href="matlab:web('http://en.wikipedia.org/wiki/Regular_expression#Examples')">Regex Wiki</a>.
%
%   udfind searches both jsisstruct.Name and jsisstruct.Description for
%   matches.
%
%   y is a vector of channel numbers. y is empty if no matches were found.
%

namechans = find(~cellfun(@isempty, regexp(jsisstruct.Name, findstr)));
if isfield(jsisstruct, 'Description')
    descchans = find(~cellfun(@isempty, ...
        regexp(jsisstruct.Description, findstr)));
else descchans = [];
end

y = unique([namechans descchans]);

end % fun udfind

