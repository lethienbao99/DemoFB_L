using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoFB_L
{
    public class LinkedInService
    {
        public bool CheckIsValidToken(string token, int expires_in)
        {
            int expiresIn = expires_in; // seconds (from "expires_in")
            DateTime tokenIssuedAt = DateTime.Now; // Assuming token is issued now
            // Calculate expiration time
            TimeSpan expirationTimeSpan = TimeSpan.FromSeconds(expiresIn);
            DateTime expirationTime = tokenIssuedAt.Add(expirationTimeSpan);
            if (DateTime.Now >= expirationTime)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public string GetAccessToken(string UserId)
        {
            return "AQX5GRUei5nlwA7ZFQfHTUv3pgaT9jr67K3y2gwqLeqsVDB2Q5GrAIOe9Tcl-fF_kTpmiOH2TWxlEiiyVU8y2htGoj0sNEig7ujd5iKhs2r33aGJ1Z3JoQn6tSu4ggmJRqVRm-SXuM43tmXAWVS21cpUciBwl4LnCs-Y2EnqWD4xW5mTGP36OgdyvvwlajgE0WEDWB9x7C7-xocqE0Zq404kLCzxtZ2pEWkvJcrsN3P92Dankx03hDonoetJyj1t3ZvEHqB1H1Xa4crcLf9IyG7BwQMcjFHcpTNS6677qDJLJy_kAHlksYy6qjnLnv9RX220_ZN9yIOUmctzd0JY3JILnRlAag";
        }

        public LinkedInToken ExtendAccessToken(string client_id, string client_secret, string refresh_token)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://www.linkedin.com/oauth/v2/accessToken");
            var collection = new List<KeyValuePair<string, string>>();
            collection.Add(new("grant_type", "refresh_token"));
            collection.Add(new("refresh_token", refresh_token));
            collection.Add(new("client_id", client_id));
            collection.Add(new("client_secret", refresh_token));
            var content = new FormUrlEncodedContent(collection);
            request.Content = content;
            var response = client.SendAsync(request).Result;
            var responseString = response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
            {
                response.EnsureSuccessStatusCode();
                LinkedInToken result = JsonConvert.DeserializeObject<LinkedInToken>(responseString);
                if (result != null)
                {
                    return result;
                }
            }
            else
            {
                ErrorResponseLinkedIn rrrorResponse = JsonConvert.DeserializeObject<ErrorResponseLinkedIn>(responseString);
                if (rrrorResponse != null)
                {
                    //OAth fail
                    if (rrrorResponse.status == 401)
                    {
                        //Handler...
                    }
                }
                return new LinkedInToken();
            }
            return new LinkedInToken();
        }



        public List<DataCompany> GetPagesByUser(string UserId)
        {
            var accessToken = GetAccessToken(UserId);

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.linkedin.com/v2/organizationAcls?q=roleAssignee&role=ADMINISTRATOR&projection=(elements*(*,roleAssignee~(localizedFirstName, localizedLastName), organization~(localizedName)))");
            request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");
            request.Headers.Add("Linkedin-Version", "202409");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            var response = client.SendAsync(request).Result;
            var responseString = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
            {
                response.EnsureSuccessStatusCode();
                CompaniesByUser result = JsonConvert.DeserializeObject<CompaniesByUser>(responseString);
                if (result != null)
                {
                    var resltNew = new List<DataCompany>();

                    foreach(var company in result.elements)
                    {
                        resltNew.Add(new DataCompany() { id = company.organization, name = company.organizationObject.localizedName });
                    }
                    return resltNew;
                }
            }
            else
            {
                ErrorResponseLinkedIn rrrorResponse = JsonConvert.DeserializeObject<ErrorResponseLinkedIn>(responseString);
                if (rrrorResponse != null)
                {
                    //OAth fail
                    if (rrrorResponse.status == 401)
                    {
                        //Handler...
                    }
                }
                return new List<DataCompany>(); 
            }
            return new List<DataCompany>();
        }

        public string PostContent(string Content, List<string> URLImages, string userId, string UrnCompany, string accessToken)
        {
            //Từ UserID lấy đc thông tin token đã lưu khi kết nối
            //Step 1: Create Session Upload (image/video)
            var listMediaUri = new List<string>();
            if (URLImages.Any())
            {
                foreach(var image in URLImages)
                {
                    var client = new HttpClient();
                    var request = new HttpRequestMessage(HttpMethod.Post, "https://api.linkedin.com/v2/assets?action=registerUpload");
                    request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");
                    request.Headers.Add("Authorization", $"Bearer {accessToken}");
                    // Create the request body using object serialization
                    var uploadRequest = new UploadRequest
                    {
                        RegisterUploadRequest = new RegisterUploadRequest
                        {
                            Recipes = new[] { "urn:li:digitalmediaRecipe:feedshare-image" },
                            Owner = UrnCompany, //"urn:li:organization:104415594",
                            ServiceRelationships = new[]
                            {
                        new ServiceRelationship
                        {
                            RelationshipType = "OWNER",
                            Identifier = "urn:li:userGeneratedContent"
                        }
                    }
                        }
                    };
                    var json = JsonConvert.SerializeObject(uploadRequest);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = client.SendAsync(request).Result;
                    var responseString = response.Content.ReadAsStringAsync().Result;
                    if (response.IsSuccessStatusCode)
                    {
                        //Step 2: Upload image
                        response.EnsureSuccessStatusCode();
                        SessionUploadLinkedIn result = JsonConvert.DeserializeObject<SessionUploadLinkedIn>(responseString);
                        if (result != null)
                        {
                            string URLUpload = result.value.uploadMechanism.comlinkedindigitalmediauploadingMediaUploadHttpRequest.uploadUrl;
                            //string MediaUri = result.value.asset;
                            var clientUploadImage = new HttpClient();
                            var requestUploadImage = new HttpRequestMessage(HttpMethod.Put, URLUpload);
                            requestUploadImage.Headers.Add("Authorization", "Bearer redacted");
                            //requestUploadImage.Content = new StreamContent(File.OpenRead("C:/Users/bao.le/Downloads/test123.jpg"));
                            requestUploadImage.Content = new StreamContent(File.OpenRead(image));
                            var responseUploadImage = clientUploadImage.SendAsync(requestUploadImage).Result;
                            if (responseUploadImage.IsSuccessStatusCode)
                            {
                                listMediaUri.Add(result.value.asset);
                            }
                        }
                    }
                    else
                    {
                        ErrorResponseLinkedIn rrrorResponse = JsonConvert.DeserializeObject<ErrorResponseLinkedIn>(responseString);
                        if (rrrorResponse != null)
                        {
                            //OAth fail
                            if (rrrorResponse.status == 401)
                            {
                                //Handler...
                            }
                        }
                    }
                }

               
            }

            //Step 3: Post content
            var clientPost = new HttpClient();
            var requestPost = new HttpRequestMessage(HttpMethod.Post, "https://api.linkedin.com/v2/ugcPosts");
            requestPost.Headers.Add("X-Restli-Protocol-Version", "2.0.0");
            requestPost.Headers.Add("Authorization", $"Bearer {accessToken}");

            var mediaObject = new List<Media>();
            string ShareMediaCategory = "NONE";
            if (listMediaUri.Any())
            {
                foreach (var mediaUri in listMediaUri)
                {
                    mediaObject.Add(new Media()
                    {
                        Status = "READY",
                        Description = new Description { Text = "Center stage!" },
                        MediaUri = mediaUri,
                        Title = new Title { Text = "Image" }
                    });
                }
                ShareMediaCategory = "IMAGE";
            }
            else
                mediaObject = new List<Media>();

            var shareRequest = new ShareRequest
            {
                Author = UrnCompany,
                LifecycleState = "PUBLISHED",
                SpecificContent = new SpecificContent
                {
                    ShareContent = new ShareContent
                    {
                        ShareCommentary = new ShareCommentary
                        {
                            Text = Content
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
            if (responsePost.IsSuccessStatusCode)
            {
                responsePost.EnsureSuccessStatusCode();
                ResponseSuccessPostContentLinkedIn resultPost = JsonConvert.DeserializeObject<ResponseSuccessPostContentLinkedIn>(responsePostString);
                if (resultPost != null)
                    return resultPost.id;
            }
            else
            {
                ErrorResponseLinkedIn rrrorResponse = JsonConvert.DeserializeObject<ErrorResponseLinkedIn>(responsePostString);
                if (rrrorResponse != null)
                {
                    //OAth fail
                    if (rrrorResponse.status == 401)
                    {
                        //Handler...
                    }
                }
            }
            return string.Empty;
        }


        public LinkedInSocialAction GetInsightByPostID(string URNPageID, string URNPostID, string URNUserID)
        {

            var accessToken = GetAccessToken(URNUserID);
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.linkedin.com/v2/socialActions/{URNPostID}");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            var response = client.SendAsync(request).Result;
            var responseString = response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
            {
                response.EnsureSuccessStatusCode();
                var socialAction = JsonConvert.DeserializeObject<LinkedInSocialAction>(responseString);
                return socialAction;
            }
            else
            {
                ErrorResponseLinkedIn rrrorResponse = JsonConvert.DeserializeObject<ErrorResponseLinkedIn>(responseString);
                if (rrrorResponse != null)
                {
                    //OAth fail
                    if (rrrorResponse.status == 401)
                    {
                        //Handler...
                    }
                }
            }
            return new LinkedInSocialAction();
        }

    }
}
