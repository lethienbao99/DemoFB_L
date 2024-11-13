using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoFB_L
{
    public class LinkedInModel
    {
    }

    public class Element
    {
        [JsonProperty("organization~")]
        public Organization organizationObject { get; set; }
        public string role { get; set; }
        public string organization { get; set; }
        public string roleAssignee { get; set; }
        public string state { get; set; }

        [JsonProperty("roleAssignee~")]
        public RoleAssignee roleAssigneeObject { get; set; }
    }

    public class Organization
    {
        public string localizedName { get; set; }
    }

    public class RoleAssignee
    {
        public string localizedLastName { get; set; }
        public string localizedFirstName { get; set; }
    }

    public class CompaniesByUser
    {
        public List<Element> elements { get; set; }
    }

    public class DataCompany
    {
        public string id { get; set; }
        public string name { get; set; }
    }



    public class ErrorResponseLinkedIn
    {
        public int status { get; set; }
        public int serviceErrorCode { get; set; }
        public string code { get; set; }
        public string message { get; set; }
    }

    public class LinkedInToken
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
        public int refresh_token_expires_in { get; set; }
        public string scope { get; set; }
    }


    public class RegisterUploadRequest
    {
        [JsonProperty("recipes")]
        public string[] Recipes { get; set; }
        [JsonProperty("owner")]
        public string Owner { get; set; }
        [JsonProperty("serviceRelationships")]
        public ServiceRelationship[] ServiceRelationships { get; set; }
    }

    public class ServiceRelationship
    {
        [JsonProperty("relationshipType")]
        public string RelationshipType { get; set; }
        [JsonProperty("identifier")]
        public string Identifier { get; set; }
    }

    public class UploadRequest
    {
        [JsonProperty("registerUploadRequest")]
        public RegisterUploadRequest RegisterUploadRequest { get; set; }
    }



    public class ComLinkedinDigitalmediaUploadingMediaUploadHttpRequest
    {
        public string uploadUrl { get; set; }
        public Headers headers { get; set; }
    }

    public class Headers
    {
        [JsonProperty("media-type-family")]
        public string mediatypefamily { get; set; }
    }

    public class SessionUploadLinkedIn
    {
        public Value value { get; set; }
    }

    public class UploadMechanism
    {
        [JsonProperty("com.linkedin.digitalmedia.uploading.MediaUploadHttpRequest")]
        public ComLinkedinDigitalmediaUploadingMediaUploadHttpRequest comlinkedindigitalmediauploadingMediaUploadHttpRequest { get; set; }
    }

    public class Value
    {
        public string mediaArtifact { get; set; }
        public UploadMechanism uploadMechanism { get; set; }
        public string asset { get; set; }
        public string assetRealTimeTopic { get; set; }
    }






    public class ShareCommentary
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class Description
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class Title
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class Media
    {
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("description")]
        public Description Description { get; set; }
        [JsonProperty("media")]
        public string MediaUri { get; set; }
        [JsonProperty("title")]
        public Title Title { get; set; }
    }

    public class ShareContent
    {
        [JsonProperty("shareCommentary")]
        public ShareCommentary ShareCommentary { get; set; }
        [JsonProperty("shareMediaCategory")]
        public string ShareMediaCategory { get; set; }
        [JsonProperty("media")]
        public List<Media> Media { get; set; }
    }

    public class SpecificContent
    {
        [JsonProperty("com.linkedin.ugc.ShareContent")]
        public ShareContent ShareContent { get; set; }
    }

    public class Visibility
    {
        [JsonProperty("com.linkedin.ugc.MemberNetworkVisibility")]
        public string MemberNetworkVisibility { get; set; }
    }

    public class ShareRequest
    {

        [JsonProperty("author")]
        public string Author { get; set; }
        [JsonProperty("lifecycleState")]
        public string LifecycleState { get; set; }
        [JsonProperty("specificContent")]
        public SpecificContent SpecificContent { get; set; }
        [JsonProperty("visibility")]
        public Visibility Visibility { get; set; }
    }

    public class ResponseSuccessPostContentLinkedIn
    {
        public string id { get; set; }
    }


    public class LinkedInSocialAction
    {
        public CommentsSummary commentsSummary { get; set; }
        public LikesSummary likesSummary { get; set; }
        public string Urn { get; set; }
        public string Target { get; set; }

        public int TotalLikes => likesSummary?.aggregatedTotalLikes ?? 0;
        public int TotalComments => commentsSummary?.aggregatedTotalComments ?? 0;
    }

    public class CommentsSummary
    {
        public int totalFirstLevelComments { get; set; }
        public int aggregatedTotalComments { get; set; }
    }

    public class LikesSummary
    {
        public int aggregatedTotalLikes { get; set; }
        public bool likedByCurrentUser { get; set; }
        public int totalLikes { get; set; }
    }

}
