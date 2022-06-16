using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace factograph
{
    public static class ONames
    {
            static public XName TagSourcemeta = XName.Get("sourcemeta");        
            static public XName TagFoldername = XName.Get("foldername");        
            static public XName TagDocumentNumber = XName.Get("documentnumber");

            static public XName TagName = XName.Get("name");     
            static public XName TagFromdate = XName.Get("from-date");
            static public XName TagComment = XName.Get("comment"); 
            static public XName TagIisstore = XName.Get("iisstore");     
            static public XName TagCassette = XName.Get("cassette");    
            static public XName TagCassetteUri = XName.Get("cassetteUri");
            static public XName TagCollection = XName.Get("collection");  
            static public XName TagCollectionmember = XName.Get("collection-member");
            static public XName TagIncollection = XName.Get("in-collection");
            static public XName TagCollectionitem = XName.Get("collection-item");
            static public XName TagDocument = XName.Get("document");
            static public XName TagPhotodoc = XName.Get("photo-doc");     
            static public XName TagVideo = XName.Get("video-doc"); 
            static public XName TagAudio = XName.Get("audio-doc");
            static public XName TagReflection = XName.Get("reflection");
            static public XName TagReflected = XName.Get("reflected");
            static public XName TagIndoc = XName.Get("in-doc");
            static public XName TagOrgsys = XName.Get("org-sys");
            static public XName TagOrgrelatives = XName.Get("org-relatives");
            static public XName TagOrgparent = XName.Get("org-parent");
            static public XName TagOrgchild = XName.Get("org-child");
            //
            static public XName AttDbid = XName.Get("dbid");
            static public XName AttOwner = XName.Get("owner");
            static public XName AttPrefix = XName.Get("prefix");
            static public XName AttCounter = XName.Get("counter");
            static public XName AttOriginalname = XName.Get("originalname");
            static public XName AttDocumenttype = XName.Get("documenttype");    
            static public XName AttSize = XName.Get("size");
            static public XName AttUri = XName.Get("uri");  
            static public XName AttDocumentcreation = XName.Get("documentcreation");
            static public XName AttDocumentmodification = XName.Get("documentmodification");
            static public XName AttDocumentfirstuploaded = XName.Get("documentfirstuploaded");
            static public XName AttDocumentlastuploaded = XName.Get("documentlastuploaded");
            static public XName AttChecksum = XName.Get("checksum");
            static public XName AttTransform = XName.Get("transform");
    }
}
