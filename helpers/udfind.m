function y = udfind( jsisstruct, findstr )
%UDFIND Finds channels within a JSIS data structure
%   y = udfind(jsisstruct, findstr) finds the channel number(s)
%   associated with findstr within a jsisstruct where jsisstruct is a JSIS
%   data structure. findstr is a regular expression. For more information
%   on regular expressions see .
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

