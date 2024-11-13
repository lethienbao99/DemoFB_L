using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoFB_L
{
    public class FacebookVideoManager
    {
        private readonly HttpClient _client;
        private readonly string _pageId;
        private readonly string _accessToken;

        public FacebookVideoManager(HttpClient client, string pageId, string accessToken)
        {
            _client = client;
            _pageId = pageId;
            _accessToken = accessToken;
        }




        public async Task<string> UploadLargeVideoFromUrlAsync(string videoUrl, string message)
        {
            // Step 1: Start the upload session
            string startUploadUri = $"https://graph-video.facebook.com/v21.0/{_pageId}/videos";

            var startUploadForm = new MultipartFormDataContent
            {
                { new StringContent(_accessToken), "access_token" },
                { new StringContent("start"), "upload_phase" },
                { new StringContent("0"), "file_size" } // Provide "0" for file size as the file will be fetched from URL
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

            // Step 2: Upload the video URL in the "transfer" phase
            var uploadForm = new MultipartFormDataContent
            {
                { new StringContent(_accessToken), "access_token" },
                { new StringContent("transfer"), "upload_phase" },
                { new StringContent(uploadSessionId), "upload_session_id" },
                { new StringContent(videoUrl), "file_url" } // Provide the video URL
            };

            var uploadResponse = await _client.PostAsync(startUploadUri, uploadForm);
            var uploadResult = await uploadResponse.Content.ReadAsStringAsync();
            dynamic uploadData = Newtonsoft.Json.JsonConvert.DeserializeObject(uploadResult);

            if (uploadData.error != null)
            {
                Console.WriteLine("Error uploading video: " + uploadData.error.message);
                return null;
            }

            // Step 3: Finalize the upload
            var finalizeForm = new MultipartFormDataContent
            {
                { new StringContent(_accessToken), "access_token" },
                { new StringContent("finish"), "upload_phase" },
                { new StringContent(uploadSessionId), "upload_session_id" },
                { new StringContent(Uri.EscapeDataString(message)), "description" } // Provide the video description/message
            };

            var finalizeResponse = await _client.PostAsync(startUploadUri, finalizeForm);
            var finalizeResult = await finalizeResponse.Content.ReadAsStringAsync();
            dynamic finalizeData = Newtonsoft.Json.JsonConvert.DeserializeObject(finalizeResult);

            if (finalizeData.error != null)
            {
                Console.WriteLine("Error finalizing upload: " + finalizeData.error.message);
                return null;
            }

            Console.WriteLine("Large video uploaded successfully!");
            return finalizeData.id; // Return the video ID
        }



        public async Task<string> UploadVideoFromUrlAsync(string videoUrl, string message)
        {
            string uploadUrl = $"https://graph-video.facebook.com/v21.0/{_pageId}/videos";

            var formData = new MultipartFormDataContent
            {
                { new StringContent(_accessToken), "access_token" },
                { new StringContent("https://eu-central.storage.cloudconvert.com/tasks/aa6c7a42-6b60-472b-bf36-295a6e27a4e6/11videotest123.mp4?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Content-Sha256=UNSIGNED-PAYLOAD&X-Amz-Credential=cloudconvert-production%2F20241019%2Ffra%2Fs3%2Faws4_request&X-Amz-Date=20241019T090443Z&X-Amz-Expires=86400&X-Amz-Signature=112a07d29ea58254d2896539246e43965a27db051665653da7c716bd432e8303&X-Amz-SignedHeaders=host&response-content-disposition=attachment%3B%20filename%3D%2211videotest123.mp4%22&response-content-type=video%2Fmp4&x-id=GetObject"), "file_url" },  // Video URL
                { new StringContent(message), "description" } // Video description/message
            };

            var response = await _client.PostAsync(uploadUrl, formData);
            var result = await response.Content.ReadAsStringAsync();
            dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(result);

            if (data.error != null)
            {
                Console.WriteLine("Error uploading video: " + data.error.message);
                return null;
            }

            Console.WriteLine("Video uploaded successfully!");
            return data.id; // Return the video ID
        }


        public async Task<string> UploadVideoAsync(string filePath, string message)
        {
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
                return null;
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
                        return null;
                    }

                    chunkIndex++;
                }
            }

            // Finalize the upload with the description
            var finalizeForm = new MultipartFormDataContent
            {
                { new StringContent(_accessToken), "access_token" },
                { new StringContent("finish"), "upload_phase" },
                { new StringContent(uploadSessionId), "upload_session_id" },
                { new StringContent(Uri.EscapeDataString(message)), "description" } // Pass the actual message description
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
            return finalizeData.id;
        }



        // Post multiple videos in a single post
        public async Task PostMultipleVideosAsync(List<string> videoIds, string message)
        {
            // Prepare the request URL to create a new post
            string requestUri = $"https://graph.facebook.com/v21.0/{_pageId}/feed";

            // Create the post content
            var postData = new
            {
                message = message,
                attached_media = videoIds.ConvertAll(videoId => new { media_fbid = videoId })
            };

            var jsonContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(postData), System.Text.Encoding.UTF8, "application/json");

            try
            {
                // Send the POST request
                var response = await _client.PostAsync(requestUri + $"?access_token={_accessToken}", jsonContent);
                response.EnsureSuccessStatusCode(); // Throws if not a success code.

                // Read and display the response content
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Post successful!");
                Console.WriteLine(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error posting videos: " + ex.Message);
            }
        }
    }
}
