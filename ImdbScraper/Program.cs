﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace ImdbScraper
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            // Stopwatch stopwatch = Stopwatch.StartNew();
            var threadsNum = 8;
            var moviesNum = 8000;

            var titles = await GetMovieCodes(moviesNum);

            List<int> trechos = new List<int>();
            for (int i = 0; i <= moviesNum; i+=(moviesNum/threadsNum))
                trechos.Add(i);

            for (int i = 0; i < trechos.Count-1; i++)
            { 
                var partialMovieList = titles.GetRange(trechos[i], (moviesNum/threadsNum));
                new Thread(() => SaveReviewsAndDataFromMovies(partialMovieList)).Start();    
            }
            // stopwatch.Stop();
            // Console.WriteLine(moviesNum + " movie data saved in: ");
            // Console.WriteLine(stopwatch.ElapsedMilliseconds);
        }

        public static async Task<List<string>> GetMovieCodes(int pMovieNum)
        {
            List<string> pageTitles = new List<string>();
            var url = "";
            var num = 0;
            for (var i = 0; i < pMovieNum; i += 50)
            {
                if(i != 0) num = i + 1;
                url = "https://www.imdb.com/search/title/?title_type=feature,tv_movie&release_date=2014-01-01,2020-02-11&start=" +
                      num + "&view=simple";
                //Console.WriteLine("Reading Imdb search page starting from: " + num);
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
        
        public static void SaveReviewsAndDataFromMovies(IEnumerable<string> movieCodes)
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
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows; U; Windows NT 6.1; en-GB;     rv:1.9.2.13) Gecko/20101203 Firefox/3.6.13 (.NET CLR 3.5.30729)");
                var html = "";
                try
                {
                    html = httpClient.GetStringAsync(url).Result;
                }
                catch (Exception e)
                {
                    continue;
                }

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