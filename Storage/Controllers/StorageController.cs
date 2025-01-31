using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Storage.Controllers
{
    [Route("api/files")]
    [ApiController]
    public class StorageController : ControllerBase
    {
        private readonly string _storagePath = "./SharedStorage";
        public StorageController()
        {
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> FileUpload (IFormFile file)
        {
            if (file == null) {
                return BadRequest("No File Was Uploaded");
            }
            var PathToFile = Path.Combine(_storagePath, file.Name);
            using (var stream = new FileStream (PathToFile, FileMode.Create, FileAccess.ReadWrite))
            {
                await file.CopyToAsync (stream);
            }
            return Ok(new {  PathToFile, file.FileName, file.Length});
        }

        [HttpGet("View")]
        public IActionResult ViewFiles()
        {
            var filesDirectory = Directory.GetFiles(_storagePath);
            var NameOfFile = new List<string>();
            foreach (var file in filesDirectory) 
            {
                NameOfFile.Add(Path.GetFileName(file));
            }
            return Ok (NameOfFile);
        }

        [HttpGet("download/{fileName}")]
        public IActionResult DownloadFiles(string fileName) 
        {
            var Download = Path.Combine(_storagePath, fileName);
            if (!System.IO.File.Exists(Download)) 
                return BadRequest("No such file");
            var fileBytes = System.IO.File.ReadAllBytes(Download);
            return File(fileBytes, "application/octet-stream", fileName);
        }

        [HttpDelete("delete/{fileName}")]
        public IActionResult DeleteFiles (string fileName)
        {
            var filePath = Path.Combine(_storagePath, fileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound("No Such File");

            System.IO.File.Delete(filePath);
            return Ok(new { Message = "File deleted successfully" });
        }
    }
}

