using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace DemoFB_L
{
    public class FacebookService
    {
        public string GetAccessToken(string UserId)
        {
            return "EAALDu3SzWIIBOwdnCcMRxbNwmaMxS9rh5nDZBpqcDWdASpdqGhoboO4Mh7o2keYpLmxcHUglR3igNXfy9acgSKAXTOYohZAZBRfwQchHw5qgrD4ERImUjwjU4TYQsTYYjagaGMx8oqZCLAS0IiZCa5Wic4q3e9pZBQrStSJus7eP4Soh6mUhinc0QVrRzQ99gPVvZAZARoQOUKjGQAjIawZDZD";
        }


        public string ExtendAccessToken(string client_id, string client_secret, string fb_exchange_token)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, 
                $"https://graph.facebook.com/v20.0/oauth/access_token?grant_type=fb_exchange_token&client_id={client_id}&client_secret={client_secret}&fb_exchange_token={fb_exchange_token}");
            var response = client.SendAsync(request).Result;
            var responseString = response.Content.ReadAsStringAsync().Result;
            //Success
            if (response.IsSuccessStatusCode)
            {
                response.EnsureSuccessStatusCode();
                ExtendAccessToken result = JsonConvert.DeserializeObject<ExtendAccessToken>(responseString);
                if (result != null)
                {
                    return result.access_token;
                }
            }
            else
            {
                ErrorResponse rrrorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseString);
                if (rrrorResponse != null)
                {
                    //OAth fail
                    if (rrrorResponse.error?.code == 190)
                    {
                        //Handler...
                    }
                }
                return "Fail";
            }
            return "Fail";
        }

        //2508826562634543
        public List<PageData> GetPagesByUser(string UserId, string UserPageId)
        {
            var accessToken = GetAccessToken(UserId);

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://graph.facebook.com/{UserId}/accounts?access_token={accessToken}");
            var response = client.SendAsync(request).Result;

            var responseString = response.Content.ReadAsStringAsync().Result;

            //Success
            if (response.IsSuccessStatusCode)
            {
                response.EnsureSuccessStatusCode();
                PagesByUser result = JsonConvert.DeserializeObject<PagesByUser>(responseString);
                if(result != null)
                {
                    if (!string.IsNullOrEmpty(UserPageId))
                        result.data = result.data.Where(s => s.id == UserPageId).ToList();
                    return result.data;

                }
                return new List<PageData>();
            }
            else
            {
                ErrorResponse rrrorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseString);
                if(rrrorResponse != null)
                {
                    //OAth fail
                    if(rrrorResponse.error?.code == 190)
                    {
                        //Handler...
                    }
                }
                return new List<PageData>();
            }

        }

        public class VideoUploadModel
        {
            public string FilePath { get; set; }
            public string Description { get; set; }
        }


        public int GetChunkSize(string filePath)
        {
            const int MaxChunkSize = 4 * 1024 * 1024; // 4 MB
            long fileSize = new FileInfo(filePath).Length;

            // If the file is smaller than the max chunk size, set chunk size to the file size
            int chunkSize = (fileSize < MaxChunkSize) ? (int)fileSize : MaxChunkSize;

            return chunkSize;
        }


        public async Task UploadVideoAsync(string filePath, string PageId, string AccessToken)
        {

            HttpClient client = new HttpClient();
            int ChunkSize = GetChunkSize(filePath);

            // Step 1: Start the upload session
            string uploadSessionId;
            long fileSize = new FileInfo(filePath).Length;

            var startContent = new MultipartFormDataContent
            {
                { new StringContent(AccessToken), "access_token" },
                { new StringContent("start"), "upload_phase" },
                { new StringContent(fileSize.ToString()), "file_size" }
            };

            var startResponse = await client.PostAsync($"https://graph-video.facebook.com/v21.0/{PageId}/videos", startContent);
            startResponse.EnsureSuccessStatusCode();
            var startResponseBody = await startResponse.Content.ReadAsStringAsync();

            var startJson = JsonDocument.Parse(startResponseBody);
            uploadSessionId = startJson.RootElement.GetProperty("upload_session_id").GetString();

            Console.WriteLine("Upload session started. ID: " + uploadSessionId);

            // Step 2: Upload the video in chunks
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            string startOffset = "0";
            string endOffset;

            while (true)
            {
                // Prepare the chunk data
                var chunkData = new byte[ChunkSize];
                int bytesRead = await fileStream.ReadAsync(chunkData, 0, ChunkSize);

                if (bytesRead == 0) break; // No more data to read

                var chunkContent = new MultipartFormDataContent
            {
                { new StringContent(AccessToken), "access_token" },
                { new StringContent("transfer"), "upload_phase" },
                { new StringContent(uploadSessionId), "upload_session_id" },
                { new ByteArrayContent(chunkData, 0, bytesRead), "video_file_chunk" },
                { new StringContent(startOffset), "start_offset" }
            };

                var chunkResponse = await client.PostAsync($"https://graph-video.facebook.com/v21.0/{PageId}/videos", chunkContent);
                chunkResponse.EnsureSuccessStatusCode();
                var chunkResponseBody = await chunkResponse.Content.ReadAsStringAsync();

                var chunkJson = JsonDocument.Parse(chunkResponseBody);
                startOffset = chunkJson.RootElement.GetProperty("start_offset").GetString();
                endOffset = chunkJson.RootElement.GetProperty("end_offset").GetString();

                Console.WriteLine($"Uploaded chunk from offset {startOffset} to {endOffset}");

                if (startOffset == endOffset) break; // All chunks uploaded
            }

            // Step 3: Complete the upload
            var finishContent = new MultipartFormDataContent
        {
            { new StringContent(AccessToken), "access_token" },
            { new StringContent("finish"), "upload_phase" },
            { new StringContent(uploadSessionId), "upload_session_id" }
        };

            var finishResponse = await client.PostAsync($"https://graph-video.facebook.com/v21.0/{PageId}/videos", finishContent);
            finishResponse.EnsureSuccessStatusCode();
            var finishResponseBody = await finishResponse.Content.ReadAsStringAsync();

            Console.WriteLine("Video upload completed. Response: " + finishResponseBody);
        }

        public async Task UploadVideoAsync2(string filePath, string _pageId, string _accessToken)
        {

            var _client = new HttpClient();

            const int chunkSize = 4 * 1024 * 1024; // 4 MB chunks
            long fileSize = new FileInfo(filePath).Length;
            string startUploadUri = $"https://graph-video.facebook.com/v21.0/{_pageId}/videos";

            // Start the upload session
            var startResponse = await _client.PostAsync($"{startUploadUri}?access_token={_accessToken}&upload_phase=start&file_size={fileSize}", null);
            var startResult = await startResponse.Content.ReadAsStringAsync();
            dynamic startData = Newtonsoft.Json.JsonConvert.DeserializeObject(startResult);

            if (startData.error != null)
            {
                Console.WriteLine("Error starting upload session: " + startData.error.message);
                return;
            }

            string uploadSessionId = startData.upload_session_id;

            // Upload each chunk
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int chunkIndex = 0;
                byte[] buffer = new byte[chunkSize];
                int bytesRead;

                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, chunkSize)) > 0)
                {
                    var chunkContent = new ByteArrayContent(buffer, 0, bytesRead);
                    chunkContent.Headers.Add("Content-Type", "application/octet-stream");

                    var form = new MultipartFormDataContent
                {
                    { new StringContent(_accessToken), "access_token" },
                    { new StringContent("transfer"), "upload_phase" },
                    { new StringContent(uploadSessionId), "upload_session_id" },
                    { new StringContent(chunkIndex.ToString()), "start_offset" },
                    { chunkContent, "video_file_chunk", $"chunk{chunkIndex}" }
                };

                    var uploadResponse = await _client.PostAsync(startUploadUri, form);
                    var uploadResult = await uploadResponse.Content.ReadAsStringAsync();
                    dynamic uploadData = Newtonsoft.Json.JsonConvert.DeserializeObject(uploadResult);

                    if (uploadData.error != null)
                    {
                        Console.WriteLine("Error uploading chunk: " + uploadData.error.message);
                        return;
                    }

                    chunkIndex++;
                }
            }

            // Finalize the upload
            var finalizeResponse = await _client.PostAsync($"{startUploadUri}?access_token={_accessToken}&upload_phase=finish&upload_session_id={uploadSessionId}", null);
            var finalizeResult = await finalizeResponse.Content.ReadAsStringAsync();
            dynamic finalizeData = Newtonsoft.Json.JsonConvert.DeserializeObject(finalizeResult);

            if (finalizeData.error != null)
            {
                Console.WriteLine("Error finalizing upload: " + finalizeData.error.message);
                return;
            }

            Console.WriteLine("Video uploaded successfully!");
        }


        private async Task<string> UploadSingleVideoAsync(VideoUploadModel video, string _pageId, string _accessToken)
        {
            await UploadVideoAsync2(video.FilePath, _pageId, _accessToken);
            return "";
        }

        private string ParseVideoId(string responseContent)
        {
            // Extract video ID from responseContent. Adjust this as needed based on actual response.
            // Assuming JSON response contains "id" field for simplicity.
            var startIndex = responseContent.IndexOf("\"id\":\"") + 6;
            var endIndex = responseContent.IndexOf("\"", startIndex);
            return responseContent.Substring(startIndex, endIndex - startIndex);
        }



        public string PostContent(string ContentPost, List<string> URLImages, string accessTokenPage, List<VideoUploadModel> videos)
        {
            var listIdImage = new List<string>();

         /*   var videoIds = new List<string>();
            foreach (var video in videos)
            {
                var videoId = UploadSingleVideoAsync(video, "415199451678276", accessTokenPage).Result;
                if (!string.IsNullOrEmpty(videoId))
                {
                    videoIds.Add(videoId);
                }
            }
*/

            foreach (var image in URLImages)
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://graph.facebook.com/me/photos");
                var collection = new List<KeyValuePair<string, string>>();
                collection.Add(new("url", image));
                collection.Add(new("published", "false"));
                collection.Add(new("access_token", accessTokenPage));
                var content = new FormUrlEncodedContent(collection);
                request.Content = content;
                var response = client.SendAsync(request).Result;
                var responseString = response.Content.ReadAsStringAsync().Result;
                //Success
                if (response.IsSuccessStatusCode)
                {
                    response.EnsureSuccessStatusCode();
                    ResponseSuccessUploadImage result = JsonConvert.DeserializeObject<ResponseSuccessUploadImage>(responseString);
                    if (result != null)
                        listIdImage.Add(result.id);
                }
                else
                {
                    ErrorResponse rrrorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseString);
                    if (rrrorResponse != null)
                    {
                        //OAth fail
                        if (rrrorResponse.error?.code == 190)
                        {
                            //Handler...
                        }
                    }
                }
            }

            var clientPost = new HttpClient();
            var requestPost = new HttpRequestMessage(HttpMethod.Post, "https://graph.facebook.com/me/feed");
            var collectionPost = new List<KeyValuePair<string, string>>();
            collectionPost.Add(new("message", ContentPost));
            if(listIdImage.Any())
            {
                for (int i = 0; i < listIdImage.Count; i++)
                    collectionPost.Add(new($"attached_media[{i}]", $"{{media_fbid:{listIdImage[i]}}}"));
            }
            collectionPost.Add(new("access_token", accessTokenPage));
            var contentPostApi = new FormUrlEncodedContent(collectionPost);
            requestPost.Content = contentPostApi;
            var responsePost = clientPost.SendAsync(requestPost).Result;
            var responsePostString = responsePost.Content.ReadAsStringAsync().Result;
            //Success
            if (responsePost.IsSuccessStatusCode)
            {
                responsePost.EnsureSuccessStatusCode();
                ResponseSuccessPostContent result = JsonConvert.DeserializeObject<ResponseSuccessPostContent>(responsePostString);
                if (result != null)
                    return result.id;
            }
            else
            {
                ErrorResponse rrrorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responsePostString);
                if (rrrorResponse != null)
                {
                    //OAth fail
                    if (rrrorResponse.error?.code == 190)
                    {
                        //Handler...
                    }
                }
            }
            return string.Empty;
        }


        public FacebookPostInsights GetInsightByPostID(string URNPageID, string URNPostID, string URNUserID)
        {
            var accessToken = GetAccessToken(URNUserID);
            var pageByUser = GetPagesByUser(URNUserID, URNPageID);
            if (pageByUser != null)
            {
                var accessTokenPage = pageByUser[0].access_token;
                var client = new HttpClient();
                var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get,
                    $"https://graph.facebook.com/{URNPostID}?fields=comments.summary(true),shares,insights.metric(post_impressions)&access_token={accessTokenPage}");
                var response = client.SendAsync(request).Result;
                var responseString = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                {
                    response.EnsureSuccessStatusCode();
                    FacebookPostInsights result = JsonConvert.DeserializeObject<FacebookPostInsights>(responseString);
                    return result;
                }
                else
                {
                    ErrorResponse errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseString);
                    if (errorResponse != null)
                    {
                        //OAth fail
                        if (errorResponse.error?.code == 190)
                        {
                            //Handler...
                        }
                    }
                }
            }
            return new FacebookPostInsights();
        }





    }
}
