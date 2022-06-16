using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
//using Fogid.Cassettes;
using System.IO;

namespace Polar.Cassettes
{
   public static class CassetteMakeDZCollection
    {
        public static void MakeDZCollection(this Cassette cass, XElement xitem, Func<Cassette, XElement, string, Uri> makePhotoPreviews)
        {
            string uri = "iiss://" + cass.Name + "@iis.nsk.su/"
                + cass._folderNumber + "/" + cass._documentNumber;
            string sdocId = cass.GenerateNewId();
            XElement sdoc = new XElement(ONames.TagDocument,
                new XAttribute(ONames.rdfabout, sdocId),
                xitem.Element(ONames.TagName),
                new XElement(ONames.TagIisstore,
                    new XAttribute(ONames.AttDocumenttype, "scanned/dz"),
                    new XAttribute(ONames.AttUri, uri)));
            cass.db.Add(sdoc);
            string xitemId = xitem.Attribute(ONames.rdfabout).Value;
            foreach (XAttribute att in
                cass.GetInverseXItems(xitemId)
                .Select(cm => cm.Elements()
                                .Select(sel => sel.Attribute(ONames.rdfresource))
                                .Where(att => att != null && att.Value == xitemId))
                 .SelectMany(resourceattributes => resourceattributes))
                att.Value = sdocId;

            // 
            List<XElement> colmembers = cass.GetInverseXItems(sdocId, ONames.TagIncollection)
                .OrderBy(x =>
                {
                    var photoDocId = x.Element(ONames.TagCollectionitem).Attribute(ONames.rdfresource).Value;
                    var path = cass.GetXItemById(photoDocId)
                        .Element("iisstore")
                        .Attribute(ONames.AttOriginalname).Value;
                    var last = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
                    return path.Substring(last + 1).ToSpecialCase();
                })
                .ToList();

            int ii = 0;
            List<XElement> photoDocs = new List<XElement>();
            foreach (XElement collmem in colmembers)
            {
                string scanDocId = cass.GenerateNewId();
                var photoDocLink = collmem.Element(ONames.TagCollectionitem);
                var photoDocId = photoDocLink.Attribute(ONames.rdfresource).Value;
                var photoDoc = cass.GetXItemById(photoDocId);
                photoDocs.Add(photoDoc);
                XElement scanDoc = new XElement("scanned-page",
                 new XAttribute(ONames.rdfabout, scanDocId),
                 new XElement("page-number", ++ii),
                 new XElement("scanned-document",
                     new XAttribute(ONames.rdfresource, sdocId)),
                 new XElement("photo-page",
                     new XAttribute(ONames.rdfresource, photoDocId)));
                cass.db.Add(scanDoc);
                collmem.Remove();
                makePhotoPreviews(cass, photoDoc.Element(ONames.TagIisstore), "z");
            }

            XNamespace dp = "http://schemas.microsoft.com/deepzoom/2009";
            var rootDir = cass.Dir + "/documents/deepzoom";
            if (!Directory.Exists(rootDir))
                Directory.CreateDirectory(rootDir);
            rootDir += "/";
            // var xmlsToArch = new Dictionary<string, string>();
            var document = new XElement(dp + "Collection",
                                new XAttribute(XNamespace.Xmlns + "dp", dp),
                                new XAttribute("xmlns", dp.NamespaceName),
                                new XAttribute(dp + "MaxLevel", "7"),
                                new XAttribute(dp + "TileSize", "256"),
                                new XAttribute(dp + "Format", "jpg"),
                                new XAttribute(dp + "NextItemId", photoDocs.Count()),
                                new XAttribute(dp + "ServerFormat", "Default"),
                                photoDocs.Select((photoDoc, i) =>
                                {
                                    var uriPhoto = photoDoc.Element("iisstore").Attribute("uri").Value;
                                    var url = uriPhoto.Substring(uriPhoto.Length - 9);
                                    // xmlsToArch.Add(rootDir + url+"_files", url+"_files");
                                    url += ".xml";
                                    // xmlsToArch.Add(rootDir + url, url.Substring(0, 4));
                                    var Width = photoDoc.Element("iisstore").Attribute("width").Value;
                                    var Height = photoDoc.Element("iisstore").Attribute("height").Value;
                                    return new XElement(dp + "I",
                                        new XAttribute(dp + "Id", i),
                                        new XAttribute(dp + "N", i),
                                        new XAttribute(dp + "Source", url),
                                        new XElement(dp + "Size",
                                            new XAttribute(dp + "Width", Width)
                                            , new XAttribute(dp + "Height", Height)));
                                }));

            var imageXml = rootDir + cass._folderNumber + cass._documentNumber;
            document.Save(imageXml + ".xml");

            //   xmlsToArch.Add(imageXml + ".xml", "");
            //  Archive.ToZip.ZipFiles(imageXml+".zip", xmlsToArch);
            //Archive.ToArchive.Create(imageXml, xmlsToArch);             
            cass.IncrementDocumentNumber();
            cass.Save();
        }
    }
}
