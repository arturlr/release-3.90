using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        #region Fields

        Dictionary<string, string> _settings = null;
        Dictionary<string, string> _lang = null;
        //custom code by nopCommerce team
        string confFile = "~/Administration/Content/Roxy_Fileman/conf.json";
        
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
        /// A <c>~/</c>-rooted path maps cleanly onto <c>CommonHelper.MapPath</c>, which resolves
        /// against <c>CommonHelper.BaseDirectory</c> - the content root, assigned by task 7.2
        /// (deferral 1.5).
        /// <para>
        /// A <b>relative</b> path has no ASP.NET Core equivalent at all and is <b>deferral 8.1-3,
        /// owned by task 8.6</b>: <c>HttpServerUtility.MapPath</c> resolved a relative path against
        /// the <i>current request's</i> virtual directory, and nothing in ASP.NET Core has that
        /// notion. Rather than silently guessing a target - the two call sites are
        /// <c>"../Uploads"</c> and <c>"../tmp/"</c>, and
        /// <c>~/Administration/Content/Roxy_Fileman/../Uploads</c> is
        /// <c>~/Administration/Content/Uploads</c>, which does not exist on disk - this throws with
        /// the deferral named. Both live callers are already inoperative for unrelated reasons: the
        /// <c>"../Uploads"</c> one is the fallback for an empty <c>FILES_ROOT</c> and the shipped
        /// <c>conf.json</c> sets <c>FILES_ROOT</c> to <c>~/Content/Images/uploaded</c>, and the
        /// <c>"../tmp/"</c> one already fails with <c>DirectoryNotFoundException</c> because that
        /// directory does not exist in source control (deferral 8.1-1). A named throw is therefore
        /// strictly more informative than what happens today.
        /// </para>
        /// </remarks>
        protected virtual string MapPath(string path)
        {
            if (path != null && path.StartsWith("~"))
                return CommonHelper.MapPath(path);

            throw new NopException(string.Format(
                "RoxyFileman: Server.MapPath('{0}') - a RELATIVE path has no ASP.NET Core equivalent. " +
                "See deferral 8.1-3; task 8.6 owns resolving this to an explicit content-root-relative path.",
                path));
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
                        using (FileStream fs = new FileStream(f.FullName, FileMode.Open))
                        {
                            using (Image img = Image.FromStream(fs))
                            {
                                w = img.Width;
                                h = img.Height;
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
        public virtual bool ThumbnailCallback()
        {
            return false;
        }

        protected virtual void ShowThumbnail(string path, int width, int height)
        {
            CheckPath(path);
            FileStream fs = new FileStream(FixPath(path), FileMode.Open);
            Bitmap img = new Bitmap(Bitmap.FromStream(fs));
            fs.Close();
            fs.Dispose();
            int cropX = 0, cropY = 0;

            double imgRatio = (double)img.Width / (double)img.Height;
        
            if(height == 0)
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
            if(cropWidth < img.Width){
                cropX = Convert.ToInt32(Math.Floor((double)(img.Width - cropWidth) / 2));
            }
            if(cropHeight < img.Height){
                cropY = Convert.ToInt32(Math.Floor((double)(img.Height - cropHeight) / 2));
            }

            Rectangle area = new Rectangle(cropX, cropY, cropWidth, cropHeight);
            Bitmap cropImg = img.Clone(area, System.Drawing.Imaging.PixelFormat.DontCare);
            img.Dispose();
            Image.GetThumbnailImageAbort imgCallback = new Image.GetThumbnailImageAbort(ThumbnailCallback);

            _r.Headers["Content-Type"] = MimeTypes.ImagePng;
            cropImg.GetThumbnailImage(width, height, imgCallback, IntPtr.Zero).Save(_r.Body, ImageFormat.Png);
            cropImg.Dispose();
        }
        private ImageFormat GetImageFormat(string filename){
            ImageFormat ret = ImageFormat.Jpeg;
            switch(new FileInfo(filename).Extension.ToLower()){
                case ".png": ret = ImageFormat.Png; break;
                case ".gif": ret = ImageFormat.Gif; break;
            }
            return ret;
        }
        protected virtual void ImageResize(string path, string dest, int width, int height)
        {
            FileStream fs = new FileStream(path, FileMode.Open);
            Image img = Image.FromStream(fs);
            fs.Close();
            fs.Dispose();
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
            Bitmap newImg = new Bitmap(newWidth, newHeight);
            Graphics g = Graphics.FromImage((Image)newImg);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(img, 0, 0, newWidth, newHeight);
            img.Dispose();
            g.Dispose();
            if(dest != ""){
                newImg.Save(dest, GetImageFormat(dest));
            }
            newImg.Dispose();
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