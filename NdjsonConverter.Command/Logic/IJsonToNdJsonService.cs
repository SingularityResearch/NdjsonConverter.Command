namespace NdjsonConverter.Command.Logic
{
    public interface IJsonToNdJsonService
    {
        event EventHandler<JsonToNdJsonEventArgs> RaiseLineCompletedEvent;

        void ToNdJson(Stream readStream, Stream writeStream, CancellationToken cancellationToken);
    }
}
