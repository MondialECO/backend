namespace WebApp.Services
{
    public class SaveFile
    {

        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            // Allowed extensions
            var allowedImageExt = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var allowedVideoExt = new[] { ".mp4", ".webm", ".mov" };
            var allowedDocExt = new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx" };

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            bool isValid = false;

            if (folderName == "media")
            {
                isValid = allowedImageExt.Contains(extension) || allowedVideoExt.Contains(extension);
            }
            else if (folderName == "documents")
            {
                isValid = allowedDocExt.Contains(extension);
            }

            if (!isValid)
                throw new ArgumentException($"Invalid file type: {extension}");

            // Max size: 30MB for media, 20MB for documents
            long maxSize = folderName == "media" ? 30 * 1024 * 1024 : 20 * 1024 * 1024;
            if (file.Length > maxSize)
                throw new ArgumentException($"File too large. Max allowed: {maxSize / (1024 * 1024)}MB");

            // Unique file name
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var uploadPath = Path.Combine("wwwroot", "uploads", folderName, uniqueFileName);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(uploadPath)!);

            // Save file
            using (var stream = new FileStream(uploadPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path (frontend / API)
            return $"/uploads/{folderName}/{uniqueFileName}";
        }



    }
}
