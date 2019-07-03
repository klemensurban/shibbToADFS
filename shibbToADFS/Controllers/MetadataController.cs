using System;
using System.Web.Mvc;
using System.Xml;
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

            var adfsns = adfsMetaData.DocumentElement.NamespaceURI;
            var adfsNamespaceManager = new XmlNamespaceManager(adfsMetaData.NameTable);
            adfsNamespaceManager.AddNamespace("md", adfsns);

            DeleteNodes(adfsMetaData, adfsNamespaceManager, "//md:SPSSODescriptor");
            DeleteNodes(adfsMetaData, adfsNamespaceManager, "//md:AttributeAuthorityDescriptor");

            return new XmlActionResult(adfsMetaData);
        }

        [HttpGet]
        [Route("sp")]
        // ReSharper disable once InconsistentNaming
        public ActionResult Sp(string entityID)
        {
            XmlDocument adfsMetaData = GetMetadataformEntityID(entityID);

            var adfsns = adfsMetaData.DocumentElement.NamespaceURI;
            var adfsNamespaceManager = new XmlNamespaceManager(adfsMetaData.NameTable);
            adfsNamespaceManager.AddNamespace("md", adfsns);

            DeleteNodes(adfsMetaData, adfsNamespaceManager, "//md:IDPSSODescriptor");
            DeleteNodes(adfsMetaData, adfsNamespaceManager, "//md:AttributeAuthorityDescriptor");

            return new XmlActionResult(adfsMetaData);
        }

        private static XmlDocument GetMetadataformEntityID(string entityID)
        {
            XmlDocument adfsMetaData = new XmlDocument();
            adfsMetaData.LoadXml(Settings.Default.ADFSMetaData);

            MetadataDocument metadataDocument = MetadataDocument.Instance();
            if (!metadataDocument.SignatureValid) throw new Exception("Metadata Signature invalid");


            lock (metadataDocument.ReadLock)
            {
                string xmlns = metadataDocument.Document.DocumentElement.NamespaceURI;
                var xmlNamespaceManager = new XmlNamespaceManager(metadataDocument.Document.NameTable);
                xmlNamespaceManager.AddNamespace("md", xmlns);
                var entitiesDescriptorNode = metadataDocument.Document.SelectSingleNode($"/md:EntitiesDescriptor/md:EntityDescriptor[@entityID=\"{entityID}\"]", xmlNamespaceManager);
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
