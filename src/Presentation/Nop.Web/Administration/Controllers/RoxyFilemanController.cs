using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using Nop.Core;
using Nop.Services.Security;
using Nop.Web.Framework.Security;

namespace Nop.Admin.Controllers
{
    //Controller for Roxy fileman (http://www.roxyfileman.com/) for TinyMCE editor
    //the original file was \RoxyFileman-1.4.3-net\fileman\asp_net\main.ashx
    //some custom changes by wooncherk contribution

    //do not validate request token (XSRF)
    [AdminAntiForgery(true)]
    public class RoxyFilemanController : BaseAdminController
    {
        #region Const

        /// <summary>
        /// The virtual path of the Roxy Fileman installation directory - the directory that
        /// holds <c>conf.json</c>, <c>index.html</c>, <c>lang/</c>, <c>js/</c>, <c>css/</c> and
        /// <c>images/</c>. Every RELATIVE path in this file resolves against it; see
        /// <see cref="MapPath"/> for why, and for the evidence.
        /// </summary>
        private const string ROXY_FILEMAN_ROOT = "~/Administration/Content/Roxy_Fileman/";

        #endregion

        #region Fields

        Dictionary<string, string> _settings = null;
        Dictionary<string, string> _lang = null;
        //custom code by nopCommerce team
        //TASK 8.6 - derived from ROXY_FILEMAN_ROOT rather than repeated as a literal, so the
        //relationship between the configuration file and the base that relative paths resolve
        //against is explicit rather than coincidental. Same string as 3.90.
        string confFile = ROXY_FILEMAN_ROOT + "conf.json";
        
        //custom code by nopCommerce team
        private readonly IPermissionService _permissionService;

        #endregion

        #region Ctor

        //custom code by nopCommerce team
        //TASK 8.3 - the HttpContextBase constructor parameter is GONE. Task 6.4 deleted the five
        //HttpContextBase/HttpRequestBase/HttpResponseBase/HttpServerUtilityBase/HttpSessionStateBase
        //registrations from Nop.Web.Framework/DependencyRegistrar.cs (runtime-deferrals.md section
        //9a), so nothing resolves it any more - and a controller never needed it: ControllerBase
        //exposes the ambient HttpContext directly. This is a breaking constructor change, but the
        //controller is registered reflectively (RegisterAssemblyTypes over ControllerBase, task
        //6.4 section 17.5), so no DI edit is required.
        public RoxyFilemanController(IPermissionService permissionService)
        {
            this._permissionService = permissionService;
        }

        #endregion

        #region ASP.NET Core seams replacing the legacy hosting members this file was written against

        //_context/_r are kept as names so the ~90 call sites below read as they did in 3.90. They
        //are now the controller's own ambient context rather than injected wrappers.
        private HttpContext _context { get { return HttpContext; } }

        private HttpResponse _r { get { return Response; } }

        /// <summary>
        /// Replaces <c>HttpResponse.Write(string)</c>, which became the asynchronous
        /// <c>WriteAsync</c>. Every caller here is a synchronous <c>void</c> helper reached from a
        /// synchronous action, so the wait is blocking - the trade-off tasks 4.2, 6.2 and 7.3
        /// already accepted (ASP.NET Core installs no SynchronizationContext, so it cannot
        /// deadlock).
        /// </summary>
        protected virtual void WriteResponse(string value)
        {
            Response.WriteAsync(value).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Replaces <c>HttpServerUtility.MapPath</c>.
        /// </summary>
        /// <remarks>
        /// A <c>~/</c>-rooted path maps straight onto <c>CommonHelper.MapPath</c>, which resolves
        /// against <c>CommonHelper.BaseDirectory</c> - the content root, assigned by task 7.2
        /// (deferral 1.5).
        /// <para>
        /// <b>DEFERRAL 8.1-3 / 8.3-3 - RESOLVED HERE.</b> A <b>relative</b> path has no ASP.NET
        /// Core equivalent: <c>HttpServerUtility.MapPath</c> resolved one against the directory of
        /// the <i>current request's</i> virtual path, and nothing in ASP.NET Core models that.
        /// Task 8.3 made this method throw <c>NopException</c> naming the deferral rather than
        /// guess a translation, and recorded that task 8.6 must establish the <i>intended</i>
        /// target rather than mechanically translate the <c>..</c>. It is established as
        /// <see cref="ROXY_FILEMAN_ROOT"/>, on four pieces of evidence:
        /// </para>
        /// <list type="number">
        /// <item><description>
        /// In the upstream Roxy Fileman 1.4.3 distribution the handler this file was ported from
        /// is <c>fileman/asp_net/main.ashx</c>, so <c>../lang/</c>, <c>../tmp/</c> and
        /// <c>../Uploads</c> all mean "a sibling of <c>asp_net</c>, i.e. inside the fileman
        /// installation directory". nopCommerce's fileman installation directory is
        /// <c>~/Administration/Content/Roxy_Fileman/</c>.
        /// </description></item>
        /// <item><description>
        /// <c>lang/</c> exists on disk at exactly <c>Administration/Content/Roxy_Fileman/lang/</c>
        /// and <b>nowhere else in the repository</b> (verified: it is the only directory named
        /// <c>lang</c> in the whole tree), and it holds the 11 <c>&lt;code&gt;.json</c> files
        /// <see cref="GetLangFile"/> asks for.
        /// </description></item>
        /// <item><description>
        /// Deferral <b>8.1-1</b> independently identifies the directory <c>../tmp/</c> refers to as
        /// <c>Content/Roxy_Fileman/tmp/</c> - it was carried by a
        /// <c>&lt;Folder Include="Content\Roxy_Fileman\tmp\"/&gt;</c> placeholder in the legacy
        /// project file.
        /// </description></item>
        /// <item><description>
        /// <c>conf.json</c>'s own action URLs are written relative to the same directory
        /// (<c>"../../../Admin/RoxyFileman/ProcessRequest"</c> resolves to the application root
        /// from <c>Administration/Content/Roxy_Fileman/</c>).
        /// </description></item>
        /// </list>
        /// <para>
        /// <b>How a relative path is resolved.</b> Upstream, the handler this file was ported from
        /// sits one level BELOW the fileman root (<c>fileman/asp_net/main.ashx</c>), which is
        /// precisely what the leading <c>../</c> in every one of these literals exists to climb
        /// out of. nopCommerce has no such subdirectory - the controller has no directory at all -
        /// so the leading run of <c>./</c> and <c>../</c> segments is consumed and the remainder is
        /// resolved against <see cref="ROXY_FILEMAN_ROOT"/>. <c>../lang/en.json</c> therefore lands
        /// on <c>Administration/Content/Roxy_Fileman/lang/en.json</c>, which is where the 11
        /// language files actually are. Any <c>..</c> left after that run is normalised by
        /// <see cref="Path.GetFullPath(string)"/> and then rejected if it escapes the fileman
        /// directory, so a relative path cannot be used to traverse out of it - no in-tree caller
        /// can trigger that (every relative literal is a compile-time constant or comes from
        /// <c>conf.json</c>), it is simply cheaper to be safe by construction than to argue about
        /// it.
        /// </para>
        /// <para>
        /// <b>This is deliberately NOT what 3.90 resolved to at runtime, and that is recorded
        /// rather than glossed.</b> Under System.Web the base was the request's own directory, so
        /// for the MVC route <c>/Admin/RoxyFileman/ProcessRequest</c> the base was
        /// <c>/Admin/RoxyFileman/</c> and <c>../lang/en.json</c> resolved to
        /// <c>&lt;root&gt;/Admin/lang/en.json</c> - a path that does not exist. 3.90's language
        /// file therefore never loaded: <c>ParseJSON</c>'s empty <c>catch</c> swallowed the read
        /// failure and <see cref="LangRes"/> returned the resource <i>key</i>
        /// ("E_UploadNotAll") instead of a sentence. All three relative paths were broken by the
        /// .ashx-to-MVC port in 3.90 and were never fixed. Reproducing that would mean preserving
        /// a defect, so the intended base is implemented instead; the observable improvement is
        /// that Roxy Fileman error messages are localized again.
        /// </para>
        /// <para>
        /// Only this relative branch normalises paths - <see cref="FixPath"/> always builds a
        /// <c>~/</c>-rooted path and takes the first branch, so <see cref="CheckPath"/>'s prefix
        /// comparison behaves exactly as before.
        /// </para>
        /// </remarks>
        protected virtual string MapPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return CommonHelper.MapPath(ROXY_FILEMAN_ROOT);

            if (path.StartsWith("~"))
                return CommonHelper.MapPath(path);

            //relative to the Roxy Fileman installation directory - see the remarks
            var relative = path.Replace('\\', '/').TrimStart('/');
            while (true)
            {
                if (relative.StartsWith("./")) { relative = relative.Substring(2); continue; }
                if (relative.StartsWith("../")) { relative = relative.Substring(3); continue; }
                break;
            }

            var roxyRoot = CommonHelper.MapPath(ROXY_FILEMAN_ROOT);
            var resolved = Path.GetFullPath(Path.Combine(roxyRoot,
                relative.Replace('/', Path.DirectorySeparatorChar)));

            var roxyRootFull = Path.GetFullPath(roxyRoot);
            var ceiling = roxyRootFull.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? roxyRootFull
                : roxyRootFull + Path.DirectorySeparatorChar;
            if (resolved != roxyRootFull.TrimEnd(Path.DirectorySeparatorChar) &&
                !resolved.StartsWith(ceiling, StringComparison.Ordinal))
            {
                throw new NopException(string.Format(
                    "RoxyFileman: the relative path '{0}' resolves outside the Roxy Fileman directory", path));
            }

            return resolved;
        }

        #endregion

        #region Methods

        public virtual void ProcessRequest() {
            string action = "DIRLIST";

            //custom code by nopCommerce team
            if (!_permissionService.Authorize(StandardPermissionProvider.HtmlEditorManagePictures))
                WriteResponse(GetErrorRes("You don't have required permission"));

            try{
                if (GetRequestValue("a") != null)
                    action = (string)GetRequestValue("a");

                //custom code by nopCommerce team
                //VerifyAction(action);
                switch (action.ToUpper())
                {
                    case "DIRLIST":
                        ListDirTree(GetRequestValue("type"));
                        break;
                    case "FILESLIST":
                        ListFiles(GetRequestValue("d"), GetRequestValue("type"));
                        break;
                    case "COPYDIR":
                        CopyDir(GetRequestValue("d"), GetRequestValue("n"));
                        break;
                    case "COPYFILE":
                        CopyFile(GetRequestValue("f"), GetRequestValue("n"));
                        break;
                    case "CREATEDIR":
                        CreateDir(GetRequestValue("d"), GetRequestValue("n"));
                        break;
                    case "DELETEDIR":
                        DeleteDir(GetRequestValue("d"));
                        break;
                    case "DELETEFILE":
                        DeleteFile(GetRequestValue("f"));
                        break;
                    case "DOWNLOAD":
                        DownloadFile(GetRequestValue("f"));
                        break;
                    case "DOWNLOADDIR":
                        DownloadDir(GetRequestValue("d"));
                        break;
                    case "MOVEDIR":
                        MoveDir(GetRequestValue("d"), GetRequestValue("n"));
                        break;
                    case "MOVEFILE":
                        MoveFile(GetRequestValue("f"), GetRequestValue("n"));
                        break;
                    case "RENAMEDIR":
                        RenameDir(GetRequestValue("d"), GetRequestValue("n"));
                        break;
                    case "RENAMEFILE":
                        RenameFile(GetRequestValue("f"), GetRequestValue("n"));
                        break;
                    case "GENERATETHUMB":
                        int w = 140, h = 0;
                        int.TryParse(GetRequestValue("width").Replace("px", ""), out w);
                        int.TryParse(GetRequestValue("height").Replace("px", ""), out h);
                        ShowThumbnail(GetRequestValue("f"), w, h);
                        break;
                    case "UPLOAD":
                        Upload(GetRequestValue("d"));
                        break;
                    default:
                        WriteResponse(GetErrorRes("This action is not implemented."));
                        break;
                }
        
            }
            catch(Exception ex){
                if (action == "UPLOAD" && !IsAjaxUpload())
                {
                    WriteResponse("<script>");
                    WriteResponse("parent.fileUploaded(" + GetErrorRes(LangRes("E_UploadNoFiles")) + ");");
                    WriteResponse("</script>");
                }
                else{
                    WriteResponse(GetErrorRes(ex.Message));
                }
            }
        
        }
        
        #endregion

        #region Utitlies

        private string FixPath(string path)
        {
            //custom code by nopCommerce team
            if (path == null)
                path = "";

            if (!path.StartsWith("~")){
                if (!path.StartsWith("/"))
                    path = "/" + path;
                path = "~" + path;
            }

            //custom code by nopCommerce team
            var rootDirectory = GetSetting("FILES_ROOT");
            if (!path.ToLowerInvariant().Contains(rootDirectory.ToLowerInvariant()))
                path = rootDirectory;

            return MapPath(path);
        }
        private string GetLangFile(){
            string filename = "../lang/" + GetSetting("LANG") + ".json";
            if (!System.IO.File.Exists(MapPath(filename)))
                filename = "../lang/en.json";
            return filename;
        }
        protected virtual string LangRes(string name)
        {
            string ret = name;
            if (_lang == null)
                _lang = ParseJSON(GetLangFile());
            if (_lang.ContainsKey(name))
                ret = _lang[name];

            return ret;
        }
        protected virtual string GetFileType(string ext){
            string ret = "file";
            ext = ext.ToLower();
            if(ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif")
                ret = "image";
              else if(ext == ".swf" || ext == ".flv")
                ret = "flash";
            return ret;
        }
        protected virtual bool CanHandleFile(string filename)
        {
            bool ret = false;
            FileInfo file = new FileInfo(filename);
            string ext = file.Extension.Replace(".", "").ToLower();
            string setting = GetSetting("FORBIDDEN_UPLOADS").Trim().ToLower();
            if (setting != "")
            {
                ArrayList tmp = new ArrayList();
                tmp.AddRange(Regex.Split(setting, "\\s+"));
                if (!tmp.Contains(ext))
                    ret = true;
            }
            setting = GetSetting("ALLOWED_UPLOADS").Trim().ToLower();
            if (setting != "")
            {
                ArrayList tmp = new ArrayList();
                tmp.AddRange(Regex.Split(setting, "\\s+"));
                if (!tmp.Contains(ext))
                    ret = false;
            }
        
            return ret;
        }
        protected virtual Dictionary<string, string> ParseJSON(string file){
            Dictionary<string, string> ret = new Dictionary<string,string>();
            string json = "";
            try{
                json = System.IO.File.ReadAllText(MapPath(file), System.Text.Encoding.UTF8);
            }
            catch{}

            json = json.Trim();
            if(json != ""){
                if (json.StartsWith("{"))
                    json = json.Substring(1, json.Length - 2);
                json = json.Trim();
                json = json.Substring(1, json.Length - 2);
                string[] lines = Regex.Split(json, "\"\\s*,\\s*\"");
                foreach(string line in lines){
                    string[] tmp = Regex.Split(line, "\"\\s*:\\s*\"");
                    try{
                        if (tmp[0] != "" && !ret.ContainsKey(tmp[0]))
                        {
                           ret.Add(tmp[0], tmp[1]);
                        }
                    }
                    catch{}
                }
            }
            return ret;
        }
        protected virtual string GetFilesRoot(){
            string ret = GetSetting("FILES_ROOT");
            //TASK 8.3 - System.Web's HttpSessionState had an object indexer; ASP.NET Core's
            //ISession is a byte-array store with typed accessors and no implicit serialization
            //(same change task 4.2 made in ExternalAuthorizerHelper). The stored value is a path
            //string, so GetString is an exact fit. SESSION_PATH_KEY is "" in the shipped
            //conf.json, so this branch is not reached by default.
            if (GetSetting("SESSION_PATH_KEY") != "")
            {
                var sessionPath = _context.Session.GetString(GetSetting("SESSION_PATH_KEY"));
                if (sessionPath != null)
                    ret = sessionPath;
            }
        
            if(ret == "")
                ret = MapPath("../Uploads");
            else
                ret = FixPath(ret);
            return ret;
        }
        protected virtual void LoadConf(){
            if(_settings == null)
                _settings = ParseJSON(confFile);
        }
        protected virtual string GetSetting(string name){
            string ret = "";
            LoadConf();
            if(_settings.ContainsKey(name))
                ret = _settings[name];
        
            return ret;
        }
        protected virtual void CheckPath(string path)
        {
            if (FixPath(path).IndexOf(GetFilesRoot()) != 0)
            {
                throw new Exception("Access to " + path + " is denied");
            }
        }
        protected virtual void VerifyAction(string action)
        {
            string setting = GetSetting(action);
            if (setting.IndexOf("?") > -1)
                setting = setting.Substring(0, setting.IndexOf("?"));
            if (!setting.StartsWith("/"))
                setting = "/" + setting;
            setting = ".." + setting;
        
            if (MapPath(setting) != MapPath("~" + _context.Request.Path.ToString()))
                throw new Exception(LangRes("E_ActionDisabled"));
        }
        protected virtual string GetResultStr(string type, string msg)
        {
            return "{\"res\":\"" + type + "\",\"msg\":\"" + msg.Replace("\"","\\\"") + "\"}";
        }
        protected virtual string GetSuccessRes(string msg)
        {
            return GetResultStr("ok", msg);
        }
        protected virtual string GetSuccessRes()
        {
            return GetSuccessRes("");
        }
        protected virtual string GetErrorRes(string msg)
        {
            return GetResultStr("error", msg);
        }
        private void _copyDir(string path, string dest){
            if(!Directory.Exists(dest))
                Directory.CreateDirectory(dest);
            foreach(string f in  Directory.GetFiles(path)){
                FileInfo file = new FileInfo(f);
                if (!System.IO.File.Exists(Path.Combine(dest, file.Name)))
                {
                    System.IO.File.Copy(f, Path.Combine(dest, file.Name));
                }
            }
            foreach (string d in Directory.GetDirectories(path))
            {
                DirectoryInfo dir = new DirectoryInfo(d);
                _copyDir(d, Path.Combine(dest, dir.Name));
            }
        }
        protected virtual void CopyDir(string path, string newPath)
        {
            CheckPath(path);
            CheckPath(newPath);
            DirectoryInfo dir = new  DirectoryInfo(FixPath(path));
            DirectoryInfo newDir = new DirectoryInfo(FixPath(newPath + "/" + dir.Name));
        
            if (!dir.Exists)
            {
                throw new Exception(LangRes("E_CopyDirInvalidPath"));    
            }
            else if (newDir.Exists)
            {
                throw new Exception(LangRes("E_DirAlreadyExists"));
            }
            else{
                _copyDir(dir.FullName, newDir.FullName);
            }
            WriteResponse(GetSuccessRes());
        }
        protected virtual string MakeUniqueFilename(string dir, string filename){
            string ret = filename;
            int i = 0;
            while (System.IO.File.Exists(Path.Combine(dir, ret)))
            {
                i++;
                ret = Path.GetFileNameWithoutExtension(filename) + " - Copy " + i.ToString() + Path.GetExtension(filename);
            }
            return ret;
        }
        protected virtual void CopyFile(string path, string newPath)
        {
            CheckPath(path);
            FileInfo file = new FileInfo(FixPath(path));
            newPath = FixPath(newPath);
            if (!file.Exists)
                throw new Exception(LangRes("E_CopyFileInvalisPath"));
            else{
                string newName = MakeUniqueFilename(newPath, file.Name);
                try{
                    System.IO.File.Copy(file.FullName, Path.Combine(newPath, newName));
                    WriteResponse(GetSuccessRes());
                }
                catch{
                    throw new Exception(LangRes("E_CopyFile"));
                }
            }
        }
        protected virtual void CreateDir(string path, string name)
        {
            CheckPath(path);
            path = FixPath(path);
            if(!Directory.Exists(path))
                throw new Exception(LangRes("E_CreateDirInvalidPath"));
            else{
                try
                {
                    path = Path.Combine(path, name);
                    if(!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                    WriteResponse(GetSuccessRes());
                }
                catch
                {
                    throw new Exception(LangRes("E_CreateDirFailed"));
                }
            }
        }
        protected virtual void DeleteDir(string path)
        {
            CheckPath(path);
            path = FixPath(path);
            if (!Directory.Exists(path))
                throw new Exception(LangRes("E_DeleteDirInvalidPath"));
            else if (path == GetFilesRoot())
                throw new Exception(LangRes("E_CannotDeleteRoot")); 
            else if(Directory.GetDirectories(path).Length > 0 || Directory.GetFiles(path).Length > 0)
                throw new Exception(LangRes("E_DeleteNonEmpty"));
            else
            {
                try
                {
                    Directory.Delete(path);
                    WriteResponse(GetSuccessRes());
                }
                catch
                {
                    throw new Exception(LangRes("E_CannotDeleteDir"));
                }
            }
        }
        protected virtual void DeleteFile(string path)
        {
            CheckPath(path);
            path = FixPath(path);
            if (!System.IO.File.Exists(path))
                throw new Exception(LangRes("E_DeleteFileInvalidPath"));
            else
            {
                try
                {
                    System.IO.File.Delete(path);
                    WriteResponse(GetSuccessRes());
                }
                catch
                {
                    throw new Exception(LangRes("E_DeletеFile"));
                }
            }
        }
        private List<string> GetFiles(string path, string type){
            List<string> ret = new List<string>();
            if(type == "#")
                type = "";
            string[] files = Directory.GetFiles(path);
            foreach(string f in files){
                if ((GetFileType(new FileInfo(f).Extension) == type) || (type == ""))
                    ret.Add(f);
            }
            return ret;
        }
        private ArrayList ListDirs(string path){
            string[] dirs = Directory.GetDirectories(path);
            ArrayList ret = new ArrayList();
            foreach(string dir in dirs){
                ret.Add(dir);
                ret.AddRange(ListDirs(dir));
            }
            return ret;
        }
        protected virtual void ListDirTree(string type)
        {
            DirectoryInfo d = new DirectoryInfo(GetFilesRoot());
            if(!d.Exists)
                throw new Exception("Invalid files root directory. Check your configuration.");
            
            ArrayList dirs = ListDirs(d.FullName);
            dirs.Insert(0, d.FullName);
        
            string localPath = MapPath("~/");
            WriteResponse("[");
            for(int i = 0; i <dirs.Count; i++){
                string dir = (string) dirs[i];
                WriteResponse("{\"p\":\"/" + dir.Replace(localPath, "").Replace("\\", "/") + "\",\"f\":\"" + GetFiles(dir, type).Count.ToString() + "\",\"d\":\"" + Directory.GetDirectories(dir).Length.ToString() + "\"}");
                if(i < dirs.Count -1)
                    WriteResponse(",");
            }
            WriteResponse("]");
        }
        protected virtual double LinuxTimestamp(DateTime d){
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0).ToLocalTime();
            TimeSpan timeSpan = (d.ToLocalTime() - epoch);
        
            return timeSpan.TotalSeconds;

        }
        protected virtual void ListFiles(string path, string type)
        {
            CheckPath(path);
            string fullPath = FixPath(path);
            List<string> files = GetFiles(fullPath, type);
            WriteResponse("[");
            for(int i = 0; i < files.Count; i++){
                FileInfo f = new FileInfo(files[i]);
                int w = 0, h = 0;
                if (GetFileType(f.Extension) == "image"){
                    try{
                        //TASK 8.6 - System.Drawing's Image.FromStream DECODED the whole image just
                        //to read two integers, and on a non-Windows host it throws before it gets
                        //that far (measured: TypeInitializationException -> DllNotFoundException
                        //'libgdiplus'). ImageSharp's Image.Identify reads only the header, so this
                        //is both portable and strictly cheaper - a directory of large JPEGs no
                        //longer decodes every one of them to populate the listing.
                        //BEHAVIOUR DIFFERENCE, recorded: Identify returns null for a file it
                        //cannot recognise where Image.FromStream threw. 3.90's rethrow below then
                        //aborted the ENTIRE directory listing on one corrupt file; the entry now
                        //simply reports 0x0 and the listing completes. The catch/rethrow is kept
                        //for anything that does throw (an unreadable file, an I/O error).
                        using (FileStream fs = new FileStream(f.FullName, FileMode.Open, FileAccess.Read))
                        {
                            var imageInfo = Image.Identify(fs);
                            if (imageInfo != null)
                            {
                                w = imageInfo.Width;
                                h = imageInfo.Height;
                            }
                        }
                    }
                    //TASK 8.3 - `throw ex` reset the stack trace (CA2200); `throw` preserves it.
                    //Pre-existing 3.90 defect, fixed in passing because it was previously masked by
                    //the declaration errors in this file and would otherwise land as a new warning.
                    catch(Exception){throw;}
                }
                WriteResponse("{");
                WriteResponse("\"p\":\""+path + "/" + f.Name+"\"");
                WriteResponse(",\"t\":\"" + Math.Ceiling(LinuxTimestamp(f.LastWriteTime)).ToString() + "\"");
                WriteResponse(",\"s\":\""+f.Length.ToString()+"\"");
                WriteResponse(",\"w\":\""+w.ToString()+"\"");
                WriteResponse(",\"h\":\""+h.ToString()+"\"");
                WriteResponse("}");
                if (i < files.Count - 1)
                    WriteResponse(",");
            }
            WriteResponse("]");
        }
        public virtual void DownloadDir(string path)
        {
            path = FixPath(path);
            if(!Directory.Exists(path))
                throw new Exception(LangRes("E_CreateArchive"));
            string dirName = new FileInfo(path).Name;
            string tmpZip = MapPath("../tmp/" + dirName + ".zip");
            if (System.IO.File.Exists(tmpZip))
                System.IO.File.Delete(tmpZip);
            ZipFile.CreateFromDirectory(path, tmpZip,CompressionLevel.Fastest, true);
            //TASK 8.3 - HttpResponse.TransmitFile/Flush/End have no ASP.NET Core counterparts.
            //SendFileAsync is the replacement for TransmitFile (and, like it, can use the host's
            //sendfile fast path). Response.End() is simply gone - a handler returns to the
            //pipeline instead of aborting the request (task 6.2 recorded this for
            //XmlDownloadResult/RemotePost); nothing is written after these calls.
            //NOTE the file must be deleted AFTER the body has been sent, not after a synchronous
            //Flush() as 3.90 did - SendFileAsync streams it.
            _r.Clear();
            _r.Headers["Content-Disposition"] = "attachment; filename=\"" + dirName + ".zip\"";
            _r.ContentType = MimeTypes.ApplicationForceDownload;
            _r.SendFileAsync(tmpZip).GetAwaiter().GetResult();
            System.IO.File.Delete(tmpZip);
        }
        protected virtual void DownloadFile(string path)
        {
            CheckPath(path);
            FileInfo file = new FileInfo(FixPath(path));
            if(file.Exists){
                _r.Clear();
                _r.Headers["Content-Disposition"] = "attachment; filename=\"" + file.Name + "\"";
                _r.ContentType = MimeTypes.ApplicationForceDownload;
                _r.SendFileAsync(file.FullName).GetAwaiter().GetResult();
            }
        }
        protected virtual void MoveDir(string path, string newPath)
        {
            CheckPath(path);
            CheckPath(newPath);
            DirectoryInfo source = new DirectoryInfo(FixPath(path));
            DirectoryInfo dest = new DirectoryInfo(FixPath(Path.Combine(newPath, source.Name)));
            if(dest.FullName.IndexOf(source.FullName) == 0)
                throw new Exception(LangRes("E_CannotMoveDirToChild"));
            else if (!source.Exists)
                throw new Exception(LangRes("E_MoveDirInvalisPath"));
            else if (dest.Exists)
                throw new Exception(LangRes("E_DirAlreadyExists"));
            else{
                try{
                    source.MoveTo(dest.FullName);
                    WriteResponse(GetSuccessRes());
                }
                catch{
                    throw new Exception(LangRes("E_MoveDir") + " \"" + path + "\"");
                }
            }
        
        }
        protected virtual void MoveFile(string path, string newPath)
        {
            CheckPath(path);
            CheckPath(newPath);
            FileInfo source = new FileInfo(FixPath(path));
            FileInfo dest = new FileInfo(FixPath(newPath));
            if (!source.Exists)
                throw new Exception(LangRes("E_MoveFileInvalisPath"));
            else if (dest.Exists)
                throw new Exception(LangRes("E_MoveFileAlreadyExists"));
            else
            {
                try
                {
                    source.MoveTo(dest.FullName);
                    WriteResponse(GetSuccessRes());
                }
                catch
                {
                    throw new Exception(LangRes("E_MoveFile") + " \"" + path + "\"");
                }
            }
        }
        protected virtual void RenameDir(string path, string name)
        {
            CheckPath(path);
            DirectoryInfo source = new DirectoryInfo(FixPath(path));
            DirectoryInfo dest = new DirectoryInfo(Path.Combine(source.Parent.FullName, name));
            if(source.FullName == GetFilesRoot())
                throw new Exception(LangRes("E_CannotRenameRoot"));
            else if (!source.Exists)
                throw new Exception(LangRes("E_RenameDirInvalidPath"));
            else if (dest.Exists)
                throw new Exception(LangRes("E_DirAlreadyExists"));
            else
            {
                try
                {
                    source.MoveTo(dest.FullName);
                    WriteResponse(GetSuccessRes());
                }
                catch
                {
                    throw new Exception(LangRes("E_RenameDir") + " \"" + path + "\"");
                }
            }
        }
        protected virtual void RenameFile(string path, string name)
        {
            CheckPath(path);
            FileInfo source = new FileInfo(FixPath(path));
            FileInfo dest = new FileInfo(Path.Combine(source.Directory.FullName, name));
            if (!source.Exists)
                throw new Exception(LangRes("E_RenameFileInvalidPath"));
            else if (!CanHandleFile(name))
                throw new Exception(LangRes("E_FileExtensionForbidden"));
            else
            {
                try
                {
                    source.MoveTo(dest.FullName);
                    WriteResponse(GetSuccessRes());
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message + "; " + LangRes("E_RenameFile") + " \"" + path + "\"");
                }
            }
        }
        /// <summary>
        /// Gets the resampler used for every resize in this controller.
        /// </summary>
        /// <remarks>
        /// TASK 8.6. 3.90's <see cref="ImageResize"/> asked GDI+ for
        /// <c>InterpolationMode.HighQualityBicubic</c> explicitly, so bicubic is the faithful
        /// choice; it is also ImageSharp's own default and what
        /// <c>Nop.Services/Media/PictureService.cs</c> selected at task 4.2, so all resizing in
        /// the application now shares one resampler.
        /// </remarks>
        protected virtual IResampler Resampler
        {
            get { return KnownResamplers.Bicubic; }
        }

        /// <summary>
        /// Renders a cropped, scaled thumbnail of a picture straight to the response as PNG.
        /// </summary>
        /// <remarks>
        /// TASK 8.6 (design section 7, Requirements 5.8, 5.9, 5.11). The
        /// <c>Bitmap</c>/<c>Bitmap.FromStream</c>/<c>Bitmap.Clone(Rectangle, PixelFormat)</c>/
        /// <c>Image.GetThumbnailImage</c> pipeline is Windows-only from .NET 6 and, with the
        /// <c>System.Drawing.Common</c> 4.7.2 pin this solution carries, fails on Linux with
        /// <c>TypeInitializationException</c> wrapping
        /// <c>DllNotFoundException: Unable to load shared library 'libgdiplus'</c> - measured, not
        /// assumed. That is the defect this task removes.
        ///
        /// <para>
        /// <b>Every line of arithmetic below is 3.90's, unchanged.</b> Only the imaging primitives
        /// moved: the centre-crop rectangle is still computed the same way, the requested box is
        /// still clamped to the source dimensions, <c>height == 0</c> still derives the height from
        /// the source aspect ratio, and the output is still PNG regardless of the source format.
        /// Verified by execution: a 200x100 source asked for 140x120 still yields exactly 140x100
        /// (height clamped), asked for 140x0 still yields 140x70, and a 50x30 source asked for
        /// 500x400 still yields 50x30.
        /// </para>
        /// <para>
        /// <b><see cref="ResizeMode.Stretch"/>, not <c>Max</c> - a deliberate difference from
        /// PictureService.</b> The explicit <c>Crop</c> has already produced a region with the
        /// target aspect ratio, and GDI+'s <c>GetThumbnailImage(width, height, ...)</c> produced
        /// <i>exactly</i> that box, so <c>Stretch</c> is what reproduces it. <c>PictureService</c>
        /// uses <c>Max</c> because it is replacing ImageResizer's padding <c>FitMode</c>, a
        /// different problem.
        /// </para>
        /// <para>
        /// <b>Two things GDI+ did that are deliberately not reproduced.</b>
        /// <c>Image.GetThumbnailImageAbort</c> has no ImageSharp counterpart and is dropped
        /// (design section 7), along with the <c>ThumbnailCallback</c> method that existed only to
        /// satisfy it - ImageSharp's resize needs no abort callback. And
        /// <c>GetThumbnailImage</c> would silently return a JPEG's <b>embedded EXIF thumbnail</b>
        /// when one was present and large enough, which is typically 160x120 and visibly worse
        /// than a real resample; ImageSharp always resamples the full image, so thumbnails of
        /// camera JPEGs come out better than 3.90's. Neither changes the output dimensions.
        /// </para>
        /// <para>
        /// <b>The response write is buffered.</b> 3.90 saved the thumbnail directly into the
        /// response stream. Kestrel disallows synchronous writes to <c>Response.Body</c>
        /// (<c>AllowSynchronousIO</c> is <c>false</c> by default), so the image is encoded into
        /// memory and written with <c>WriteAsync</c> - the same substitution task 6.2 made for
        /// <c>Response.BinaryWrite</c>. A thumbnail is at most a few hundred KB.
        /// </para>
        /// </remarks>
        protected virtual void ShowThumbnail(string path, int width, int height)
        {
            CheckPath(path);

            byte[] thumbnail;
            using (var fs = new FileStream(FixPath(path), FileMode.Open, FileAccess.Read))
            using (var img = Image.Load(fs))
            {
                int cropX = 0, cropY = 0;

                double imgRatio = (double)img.Width / (double)img.Height;

                if (height == 0)
                    height = Convert.ToInt32(Math.Floor((double)width / imgRatio));

                if (width > img.Width)
                    width = img.Width;
                if (height > img.Height)
                    height = img.Height;

                double cropRatio = (double)width / (double)height;
                int cropWidth = Convert.ToInt32(Math.Floor((double)img.Height * cropRatio));
                int cropHeight = Convert.ToInt32(Math.Floor((double)cropWidth / cropRatio));
                if (cropWidth > img.Width)
                {
                    cropWidth = img.Width;
                    cropHeight = Convert.ToInt32(Math.Floor((double)cropWidth / cropRatio));
                }
                if (cropHeight > img.Height)
                {
                    cropHeight = img.Height;
                    cropWidth = Convert.ToInt32(Math.Floor((double)cropHeight * cropRatio));
                }
                if (cropWidth < img.Width)
                {
                    cropX = Convert.ToInt32(Math.Floor((double)(img.Width - cropWidth) / 2));
                }
                if (cropHeight < img.Height)
                {
                    cropY = Convert.ToInt32(Math.Floor((double)(img.Height - cropHeight) / 2));
                }

                var area = new Rectangle(cropX, cropY, cropWidth, cropHeight);
                //locals, because Mutate's lambda cannot capture the by-value parameters that the
                //clamping above reassigned without the compiler complaining about them later
                var targetWidth = width;
                var targetHeight = height;

                img.Mutate(x => x
                    .Crop(area)
                    .Resize(new ResizeOptions
                    {
                        Size = new Size(targetWidth, targetHeight),
                        //the crop above already matched the aspect ratio; Stretch is what
                        //reproduces GDI+ GetThumbnailImage's exact target box - see the remarks
                        Mode = ResizeMode.Stretch,
                        Sampler = Resampler
                    }));

                using (var destStream = new MemoryStream())
                {
                    img.Save(destStream, new PngEncoder());
                    thumbnail = destStream.ToArray();
                }
            }

            _r.Headers["Content-Type"] = MimeTypes.ImagePng;
            _r.Body.WriteAsync(thumbnail, 0, thumbnail.Length).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets the encoder to write a resized image back with, chosen from the destination
        /// file extension.
        /// </summary>
        /// <remarks>
        /// TASK 8.6 - the replacement for 3.90's <c>GetImageFormat</c>, which returned a
        /// <c>System.Drawing.Imaging.ImageFormat</c>. The same three-way mapping is preserved
        /// exactly, including the JPEG default for any other extension. The GIF branch is where
        /// design section 7's note applies: ImageSharp's built-in GIF encoder quantizes to a
        /// palette by default, covering what <c>ImageResizer.Plugins.PrettyGifs</c> provided for
        /// <c>Nop.Services</c> and what GDI+'s GIF encoder did here. The palette it chooses is
        /// not byte-identical to GDI+'s, so a re-encoded GIF's exact pixels may differ; its
        /// dimensions and format do not.
        /// </remarks>
        private IImageEncoder GetImageEncoder(string filename)
        {
            IImageEncoder ret = new JpegEncoder();
            switch(new FileInfo(filename).Extension.ToLower()){
                case ".png": ret = new PngEncoder(); break;
                case ".gif": ret = new GifEncoder(); break;
            }
            return ret;
        }

        /// <summary>
        /// Shrinks an uploaded image in place so that it fits within the configured
        /// <c>MAX_IMAGE_WIDTH</c>/<c>MAX_IMAGE_HEIGHT</c>.
        /// </summary>
        /// <remarks>
        /// TASK 8.6. <c>new Bitmap(w, h)</c> + <c>Graphics.FromImage</c> +
        /// <c>InterpolationMode.HighQualityBicubic</c> + <c>DrawImage</c> becomes a single
        /// <c>Mutate(x =&gt; x.Resize(...))</c> with the bicubic <see cref="Resampler"/>.
        ///
        /// <para>
        /// <b>The arithmetic is 3.90's verbatim, including two of its quirks.</b> The early
        /// return when the image already fits (or when both bounds are zero) is preserved, so an
        /// in-range upload is still <b>not rewritten at all</b> - it keeps its original bytes and
        /// its original encoder, which matters because a re-encode would recompress a JPEG.
        /// And <c>Convert.ToInt16</c> is kept rather than quietly widened to
        /// <c>Convert.ToInt32</c>: it is 3.90's, the bound comes from <c>conf.json</c>
        /// (<c>MAX_IMAGE_WIDTH</c>/<c>HEIGHT</c>, shipped as 1000), and widening it would change
        /// which inputs throw.
        /// </para>
        /// <para>
        /// Verified by execution: 1200x600 constrained to 1000x1000 yields 1000x500; a 100x50
        /// source constrained to 1000x1000 writes nothing at all; width and height both 0 writes
        /// nothing; and a <c>.png</c>/<c>.jpg</c>/<c>.gif</c> destination is encoded as PNG/JPEG/GIF
        /// respectively.
        /// </para>
        /// <para>
        /// The source stream is fully read and closed before the destination is written, because
        /// <see cref="Upload"/> calls this with <c>path == dest</c> - the same file. 3.90 relied on
        /// the same ordering.
        /// </para>
        /// </remarks>
        protected virtual void ImageResize(string path, string dest, int width, int height)
        {
            //read and release the source file before anything is written back - Upload calls
            //this with dest == path, i.e. the same file. 3.90 relied on the same ordering.
            byte[] resized;
            using (var img = Image.Load(System.IO.File.ReadAllBytes(path)))
            {
                float ratio = (float)img.Width / (float)img.Height;
                if ((img.Width <= width && img.Height <= height) || (width == 0 && height == 0))
                    return;

                int newWidth = width;
                int newHeight = Convert.ToInt16(Math.Floor((float)newWidth / ratio));
                if ((height > 0 && newHeight > height) || (width == 0))
                {
                    newHeight = height;
                    newWidth = Convert.ToInt16(Math.Floor((float)newHeight * ratio));
                }

                img.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(newWidth, newHeight),
                    Mode = ResizeMode.Stretch,
                    Sampler = Resampler
                }));

                if (dest == "")
                    return;

                using (var destStream = new MemoryStream())
                {
                    img.Save(destStream, GetImageEncoder(dest));
                    resized = destStream.ToArray();
                }
            }

            System.IO.File.WriteAllBytes(dest, resized);
        }
        protected virtual bool IsAjaxUpload()
        {
            return (GetRequestValue("method") != null && GetRequestValue("method").ToString() == "ajax");
        }
        protected virtual void Upload(string path)
        {
            CheckPath(path);
            path = FixPath(path);
            string res = GetSuccessRes();
            bool hasErrors = false;
            try{
                //TASK 8.3 - Request.Files -> Request.Form.Files (guarded, because ASP.NET Core
                //throws on Request.Form for a non-form request), and HttpPostedFileBase.SaveAs ->
                //IFormFile.CopyTo over a FileStream. IFormFile has no SaveAs.
                var uploadedFiles = Request.HasFormContentType
                    ? (IList<IFormFile>)Request.Form.Files
                    : new List<IFormFile>();
                for(int i = 0; i < uploadedFiles.Count; i++){
                    if (CanHandleFile(uploadedFiles[i].FileName))
                    {
                        FileInfo f = new FileInfo(uploadedFiles[i].FileName);
                        string filename = MakeUniqueFilename(path, f.Name);
                        string dest = Path.Combine(path, filename);
                        using (var destStream = new FileStream(dest, FileMode.Create, FileAccess.Write))
                            uploadedFiles[i].CopyTo(destStream);
                        if (GetFileType(new FileInfo(filename).Extension) == "image")
                        {
                            int w = 0;
                            int h = 0;
                            int.TryParse(GetSetting("MAX_IMAGE_WIDTH"), out w);
                            int.TryParse(GetSetting("MAX_IMAGE_HEIGHT"), out h);
                            ImageResize(dest, dest, w, h);
                        }
                    }
                    else
                    {
                        hasErrors = true;
                        res = GetSuccessRes(LangRes("E_UploadNotAll"));
                    }
                }
            }
            catch(Exception ex){
                res = GetErrorRes(ex.Message);
            }
            if (IsAjaxUpload())
            {
                if(hasErrors)
                    res = GetErrorRes(LangRes("E_UploadNotAll"));
                WriteResponse(res);
            }
            else
            {
                WriteResponse("<script>");
                WriteResponse("parent.fileUploaded(" + res + ");");
                WriteResponse("</script>");
            }
        }
        
        #endregion

    }
}