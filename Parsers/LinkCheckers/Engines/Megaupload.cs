﻿namespace RoliSoft.TVShowTracker.Parsers.LinkCheckers.Engines
{
    using System;
    using System.Text.RegularExpressions;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for checking Megaupload links.
    /// </summary>
    [TestFixture]
    public class Megaupload : LinkCheckerEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Megaupload";
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
                return "http://www.megaupload.com/";
            }
        }

        /// <summary>
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>
        /// The icon location.
        /// </value>
        public override string Icon
        {
            get
            {
                return "http://wwwstatic.megaupload.com/images/icon.ico";
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
                return Utils.DateTimeToVersion("2011-09-23 2:49 AM");
            }
        }

        /// <summary>
        /// Checks the availability of the link on the service.
        /// </summary>
        /// <param name="url">The link to check.</param>
        /// <returns>
        ///   <c>true</c> if the link is available; otherwise, <c>false</c>.
        /// </returns>
        public override bool Check(string url)
        {
            var id  = Regex.Match(url, @"\?d=([^&$]+)").Groups[1].Value;
            var req = Utils.GetURL(Site + "mgr_linkcheck.php", "id0=" + id);
            
            return Regex.IsMatch(req, "&n=.+");
        }

        /// <summary>
        /// Determines whether this instance can check the availability of the link on the specified service.
        /// </summary>
        /// <param name="url">The link to check.</param>
        /// <returns>
        ///   <c>true</c> if this instance can check the specified service; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanCheck(string url)
        {
            return new Uri(url).Host.EndsWith("megaupload.com");
        }

        /// <summary>
        /// Tests the link checker.
        /// </summary>
        [Test]
        public override void Test()
        {
            var s1 = Check(Utils.Decrypt("fQvwj9uty5cwz66A5BiBp1b/pbu8Jsdd1662wCY6ZmdN8N7Bi/MUkYrdQSbTRxUX", Signature.Software));
            Assert.IsTrue(s1);

            var s2 = Check(Utils.Decrypt("fQvwj9uty5cwz66A5BiBp2ZYBN+KWGNxHdglcmNclXBWtxOkciTvaavfSaJ1sIvH", Signature.Software));
            Assert.IsFalse(s2);
        }
    }
}