namespace NdjsonConverter.Command.Logic;

public interface IGzipService
{
    Task CompressFileAsync(Stream readStream, Stream writeStream, CancellationToken cancellationToken);
    Task DecompressFileAsync(Stream readStream, Stream writeStream, CancellationToken cancellationToken);
}