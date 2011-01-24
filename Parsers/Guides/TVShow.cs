﻿namespace RoliSoft.TVShowTracker.Parsers.Guides
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a TV show in the guide.
    /// </summary>
    public class TVShow
    {
        /// <summary>
        /// Gets or sets the title of the show.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the genre of the show, comma separated if multiple are provided.
        /// </summary>
        /// <value>The genre.</value>
        public string Genre { get; set; }

        /// <summary>
        /// Gets or sets the description of the show.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the URL location of a DVD cover.
        /// </summary>
        /// <value>The URL of the cover.</value>
        public string Cover { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="TVShow"/> is airing.
        /// </summary>
        /// <value><c>true</c> if airing; otherwise, <c>false</c>.</value>
        public bool Airing { get; set; }

        /// <summary>
        /// Gets or sets the time when the show is aired usually.
        /// </summary>
        /// <value>The air time.</value>
        public string AirTime { get; set; }

        /// <summary>
        /// Gets or sets the day when the show is aired usually.
        /// </summary>
        /// <value>The air day.</value>
        public string AirDay { get; set; }

        /// <summary>
        /// Gets or sets the name of the network on which the show is aired.
        /// </summary>
        /// <value>The network.</value>
        public string Network { get; set; }

        /// <summary>
        /// Gets or sets the length of an episode.
        /// </summary>
        /// <value>The runtime.</value>
        public int Runtime { get; set; }

        /// <summary>
        /// Gets or sets the episode list.
        /// </summary>
        /// <value>The episode list.</value>
        public List<Episode> Episodes { get; set; }

        /// <summary>
        /// Represents an episode in a TV show.
        /// </summary>
        public class Episode
        {
            /// <summary>
            /// Gets or sets the season number.
            /// </summary>
            /// <value>The season.</value>
            public int Season { get; set; }

            /// <summary>
            /// Gets or sets the episode number.
            /// </summary>
            /// <value>The episode.</value>
            public int Number { get; set; }

            /// <summary>
            /// Gets or sets the date when the episode was first aired.
            /// </summary>
            /// <value>The air date.</value>
            public DateTime Airdate { get; set; }

            /// <summary>
            /// Gets or sets the title of the episode.
            /// </summary>
            /// <value>The title.</value>
            public string Title { get; set; }

            /// <summary>
            /// Gets or sets the summary of the episode.
            /// </summary>
            /// <value>The summary.</value>
            public string Summary { get; set; }

            /// <summary>
            /// Gets or sets the URL to a screen capture of the episode.
            /// </summary>
            /// <value>The URL to the screen capture.</value>
            public string Picture { get; set; }
        }
    }
}
