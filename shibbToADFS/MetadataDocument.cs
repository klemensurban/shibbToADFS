using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Web;
using System.Xml;
using shibbToADFS.Properties;

namespace shibbToADFS
{
    public class MetadataDocument
    {
        private static volatile MetadataDocument _instance = null; //cached instance of MetadataDocument
        private static readonly Object CacheLock = new Object();
        private static bool _rebuilding = false; // flag that we a rebuilding the Cache

        private DateTime _metadataLoadedUtc;

        public readonly Object ReadLock = new Object();
        public bool SignatureValid { get; }
        public XmlDocument Document;


        public static MetadataDocument Instance()
        {
            lock (CacheLock)
            {
                if (_instance == null)
                {
                    _instance = new MetadataDocument();
                    return _instance;
                }
                else
                {
                    TimeSpan timespan = DateTime.UtcNow - _instance._metadataLoadedUtc;
                    if (timespan.TotalMinutes >= Settings.Default.CacheTimeMinutes)
                    {
                        if (!_rebuilding)
                        {
                            // Cache too old ... rebuild MetadataDocument (var newMetadataDocument = new MetadataDocument();)
                            // and set the flag to other request that MetadataDocument is rebuilt
                            _rebuilding = true;
                        }
                        else
                        {
                            // Cache too old, but another request is already rebuilding the Cache
                            // still return "old" MetadataDocument
                            return _instance;
                        }
                    }
                    else
                    {
                        // within Cache-Timespan -> return cached MetadataDocument
                        return _instance;
                    }
                }
            }

            // we are the request to renew cache data
            // just do that outside the CacheLock in a local Scope - other request are still served from the cache ..
            var newMetadataDocument = new MetadataDocument();
            lock (CacheLock)
            {
                // assign the new MetadataDocument to the cache instance - new request are now served with the renewed MetadataDocument
                _instance = newMetadataDocument;
                _rebuilding = false;
                return _instance;
            }
        }

        private MetadataDocument()
        {
            Document = new XmlDocument { PreserveWhitespace = true };
            X509Certificate2 cert = new X509Certificate2(Convert.FromBase64String(Settings.Default.SigningCert));
            HttpWebRequest request = WebRequest.CreateHttp(Settings.Default.metadataUrl);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (Stream receiveStream = response.GetResponseStream())
            {
                Document.Load(receiveStream);
                SignedXml signedXml = new SignedXml(Document);
                XmlNodeList nodeList = Document.GetElementsByTagName("Signature", @"http://www.w3.org/2000/09/xmldsig#");
                signedXml.LoadXml((XmlElement)nodeList[0]);
                SignatureValid = signedXml.CheckSignature(cert, true);

            }

            _metadataLoadedUtc = DateTime.UtcNow;
        }

    }
}