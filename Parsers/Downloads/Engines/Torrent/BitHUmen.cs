﻿namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Security.Authentication;
    using System.Text;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping bitHUmen.
    /// </summary>
    [TestFixture]
    public class BitHUmen : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "bitHUmen";
            }
        }

        /// <summary>
        /// Gets the URL of the site.
        /// </summary>
        /// <value>The site location.</value>
        public override string Site
        {
            get
            {
                return "http://bithumen.be/";
            }
        }

        /// <summary>
        /// Gets the name of the plugin's developer.
        /// </summary>
        /// <value>The name of the plugin's developer.</value>
        public override string Developer
        {
            get
            {
                return "RoliSoft";
            }
        }

        /// <summary>
        /// Gets the version number of the plugin.
        /// </summary>
        /// <value>The version number of the plugin.</value>
        public override Version Version
        {
            get
            {
                return Utils.DateTimeToVersion("2012-04-17 7:51 PM");
            }
        }

        /// <summary>
        /// Gets a value indicating whether the site requires authentication.
        /// </summary>
        /// <value><c>true</c> if requires authentication; otherwise, <c>false</c>.</value>
        public override bool Private
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the names of the required cookies for the authentication.
        /// </summary>
        /// <value>The required cookies for authentication.</value>
        public override string[] RequiredCookies
        {
            get
            {
                return new[] { "uid", "pass", "rid" };
            }
        }

        /// <summary>
        /// Gets a value indicating whether this search engine can login using a username and password.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this search engine can login; otherwise, <c>false</c>.
        /// </value>
        public override bool CanLogin
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the URL to the login page.
        /// </summary>
        /// <value>The URL to the login page.</value>
        public override string LoginURL
        {
            get
            {
                return Site + "takelogin.php";
            }
        }

        /// <summary>
        /// Gets the input fields of the login form.
        /// </summary>
        /// <value>The input fields of the login form.</value>
        public override Dictionary<string, object> LoginFields
        {
            get
            {
                return new Dictionary<string, object>
                    {
                        { "salted_passhash", string.Empty             },
                        { "username",        LoginFieldTypes.UserName },
                        { "password",        LoginFieldTypes.Password },
                        { "megjegyez",       "on"                     },
                        {
                            "vxx",
                            (Func<string>)(() =>
                                {
                                    var login = Utils.GetHTML(Site + "login.php?simplelogin=1");
                                    var token = login.DocumentNode.GetNodeAttributeValue("//input[@type='hidden' and @name='vxx']", "value");

                                    return token;
                                })
                        },
                    };
            }
        }

        /// <summary>
        /// Gets the type of the link.
        /// </summary>
        /// <value>The type of the link.</value>
        public override Types Type
        {
            get
            {
                return Types.Torrent;
            }
        }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var html = Utils.GetHTML(Site + "browse.php?c7=1&c26=1&genre=0&search=" + Utils.EncodeURL(query), cookies: Cookies, encoding: Encoding.GetEncoding("iso-8859-2"));

            if (GazelleTrackerLoginRequired(html.DocumentNode))
            {
                throw new InvalidCredentialException();
            }

            var links = html.DocumentNode.SelectNodes("//table[@id='torrenttable']/tr/td[2]/a[1]/b");

            if (links == null)
            {
                yield break;
            }

            var fl = html.DocumentNode.GetHtmlValue("//font[@color='red']/h2[contains(translate(text(), 'FRELCH ', 'frelch'), 'freeleech')]") != null; 

            foreach (var node in links)
            {
                var link = new Link(this);

                link.Release = node.GetNodeAttributeValue("../", "title") ?? node.InnerText;
                link.InfoURL = Site + node.GetNodeAttributeValue("../../a", "href");
                link.FileURL = Site + node.GetNodeAttributeValue("../../a[starts-with(@title, 'Let')]", "href");
                link.Size    = node.GetHtmlValue("../../../td[6]/u").Replace("<br>", " ");
                link.Quality = FileNames.Parser.ParseQuality(link.Release);
                link.Infos   = Link.SeedLeechFormat.FormatWith(node.GetTextValue("../../../td[8]").Trim(), node.GetTextValue("../../../td[9]").Trim().Split('/')[1].Trim())
                             + (fl ? ", Global Freeleech" : string.Empty);

                yield return link;
            }
        }

        /// <summary>
        /// Authenticates with the site and returns the cookies.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>Cookies on success, <c>string.Empty</c> on failure.</returns>
        public override string Login(string username, string password)
        {
            return GazelleTrackerLogin(username, password);
        }
    }
}
