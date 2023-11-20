# NdjsonConverter.Command
Command line utility for the following workflow:

AWS S3 Bucket > Download gzip file(s) locally > expand .gz file content to reveal source CSV file > convert to NDJSON > gzip resultant NDJSON file > upload back to AWS S3 bucket

 
Usage: NdjsonConverter.Command [options]

Options:

    --infile    The input GZ file.

    --outfile    The output NDJSON file.

    --inbucket   The input Amazon S3 Bucket.

    --inkey      The input file key in Amazon S3.

    --outbucket  The output Amazon S3 Bucket.

    --outkey     The output file key in Amazon S3.


Examples:

    NdjsonConverter.Command.exe --infile C:\NDJSON\Input\test.gz --outfile C:\NDJSON\Output\test.ndjson --inregion us-east-1 --inbucket singularityresearch --inkey dev/test.gz --outregion us-east-1 --outbucket singularityresearch --outkey dev/test.ndjson.gz

Explanation:
The GZ compressed file is downloaded from Amazon S3 (*inbucket*/*inkey*).
The downloaded file is decompressed to the local filesystem as *infile* (CSV file.)
The *infile* is converted from CSV to NDJSON to the local filesystem as *outfile* (NDJSON file.)
The *outfile* is GZ compressed to the local filesystem as *outfile*.gz.
The *outfile*.gz is uploaded to Amazon S3 as *outbucket*/*outkey*.

Build Requirements:

Visual Studio 2022 (version 17.8.0 or above)
.NET 8 SDK
