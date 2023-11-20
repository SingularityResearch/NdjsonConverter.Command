using System.Text;
using Newtonsoft.Json;

namespace NdjsonConverter.Command.Logic
{
    public class JsonToNdJsonEventArgs : EventArgs
    {
        public JsonToNdJsonEventArgs(int rowCount)
        {
            RowCount = rowCount;
        }

        public int RowCount { get; set; }
    }

    public class JsonToNdJsonService : IJsonToNdJsonService
    {
        public event EventHandler<JsonToNdJsonEventArgs>? RaiseLineCompletedEvent;
        private int _rowCount;

        protected virtual void OnRaiseLineCompletedEvent(JsonToNdJsonEventArgs e)
        {
            var raiseEvent = RaiseLineCompletedEvent;
            if (raiseEvent == null) return;
            e.RowCount = _rowCount;
            raiseEvent(this, e);
        }

        public void ToNdJson(Stream readStream, Stream writeStream, CancellationToken cancellationToken)
        {
            _rowCount = 0;
            var encoding = new UTF8Encoding(false, true);
            using var textReader = new StreamReader(readStream, encoding, true, 1024, true);
            using var textWriter = new StreamWriter(writeStream, encoding, 1024, true);
            ToNdJson(textReader, textWriter, cancellationToken);
        }

        public void ToNdJson(TextReader textReader, TextWriter textWriter, CancellationToken cancellationToken)
        {
            using var jsonReader = new JsonTextReader(textReader);
            jsonReader.CloseInput = false;
            jsonReader.DateParseHandling = DateParseHandling.None;
            ToNdJson(jsonReader, textWriter, cancellationToken);
        }

        private enum State { BeforeArray, InArray, AfterArray };

        public void ToNdJson(JsonReader jsonReader, TextWriter textWriter, CancellationToken cancellationToken)
        {
            var state = State.BeforeArray;
            do
            {
                if (jsonReader.TokenType is JsonToken.Comment or JsonToken.None or JsonToken.Undefined or JsonToken.PropertyName)
                {
                }
                else if (state == State.BeforeArray && jsonReader.TokenType == JsonToken.StartArray)
                {
                    state = State.InArray;
                }
                else if (state == State.InArray && jsonReader.TokenType == JsonToken.EndArray)
                {
                    state = State.AfterArray;
                }
                else
                {
                    using (var jsonWriter = new JsonTextWriter(textWriter))
                    {
                        jsonWriter.Formatting = Formatting.None;
                        jsonWriter.CloseOutput = false;
                        jsonWriter.WriteToken(jsonReader);
                    }
                    // https://github.com/ndjson/ndjson-spec
                    textWriter.Write("\n");
                    Interlocked.Increment(ref _rowCount);
                    OnRaiseLineCompletedEvent(new JsonToNdJsonEventArgs(_rowCount));
                    if (state == State.BeforeArray)
                        state = State.AfterArray;
                }
            }
            while (jsonReader.Read() && state != State.AfterArray && !cancellationToken.IsCancellationRequested);
        }
    }
}
