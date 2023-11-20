using System.IO.Compression;
using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Logging;

namespace NdjsonConverter.Command.Logic
{
    public class GzipService : IGzipService
    {
        private readonly ILogger<GzipService> _logger;

        public GzipService(ILogger<GzipService> logger)
        {
            _logger = logger;
        }

        public async Task CompressFileAsync(Stream readStream, Stream writeStream, CancellationToken cancellationToken)
        {
            try
            {
                using var compressor = new GZipStream(writeStream, CompressionMode.Compress);
                await readStream.CopyToAsync(compressor, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("{Message}", ex.Message);
                throw;
            }
        }

        public async Task DecompressFileAsync(Stream readStream, Stream writeStream, CancellationToken cancellationToken)
        {
            try
            {
                using var decompressor = new GZipStream(readStream, CompressionMode.Decompress);
                await decompressor.CopyToAsync(writeStream, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("{Message}", ex.Message);
                throw;
            }
        }
    }
}
