using System;
using System.Linq;
using System.Web.Mvc;
using GWAB.Web.Models;
using Nest;

namespace GWAB.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ElasticClient _searchClient;

        public HomeController()
        {
            const string elasticsearchEndpoint = "http://gwab2014-elasticsearch-cluster.cloudapp.net";

            var uri = new Uri(elasticsearchEndpoint);

            var settings = new ConnectionSettings(uri)
                .SetDefaultIndex("newspapers")
                .MapDefaultTypeNames(i => i.Add(typeof(RssItem), "page"))
                .EnableTrace();

            _searchClient = new ElasticClient(settings);
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

        public ActionResult Search(string querystring)
        {
            var model = new SearchResultsModel();

            if (string.IsNullOrEmpty(querystring) == false)
            {
                var results = _searchClient.Search<RssItem>(s => s
                        .From(0)
                        .Size(100)
                        .Query(q => q.QueryString(qs => qs.OnFields(f => f.Title, f => f.Description).Query(querystring)))
                );

                model.Items = results.Documents.ToList();
            }

            return View("SearchResults", model);
        }
    }
}