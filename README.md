udreader
========

A reader for common utility data formats

With luck, this project will become a useful reader for various formats of time series data found in the electric utility industry. The intent is to provide a tool that can be used from many different analysis and display platforms such as Matlab, R and Excel. The core code is encapsulated in a .NET library, which in turn can be "consumed" by many different software packages in common use in the industry such as the aforementioned Matlab and Excel.

###Formats supported or pending
* Comtrade (IEEE C37.111) -- rev 1 
* PSLF chf (GE PSLF) -- rev 1 
* Powerworld -- pending 
* PSS/E -- pending 
* JSIS Matlab format -- pending 
* dst (BPA phasor) -- pending
* pdat (BPA time series) -- pending
* JSIS XML -- pending

###Helpers supported or pending
* Matlab -- rev 1
* Excel -- pending
* R -- pending


#For Matlab users
Click on the *Download zip* button on the right side of the udreader home page. When the zip file is safely stored on your computer, open it and extract "UdReader.dll" from the *compiled-lib* folder. Also exract all m-files from the *helpers* folder. Save all of these files in a folder on the Matlab path. Throw the zip file away. In Matlab, read the help for "udread.m".

#For users
The best way to get started is to click on the *Download zip* button above, or at http://github.com/contributor51/udreader if you are not currently looking at the *udreader* page. After you download the zip file, extract "UdReader.dll" from the *compiled-lib* folder. That's all you need to have if you wish to incorporate the udreader into your work platform. You can throw away everything else if you don't want to worry about the source code.

Users may also want to take advantage of the helper functions included in the *helpers* folder above or at the GitHub link in the preceding paragraph. The helper functions show you how to use the udreader within some of the common analysis and plotting tools.

If you run into issues please log them by reporting them as GitHub "issues". If you are able to send a copy of the file that broke the reader that would also be helpful.

#For contributors
We've tried to set up the code with a generic framework so that you can write readers/parsers for your favorite time series data format. We hope the process is relatively straightforward. We're working in F#, but you should be able to write your reader/parser in any .NET language and together we can make it work.

Documentation on how to get started is forthcoming.

#Contact
Contact us at mdonnelly at the domain mtech.edu. We've obscured the email address to reduce spam.