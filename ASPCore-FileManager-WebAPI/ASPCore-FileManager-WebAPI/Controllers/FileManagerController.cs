using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using Syncfusion.EJ2.FileManager.PhysicalFileProvider;
using Syncfusion.EJ2.FileManager.Base;
using Microsoft.AspNetCore.Cors;
using System.Text.Json;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using System.Data;

namespace ASPCore_FileManager_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [EnableCors("AllowAllOrigins")]
    public class FileManagerController : Controller
    {
        PhysicalFileProvider operation;
        string basePath;
        string root = "wwwroot\\Files";

        public FileManagerController(IWebHostEnvironment hostingEnvironment)
        {
            this.basePath = hostingEnvironment.ContentRootPath;
            //this.basePath = "wwwroot";

            this.operation = new PhysicalFileProvider();
            if (this.basePath.EndsWith("\\"))
                this.operation.RootFolder(this.basePath + this.root);
            else
                this.operation.RootFolder(this.basePath + "\\" + this.root);
        }
        //Validate the requested response using assigned role value
        //[HttpPost]
        [Route("FileOperations")]
        [Authorize(Roles = "Admin")]
        public object FileOperations([FromBody] FileManagerDirectoryContent args)
        {
            if (args.Action == "delete" || args.Action == "rename")
            {
                if ((args.TargetPath == null) && (args.Path == ""))
                {
                    FileManagerResponse response = new FileManagerResponse();
                    response.Error = new ErrorDetails { Code = "401", Message = "Restricted to modify the root folder." };
                    return this.operation.ToCamelCase(response);
                }
            }
            switch (args.Action)
            {
                case "read":
                    // reads the file(s) or folder(s) from the given path.
                    return this.operation.ToCamelCase(this.operation.GetFiles(args.Path, args.ShowHiddenItems));
                case "delete":
                    // deletes the selected file(s) or folder(s) from the given path.
                    return this.operation.ToCamelCase(this.operation.Delete(args.Path, args.Names));
                case "copy":
                    // copies the selected file(s) or folder(s) from a path and then pastes them into a given target path.
                    return this.operation.ToCamelCase(this.operation.Copy(args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData));
                case "move":
                    // cuts the selected file(s) or folder(s) from a path and then pastes them into a given target path.
                    return this.operation.ToCamelCase(this.operation.Move(args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData));
                case "details":
                    // gets the details of the selected file(s) or folder(s).
                    return this.operation.ToCamelCase(this.operation.Details(args.Path, args.Names, args.Data));
                case "create":
                    // creates a new folder in a given path.
                    return this.operation.ToCamelCase(this.operation.Create(args.Path, args.Name));
                case "search":
                    // gets the list of file(s) or folder(s) from a given path based on the searched key string.
                    return this.operation.ToCamelCase(this.operation.Search(args.Path, args.SearchString, args.ShowHiddenItems, args.CaseSensitive));
                case "rename":
                    // renames a file or folder.
                    return this.operation.ToCamelCase(this.operation.Rename(args.Path, args.Name, args.NewName));
                case "filter":
                    if (args.Data[0].SearchString == "")
                    {
                        // Perform read operation while search string is empty.
                        return this.operation.ToCamelCase(this.operation.GetFiles(args.Path, args.ShowHiddenItems));
                    }
                    else
                    {
                        // Perform Search operation while serach string has value.
                        args.SearchString = args.Data[0].SearchString + "*";
                        return this.operation.ToCamelCase(this.operation.Search(args.Path, args.SearchString, args.ShowHiddenItems, args.CaseSensitive));
                    }
            }
            return null;
        }
        /// <summary>
        /// uploads the file(s) into a specified path
        /// </summary>
        /// <param name="path"></param>
        /// <param name="uploadFiles"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        //[HttpPost]
        [Route("Upload")]
        [Authorize(Roles = "Admin")]
        [DisableRequestSizeLimit]
        public IActionResult Upload(string path, long size,IList<IFormFile> uploadFiles, string action)
        {
            FileManagerResponse uploadResponse;
            foreach (var file in uploadFiles)
            {
                var folders = (file.FileName ?? string.Empty).Split('/');
                // checking the folder upload
                if (folders.Length > 1)
                {
                    for (var i = 0; i < folders.Length - 1; i++)
                    {
                        string newDirectoryPath = Path.Combine(this.basePath + path, folders[i]);
                        if (!Directory.Exists(newDirectoryPath))
                        {
                            this.operation.ToCamelCase(this.operation.Create(path, folders[i]));
                        }
                        path += folders[i] + "/";
                    }
                }
            }
            uploadResponse = operation.Upload(path, uploadFiles, action, size);
            if (uploadResponse.Error != null)
            {
                Response.Clear();
                Response.ContentType = "application/json; charset=utf-8";
                Response.StatusCode = Convert.ToInt32(uploadResponse.Error.Code);
                Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = uploadResponse.Error.Message;
            }
            return Content("");
        }
        /// <summary>
        /// downloads the selected file(s) and folder(s)
        /// </summary>
        /// <param name="downloadInput"></param>
        /// <returns></returns>
        //[HttpPost]
        [Route("Download")]
        [Authorize(Roles = "Admin")]
        public IActionResult Download([FromBody] FileManagerDirectoryContent args)
        {
            return operation.Download(args.Path, args.Names, args.Data);
        }

        /// <summary>
        ///  gets the image(s) from the given path
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        //[HttpGet]
        [Route("GetImage")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetImage([FromBody] FileManagerDirectoryContent args)
        {
            return this.operation.GetImage(args.Path, args.Id, false, null, null);
        }
    }
    public class CustomParams
    {
        public string FileName { get; set; }
        public string Action { get; set; }
    }
}