using System;
using System.Text;
using System.Web.Mvc;
using System.Xml;

namespace shibbToADFS.Models
{
    public sealed class XmlActionResult : ActionResult
    {
        private readonly XmlDocument _xmlDocument;

        public XmlActionResult(XmlDocument xmlDocument)
        {
            // ReSharper disable once NotResolvedInText
            _xmlDocument = xmlDocument ?? throw new ArgumentNullException(@"XmlDocument missing");
        }

        public override void ExecuteResult(ControllerContext context)
        {
            context.HttpContext.Response.Clear();
            context.HttpContext.Response.ContentType = "application/samlmetadata+xml";

            using (var writer = new XmlTextWriter(context.HttpContext.Response.OutputStream, Encoding.UTF8) { Formatting = Formatting.None })
                _xmlDocument.WriteTo(writer);
        }
    }
}