using System;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using GWAB.Web.Models;
using Nest;

namespace GWAB.Web.Controllers
{
    public class HomeController : Controller
    {
        public HomeController()
        {
            const string elasticsearchEndpoint = "http://gwab2014-elasticsearch-cluster.cloudapp.net";
            
            var uri = new Uri(elasticsearchEndpoint);

            var settings = new ConnectionSettings(uri)
                .SetDefaultIndex("newspapers")
                .MapDefaultTypeNames(i => i.Add(typeof(RssItem), "page"))
                .EnableTrace();

            var client = new ElasticClient(settings);

            var results = client.Search<RssItem>(s => s
                    .From(0)
                    .Size(10)
                    .Query(q => q.QueryString(qs => qs.OnFields(f => f.Description).Query("Cantona")))
            );

            var dataItems = results.Documents;
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}