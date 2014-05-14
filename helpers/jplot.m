function jplot( jsisstruct, findstr )
%JPLOT Plots channels within a JSIS data structure
%   jplot(jsisstruct, findstr) plots the channel number(s)
%   associated with findstr within a jsisstruct where jsisstruct is a JSIS
%   data structure. findstr is a regular expression. Regular expressions
%   form a powerful search syntax, but you don't need to know the full
%   syntax to make it work for you. Just use a simple search string such
%   as 'North Bus' to get a match. More information on regular expressions
%   can be found at <a href="matlab:web('http://en.wikipedia.org/wiki/Regular_expression#Examples')">Regex Wiki</a>.
%
%   jplot searches both jsisstruct.Name and jsisstruct.Description for
%   matches.
%

% Get the channels to plot. This will throw a warning if the jsis struct
% is not valid.
chans = jfind(jsisstruct, findstr);

% Warn if no channels found
if isempty(chans), warning('No matching channels found.'); return, end

% Warn if no data is present
if isempty(jsisstruct.Data)
    warning('No data present in the Data field.'); 
	return
end

% Plot and label
plot(jsisstruct.Data(:,1), jsisstruct.Data(:,chans))
legend(jsisstruct.Name{chans})
xlabel('Time (s)')

end % fun jplot

