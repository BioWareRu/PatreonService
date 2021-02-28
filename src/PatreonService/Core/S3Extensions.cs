using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;

namespace PatreonService.Core
{
    public static class S3Extensions
    {
        public static async Task<bool> UploadJsonAsync<T>(this AmazonS3Client client, T data, string objectKey,
            string bucket)
        {
            using var fileTransferUtility = new TransferUtility(client);
            var path = GetTmpFile();
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data));
            await fileTransferUtility.UploadAsync(path, bucket, objectKey);
            File.Delete(path);
            return true;
        }

        public static async Task<T?> DownloadJsonAsync<T>(this AmazonS3Client client, string objectKey, string bucket)
        {
            using var fileTransferUtility = new TransferUtility(client);
            var path = GetTmpFile();
            await fileTransferUtility.DownloadAsync(path, bucket, objectKey);
            var json = await File.ReadAllTextAsync(path);
            File.Delete(path);
            return JsonSerializer.Deserialize<T>(json);
        }

        private static string GetTmpFile()
        {
            return Path.GetTempFileName();
        }
    }
}