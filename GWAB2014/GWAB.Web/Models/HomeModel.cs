using System.ComponentModel.DataAnnotations;

namespace GWAB.Web.Models
{
    public class HomeModel
    {
        [Display(Name = "Search for news:")]
        public string QueryString { get; set; }

        public HomeModel()
        {
            QueryString = string.Empty;
        }
    }
}