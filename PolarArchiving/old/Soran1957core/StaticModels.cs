using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using SGraph;
using factograph;
using System.ComponentModel;
using System.Threading;
//using LOG = factograph.LOG;

namespace Soran1957core
{
    public class StaticModels
    {
        public static SDataModel sDataModel;
        public static bool Initiated = false;
        static string pathString;
        public static ROntologyObjectPropertyDefinition referred;
        public static ROntologyDatatypePropertyDefinition name, alias;
        public static ROntologyClassDefinition naming;
        protected static Dictionary<string, bool> undeleted = new Dictionary<string, bool>();
        static object locker = new object();
       // static BackgroundWorker loadProcessWorker = new BackgroundWorker {WorkerReportsProgress=true };
        public static void Load(object arg)
        {
            Initiated = false;
            string path = arg as string;//e.Argument as string;
            lock (locker)
            {
                //SGraph.LOG.StartTimer();
                //path = VirtualPathUtility.AppendTrailingSlash(path);
                if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\') path += '/';
                pathString = path;
                sDataModel = new SDataModel { OntologyModel = new SOntologyModel() };
                //  try 
                {
                    XElement ontology = XElement.Load(StaticObjects.PublicuemCommon_path + "ontology_iis-v8-doc_ruen.xml"); // @"J:\home\dev\SGraph\ontology_iis-v8-doc_ruen.xml");//StaticObjects.PublicuemCommon_path + "ontology_iis-v8-doc_ruen.xml");
                    sDataModel.OntologyModel.LoadRDF(ontology);

                    //SGraph.LOG.WriteLine("ontology ok.");
                }
                //  catch (Exception) { }
                //factograph.LOG.DataBackupfileName = Path.Combine(path, "backUp");
                //if(!File.Exists(LOG.DataBackupfileName))
                //    using(var temp = File.Create(LOG.DataBackupfileName));
                sDataModel.AddToUserWorkDoc = ((x, docId) =>
                {

                    if (docsInfo.ContainsKey(docId.ToLower()))
                    {
                        var doc = docsInfo[docId.ToLower()];
                        //LOG.WriteBackUp(x);
                        Action<XElement, int> add = null;
                        add = (xml, i) =>
                        {
                            var list = xml.Elements().ToList();
                            foreach (var sx in list)
                            {
                                sx.Remove();
                                add(sx, i + 1);
                                xml.Add(Environment.NewLine, Enumerable.Repeat("\t", i), sx);
                            }
                        };

                        add(x, 1);
                        doc.Root.Add(x);
                        doc.isChanged = true;
                        doc.Save();
                    }
                    //LOG.WriteBackUp(x);
                });
                sDataModel.Include(
                    new XElement("collection",
                        new XAttribute(SGraph.SNames.rdfabout, "cassetterootcollection"),
                        new XElement("name", "Добро пожаловать")));

                string config = path + @"/config.xml";
                if (!File.Exists(config)) return;
                //Надо почистить таблицы 
                cassettesInfo = new Dictionary<string, CassetteInfo>();
                docsInfo = new Dictionary<string, RDFDocumentInfo>();

                XElement xconfig = new XElement("config");
                try
                {
                    xconfig = XElement.Load(config);
                }
                catch (Exception ex)
                {
                    //SGraph.LOG.WriteLine("Ошибка при загрузке кассет: " + ex.Message);
                }
                ProgressPercentage = 0;
                int cassetessCount = xconfig.Elements("LoadCassette").Count(), cassettesLoadedCount = -1, docsInCassetCount=0, docsInCassetLoadedCount=0;
                foreach (XElement lc in xconfig.Elements("LoadCassette"))
                {
                    bool loaddata = true;
                    if (lc.Attribute("regime") != null && lc.Attribute("regime").Value == "nodata") loaddata = false;
                    string cassettePath = lc.Value;

                    CassetteInfo ci = null;
                    cassettesLoadedCount++;
                    try
                    {
                        ci = Cassette.LoadCassette(cassettePath.Contains(':') || cassettePath.StartsWith("\\") ? cassettePath : path + "/" + cassettePath, loaddata);
                    }
                    catch (Exception ex)
                    {
                        //SGraph.LOG.WriteLine("Ошибка при загрузке кассеты [" + cassettePath + "]: " + ex.Message);
                    }
                    if (ci == null || cassettesInfo.ContainsKey(ci.fullName)) continue;
                    cassettesInfo.Add(ci.fullName.ToLower(), ci);
                    if (loaddata)
                    {
                        var documents=ci.docsInfo.Where(di => !docsInfo.ContainsKey(di.dbId.ToLower()));
                        docsInCassetCount = documents.Count();
                        docsInCassetLoadedCount = 0;
                        foreach (var docInfo in documents)
                        {
                            try
                            {
                                docInfo.isEditable = (lc.Attribute("write") != null && docInfo.Root.Attribute("counter") != null);
                                docsInfo.Add(docInfo.dbId.ToLower(), docInfo);
                                sDataModel.LoadRDF(docInfo.Root);
                                //if (!docInfo.isEditable) docInfo.ClearRoot(); //Иногда это действие нужно закомментаривать...
                            }
                            catch (Exception ex)
                            {
                                //LOG.WriteLine("error in document " + docInfo.uri + "\n" + ex.Message);
                            }
                            ProgressPercentage = (cassettesLoadedCount + ((++docsInCassetLoadedCount) / docsInCassetCount)) * 100 / cassetessCount;//);
                        }
                       // loadProcessWorker.ReportProgress(
                    }
                }                     

                // вычисление типов системных сущностей
                var sysObjType = StaticModels.sDataModel.OntologyModel.GetOntologyNode("sys-obj") as SGraph.ROntologyClassDefinition;
                sysTypes = sysObjType.Descendants()
                    .Where(st => !st.IsAbstract).ToArray();
                docNumber = sDataModel.Elements().Where(el => el.Definition.OfDocType()).ToList().Count();
                System.GC.Collect();
                name = sDataModel.OntologyModel.GetOntologyNode<ROntologyDatatypePropertyDefinition>("name");
                alias = sDataModel.OntologyModel.GetOntologyNode<ROntologyDatatypePropertyDefinition>("alias");
                referred = sDataModel.OntologyModel.GetOntologyNode<ROntologyObjectPropertyDefinition>("referred-sys");
                naming = StaticModels.sDataModel.OntologyModel.GetOntologyNode<ROntologyClassDefinition>("naming");

                //Publicuem.User.Init(path);

                Initiated = true;
                //SGraph.LOG.WriteLine(System.DateTime.Now.ToString() + " loading... " + SGraph.LOG.LookTimer());
                initiationInProcess = false;
            }
        }
        static bool initiationInProcess;
        public static int Init(string path)
        {
            if (initiationInProcess)//loadProcessWorker.IsBusy)
            {
                return ProgressPercentage;
            }
            else
            {
                initiationInProcess = true;
                Thread parallelLoad = new Thread(new ParameterizedThreadStart(Load));
                parallelLoad.Start(path);
                return ProgressPercentage;
            }

        }

        //static void loadProcessWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        //{
        //    loadProcessWorker.DoWork -= Load;
        //    loadProcessWorker.ProgressChanged -= loadProcessWorker_ProgressChanged;
        //    loadProcessWorker.RunWorkerCompleted -= loadProcessWorker_RunWorkerCompleted;
        //    initiationInProcess = false;
        //}

        static void loadProcessWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressPercentage = e.ProgressPercentage;
        }
        public static int ProgressPercentage;
        public static ROntologyClassDefinition[] sysTypes;
        public static int docNumber = 0;
        public static Dictionary<string, CassetteInfo> cassettesInfo;
        public static Dictionary<string, RDFDocumentInfo> docsInfo;
    }          //    foreach(var f in Directory.GetFiles(@"G:\Home\FactographProjects\PA_cassettes\PA_Users\originals\0001"))
    //File.WriteAllText(f,  
    //    File.ReadAllText(f)
    //        .Replace(Environment.NewLine, "").Replace("><",">"+Environment.NewLine+"<")
    //        .Replace(">\t<", ">" + Environment.NewLine + "\t" + "<")
    //        .Replace("> <", ">" + Environment.NewLine + " " + "<")
    //        .Replace(">  <", ">" + Environment.NewLine + "  " + "<")
    //        .Replace(">   <", ">" + Environment.NewLine + "   " + "<")
    //        .Replace(">    <", ">" + Environment.NewLine + "    " + "<")
    //        .Replace(">		<", ">" + Environment.NewLine + "		" + "<")
    //        .Replace("ru\">" + Environment.NewLine, "ru\">"));           //XElement.Load(config)
    //        .Elements("LoadCassette")
    //        .Select(LCElement => LCElement.Value)
    //        .Select(cassettePath =>
    //            Cassette.LoadCassette(cassettePath.Contains(':') ? cassettePath : path + "/" + cassettePath))
    //        .Where(cassetteInfo => cassetteInfo != null && !cassettesInfo.ContainsKey(cassetteInfo.fullName))
    //        .ApplyReturn(cassetteInfo => cassettesInfo.Add(cassetteInfo.fullName, cassetteInfo))
    //        .SelectMany(cassetteInfo => cassetteInfo.docsInfo)
    //        .Where(docInfo => !docsInfo.ContainsKey(docInfo.dbId))
    //        .ApplyReturn(docInfo => docsInfo.Add(docInfo.dbId, docInfo))
    //        .Apply(docInfo => 
    //        { 
    //            sDataModel.LoadRDF(docInfo.root); 
    //            //docInfo.root = null; 
    //        });
}
