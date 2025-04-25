using dotenv.net;
using FFmpeg.NET;
using idunno.Bluesky;
using idunno.Bluesky.Embed;
using RandomDukNET;

DotEnv.Load(options: new DotEnvOptions(probeForEnv: true, envFilePaths: [".env"]));

var username = Environment.GetEnvironmentVariable("BSKY_USERNAME") ?? string.Empty;
var password = Environment.GetEnvironmentVariable("BSKY_PASSWORD") ?? string.Empty;
var pds = new Uri(Environment.GetEnvironmentVariable("BSKY_PDS") ?? "https://bsky.social");

// Credits to SoNearSonar for the RandomDukNET library
var dukManager = new RandomDukManager();

BlueskyAgent agent = new();

var loginResult = await agent.Login(username, password, service: pds);

if (loginResult.Succeeded)
{
    var duckFound = await dukManager.GetQuack();
    byte[]? duckPic = null;
    var duckId = Convert.ToInt64(duckFound.Url?.Replace(".gif", "").Replace("https://random-d.uk/api/", "")
        .Replace(".jpg", ""));
    if (duckFound.Url != null && duckFound.Url.EndsWith(".jpg"))
    {
        duckPic = await dukManager.GetDuckImageJpegById(duckId);
        var imageUploadResult = await agent.UploadImage(duckPic, "image/jpg", "A picture of a duck", null);
        if (imageUploadResult.Succeeded)
        {
            var res = await agent.Post("\u200b", imageUploadResult.Result, null, null);
        }
    }
    else if (duckFound.Url != null && duckFound.Url.EndsWith(".gif"))
    {
        duckPic = await dukManager.GetDuckImageGifById(duckId);
        // Create temporary files for conversion
        var tempGifPath = Path.Combine(Path.GetTempPath(), $"duck_{duckId}.gif");
        var tempWebmPath = Path.Combine(Path.GetTempPath(), $"duck_{duckId}.webm");

        try
        {
            // Save GIF to temporary file
            await File.WriteAllBytesAsync(tempGifPath, duckPic);

            // Set up FFmpeg conversion
            var ffmpeg = new Engine("ffmpeg");
            var inputFile = new InputFile(tempGifPath);
            var outputFile = new OutputFile(tempWebmPath);

            // Configure conversion options
            var conversionOptions = new ConversionOptions
            {
            };

            // Execute conversion
            await ffmpeg.ConvertAsync(inputFile, outputFile, conversionOptions, CancellationToken.None);

            // Read the converted WEBM file
            var webmBytes = await File.ReadAllBytesAsync(tempWebmPath);

            Console.WriteLine(webmBytes);

            // Upload the WEBM
            var videoUploadResult = await agent.UploadVideo("duck.webm", webmBytes);
            videoUploadResult.EnsureSucceeded();
            while (videoUploadResult.Succeeded &&
                   (videoUploadResult.Result.State == idunno.Bluesky.Video.JobState.Created ||
                    videoUploadResult.Result.State == idunno.Bluesky.Video.JobState.InProgress))
            {
                // Give the user some feedback
                Console.WriteLine(
                    $"Video job # {videoUploadResult.Result.JobId} processing, progress {videoUploadResult.Result.Progress}");

                await Task.Delay(1000);
                videoUploadResult = await agent.GetVideoJobStatus(videoUploadResult.Result.JobId);
                videoUploadResult.EnsureSucceeded();
            }

            if (videoUploadResult is { Succeeded: true })
            {
                EmbeddedVideo video = new(videoUploadResult.Result.Blob!, altText: "Alt Text");
                var res = await agent.Post("\u200b", video);
            }
        }
        finally
        {
            // Clean up temporary files
            if (File.Exists(tempGifPath))
                File.Delete(tempGifPath);
            if (File.Exists(tempWebmPath))
                File.Delete(tempWebmPath);
        }
    }
}