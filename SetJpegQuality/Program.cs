using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiniSite;
using System.Xml;
using System.Collections;
using System.IO;

namespace SetJpegQuality {
    class Program {

        static string ApplicationRootPath;

        static string outputFile_ = "";
        static string siteList_ = "";
        static bool verbose_ = false;
        static bool stage2_ = false;
        static bool showHelp_ = false;
        static string action_;
        static string moveDest_ = "";
        static long quality_ = 85;
        static char startLetter_ = 'd';

        static int totalCount_ = 0;
        static ArrayList argArr_ = null;
        static string[] args_ = null;
        static void Init() {
            argArr_ = new ArrayList(args_);

            showHelp_ = argArr_.Contains("-h") || args_.Length == 0;

            if (showHelp_) return;
            ApplicationRootPath = args_[0];
            XmlDocument docWebConfig = new XmlDocument();
            docWebConfig.Load(ApplicationRootPath + "Web.Config");

            System.IO.File.Copy(ApplicationRootPath + "Web.Config", System.IO.Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase) + ".Config", true);
            siteList_ = parseArg("-sites");
            quality_ = long.Parse(parseArg("-q", "85"));
            verbose_ = argArr_.Contains("-v");
            stage2_ = argArr_.Contains("-stage2");
            showHelp_ = argArr_.Contains("-h");

        }

        private static string parseArg(string flag) {
            return parseArg(flag, "");
        }

        private static string parseArg(string flag, string def) {
            string ans = def;
            if (argArr_.Contains(flag)) {
                ans = args_[argArr_.IndexOf(flag) + 1];
            }
            return ans;
        }

        public static void Main(string[] args) {
            args_ = args;
            Init();
            if (showHelp_) {
                showHelp();
            }
            else {
                string sqlStr = "SELECT SiteID FROM tblSites where SiteState <> 2 " + (siteList_.Trim().Length > 0 ? "AND SiteID in(" + siteList_ + ")" : "");
                MiniSite.DAL.Sites.tblSitesDataTable sites = MiniSite.DAL.DataStore.GetSites(sqlStr);
                foreach (MiniSite.DAL.Sites.tblSitesRow rowSite in sites.Rows) {
                    Work(rowSite);
                }
            }
            Console.WriteLine("");
        }

        private static void showHelp() {
            Console.WriteLine("PictureCleanup <configPath>");
            Console.WriteLine("\t-v : Verbose mode - Only used when action is \"check\"");
            Console.WriteLine("\t-q <quality>: Set quality");
            Console.WriteLine("\t-stage2 <quality>: Set quality for jpeg and optimize png, bmp, gif, pnm, tiff");
            Console.WriteLine("\t-sites <siteID1,siteID2,...> - If not present, all sites processed");
        }

        private static void Output(object message) {
            if (outputFile_ != "") {
                FileStream fs = File.Open(outputFile_, FileMode.Append);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(message);
                sw.Close();
            }
            else {
                Console.WriteLine(message);
            }
        }

        static void SetQuality(string file) {
            if (verbose_) {
                Output(file);
            }

            System.Drawing.Image imgInput = null;
            //System.IO.FileStream strmInput = System.IO.File.Open(file, System.IO.FileMode.Open);
            try {
                imgInput = System.Drawing.Image.FromFile(file);
            }
            catch (OutOfMemoryException) {
            }
            //strmInput.Close();
            if (verbose_) {
                Output(imgInput.Width + "x" + imgInput.Height);
            }
            string newFile = file.Substring(0, file.LastIndexOf(".") + 1) + "new";
            MiniSite.Util.SaveJpeg(imgInput, newFile, quality_);
            imgInput.Dispose();
            System.IO.File.Delete(file);
            MiniSite.Util.MoveFile(newFile, file);
        }

        static void Work(MiniSite.DAL.Sites.tblSitesRow rowSite) {
            Output(rowSite.SiteID + " " + rowSite.Symbol);
            string folder = System.Configuration.ConfigurationSettings.AppSettings["MotherSitePath"].TrimEnd('\\') + "\\Sites\\" + MiniSite.Util.SiteIDToSiteFolderName(rowSite.SiteID) + "\\img\\";
            if (verbose_) {
                Output(folder);
            }
            //int count = 0;
            if (!stage2_) {
                foreach (string file in System.IO.Directory.GetFiles(folder, "*.jpg", System.IO.SearchOption.AllDirectories)) {
                    try {
                        SetQuality(file);
                    }
                    catch (System.Exception exp) {
                        System.Console.Error.WriteLine(MiniSite.Util.GetErrorDebug(exp));
                    }
                    //if (count++ > 9) break;
                }
            }
            else {
                foreach (string file in System.IO.Directory.GetFiles(folder, "*.jpeg", System.IO.SearchOption.AllDirectories)) {
                    try {
                        SetQuality(file);
                    }
                    catch (System.Exception exp) {
                        System.Console.Error.WriteLine(MiniSite.Util.GetErrorDebug(exp));
                    }
                    //if (count++ > 9) break;
                }
                string[] types = new string[] { "png", "bmp", "gif", "pnm", "tiff" };
                foreach (string type in types) {
                    foreach (string file in System.IO.Directory.GetFiles(folder, "*." + type, System.IO.SearchOption.AllDirectories)) {
                        try {
                            if (verbose_) {
                                Output(file);
                            }
                            MiniSite.Util.OptimizeImage(file, true);
                        }
                        catch (System.Exception exp) {
                            System.Console.Error.WriteLine(MiniSite.Util.GetErrorDebug(exp));
                        }
                        //if (count++ > 9) break;
                    }
                }
            }
        }
    }
}
