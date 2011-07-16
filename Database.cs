﻿namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Data.SQLite;
    using System.IO;
    using System.Linq;

    using RoliSoft.TVShowTracker.Tables;

    using DictList = System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>>;

    /// <summary>
    /// Provides access to the default database.
    /// </summary>
    public static class Database
    {
        /// <summary>
        /// Gets or sets the active connection.
        /// </summary>
        /// <value>The active connection.</value>
        public static SQLiteConnection Connection { get; set; }

        /// <summary>
        /// Gets or sets the date when the data was last changed. This field is used for caching purposes, and it's not automatically updated by <c>Execute()</c>.
        /// </summary>
        /// <value>The date of last change.</value>
        public static DateTime DataChange { get; set; }

        /// <summary>
        /// Gets the version of the database structure.
        /// </summary>
        /// <value>The version.</value>
        public static int Version
        {
            get
            {
                return 2;
            }
        }

        /// <summary>
        /// Gets or sets the contents of the TV shows table in the database.
        /// </summary>
        /// <value>
        /// The contents of the TV shows table in the database.
        /// </value>
        public static List<TVShow> TVShows { get; set; }

        /// <summary>
        /// Gets or sets the contents of the TV show key-value store table in the database.
        /// </summary>
        /// <value>
        /// The contents of the TV show key-value store table in the database.
        /// </value>
        public static List<ShowData> ShowDatas { get; set; } 

        /// <summary>
        /// Gets or sets the contents of the episodes table in the database.
        /// </summary>
        /// <value>
        /// The contents of the episodes table in the database.
        /// </value>
        public static List<Episode> Episodes { get; set; }

        /// <summary>
        /// Gets or sets the contents of the episode tracking table in the database.
        /// </summary>
        /// <value>
        /// The contents of the episode tracking table in the database.
        /// </value>
        public static List<int> Trackings { get; set; }

        private static readonly string _dbFile;

        /// <summary>
        /// Initializes the <see cref="Database"/> class.
        /// </summary>
        static Database()
        {
            if (string.IsNullOrWhiteSpace(Signature.FullPath))
            {
                return;
            }

            _dbFile = Path.Combine(Signature.FullPath, "TVShows.db3");

            // hackity-hack :)
            if (Environment.MachineName == "ROLISOFT-PC" && File.Exists(Path.Combine(Signature.FullPath, ".hack")))
            {
                _dbFile = @"C:\Users\RoliSoft\Documents\Visual Studio 2010\Projects\RS TV Show Tracker\RS TV Show Tracker\TVShows.db3";
            }

            var exists = File.Exists(_dbFile);

            Connection = new SQLiteConnection("Data Source=" + _dbFile);
            DataChange = DateTime.Now;
            
            Connection.Open();

            // if this is a new database, run table creations

            if (!exists)
            {
                var tables = Properties.Resources.DatabaseStructure.Split(new[]{ "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var table in tables)
                {
                    Execute(table);
                }
            }

            // if this is an older database, upgrade

            var sver = Setting("Version");
            var ver  = !string.IsNullOrWhiteSpace(sver)
                       ? sver.ToInteger()
                       : 0;

            // -> revision 9bec72227f611e4283cbdc24357c83a23b5798a9
            if (ver == 0)
            {
                Execute("alter table episodes add column url TEXT");

                ver++;
                Setting("Version", ver.ToString());
            }

            if (ver == 1)
            {
                Execute("alter table tvshows add column release TEXT");

                ver++;
                Setting("Version", ver.ToString());
            }

            // copy the database to the memory
            CopyToMemory();
        }

        /// <summary>
        /// Copies the contents of the SQLite database to the memory.
        /// </summary>
        public static void CopyToMemory()
        {
            TVShows   = GetTVShows();
            Trackings = GetTrackings();
            Episodes  = GetEpisodes(true);
        }

        /// <summary>
        /// Queries the SQL database.
        /// </summary>
        /// <param name="sql">The SQL query.</param>
        /// <param name="args">The arguments in the SQL query.</param>
        /// <returns>List of dictionary of key-value.</returns>
        public static DictList Query(string sql, params object[] args)
        {
            using (var cmd = new SQLiteCommand(sql, Connection))
            {
                if (args.Length != 0)
                {
                    foreach (var arg in args)
                    {
                        cmd.Parameters.Add(new SQLiteParameter { Value = arg != null ? arg.ToString() : string.Empty });
                    }
                }

                lock (Connection)
                {
                    using (var dr = cmd.ExecuteReader())
                    {
                        var al = new DictList();

                        while (dr.Read())
                        {
                            var dic = new Dictionary<string, string>();

                            for (var i = 0; i != dr.FieldCount; i++)
                            {
                                dic[dr.GetName(i)] = dr[i].ToString();
                            }

                            al.Add(dic);
                        }

                        return al;
                    }
                }
            }
        }

        /// <summary>
        /// Executes an SQL statement.
        /// </summary>
        /// <param name="sql">The SQL statement.</param>
        /// <param name="args">The arguments in the SQL statement.</param>
        /// <returns>Number of changed rows.</returns>
        public static int Execute(string sql, params object[] args)
        {
            using (var cmd = new SQLiteCommand(sql, Connection))
            {
                if (args.Length != 0)
                {
                    foreach (var arg in args)
                    {
                        cmd.Parameters.Add(new SQLiteParameter { Value = arg != null ? arg.ToString() : string.Empty });
                    }
                }

                lock (Connection)
                {
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Executes an SQL statement on a transaction.
        /// </summary>
        /// <param name="transaction">The open transaction.</param>
        /// <param name="sql">The SQL statement.</param>
        /// <param name="args">The arguments in the SQL statement.</param>
        /// <returns>Number of changed rows.</returns>
        public static int ExecuteOnTransaction(SQLiteTransaction transaction, string sql, params object[] args)
        {
            using(var cmd = new SQLiteCommand(sql, Connection, transaction))
            {
                if (args.Length != 0)
                {
                    foreach (var arg in args)
                    {
                        cmd.Parameters.Add(new SQLiteParameter { Value = arg != null ? arg.ToString() : string.Empty });
                    }
                }

                lock (Connection)
                {
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Downloads all the TV shows from the database.
        /// </summary>
        /// <returns>List of TV shows.</returns>
        public static List<TVShow> GetTVShows()
        {
            lock (Connection)
            {
                using (var cmd = new SQLiteCommand("select rowid, showid, name, release from tvshows order by rowid asc", Connection))
                using (var dr  = cmd.ExecuteReader())
                {
                    var shows = new List<TVShow>();

                    while (dr.Read())
                    {
                        try
                        {
                            var show = new TVShow();

                            show.RowID  = dr.GetInt32(0);
                            show.ShowID = dr.GetInt32(1);
                            show.Name   = dr.GetString(2);

                            if (!(dr[3] is DBNull))
                                show.Release = dr.GetString(3);

                            shows.Add(show);
                        }
                        catch
                        {
                        }
                    }

                    return shows;
                }
            }
        }

        /// <summary>
        /// Downloads all the episodes from the database.
        /// </summary>
        /// <param name="setWatched">if set to <c>true</c> the value of the <c>Watched</c> will be fetched from the <c>Database.Trackings</c> list.</param>
        /// <returns>
        /// List of episodes.
        /// </returns>
        public static List<Episode> GetEpisodes(bool setWatched = false)
        {
            lock (Connection)
            {
                using (var cmd = new SQLiteCommand("select episodeid, showid, season, episode, airdate, name, descr, pic, url from episodes", Connection))
                using (var dr  = cmd.ExecuteReader())
                {
                    var episodes = new List<Episode>();

                    while (dr.Read())
                    {
                        try
                        {
                            var episode = new Episode();

                            episode.EpisodeID = dr.GetInt32(0);
                            episode.ShowID    = dr.GetInt32(1);
                            episode.Season    = dr.GetInt32(2);
                            episode.Number    = dr.GetInt32(3);

                            if (setWatched)
                                episode.Watched = Trackings.Contains(episode.EpisodeID);

                            if (!(dr[4] is DBNull))
                                episode.Airdate = Extensions.GetUnixTimestamp(dr.GetInt32(4));

                            if (!(dr[5] is DBNull))
                                episode.Name = dr.GetString(5);

                            if (!(dr[6] is DBNull))
                                episode.Description = dr.GetString(6);

                            if (!(dr[7] is DBNull))
                                episode.Picture = dr.GetString(7);

                            if (!(dr[8] is DBNull))
                                episode.URL = dr.GetString(8);

                            episodes.Add(episode);
                        }
                        catch
                        {
                        }
                    }

                    return episodes;
                }
            }
        }

        /// <summary>
        /// Downloads all the episode tracking information from the database.
        /// </summary>
        /// <returns>List of episode trackings.</returns>
        public static List<int> GetTrackings()
        {
            lock (Connection)
            {
                using (var cmd = new SQLiteCommand("select showid, episodeid from tracking", Connection))
                using (var dr  = cmd.ExecuteReader())
                {
                    var trackings = new List<int>();

                    while (dr.Read())
                    {
                        try { trackings.Add(dr.GetInt32(1)); } catch { }
                    }

                    return trackings;
                }
            }
        }

        /// <summary>
        /// Retrieves the key from the SQL settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Stored value or empty string.</returns>
        public static string Setting(string key)
        {
            using (var cmd = new SQLiteCommand("select value from settings where key = ?", Connection))
            {
                cmd.Parameters.Add(new SQLiteParameter { Value = key });

                lock (Connection)
                {
                    using (var dr = cmd.ExecuteReader())
                    {
                        return dr.Read()
                               ? dr["value"].ToString()
                               : null;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the key from the show data table.
        /// </summary>
        /// <param name="id">The id of the show.</param>
        /// <param name="key">The key.</param>
        /// <returns>Stored value or empty string.</returns>
        public static string ShowData(string id, string key)
        {
            using (var cmd = new SQLiteCommand("select value from showdata where showid = ? and key = ?", Connection))
            {
                cmd.Parameters.Add(new SQLiteParameter { Value = id  });
                cmd.Parameters.Add(new SQLiteParameter { Value = key });

                lock (Connection)
                {
                    using (var dr = cmd.ExecuteReader())
                    {
                        return dr.Read()
                               ? dr["value"].ToString()
                               : string.Empty;
                    }
                }
            }
        }

        /// <summary>
        /// Stores the key and value into the SQL settings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static int Setting(string key, string value)
        {
            using (var cmd = new SQLiteCommand("insert into settings values (?, ?)", Connection))
            {
                cmd.Parameters.Add(new SQLiteParameter { Value = key   });
                cmd.Parameters.Add(new SQLiteParameter { Value = value });

                lock (Connection)
                {
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Stores the key and value into the show data table.
        /// </summary>
        /// <param name="id">The id of the show.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static int ShowData(string id, string key, string value)
        {
            var data = (id + "|" + key).GetHashCode();

            using (var cmd = new SQLiteCommand("insert into showdata values (?, ?, ?, ?)", Connection))
            {
                cmd.Parameters.Add(new SQLiteParameter { Value = data  });
                cmd.Parameters.Add(new SQLiteParameter { Value = id    });
                cmd.Parameters.Add(new SQLiteParameter { Value = key   });
                cmd.Parameters.Add(new SQLiteParameter { Value = value });

                lock (Connection)
                {
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Gets the ID of a show in the database.
        /// </summary>
        /// <param name="show">The name of the show.</param>
        /// <returns>ID of the show or empty string.</returns>
        public static string GetShowID(string show)
        {
            var showid = Query("select showid from tvshows where name = ? limit 1", show);

            return showid.Count != 0
                   ? showid[0]["showid"]
                   : string.Empty;
        }

        /// <summary>
        /// Gets the ID of a show in the database.
        /// </summary>
        /// <param name="show">The name of the show.</param>
        /// <param name="grabber">The grabber.</param>
        /// <param name="grabberId">The grabber id.</param>
        /// <param name="grabberLang">The grabber language.</param>
        /// <returns>ID of the show or empty string.</returns>
        public static string GetShowID(string show, string grabber, string grabberId, string grabberLang)
        {
            var query  = Query("select showid, (select value from showdata where showdata.showid = tvshows.showid and key = 'grabber') as grabber from tvshows where name = ?", show);
            var filter = query.Where(sh => sh["grabber"] == grabber && ShowData(sh["showid"], sh["grabber"] + ".id") == grabberId && ShowData(sh["showid"], sh["grabber"] + ".lang") == grabberLang).ToList();

            return filter.Count != 0
                   ? filter[0]["showid"]
                   : string.Empty;
        }

        /// <summary>
        /// Gets the ID of an episode in the database.
        /// </summary>
        /// <param name="show">The name of the show.</param>
        /// <param name="season">The season of the episode.</param>
        /// <param name="episode">The number of the episode.</param>
        /// <returns>ID of the show or empty string.</returns>
        public static string GetEpisodeID(string show, int season, int episode)
        {
            var episodeid = Query("select episodeid from episodes where showid = ? and season = ? and episode = ? limit 1", show, season, episode);

            return episodeid.Count != 0
                   ? episodeid[0]["episodeid"]
                   : string.Empty;
        }

        /// <summary>
        /// Gets the name of the show used in scene releases.
        /// </summary>
        /// <param name="show">The name of the show.</param>
        /// <param name="removeCommon">
        /// if set to <c>true</c> "and", "the", "of", and any one character word will be removed,
        /// otherwise, only "the" and any one character word that is other than "a" will be removed.
        /// </param>
        /// <returns>Name of the show used in scene releases.</returns>
        public static string[] GetReleaseName(string show, bool removeCommon = true)
        {
            var release = Query("select release from tvshows where name = ? limit 1", show);

            return release.Count != 0 && !string.IsNullOrWhiteSpace(release[0]["release"])
                   ? release[0]["release"].Split(' ')
                   : ShowNames.Parser.GetRoot(show, removeCommon);
        }
    }
}
