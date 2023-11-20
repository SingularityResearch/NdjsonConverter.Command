namespace NdjsonConverter.Command.Logic;

public interface IAmazonS3Service
{
    Task<bool> DownloadAsync(Amazon.RegionEndpoint region, string bucket, string key, string path, CancellationToken cancellationToken);

    Task UploadAsync(Amazon.RegionEndpoint region, string bucket, string key, string path,
        CancellationToken cancellationToken);
}