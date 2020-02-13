using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.Xml;
using System.Web.Mvc;
using System.Xml;
using System.Xml.XPath;
using shibbToADFS.Models;
using shibbToADFS.Properties;

namespace shibbToADFS.Controllers
{
    public class MetadataController : Controller
    {
        [HttpGet]
        [Route("idp")]
        // ReSharper disable once InconsistentNaming
        public ActionResult Idp(string entityID)
        {
            XmlDocument adfsMetaData = GetMetadataformEntityID(entityID);

            var adfsNamespaceManager = AdfsNamespaceManager(adfsMetaData);

            foreach (string xPath in Settings.Default.IdpNodes2Delete)
            {
                DeleteNodes(adfsMetaData, adfsNamespaceManager, xPath);
            }

            return new XmlActionResult(adfsMetaData);
        }

        [HttpGet]
        [Route("sp")]
        // ReSharper disable once InconsistentNaming
        public ActionResult Sp(string entityID)
        {
            XmlDocument adfsMetaData = GetMetadataformEntityID(entityID);

            // Get all defined Namespaces from Document
            var adfsNamespaceManager = AdfsNamespaceManager(adfsMetaData);

            foreach (string xPath in Settings.Default.SpNodes2Delete)
            {
                DeleteNodes(adfsMetaData, adfsNamespaceManager, xPath);
            }


            return new XmlActionResult(adfsMetaData);
        }

        [HttpGet]
        [Route("spproxy")]
        // ReSharper disable once InconsistentNaming
        public ActionResult SpProxy(string metadataUrl)
        {
            XmlDocument metaData   = new XmlDocument { PreserveWhitespace = true };

            HttpWebRequest request = WebRequest.CreateHttp(metadataUrl);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (Stream receiveStream = response.GetResponseStream())
            {
                metaData.Load(receiveStream);
            }

            // Get all defined Namespaces from Document
            var adfsNamespaceManager = AdfsNamespaceManager(metaData);

            foreach (string xPath in Settings.Default.SpProxyNodes2Delete)
            {
                DeleteNodes(metaData, adfsNamespaceManager, xPath);
            }

            return new XmlActionResult(metaData);
        }

        private static XmlNamespaceManager AdfsNamespaceManager(XmlDocument adfsMetaData)
        {
            var adfsNamespaceManager = new XmlNamespaceManager(adfsMetaData.NameTable);
            XPathNavigator nav = adfsMetaData.CreateNavigator();
            nav.MoveToFollowing(XPathNodeType.Element);
            var nameSpaces = nav.GetNamespacesInScope(XmlNamespaceScope.ExcludeXml);
            if (nameSpaces != null)
                foreach (var ns in nameSpaces)
                {
                    adfsNamespaceManager.AddNamespace(ns.Key, ns.Value);
                }

            return adfsNamespaceManager;
        }

        private static XmlDocument GetMetadataformEntityID(string entityID)
        {
            XmlDocument adfsMetaData = new XmlDocument();
            adfsMetaData.LoadXml(Settings.Default.ADFSMetaData);

            MetadataDocument metadataDocument = MetadataDocument.Instance();
            if (!metadataDocument.SignatureValid) throw new Exception("Metadata Signature invalid");


            lock (metadataDocument.ReadLock)
            {
                var xmlNamespaceManager = AdfsNamespaceManager(metadataDocument.Document);
                string xpathTemplate = Settings.Default.XPath2Copy;
                string xpath = String.Format(xpathTemplate, entityID);
                var entitiesDescriptorNode = metadataDocument.Document.SelectSingleNode(xpath, xmlNamespaceManager);
                XmlNode copiedNode = adfsMetaData.ImportNode(entitiesDescriptorNode, true);
                adfsMetaData.DocumentElement.AppendChild(copiedNode);
            }

            return adfsMetaData;
        }

        private static void DeleteNodes(XmlDocument adfsMetaData, XmlNamespaceManager adfsNamespaceManager,
            string xpath)
        {
            XmlNodeList nodes2Delete = adfsMetaData.SelectNodes(xpath, adfsNamespaceManager);
            if (nodes2Delete != null)
                foreach (XmlNode node in nodes2Delete)
                {
                    node.ParentNode.RemoveChild(node);
                }
        }

    }
}
