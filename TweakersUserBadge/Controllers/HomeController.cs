using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using HtmlAgilityPack;
using TweakersUserBadge.Helpers;
using TweakersUserBadge.Models;

namespace TweakersUserBadge.Controllers {
    public class HomeController : Controller {
        private static readonly MemoryCache Cache = MemoryCache.Default;
        private static readonly CookieContainer Cookies = new CookieContainer();
        private static readonly Encoding Encoding = Encoding.GetEncoding("iso-8859-15");

        public ActionResult Index() {
            return View();
        }
        
        public async Task<ActionResult> Result(string nameString) {
            // Setup
            var names = nameString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var users = new List<User>();

            if (Cookies.Count < 2) { // Only first time initialization
                Cookies.Add(new Cookie("TnetID", "1m7_W857c4enZACkMJE7cs6X32n7PXqf", "/", ".tweakers.net")); // new TnetID, not mine :P
                Cookies.Add(new Cookie("__gads", "ID=8b6a9a888539ccda:T=1444937371:S=ALNI_MaS6zR7ad-SOd4VNjgnz-8f_pK-9A", "/", ".tweakers.net"));
            }

            foreach (var name in names.Select(s => s.Trim().ToUpperInvariant()).Where(s => s.Length >= 3).Distinct()) {
                // Get from cache or get from web
                var user = await GetFromCache(name, async () => await GetUserFromWeb(name));
                users.Add(user);
            }

            return View(users.Where(s => s != null).ToList());
        }

        private async Task<T> GetFromCache<T>(string key, Func<Task<T>> valueFunc) {
            var newValue = new AsyncLazy<T>(valueFunc);

            var value = (AsyncLazy<T>) Cache.AddOrGetExisting(key, newValue, DateTime.Now.AddDays(1));
            return await (value ?? newValue).Value;
        }

        private async Task<User> GetUserFromWeb(string name) {
            try {
                var request = (HttpWebRequest) WebRequest.Create($"http://tweakers.net/gallery/{name}");
                request.CookieContainer = Cookies;
                var response = (HttpWebResponse) await request.GetResponseAsync();
                var dataStream = response.GetResponseStream();

                var htmlDocument = new HtmlDocument();
                htmlDocument.Load(dataStream, Encoding);

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
                var personalInfoLeftColumn = personalInfoContainer?.SelectNodes(".//table[@class='galleryInnerTable']")?[0];
                var personalInfoRightColumn = personalInfoContainer?.SelectNodes(".//table[@class='galleryInnerTable']")?[1];

                if (personalInfoLeftColumn?.SelectNodes(".//tr/td[@class='title']")?[0]?.InnerText == "Naam")
                    user.Name = personalInfoLeftColumn.SelectNodes(".//tr/td")?[1]?.InnerText;

                var rightTitles = personalInfoRightColumn?.SelectNodes(".//tr/td[@class='title']");
                for (var i = 0; i < rightTitles?.Count; i++) {
                    if (rightTitles[i]?.InnerText == null) continue;
                    switch (rightTitles[i].InnerText) {
                        case "Beroep":
                            {
                                user.Beroep = personalInfoRightColumn.SelectNodes(".//tr/td")?[i * 2 + 1]?.InnerText;
                                break;
                            }
                        case "Opleiding":
                            {
                                user.Education = personalInfoRightColumn.SelectNodes(".//tr/td")?[i * 2 + 1]?.InnerText;
                                break;
                            }
                        default: { break; }
                    }
                }

                return user;
            } catch {
                return null;
            }
        }
    }
}