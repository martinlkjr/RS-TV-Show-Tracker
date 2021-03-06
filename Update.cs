﻿namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using RoliSoft.TVShowTracker.Parsers.Guides;
    using RoliSoft.TVShowTracker.Parsers.Guides.Engines;
    using RoliSoft.TVShowTracker.Remote;

    /// <summary>
    /// Provides methods to keep the database up-to-date.
    /// </summary>
    public class Updater
    {
        /// <summary>
        /// Gets or sets a value indicating whether an update is currently in progress.
        /// </summary>
        /// <value>
        ///   <c>true</c> if an update is currently in progress; otherwise, <c>false</c>.
        /// </value>
        public static bool InProgress { get; set; }

        /// <summary>
        /// Occurs when the update is done.
        /// </summary>
        public event EventHandler<EventArgs> UpdateDone;

        /// <summary>
        /// Occurs when the update has encountered an error.
        /// </summary>
        public event EventHandler<EventArgs<string, Exception, bool, bool>> UpdateError;

        /// <summary>
        /// Occurs when the progress has changed on the update.
        /// </summary>
        public event EventHandler<EventArgs<string, double>> UpdateProgressChanged;

        /// <summary>
        /// Does the update.
        /// </summary>
        public void Update()
        {
            InProgress = true;

            var i = 0d;
            var cnt = Database.TVShows.Values.Count(s => s.Airing);
            var ids = Database.TVShows.Values.Where(s => s.Airing).OrderBy(s => s.Title).Select(s => s.ID).ToList();

            foreach (var id in ids)
            {
                UpdateProgressChanged.Fire(this, Database.TVShows[id].Title, ++i / cnt * 100);

                var tv = Database.Update(Database.TVShows[id], (e, s) =>
                    {
                        if (e == -1)
                        {
                            UpdateError.Fire(this, s, null, true, false);
                        }
                    });

                if (tv != null && tv.Language == "en")
                {
                    UpdateRemoteCache(tv);
                }
            }

            Database.Setting("update", DateTime.Now.ToUnixTimestamp().ToString());
            MainWindow.Active.DataChanged();
            UpdateDone.Fire(this);

            InProgress = false;
        }

        /// <summary>
        /// Does the update asynchronously.
        /// </summary>
        public void UpdateAsync()
        {
            Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Update();
                    }
                    catch (Exception ex)
                    {
                        UpdateError.Fire(this, "The update function has quit with an exception.", ex, false, true);
                    }
                });
        }

        /// <summary>
        /// Creates the guide object from name.
        /// </summary>
        /// <param name="name">The name of the class.</param>
        /// <returns>New guide class.</returns>
        /// <exception cref="NotSupportedException">When a name is specified which is not supported.</exception>
        public static Guide CreateGuide(string name)
        {
            switch (name)
            {
                case "TVRage":
                    return new TVRage();

                case "TVDB":
                    return new TVDB();

                case "TMDb":
                    return new TMDb();

                case "Freebase":
                    return new Freebase();

                case "TVcom":
                    return new TVcom();

                case "EPisodeWorld":
                    return new EPisodeWorld();

                case "IMDb":
                    return new IMDb();

                case "EPGuides":
                    return new EPGuides();

                case "AniDB":
                    return new AniDB();

                case "AnimeNewsNetwork":
                    return new AnimeNewsNetwork();

                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Sends metadata information about the TV show into lab.rolisoft.net cache.
        /// </summary>
        /// <param name="tv">The TV show.</param>
        public static void UpdateRemoteCache(TVShow tv)
        {
            Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var info = new Remote.Objects.ShowInfo
                            {
                                Title       = tv.Title,
                                Description = tv.Description,
                                Genre       = tv.Genre,
                                Cover       = tv.Cover,
                                Started     = (long)tv.Episodes[0].Airdate.ToUnixTimestamp(),
                                Airing      = tv.Airing,
                                AirTime     = tv.AirTime,
                                AirDay      = tv.AirDay,
                                Network     = tv.Network,
                                Runtime     = tv.Runtime,
                                Seasons     = tv.Episodes.Last().Season,
                                Episodes    = tv.Episodes.Count,
                                Source      = tv.Source,
                                SourceID    = tv.SourceID
                            };

                        var hash = BitConverter.ToString(new HMACSHA256(Encoding.ASCII.GetBytes(Utils.GetUUID() + "\0" + Signature.Version)).ComputeHash(Encoding.UTF8.GetBytes(
                            info.Title + info.Description + string.Join(string.Empty, info.Genre) + info.Cover + info.Started + (info.Airing ? "true" : "false") + info.AirTime + info.AirDay + info.Network + info.Runtime + info.Seasons + info.Episodes + info.Source + info.SourceID
                        ))).ToLower().Replace("-", string.Empty);
                
                        API.SetShowInfo(info, hash);
                    } catch { }
                });
        }
    }
}
