using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace ImdbScraper
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var titles = await GetMovieCodes();
            await SaveReviewsAndDataFromMovies(titles);
        }
        
        public static async Task<IEnumerable<string>> GetMovieCodes()
        {
            var itens = 100;
            List<string> pageTitles = new List<string>();
            var url = "";
            for (int i = 0; i < itens; i = i + 51)
            {
                url = "https://www.imdb.com/search/title/?title_type=feature,tv_movie&release_date=2014-01-01,2020-02-11&start=" +
                      i + "&view=simple";
                var httpClient = new HttpClient();
                var html = await httpClient.GetStringAsync(url);

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                var MoviesHTML = htmlDocument.DocumentNode.Descendants("div")
                    .Where(node => node.GetAttributeValue("class", "")
                        .Equals("lister-item mode-simple")).ToList();

                foreach (var Movie in MoviesHTML)
                    pageTitles.Add((Movie.Descendants("a").FirstOrDefault()?.GetAttributeValue("href", "").Substring(7, 9)));
                
            }
            return pageTitles;
        }

        public static async Task SaveReviewsAndDataFromMovies(IEnumerable<string> movieCodes)
        {
            var folderPath = Path.GetDirectoryName(Path.GetDirectoryName(System.IO.Directory.GetCurrentDirectory()));
            var folder = @"\movies\";
            var docPath = folderPath + folder;
            var urlbase = "https://www.imdb.com/title/";
            var url = "";
            IEnumerable<HtmlNode> reviewNodes;
            var title = "";
            var year = "";
            var fullPath = "";

            foreach (var movieCode in movieCodes)
            {
                url = urlbase + movieCode + "/reviews";
                fullPath = docPath + movieCode + ".txt";
                var httpClient = new HttpClient();
                var html = await httpClient.GetStringAsync(url);

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);
                
                title = htmlDocument.DocumentNode
                    .SelectNodes("//*[@id='main']/section/div[1]/div/div/h3/a").FirstOrDefault()?
                    .InnerText.Trim();

                year = htmlDocument.DocumentNode.SelectNodes("//*[@id='main']/section/div[1]/div/div/h3/span")
                    .FirstOrDefault()?.InnerText.Trim();

                reviewNodes = htmlDocument.DocumentNode.SelectNodes("//*[@class='" + "text show-more__control" + "']")?
                                  .ToList() ?? Enumerable.Empty<HtmlNode>();
                
                List<ImdbReview> reviewsList = new List<ImdbReview>();

                foreach (var node in reviewNodes)
                {
                    ImdbReview imdbReview = new ImdbReview();
                    imdbReview.Descricao = node.InnerText;
                    reviewsList.Add(imdbReview);
                }

                ImdbFile Imdb = new ImdbFile(title, year, reviewsList);

                string json = JsonConvert.SerializeObject(Imdb);
                
                File.WriteAllText(fullPath, json);
                
            }
        }
    }
}