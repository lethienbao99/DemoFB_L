using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoFB_L
{
    public class FacebookModel
    {

    }

    public class CategoryList
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class Cursors
    {
        public string before { get; set; }
        public string after { get; set; }
    }

    public class PageData
    {
        public string access_token { get; set; }
        public string category { get; set; }
        public List<CategoryList> category_list { get; set; }
        public string name { get; set; }
        public string id { get; set; }
        public List<string> tasks { get; set; }
    }

    public class Paging
    {
        public Cursors cursors { get; set; }
    }

    public class PagesByUser
    {
        public List<PageData> data { get; set; }
        public Paging paging { get; set; }
    }


    public class Error
    {
        public string message { get; set; }
        public string type { get; set; }
        public int code { get; set; }
        public string fbtrace_id { get; set; }
    }

    public class ErrorResponse
    {
        public Error error { get; set; }
    }

    public class ExtendAccessToken
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
    }


    public class ResponseSuccessUploadImage
    {
        public string id { get; set; }
    }

    public class ResponseSuccessPostContent
    {
        public string id { get; set; }
        public bool post_supports_client_mutation_id { get; set; }
    }

    public class FacebookPostInsights
    {
        public Comments comments { get; set; }
        public Shares shares { get; set; }
        public Insights insights { get; set; }
        public string id { get; set; }

        public int TotalComments => comments?.summary?.total_count ?? 0;
        public int TotalShares => shares?.count ?? 0;
        public int TotalViews => insights?.data?.FirstOrDefault(i => i.name == "post_impressions")?.values?.FirstOrDefault()?.value ?? 0;
    }

    public class Comments
    {
        public List<CommentData> data { get; set; }
        public Paging paging { get; set; }
        public Summary summary { get; set; }
    }

    public class CommentData
    {
        public string created_time { get; set; }
        public From from { get; set; }
        public string message { get; set; }
        public string id { get; set; }
    }

    public class From
    {
        public string name { get; set; }
        public string id { get; set; }
    }

    public class Summary
    {
        public string order { get; set; }
        public int total_count { get; set; }
        public bool can_comment { get; set; }
    }

    public class Shares
    {
        public int count { get; set; }
    }

    public class Insights
    {
        public List<InsightData> data { get; set; }
        public Paging paging { get; set; }
    }

    public class InsightData
    {
        public string name { get; set; }
        public string period { get; set; }
        public List<Value1> values { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string id { get; set; }
    }

    public class Value1
    {
        public int value { get; set; }
    }

}
