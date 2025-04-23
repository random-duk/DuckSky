using dotenv.net;
using idunno.Bluesky;
using RandomDukNET;

DotEnv.Load(options: new DotEnvOptions(probeForEnv: true, envFilePaths: [".env"]));

var username = Environment.GetEnvironmentVariable("BSKY_USERNAME") ?? string.Empty;
var password = Environment.GetEnvironmentVariable("BSKY_PASSWORD") ?? string.Empty;
var pds = new Uri(Environment.GetEnvironmentVariable("BSKY_PDS") ?? "https://bsky.social");

// Credits to SoNearSonar for the RandomDukNET library
var dukManager = new RandomDukManager();

BlueskyAgent agent = new ();

var loginResult = await agent.Login(username, password, service: pds );

if (loginResult.Succeeded)
{
    var duckPic = await dukManager.GetRandomImage();   
    var imageUploadResult = await agent.UploadImage(duckPic, "image/jpg", "A picture of a duck", null);
    if (imageUploadResult.Succeeded)
    {
        var post = await agent.Post("\u200b", imageUploadResult.Result, null, null);
    }
}