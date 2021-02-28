using System;
using Amazon;
using Sitko.Core.App.Web;
using Sitko.Core.Storage;
using Sitko.Core.Storage.S3;

namespace PatreonService
{
    public class PatreonApplication : WebApplication<PatreonApplication>
    {
        public PatreonApplication(string[] args) : base(args)
        {
            AddModule<S3StorageModule<S3Config>, S3Config>(
                (configuration, _, moduleConfig) =>
                {
                    var uri = configuration["STORAGE_PUBLIC_URI"];
                    if (string.IsNullOrEmpty(uri))
                    {
                        throw new ArgumentException("Storage url is empty");
                    }

                    var success = Uri.TryCreate(uri, UriKind.Absolute, out var publicUri);
                    if (!success)
                    {
                        throw new ArgumentException($"URI {uri} is not proper URI");
                    }

                    if (!string.IsNullOrEmpty(configuration["STORAGE_S3_REGION"]))
                    {
                        moduleConfig.Region = RegionEndpoint.GetBySystemName(configuration["STORAGE_S3_REGION"]);
                    }

                    var serverUriStr = configuration["STORAGE_S3_SERVER_URI"];
                    if (string.IsNullOrEmpty(serverUriStr))
                    {
                        throw new ArgumentException("S3 server url is empty");
                    }

                    var bucketName = configuration["STORAGE_S3_BUCKET"];
                    if (string.IsNullOrEmpty(bucketName))
                    {
                        throw new ArgumentException("S3 bucketName is empty");
                    }

                    var bucketPath = "/";
                    if (!string.IsNullOrEmpty(configuration["STORAGE_S3_BUCKET_PATH"]))
                    {
                        bucketPath = configuration["STORAGE_S3_BUCKET_PATH"];
                    }

                    var accessKey = configuration["STORAGE_S3_ACCESS_KEY"];
                    if (string.IsNullOrEmpty(accessKey))
                    {
                        throw new ArgumentException("S3 access key is empty");
                    }

                    var secretKey = configuration["STORAGE_S3_SECRET_KEY"];
                    if (string.IsNullOrEmpty(secretKey))
                    {
                        throw new ArgumentException("S3 secret key is empty");
                    }

                    success = Uri.TryCreate(serverUriStr, UriKind.Absolute, out var serverUri);
                    if (!success || serverUri == null)
                    {
                        throw new ArgumentException($"S3 server URI {uri} is not proper URI");
                    }

                    moduleConfig.PublicUri = publicUri;
                    moduleConfig.Server = serverUri;
                    moduleConfig.Bucket = bucketName;
                    moduleConfig.Prefix = bucketPath;
                    moduleConfig.AccessKey = accessKey;
                    moduleConfig.SecretKey = secretKey;
                });
        }
    }

    public class S3Config : StorageOptions, IS3StorageOptions
    {
        public Uri Server { get; set; }
        public string Bucket { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public RegionEndpoint Region { get; set; } = RegionEndpoint.USEast1;
    }
}