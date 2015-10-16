﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using System.Web.UI;
using HtmlAgilityPack;

namespace TweakersUserBadge.Controllers {
    public class HomeController : Controller {
        private static MemoryCache cache = MemoryCache.Default;

        public ActionResult Index() {
            return View();
        }
        
        public async Task<ActionResult> Result(string nameString) {
            // Setup
            var names = nameString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var encoding = Encoding.GetEncoding("iso-8859-15");
            var cookies = new CookieContainer();
            var users = new List<User>();
            cookies.Add(new Cookie("TnetID", "1m7_W857c4enZACkMJE7cs6X32n7PXqf", "/", ".tweakers.net")); // new TnetID, not mine :P
            cookies.Add(new Cookie("__gads", "ID=8b6a9a888539ccda:T=1444937371:S=ALNI_MaS6zR7ad-SOd4VNjgnz-8f_pK-9A", "/", ".tweakers.net"));

            foreach (var name in names.Select(s => s.Trim()).Where(s => s.Length >= 3).Distinct()) {
                if(cache.Contains(name.ToLower())) {
                    var user = cache.Get(name.ToLower()) as User;
                    if(user != null) {
                        users.Add(user);
                        continue;
                    }
                }

                // Get profielpagina
                var request = (HttpWebRequest) WebRequest.Create($"http://tweakers.net/gallery/{name}");
                request.CookieContainer = cookies;
                HttpWebResponse response;
                try {
                    response = (HttpWebResponse) await request.GetResponseAsync();
                    var dataStream = response.GetResponseStream();

                    var htmlDocument = new HtmlDocument();
                    htmlDocument.Load(dataStream, encoding);

                    var user = new User();

                    /* Username */ // we have it already but not with the right casing
                    user.Username = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='memberInfo']/h1")?.InnerText?.Trim();

                    /* Icon */
                    user.IconUrl = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='galleryHeading']/a/img")?.GetAttributeValue("src", "");

                    /* Statistieken */
                    var statistiekenContainer = htmlDocument.DocumentNode.SelectSingleNode("//table[@class='galleryTable']");
                    var statistiekenRightColumn = statistiekenContainer.SelectNodes(".//table[@class='galleryInnerTable']")[1];

                    user.Karma = statistiekenRightColumn.SelectNodes(".//tr/td/a")[1].InnerText;
                    user.KarmaRanking = statistiekenRightColumn.SelectNodes(".//tr/td/span")[2].InnerText;
                    user.Tweakotine = statistiekenRightColumn.SelectNodes(".//tr/td/text()[normalize-space(.) != '']")[2]?.InnerText?.Trim();

                    /* Persoonlijke info */
                    var personalInfoContainer = htmlDocument.DocumentNode.SelectSingleNode("//table[@class='galleryTable personal']");
                    var personalInfoLeftColumn = personalInfoContainer.SelectNodes(".//table[@class='galleryInnerTable']")[0];
                    var personalInfoRightColumn = personalInfoContainer.SelectNodes(".//table[@class='galleryInnerTable']")[1];

                    if(personalInfoLeftColumn.SelectNodes(".//tr/td[@class='title']")?[0]?.InnerText == "Naam")
                        user.Name = personalInfoLeftColumn.SelectNodes(".//tr/td")?[1]?.InnerText;

                    var rightTitles = personalInfoRightColumn.SelectNodes(".//tr/td[@class='title']");
                    for(var i = 0; i < rightTitles.Count; i++) {
                        if (rightTitles[i]?.InnerText == null) continue;
                        switch(rightTitles[i].InnerText) {
                            case "Beroep": {
                                    user.Beroep = personalInfoRightColumn.SelectNodes(".//tr/td")?[i * 2 + 1]?.InnerHtml;
                                    break;
                            }
                            case "Opleiding": {
                                    user.Education = personalInfoRightColumn.SelectNodes(".//tr/td")?[i * 2 + 1]?.InnerHtml;
                                    break;
                            }
                            default: { break; }
                        }
                    }

                    users.Add(user);
                    cache.Add(name.ToLower(), user, DateTime.Now.AddDays(1));
                } catch {
                    // ignored
                }
            }

            return View(users);
        }

        public class User {
            public string Karma { get; set; }
            public string KarmaRanking { get; set; }
            public string Username { get; set; }
            public string Name { get; set; }
            public string Beroep { get; set; }
            public string IconUrl { get; set; }
            public string Tweakotine { get; set; }
            public string Education { get; set; }
        }
    }
}