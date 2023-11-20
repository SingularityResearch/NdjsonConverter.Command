using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Util;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace NdjsonConverter.Command.Logic
{
    public class AmazonS3Service : IAmazonS3Service
    {
        private readonly ILogger<AmazonS3Service> _logger;

        public AmazonS3Service(ILogger<AmazonS3Service> logger)
        {
            _logger = logger;
        }

        public async Task<bool> DownloadAsync(Amazon.RegionEndpoint region, string bucket, string key, string path, CancellationToken cancellationToken)
        {
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = bucket,
                    Key = key
                };
                var client = new AmazonS3Client(region);
                using GetObjectResponse response = await client.GetObjectAsync(request, cancellationToken);
                await response.WriteResponseStreamToFileAsync(path, true, cancellationToken);
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError("{Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("{Message}", ex.Message);
                throw;
            }
        }

        public async Task UploadAsync(Amazon.RegionEndpoint region, string bucket, string key, string path, CancellationToken cancellationToken)
        {
            try
            {
                var request = new PutObjectRequest
                {
                    BucketName = bucket,
                    FilePath = path,
                    ContentType = "application/x-gzip",
                    Key = key
                };
                var client = new AmazonS3Client(region);
                var response = await client.PutObjectAsync(request, cancellationToken);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError("{Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("{Message}", ex.Message);
                throw;
            }
        }
    }
}
