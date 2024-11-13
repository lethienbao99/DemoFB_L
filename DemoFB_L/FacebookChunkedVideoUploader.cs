using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public class FacebookChunkedVideoUploader
{
    private readonly HttpClient _client;
    private readonly string _accessToken;
    private readonly string _pageId;

    public FacebookChunkedVideoUploader(HttpClient client, string accessToken, string pageId)
    {
        _client = client;
        _accessToken = accessToken;
        _pageId = pageId;
        _client.Timeout = TimeSpan.FromMinutes(30); // Set a long timeout for large uploads
    }

    // Method to upload large video
    public async Task<string> UploadLargeVideoAsync(string filePath, string message, string published = "true")
    {
        const int chunkSize = 2 * 1024 * 1024;
        long fileSize = new FileInfo(filePath).Length;
        string startUploadUri = $"https://graph-video.facebook.com/v21.0/{_pageId}/videos";

        // Step 1: Start the upload session
        var startUploadForm = new MultipartFormDataContent
        {
            { new StringContent(_accessToken), "access_token" },
            { new StringContent("start"), "upload_phase" },
            { new StringContent(fileSize.ToString()), "file_size" }
        };

        var startResponse = await _client.PostAsync(startUploadUri, startUploadForm);
        var startResult = await startResponse.Content.ReadAsStringAsync();
        dynamic startData = Newtonsoft.Json.JsonConvert.DeserializeObject(startResult);

        if (startData.error != null)
        {
            Console.WriteLine("Error starting upload session: " + startData.error.message);
            return null;
        }

        string uploadSessionId = startData.upload_session_id;

        // Step 2: Upload each chunk
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            byte[] buffer = new byte[chunkSize];
            int bytesRead;
            long startOffset = 0;

            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, chunkSize)) > 0)
            {
                if (!await RetryUploadChunkAsync(() => UploadChunkAsync(buffer, bytesRead, uploadSessionId, startOffset, startUploadUri)))
                {
                    Console.WriteLine("Failed to upload chunk.");
                    return null;
                }
                startOffset += bytesRead; // Increment offset for the next chunk
            }
        }

        // Step 3: Finalize the upload
        var finalizeForm = new MultipartFormDataContent
        {
            { new StringContent(_accessToken), "access_token" },
            { new StringContent("finish"), "upload_phase" },
            { new StringContent(uploadSessionId), "upload_session_id" },
            //{ new StringContent(Uri.EscapeDataString(message)), "description" }, // Add the message as video description
            { new StringContent(message), "description" }, // Add the message as video description
            { new StringContent("true"), "published" } // Ensure the video is published
        };

        var finalizeResponse = await _client.PostAsync(startUploadUri, finalizeForm);
        var finalizeResult = await finalizeResponse.Content.ReadAsStringAsync();
        dynamic finalizeData = Newtonsoft.Json.JsonConvert.DeserializeObject(finalizeResult);

        if (finalizeData.error != null)
        {
            Console.WriteLine("Error finalizing upload: " + finalizeData.error.message);
            return null;
        }

        Console.WriteLine("Video uploaded successfully!");
        return startData.video_id; // Return the uploaded video ID
    }

    // Helper method to upload a chunk
    private async Task<bool> UploadChunkAsync(byte[] buffer, int bytesRead, string uploadSessionId, long startOffset, string startUploadUri)
    {
        var chunkContent = new ByteArrayContent(buffer, 0, bytesRead);
        chunkContent.Headers.Add("Content-Type", "application/octet-stream");

        var chunkForm = new MultipartFormDataContent
        {
            { new StringContent(_accessToken), "access_token" },
            { new StringContent("transfer"), "upload_phase" },
            { new StringContent(uploadSessionId), "upload_session_id" },
            { new StringContent(startOffset.ToString()), "start_offset" },
            { chunkContent, "video_file_chunk", $"chunk{startOffset}" }
        };
        var uploadChunkResponse = await _client.PostAsync(startUploadUri, chunkForm);
        var uploadChunkResult = await uploadChunkResponse.Content.ReadAsStringAsync();
        dynamic uploadChunkData = Newtonsoft.Json.JsonConvert.DeserializeObject(uploadChunkResult);

        if (uploadChunkData.error != null)
        {
            Console.WriteLine("Error uploading chunk: " + uploadChunkData.error.message);
            return false;
        }

        return true; // Success
    }

    // Retry policy for uploading chunks
    private async Task<bool> RetryUploadChunkAsync(Func<Task<bool>> uploadFunc, int retryCount = 3)
    {
        for (int i = 0; i < retryCount; i++)
        {
            if (await uploadFunc())
            {
                return true; // Success
            }
            Console.WriteLine("Retrying upload chunk...");
        }
        return false; // Failed after retries
    }




    public async Task<bool> EditPostWithNewVideoAsync(string postId, string filePath, string message)
    {
        string videoId = await UploadLargeVideoAsync(filePath, message, "false");
        string editPostUri = $"https://graph.facebook.com/v21.0/406029269261549";
        var editForm = new MultipartFormDataContent
        {
            { new StringContent(_accessToken), "access_token" },
            { new StringContent(message), "message" },
            { new StringContent("[{\"media_fbid\":\"" + videoId + "\"}]"), "attached_media" }
        };

        var editResponse = await _client.PostAsync(editPostUri, editForm);
        var editResult = await editResponse.Content.ReadAsStringAsync();
        dynamic editData = Newtonsoft.Json.JsonConvert.DeserializeObject(editResult);

        if (editData.error != null)
        {
            Console.WriteLine("Error editing post: " + editData.error.message);
            return false;
        }

        Console.WriteLine("Post edited successfully with new video!");
        return true;
    }



    public async Task<string> GetPostIdFromVideoIdAsync(string videoId)
    {
        string videoUri = $"https://graph.facebook.com/v21.0/{videoId}?fields=permalink_url";
        var request = new HttpRequestMessage(HttpMethod.Get, videoUri);
        request.Headers.Add("Authorization", $"Bearer {_accessToken}");

        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        dynamic videoData = Newtonsoft.Json.JsonConvert.DeserializeObject(result);

        if (videoData.error != null)
        {
            Console.WriteLine("Error retrieving video data: " + videoData.error.message);
            return null;
        }

        string permalinkUrl = videoData.permalink_url;
        string postId = permalinkUrl.Split('/').Last(); // Extract post ID from URL

        Console.WriteLine("Post ID associated with video: " + postId);
        return postId;
    }

    // Method to search Page feed for the post containing the video ID
    public async Task<string> GetPostIdFromVideoIdInFeedAsync(string videoId)
    {
        string feedUri = $"https://graph.facebook.com/v21.0/{_pageId}/feed?fields=id,message,attachments&access_token={_accessToken}";
        var response = await _client.GetAsync(feedUri);
        var result = await response.Content.ReadAsStringAsync();

        Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(result);
        var data = myDeserializedClass.data.Where(s => s.attachments.data[0].target.id == videoId).FirstOrDefault().id;
        return data;
       
    }



    public class Attachments
    {
        public List<Datum> data { get; set; }
    }

    public class Cursors
    {
        public string before { get; set; }
        public string after { get; set; }
    }

    public class Datum
    {
        public string id { get; set; }
        public string message { get; set; }
        public Attachments attachments { get; set; }
        public Media media { get; set; }
        public Target target { get; set; }
        public string type { get; set; }
        public string url { get; set; }
        public string description { get; set; }
        public string title { get; set; }
        public Subattachments subattachments { get; set; }
    }

    public class Image
    {
        public int height { get; set; }
        public string src { get; set; }
        public int width { get; set; }
    }

    public class Media
    {
        public Image image { get; set; }
        public string source { get; set; }
    }

    public class Paging
    {
        public Cursors cursors { get; set; }
        public string next { get; set; }
    }

    public class Root
    {
        public List<Datum> data { get; set; }
        public Paging paging { get; set; }
    }

    public class Subattachments
    {
        public List<Datum> data { get; set; }
    }

    public class Target
    {
        public string id { get; set; }
        public string url { get; set; }
    }



}
