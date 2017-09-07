using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace PatreonService.Core
{
    [UsedImplicitly]
    public class S3Provider
    {
        private readonly TransferUtility _fileTransferUtility;

        public S3Provider(AmazonS3Client s3Client)
        {
            _fileTransferUtility = new TransferUtility(s3Client);
        }

        public async Task<bool> UploadJson<T>(T data, string bucketName, string objectKey)
        {
            var path = GetTmpFile();
            await File.WriteAllTextAsync(path, JsonConvert.SerializeObject(data));
            await _fileTransferUtility.UploadAsync(path, bucketName, objectKey);
            File.Delete(path);
            return true;
        }

        public async Task<T> DownloadJson<T>(string bucketName, string objectKey)
        {
            var path = GetTmpFile();
            await _fileTransferUtility.DownloadAsync(path, bucketName, objectKey);
            var json = await File.ReadAllTextAsync(path);
            File.Delete(path);
            return JsonConvert.DeserializeObject<T>(json);
        }

        private static string GetTmpFile()
        {
            return Path.GetTempFileName();
        }
    }
}