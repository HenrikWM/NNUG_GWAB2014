using System.Collections.Generic;

namespace WebRole1.Models
{
    public class SearchResultsModel
    {
        public ICollection<RssItem> Items { get; set; }

        public SearchResultsModel()
        {
            Items = new List<RssItem>();
        }
    }
}