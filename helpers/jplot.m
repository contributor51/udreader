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

chans = jfind(jsisstruct, findstr);
plot(jsisstruct.Data(:,1), jsisstruct.Data(:,chans))
legend(jsisstruct.Name{chans})
xlabel('Time (s)')

end % fun jplot

