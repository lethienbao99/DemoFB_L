// See https://aka.ms/new-console-template for more information
using DemoFB_L;
using static DemoFB_L.FacebookService;




//Facebook
HttpClient client = new HttpClient();
string accessToken = "EAALDu3SzWIIBO5Qa8LCbn9ZAI9VJYdtfFQBzyJe2krqZCrAomaLWqvhCjAXI2mD8gw2Wdy45ZBjUTh84N6IXt7aXc2XPt4VDRN6w9m1ynkpekp9TU0qypDZBsO79g7hrEboy2dI6R3MhT0PSFZCPq99RbvZBAHNGPTOKrE3H2AXJO5ZAusEkIUV6DZBwYzMyTixpRv1MmPKfJZBRNKspqIljZC72o3fi3IlEnzZAkpZB9IQLQAZDZD";
string pageId = "406029269261549";
FacebookChunkedVideoUploader uploader = new FacebookChunkedVideoUploader(client, accessToken, pageId);

//string filePath = @"C:\Users\bao.le\Downloads\pokemon.mp4"; // Path to the video file
string filePath = @"C:\Users\bao.le\Downloads\234342.mp4"; // Path to the video file
string message = "Video này edoit";
//string videoId = await uploader.UploadLargeVideoAsync(filePath, message);
//var videoId = await uploader.EditPostWithNewVideoAsync("406029269261549_122125319102491766", filePath, message);
var videoId = await uploader.GetPostIdFromVideoIdInFeedAsync("1125798045783118");

if (videoId != null)
{
    Console.WriteLine("Uploaded Video ID: " + videoId);
}
else
{
    Console.WriteLine("Video upload failed.");
}



//Linkedin

/*string accessToken = "AQUeRgYShfq_UmU0x-48Qb3ck-FHxHrlT0ynhUQf42fsdpJRTgpbMclKVbWvwAk8M3rDwqnDi0JKHn7-kfIzk36ff7M5BVpIrX1gEXDq3NL9rnxH3QoDvyCkUq9Y7Y5RMZEEnS0n7sqRx93Bf2sBSzH5Yawe2r2GYmiKslq4QNKuB5x6YsxFRer-LO0GM4g6teYTMUUJvghF_9FMvKeUvn6_fRTpZ5BDJM-QruWABoWz7YpeGiBGA6zkFYH1EOmDnJaoL3m9LxNQBGcWcVyya--uEKW3OEC3pDKllYgmonezlWsTA6e1CULKctBE4p6P45bR5aLnLw77CgObTRKedi83x_Xfxw"; // Your LinkedIn access token
string organizationId = "104456411"; // Your LinkedIn organization ID
string filePath = @"C:\Users\bao.le\Downloads\duoi10o.mp4"; // Path to the video file

var uploader = new LinkedInCompanyVideoUploader(accessToken, organizationId);
//await uploader.UploadVideoToLinkedInCompanyPage(filePath);


string videoFilePath = @"C:\Users\bao.le\Downloads\duoi10o.mp4"; // Update the path to your video file
var uploadResponse = await uploader.CreateUploadVideoSessionAsync2(filePath);

if (uploadResponse != null)
{
    var uploadInstructions = uploadResponse.value.uploadInstructions;
    await uploader.UploadVideoChunks(videoFilePath, uploadResponse);
}*/


Console.WriteLine("");
Console.WriteLine("");
Console.WriteLine("");
Console.WriteLine("");
Console.WriteLine("");