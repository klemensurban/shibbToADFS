# shibbToADFS
In a shibboleth-based Identity Federation, the metadata of the IDPs and SPs are typically stored in a centralized signed XML metadata document. To enable a participating ADFS instance to automatically update the metadata, this central metadata document must be split into one metadata document per IDP or SP.  Additionally, endpoints that are not supported by ADFS must be deleted from this metadata.
<br/>
The ASP.Net 4.7-based project of this repository provides this function on the fly.  The central metadata file is loaded from an http/https distribution point, the signature is verified with the certificate specified in web.config. 
<br/>
In ADFS the following must be entered as metadata url:

https://&lt;baseurl&gt;/idp?entityID=&lt;urlencoded entityID&gt; (Claims Provider) <br/>
https://&lt;baseurl&gt;/sp?entityID=&lt;urlencoded entityID&gt; (Relying Party)
<br/><br/>
In order to avoid that the central metadata document has to be reloaded from the distribution point for each IDP/SP during an ADFS metadata refresh, it is cached in RAM for a time period (minutes) that can be set in web.config.
<br/>
The XPath of those XML nodes that are to be deleted from the metadata are also configured in web.config.


.
