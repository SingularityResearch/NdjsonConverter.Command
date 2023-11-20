using System.Globalization;
using System.Text;
using CsvHelper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace NdjsonConverter.Command.Logic
{
    public class CsvToJsonService : ICsvToJsonService
    {
        private readonly ILogger<CsvToJsonService> _logger;

        public CsvToJsonService(ILogger<CsvToJsonService> logger)
        {
            _logger = logger;
        }

        public async Task ToJsonAsync(Stream readStream, Stream writeStream, CancellationToken cancellationToken)
        {
            try
            {
                var encoding = new UTF8Encoding(false, true);
                using (var reader = new StreamReader(readStream, encoding, true, 1024, true))
                using (var writer = new StreamWriter(writeStream, encoding, 1024, true))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    await csv.ReadAsync();
                    csv.ReadHeader();
                    while (await csv.ReadAsync() && !cancellationToken.IsCancellationRequested)
                    {
                        var record = new Dictionary<string, string>();
                        foreach (var header in csv.HeaderRecord!)
                        {
                            record[header] = csv.GetField(header)!;
                        }
                        var jsonRecord = JsonConvert.SerializeObject(record, Formatting.None);
                        await writer.WriteLineAsync(jsonRecord);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{Message}", ex.Message);
                throw;
            }
        }
    }
}
