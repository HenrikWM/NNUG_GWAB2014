using System.Collections.Generic;

namespace GWAB.Web.Models
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