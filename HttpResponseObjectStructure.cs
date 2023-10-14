using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace guardian_wpf
{
    //Section of articles - object structure.
    public class Response
    {
        public ResponseData response { get; set; }
    }
    public class ResponseData
    {
        public int currentPage { get; set; }
        public int pages { get; set; }
        public List<Results> results { get; set; }
    }

    //Article - object structure.
    public class ResponseArticle
    {
        public ResponseArticleData response { get; set; }
    }
    public class ResponseArticleData
    {
        public Results content;
        public List<RelatedContent> relatedContent { get; set; }
    }

    //Attributes of article - object structure. (it concerns previous both classes)
    public class Results
    {
        public string id { get; set; }
        public string webPublicationDate { get; set; }
        public string webTitle { get; set; }
        public string truncatedWebTitle { get; set; }
        public string webUrl { get; set; }
        public Fields fields { get; set; }
        public List<Tags> tags { get; set; }
    }
    public class Fields
    {
        public string thumbnail { get; set; }
        public string body { get; set; }
        public string trailText { get; set; }
    }
    public class Tags
    {
        public string bylineLargeImageUrl { get; set; }
        public string webTitle { get; set; }
    }
    public class RelatedContent
    {
        public string webTitle { get; set; }
        public string webUrl { get; set; }
    }
}
