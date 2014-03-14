using System.Runtime.Serialization;

namespace GWAB.Web.Models
{
    [DataContract]
    public class RssItem
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "author")]
        public string Author { get; set; }

        [DataMember(Name = "link")]
        public string Link { get; set; }

        public RssItem()
        {
            Title = string.Empty;
            Description = string.Empty;
            Author = string.Empty;
            Link = string.Empty;
        }
    }
}