using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;


namespace Storage.Controllers
{
    [Route("api/files")]
    [ApiController]
    public class StorageController : ControllerBase
    {
        private readonly string _storagePath;
        private const int MaxFileSizeMB = 10;
        private static readonly List<string> AllowedMimeTypesList = new();

        static StorageController()
        {
            AllowedMimeTypesList.AddRange(new string[]
            {
                "image/jpeg",
                "image/png",
                "application/pdf",
                "application/msword", 
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 
                "application/vnd.ms-excel", 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" 
            });
        }

        public static string[] AllowedMimeTypes => AllowedMimeTypesList.ToArray();

        public static void AddAllowedMimeType(string mimeType)
        {
            if (!AllowedMimeTypesList.Contains(mimeType))
            {
                AllowedMimeTypesList.Add(mimeType);
            }
        }

        public static void RemoveAllowedMimeType(string mimeType)
        {
            if (AllowedMimeTypesList.Contains(mimeType))
            {
                AllowedMimeTypesList.Remove(mimeType);
            }
        }

        public StorageController()
        {
            _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "SharedStorage");

            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(
            [Required] IFormFile file,
            [FromQuery] bool overwrite = false)
        {
            try
            {
                
                if (file.Length > MaxFileSizeMB * 1024 * 1024)
                    return BadRequest($"File size exceeds {MaxFileSizeMB}MB limit");

                if (!AllowedMimeTypes.Contains(file.ContentType))
                    return BadRequest("Unsupported file type");

                var sanitizedFileName = Path.GetFileName(file.FileName);
                var filePath = Path.Combine(_storagePath, sanitizedFileName);

                if (System.IO.File.Exists(filePath) && !overwrite)
                    return Conflict("File already exists");

                using var stream = new FileStream(
                    filePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 4096,
                    FileOptions.Asynchronous);

                await file.CopyToAsync(stream);

                return Ok(new
                {
                    Path = filePath,
                    FileName = sanitizedFileName,
                    Size = file.Length,
                    UploadDate = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Upload failed: {ex.Message}");
            }
        }

        [HttpGet("list")]
        public IActionResult ListFiles()
        {
            try
            {
                var files = Directory.EnumerateFiles(_storagePath)
                    .Select(f => new FileInfo(f))
                    .Select(fi => new Storage.Models.FileLocation
                    {
                        Name = fi.Name,
                        Size = fi.Length,
                        CreatedDate = fi.CreationTime,
                        FilePath = fi.FullName
                    });

                return Ok(files);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error listing files: {ex.Message}");
            }
        }

        [HttpGet("download/{fileName}")]
        public IActionResult DownloadFile(string fileName)
        {
            try
            {
                var sanitizedFileName = Path.GetFileName(fileName);
                var filePath = Path.Combine(_storagePath, sanitizedFileName);

                if (!System.IO.File.Exists(filePath))
                    return NotFound("File not found");

                var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096,
                    FileOptions.Asynchronous);

                return File(stream, "application/octet-stream", sanitizedFileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Download failed: {ex.Message}");
            }
        }

        [HttpDelete("delete/{fileName}")]
        public IActionResult DeleteFile(string fileName)
        {
            try
            {
                var sanitizedFileName = Path.GetFileName(fileName);
                var filePath = Path.Combine(_storagePath, sanitizedFileName);

                if (!System.IO.File.Exists(filePath))
                    return NotFound("File not found");

                System.IO.File.Delete(filePath);
                return Ok(new { Message = "File deleted successfully" });
            }
            catch (IOException ex)
            {
                return StatusCode(500, $"File in use: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Delete failed: {ex.Message}");
            }
        }

        [HttpPost("allow-mime")]
        public IActionResult AllowMimeType([FromQuery] string mimeType)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
                return BadRequest("MIME type cannot be empty");

            AddAllowedMimeType(mimeType);
            return Ok(new { Message = $"MIME type '{mimeType}' added successfully" });
        }

        [HttpPost("remove-mime")]
        public IActionResult RemoveMimeType([FromQuery] string mimeType)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
                return BadRequest("MIME type cannot be empty");

            RemoveAllowedMimeType(mimeType);
            return Ok(new { Message = $"MIME type '{mimeType}' removed successfully" });
        }

        [HttpGet("allowed-mime")]
        public IActionResult GetAllowedMimeTypes()
        {
            return Ok(AllowedMimeTypes);
        }
    }
}
