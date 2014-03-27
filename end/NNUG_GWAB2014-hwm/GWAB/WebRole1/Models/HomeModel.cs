using System.ComponentModel.DataAnnotations;

namespace WebRole1.Models
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