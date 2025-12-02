namespace MiniShare.Services
{
    public interface IFileUploadService
    {
        /// <summary>
        /// 上传商品图片
        /// </summary>
        /// <param name="file">上传的文件</param>
        /// <param name="productRequestId">商品申请ID</param>
        /// <returns>图片URL</returns>
        Task<string> UploadProductImageAsync(IFormFile file, int productRequestId);
        
        /// <summary>
        /// 删除商品图片
        /// </summary>
        /// <param name="imageUrl">图片URL</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteProductImageAsync(string imageUrl);
        
        /// <summary>
        /// 批量上传商品图片
        /// </summary>
        /// <param name="files">上传的文件列表</param>
        /// <param name="productRequestId">商品申请ID</param>
        /// <returns>图片URL列表</returns>
        Task<List<string>> UploadProductImagesAsync(List<IFormFile> files, int productRequestId);
    }
}