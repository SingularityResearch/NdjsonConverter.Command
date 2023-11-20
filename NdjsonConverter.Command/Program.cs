using System.Diagnostics;
using Amazon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NdjsonConverter.Command.Logic;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, cfg) =>
    {
        cfg.SetBasePath(Directory.GetCurrentDirectory());
        cfg.AddCommandLine(args);
    })
    .ConfigureServices((_, services) =>
    {
        services.AddLogging();
        services.AddSingleton<IJsonToNdJsonService, JsonToNdJsonService>();
        services.AddSingleton<ICsvToJsonService, CsvToJsonService>();
        services.AddSingleton<IGzipService, GzipService>();
        services.AddSingleton<IAmazonS3Service, AmazonS3Service>();
    })
    .ConfigureLogging((_, logging) =>
    {
        logging.ClearProviders();
        logging.AddSimpleConsole(options => options.IncludeScopes = true);
    }).Build();

var config = host.Services.GetRequiredService<IConfiguration>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
// This is not needed since the CSV to JSON is one row per JSON object, its already NDJSON / JSON Lines.
//var ndJsonService = host.Services.GetRequiredService<IJsonToNdJsonService>();
var jsonService = host.Services.GetRequiredService<ICsvToJsonService>();
var gzipService = host.Services.GetRequiredService<IGzipService>();
var amazonS3Service = host.Services.GetRequiredService<IAmazonS3Service>();

try
{
    var inRegionArg = config.GetValue<string>("inregion");
    var inBucketArg = config.GetValue<string>("inbucket");
    var inKeyArg = config.GetValue<string>("inkey");
    var infileArg = config.GetValue<string>("infile");
    var outfileArg = config.GetValue<string>("outfile");
    var outRegionArg = config.GetValue<string>("outregion");
    var outBucketArg = config.GetValue<string>("outbucket");
    var outKeyArg = config.GetValue<string>("outkey");
    if (string.IsNullOrEmpty(inBucketArg) || string.IsNullOrEmpty(inKeyArg) || string.IsNullOrEmpty(infileArg) 
        || string.IsNullOrEmpty(outfileArg) || string.IsNullOrEmpty(outBucketArg) || string.IsNullOrEmpty(outKeyArg)
        || string.IsNullOrEmpty(inRegionArg) || string.IsNullOrEmpty(outRegionArg))
    {
        Help();
        return;
    }

    Console.WriteLine($"inregion: {inRegionArg}");
    Console.WriteLine($"inbucket: {inBucketArg}");
    Console.WriteLine($"inkey: {inKeyArg}");
    Console.WriteLine($"infile: {infileArg}");
    Console.WriteLine($"outfile: {outfileArg}");
    Console.WriteLine($"outregion: {outRegionArg}");
    Console.WriteLine($"outbucket: {outBucketArg}");
    Console.WriteLine($"outkey: {outKeyArg}");

    var cts = new CancellationTokenSource();
    var stopWatch = new Stopwatch();
    var t = Task.Run(async () =>
    {
        try
        {
            Console.WriteLine("Application has started. Ctrl-C to cancel");
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                Console.WriteLine("Cancel event triggered");
                cts.Cancel();
                eventArgs.Cancel = true;
            };
            stopWatch.Start();
            Console.WriteLine($"Downloading {inBucketArg}/{inKeyArg}");
            var inRegion = RegionEndpoint.GetBySystemName(inRegionArg);
            var outRegion = RegionEndpoint.GetBySystemName(outRegionArg);
            var success = await amazonS3Service.DownloadAsync(inRegion, inBucketArg, inKeyArg, infileArg, cts.Token);
            if (success)
            {
                Console.WriteLine($"Download {inBucketArg}/{inKeyArg} successful");
                var csvOutfile = Path.ChangeExtension(infileArg, ".csv");
                using var readStream = File.OpenRead(infileArg);
                using var writeStream = File.Open(csvOutfile, FileMode.Create);
                Console.WriteLine($"Decompressing {infileArg}");
                await gzipService.DecompressFileAsync(readStream, writeStream, cts.Token);
                readStream.Close();
                writeStream.Close();
                Console.WriteLine($"Decompressed to {csvOutfile} successfully");
                using var csvReadStream = File.OpenRead(csvOutfile);
                using var jsonWriteStream = File.Open(outfileArg, FileMode.Create);
                Console.WriteLine($"Converting CSV to JSON for {csvOutfile}");
                await jsonService.ToJsonAsync(csvReadStream, jsonWriteStream, cts.Token);
                csvReadStream.Close();
                jsonWriteStream.Close();
                Console.WriteLine($"Converted to {outfileArg} successfully");
                File.Delete(csvOutfile);
                var gzipOutfile = Path.ChangeExtension(outfileArg, ".gz");
                Console.WriteLine($"Compressing {outfileArg}");
                using var jsonReadStream = File.OpenRead(outfileArg);
                using var gzipWriteStream = File.Open(gzipOutfile, FileMode.Create);
                await gzipService.CompressFileAsync(jsonReadStream, gzipWriteStream, cts.Token);
                jsonReadStream.Close();
                gzipWriteStream.Close();
                Console.WriteLine($"Compressed to {gzipOutfile} successfully");
                File.Delete(outfileArg);
                Console.WriteLine($"Uploading {outBucketArg}/{outKeyArg}");
                await amazonS3Service.UploadAsync(outRegion, outBucketArg, outKeyArg, gzipOutfile, cts.Token);
                Console.WriteLine($"Upload {outBucketArg}/{outKeyArg} successful");
                File.Delete(gzipOutfile);
            }
            stopWatch.Stop();
        }
        catch (Exception ex)
        {
            logger.LogError("{Message}", ex.Message);
            throw;
        }
    });
    await t.WaitAsync(cts.Token);
    Console.WriteLine($"Completed in {stopWatch.Elapsed.TotalSeconds} seconds");
}
catch (Exception ex)
{
    logger.LogError("{Message}", ex.Message);
    logger.LogCritical("Program terminated unexpectedly!");
}
void Help()
{
    Console.WriteLine($"{Environment.NewLine}Usage: {Process.GetCurrentProcess().ProcessName} [options]");
    Console.WriteLine($"{Environment.NewLine}Required Arguments: {Environment.NewLine}    --infile    The input GZ file.");
    Console.WriteLine($"{Environment.NewLine}    --outfile    The output NDJSON file.");
    Console.WriteLine($"{Environment.NewLine}    --inregion   The input Amazon S3 Region.");
    Console.WriteLine($"{Environment.NewLine}    --inbucket   The input Amazon S3 Bucket.");
    Console.WriteLine($"{Environment.NewLine}    --inkey      The input file key in Amazon S3.");
    Console.WriteLine($"{Environment.NewLine}    --outregion  The output Amazon S3 Region.");
    Console.WriteLine($"{Environment.NewLine}    --outbucket  The output Amazon S3 Bucket.");
    Console.WriteLine($"{Environment.NewLine}    --outkey     The output file key in Amazon S3.");
    Console.WriteLine();
    Console.WriteLine($"{Environment.NewLine}Examples:");
    Console.WriteLine($"    ./{Process.GetCurrentProcess().ProcessName} --infile C:\\NDJSON\\Input\\test.gz --outfile C:\\NDJSON\\Output\\test.ndjson --inregion us-east-1 --inbucket singularityresearch --inkey dev/test.gz --outregion us-east-1 --outbucket singularityresearch --outkey dev/test.ndjson.gz");
    Console.WriteLine();
    Console.WriteLine($"{Environment.NewLine}Explanation:{Environment.NewLine}The GZ compressed file is downloaded from Amazon S3 (*inbucket*/*inkey*).");
    Console.WriteLine("The downloaded file is decompressed to the local filesystem as *infile* (CSV file.)");
    Console.WriteLine("The *infile* is converted from CSV to NDJSON to the local filesystem as *outfile* (NDJSON file.)");
    Console.WriteLine("The *outfile* is GZ compressed to the local filesystem as *outfile*.gz.");
    Console.WriteLine("The *outfile*.gz is uploaded to Amazon S3 as *outbucket*/*outkey*.");
    Console.WriteLine($"{Environment.NewLine}Developed By Singularity Research: https://singularityresearch.azurewebsites.net");
}