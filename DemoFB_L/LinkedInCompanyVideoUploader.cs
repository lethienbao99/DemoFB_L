using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using DemoFB_L;
using Newtonsoft.Json;

public class LinkedInCompanyVideoUploader
{
    private readonly string _accessToken;
    private readonly string _organizationId; // Your LinkedIn organization ID

    public LinkedInCompanyVideoUploader(string accessToken, string organizationId)
    {
        _accessToken = accessToken;
        _organizationId = organizationId;
    }

    public async Task<Root> CreateUploadVideoSessionAsync2(string videoFilePath)
    {
        long fileSize = new FileInfo(videoFilePath).Length;

        //var requestUrl = "https://api.linkedin.com/v2/assets?action=registerUpload";
        var requestUrl = "https://api.linkedin.com/rest/videos?action=initializeUpload";

        var requestBody = new
        {
            initializeUploadRequest = new
            {
                owner = $"urn:li:organization:{_organizationId}",
                fileSizeBytes = fileSize,
                uploadCaptions = false,
                uploadThumbnail = false
            }
        };

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            client.DefaultRequestHeaders.Add("LinkedIn-Version", "202410");
            client.DefaultRequestHeaders.Add("X-RestLi-Protocol-Version", "2.0.0");

            var response = await client.PostAsync(requestUrl, new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"));
            var responseContent = await response.Content.ReadAsStringAsync();
            Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(responseContent);
            if (response.IsSuccessStatusCode)
            {
                return myDeserializedClass;
            }
            else
            {
                return null;
            }
        }
    }

    public class Root
    {
        public Value value { get; set; }
    }

    public class UploadInstruction
    {
        public string uploadUrl { get; set; }
        public int lastByte { get; set; }
        public int firstByte { get; set; }
    }

    public class Value
    {
        public long uploadUrlsExpireAt { get; set; }
        public string video { get; set; }
        public List<UploadInstruction> uploadInstructions { get; set; }
        public string uploadToken { get; set; }
        public string thumbnailUploadUrl { get; set; }
    }


    public async Task<string> UploadVideoChunks(string videoFilePath, Root uploadInstructions)
    {
        byte[] videoBytes = File.ReadAllBytes(videoFilePath);
        long totalBytes = videoBytes.Length;
        string videoURN = null;
        var listETags = new List<string>();
        foreach (var instruction in uploadInstructions.value.uploadInstructions)
        {
            string uploadUrl = instruction.uploadUrl;
            int firstByte = (int)instruction.firstByte;
            int lastByte = (int)instruction.lastByte + 1;

            // Calculate the chunk size
            var chunkSize = lastByte - firstByte;
            var videoChunk = new byte[chunkSize];
            Array.Copy(videoBytes, firstByte, videoChunk, 0, chunkSize);

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                var content = new ByteArrayContent(videoChunk);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                var uploadResponse = await httpClient.PutAsync(uploadUrl, content);
                uploadResponse.EnsureSuccessStatusCode();

                listETags.Add(uploadResponse.Headers.NonValidated.FirstOrDefault(s => s.Key == "ETag").Value.ToString());
                Console.WriteLine($"Uploaded bytes {firstByte} to {lastByte - 1}");
            }
        }

        // Assuming the upload response contains the URN
        videoURN = uploadInstructions.value.video; // Adjust based on the actual response structure
        var fins = FinalizeVideoUpload(videoURN, "", listETags).Result;

        await PostVideoAsync(videoURN.Split(":")[3]);

        Console.WriteLine("Video upload complete!");
        return videoURN;
    }

    private async Task<HttpResponseMessage> FinalizeVideoUpload(string videoURN, string uploadToken, List<string> uploadedPartId)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.linkedin.com/rest/videos?action=finalizeUpload");
        request.Headers.Add("LinkedIn-Version", "202410");
        request.Headers.Add("X-RestLi-Protocol-Version", "2.0.0");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken); // Replace with your actual token
        request.Headers.Add("Cookie", "YOUR_COOKIES"); // Replace with your actual cookies

        var body = new
        {
            finalizeUploadRequest = new
            {
                video = videoURN,
                uploadToken = uploadToken,
                uploadedPartIds = uploadedPartId.ToArray()
            }
        };

        // Convert the object to JSON
        var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        request.Content = jsonContent;

        // Send the request
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return response;
    }



    public async Task<string> CreateUploadVideoSessionAsync(string videoFilePath)
    {
        var fileInfo = new FileInfo(videoFilePath);
        long fileSize = fileInfo.Length;
        //var requestUrl = "https://api.linkedin.com/v2/assets?action=registerUpload";
        var requestUrl = "https://api.linkedin.com/rest/videos?action=initializeUpload";

        var requestBody = new
        {
            /*registerUploadRequest = new
            {
                owner = $"urn:li:organization:{_organizationId}",
                recipes = new[] { "urn:li:digitalmediaRecipe:feedshare-video" },
                serviceRelationships = new[]
                {
                    new { relationshipType = "OWNER", identifier = $"urn:li:userGeneratedContent" }
                }
            }*/

            initializeUploadRequest = new
            {
                owner = $"urn:li:organization:{_organizationId}",
                fileSizeBytes = fileSize,
                uploadCaptions = false,
                uploadThumbnail = true
            }
        };

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            client.DefaultRequestHeaders.Add("LinkedIn-Version", "202410");
            client.DefaultRequestHeaders.Add("X-RestLi-Protocol-Version", "2.0.0");

            var response = await client.PostAsync(requestUrl, new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"));
            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(responseContent);

            if (response.IsSuccessStatusCode)
            {
                return result.value.uploadUrl; // Save this upload URL for the next step
            }
            else
            {
                throw new Exception($"Error creating upload session: {result.message}");
            }
        }
    }

    public async Task UploadVideoInChunks(string uploadUrl, string videoFilePath, int chunkSize = 4 * 1024 * 1024) // 5 MB
    {
        var fileInfo = new FileInfo(videoFilePath);
        long fileSize = fileInfo.Length;
        int totalChunks = (int)Math.Ceiling((double)fileSize / chunkSize);

        using (var fileStream = fileInfo.OpenRead())
        {
            for (int i = 0; i < totalChunks; i++)
            {
                byte[] buffer = new byte[chunkSize];
                int bytesRead = await fileStream.ReadAsync(buffer, 0, chunkSize);
                var content = new ByteArrayContent(buffer.Take(bytesRead).ToArray());

                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                using (var client = new HttpClient())
                {
                    // Set Content-Range header for chunked upload
                    var start = i * chunkSize;
                    var end = start + bytesRead - 1;
                    content.Headers.ContentRange = new ContentRangeHeaderValue(start, end, fileSize);

                    var response = await client.PutAsync(uploadUrl, content);
                    response.EnsureSuccessStatusCode();
                }
            }
        }
    }


    public async Task UploadVideoAsync(string uploadUrl, string filePath)
    {
        string accessToken = "AQWw4L5UO1bvP45XDWFtbL2gxoZGCYdPR2i-fCBx9qN9QR06dqsVKE5oY9beOgTpL-ISw-HRkwtzTbQ1aKicAfAK0BBceTCUnCDG18kTv2DKkSjqPPAW2QqP0S0EeTYZ7KbVVOPd_qoMq-BQ0rk2Ga0E-4Iw3-1EwqVDPDc0HA3sYYxiQKdtOs2Nau2b9QCX8WfyLet2smaBqEkPoiaDJQHg0h4HemLS3CHW7HYF86SUNMsNhzQK3uWNwNbIh-KILODbrJx1mrsqwbaU7jDuftbGazY_-Y-5pz39jwPb00rxUsbgE0mDMJVPhIwhAa_KepF79sOefIBbAAJIivm087JY8T82jA"; // Your LinkedIn access token

        const int chunkSize = 5 * 1024 * 1024; // 5 MB
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            byte[] buffer = new byte[chunkSize];
            int bytesRead;
            long offset = 0;

            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, chunkSize)) > 0)
            {
                using (var content = new ByteArrayContent(buffer, 0, bytesRead))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    var client = new HttpClient();
                    var request = new HttpRequestMessage(HttpMethod.Put, $"{uploadUrl}?action=upload&offset={offset}");
                    request.Headers.Add("Authorization", $"Bearer {accessToken}");
                    request.Content = content;
                    var response = await client.SendAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("Error uploading video chunk: " + await response.Content.ReadAsStringAsync());
                    }

                    offset += bytesRead; // Increment offset for the next chunk
                }
            }
        }
    }

    public async Task PostVideoAsync(string videoURN)
    {
        var postUrl = "https://api.linkedin.com/v2/ugcPosts";
        //Step 3: Post content
        var clientPost = new HttpClient();
        var requestPost = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "https://api.linkedin.com/v2/ugcPosts");
        requestPost.Headers.Add("X-Restli-Protocol-Version", "2.0.0");
        requestPost.Headers.Add("Authorization", $"Bearer {_accessToken}");

        var mediaObject = new List<Media>();
        string ShareMediaCategory = "VIDEO";
        mediaObject.Add(new Media()
        {
            Status = "READY",
            Description = new Description { Text = "Center stage!" },
            MediaUri = $"urn:li:digitalmediaAsset:{videoURN}",
            Title = new Title { Text = "hi la" }
        });
        var shareRequest = new ShareRequest
        {
            Author = $"urn:li:organization:{_organizationId}",
            LifecycleState = "PUBLISHED",
            SpecificContent = new SpecificContent
            {
                ShareContent = new ShareContent
                {
                    ShareCommentary = new ShareCommentary
                    {
                        Text = "test"
                    },
                    ShareMediaCategory = ShareMediaCategory,
                    Media = mediaObject
                }
            },
            Visibility = new Visibility
            {
                MemberNetworkVisibility = "PUBLIC"
            }
        };
        // Serialize the request object to JSON
        var jsonContent = JsonConvert.SerializeObject(shareRequest);
        // Set the content for the HTTP request
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        requestPost.Content = content; // Attach the content to the request
        var responsePost = clientPost.SendAsync(requestPost).Result;
        var responsePostString = responsePost.Content.ReadAsStringAsync().Result;

    }



    public async Task UploadVideoToLinkedInCompanyPage(string filePath)
    {
        try
        {
            // Step 1: Create upload session
            string uploadUrl = await CreateUploadVideoSessionAsync(filePath);

            // Step 2: Upload video
            await UploadVideoInChunks(uploadUrl, filePath);

            // Step 3: Post video
            string videoURN = "urn:li:digitalmediaAsset:{your_asset_id}"; // Replace with the actual asset ID from the upload response
            await PostVideoAsync(videoURN);

            Console.WriteLine("Video uploaded and posted successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
