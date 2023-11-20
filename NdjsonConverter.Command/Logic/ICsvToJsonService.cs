namespace NdjsonConverter.Command.Logic;

public interface ICsvToJsonService
{
    Task ToJsonAsync(Stream readStream, Stream writeStream, CancellationToken cancellationToken);
}