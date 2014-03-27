using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Nest;
using WebRole1.Models;

namespace WebRole1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ElasticClient _searchClient;

        public HomeController()
        {
            const string elasticsearchEndpoint = "http://gwab2014-hwm-azure-elasticsearch-cluster.cloudapp.net:9200/";

            var uri = new Uri(elasticsearchEndpoint);

            var settings = new ConnectionSettings(uri)
                .SetDefaultIndex("newspapers") // index: newspapers
                .MapDefaultTypeNames(i => i.Add(typeof(RssItem), "page")) // mapping: page
                .EnableTrace(); // gir oss json-output fra NEST til Output-vinduet i Visual Studio

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