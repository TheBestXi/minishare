using Microsoft.AspNetCore.Hosting;
using System.ComponentModel.DataAnnotations;

namespace MiniShare.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
        private readonly string[] ALLOWED_EXTENSIONS = { ".jpg", ".jpeg", ".png", ".gif" };
        private const string PRODUCT_IMAGE_PATH = "images/products";

        public FileUploadService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<string> UploadProductImageAsync(IFormFile file, int productRequestId)
        {
            // 验证文件
            ValidateFile(file);

            // 生成唯一文件名
            string fileName = GenerateFileName(file, productRequestId);

            // 保存文件
            string filePath = GetFilePath(fileName);
            await SaveFileAsync(file, filePath);

            // 返回相对URL
            return $"/{PRODUCT_IMAGE_PATH}/{fileName}";
        }

        public async Task<List<string>> UploadProductImagesAsync(List<IFormFile> files, int productRequestId)
        {
            var imageUrls = new List<string>();
            
            foreach (var file in files)
            {
                string imageUrl = await UploadProductImageAsync(file, productRequestId);
                imageUrls.Add(imageUrl);
            }
            
            return imageUrls;
        }

        public async Task<bool> DeleteProductImageAsync(string imageUrl)
        {
            try
            {
                // 从URL中提取文件名
                string fileName = Path.GetFileName(imageUrl);
                string filePath = GetFilePath(fileName);

                // 检查文件是否存在
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    return true;
                }
                
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 验证文件大小和格式
        /// </summary>
        private void ValidateFile(IFormFile file)
        {
            // 验证文件大小
            if (file.Length > MAX_FILE_SIZE)
            {
                throw new ValidationException($"文件大小不能超过{MAX_FILE_SIZE / (1024 * 1024)}MB");
            }

            // 验证文件格式
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!ALLOWED_EXTENSIONS.Contains(extension))
            {
                throw new ValidationException($"文件格式不支持，仅支持{string.Join(", ", ALLOWED_EXTENSIONS)}");
            }
        }

        /// <summary>
        /// 生成唯一文件名
        /// </summary>
        private string GenerateFileName(IFormFile file, int productRequestId)
        {
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string uniqueId = Guid.NewGuid().ToString("N");
            return $"product-request-{productRequestId}-{uniqueId}{extension}";
        }

        /// <summary>
        /// 获取文件保存路径
        /// </summary>
        private string GetFilePath(string fileName)
        {
            // 获取wwwroot目录路径
            string wwwrootPath = _webHostEnvironment.WebRootPath;
            
            // 创建完整的文件保存路径
            string productImageDir = Path.Combine(wwwrootPath, PRODUCT_IMAGE_PATH);
            
            // 确保目录存在
            if (!Directory.Exists(productImageDir))
            {
                Directory.CreateDirectory(productImageDir);
            }
            
            return Path.Combine(productImageDir, fileName);
        }

        /// <summary>
        /// 保存文件到磁盘
        /// </summary>
        private async Task SaveFileAsync(IFormFile file, string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
        }

        #endregion
    }
}