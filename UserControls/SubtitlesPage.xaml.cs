﻿namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;

    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.ContextMenus;
    using RoliSoft.TVShowTracker.ContextMenus.Menus;
    using RoliSoft.TVShowTracker.Helpers;
    using RoliSoft.TVShowTracker.Parsers.Subtitles;
    using RoliSoft.TVShowTracker.TaskDialogs;
    using VistaControls.TaskDialog;

    /// <summary>
    /// Interaction logic for SubtitlesPage.xaml
    /// </summary>
    public partial class SubtitlesPage
    {
        /// <summary>
        /// Gets the search engines loaded in this application.
        /// </summary>
        /// <value>The search engines.</value>
        public static IEnumerable<SubtitleSearchEngine> SearchEngines
        {
            get
            {
                return Extensibility.GetNewInstances<SubtitleSearchEngine>()
                                    .OrderBy(engine => engine.Name);
            }
        }

        /// <summary>
        /// Gets the search engines activated in this application.
        /// </summary>
        /// <value>The search engines.</value>
        public static IEnumerable<SubtitleSearchEngine> ActiveSearchEngines
        {
            get
            {
                return SearchEngines.Where(engine => Actives.Contains(engine.Name));
            }
        }

        /// <summary>
        /// Gets or sets the list of activated parsers.
        /// </summary>
        /// <value>
        /// The activated parsers.
        /// </value>
        public static List<string> Actives { get; set; }

        /// <summary>
        /// Gets or sets the list of activated languages.
        /// </summary>
        /// <value>
        /// The activated languages.
        /// </value>
        public static List<string> ActiveLangs { get; set; }

        /// <summary>
        /// Gets or sets the subtitles list view item collection.
        /// </summary>
        /// <value>The subtitles list view item collection.</value>
        public ObservableCollection<SubtitleItem> SubtitlesListViewItemCollection { get; set; }

        /// <summary>
        /// Gets or sets the active search.
        /// </summary>
        /// <value>The active search.</value>
        public SubtitleSearch ActiveSearch { get; set; }

        private volatile bool _searching;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitlesPage"/> class.
        /// </summary>
        public SubtitlesPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes the <see cref="SubtitlesPage"/> class.
        /// </summary>
        static SubtitlesPage()
        {
            Actives     = Settings.Get<List<string>>("Active Subtitle Sites");
            ActiveLangs = Settings.Get<List<string>>("Active Subtitle Languages");
        }

        /// <summary>
        /// Sets the status message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="activity">if set to <c>true</c> an animating spinner will be displayed.</param>
        public void SetStatus(string message, bool activity = false)
        {
            Dispatcher.Invoke((Action)(() =>
                {
                    statusLabel.Content = message;

                    if (activity)
                    {
                        statusLabel.Padding = new Thickness(24, 0, 24, 0);
                        statusThrobber.Visibility = Visibility.Visible;
                        ((Storyboard)statusThrobber.FindResource("statusThrobberSpinner")).Begin();
                    }
                    else
                    {
                        ((Storyboard)statusThrobber.FindResource("statusThrobberSpinner")).Stop();
                        statusThrobber.Visibility = Visibility.Hidden;
                        statusLabel.Padding = new Thickness(7, 0, 7, 0);
                    }
                }));
        }

        /// <summary>
        /// Handles the Loaded event of the UserControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void UserControlLoaded(object sender, RoutedEventArgs e)
        {
            if (SubtitlesListViewItemCollection == null)
            {
                SubtitlesListViewItemCollection = new ObservableCollection<SubtitleItem>();
                listView.ItemsSource            = SubtitlesListViewItemCollection;

                appendLanguage.IsChecked = Settings.Get<bool>("Append Language to Subtitle");
            }

            if (availableEngines.Items.Count == 0)
            {
                foreach (var engine in SearchEngines)
                {
                    var mi = new MenuItem
                    {
                        Header           = new StackPanel { Orientation = Orientation.Horizontal },
                        IsCheckable      = true,
                        IsChecked        = Actives.Contains(engine.Name),
                        StaysOpenOnClick = true,
                        Tag              = engine.Name
                    };

                    (mi.Header as StackPanel).Children.Add(new Image
                        {
                            Source = new BitmapImage(engine.Icon != null ? new Uri(engine.Icon) : new Uri("/RSTVShowTracker;component/Images/navigation.png", UriKind.Relative), new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.CacheIfAvailable)),
                            Width  = 16,
                            Height = 16,
                            Margin = new Thickness(3, -2, 0, 0)
                        });
                    (mi.Header as StackPanel).Children.Add(new Label
                        {
                            Content = engine.Name,
                            Padding = new Thickness(5, 0, 0, 0)
                        });

                    mi.Checked   += SearchEngineMenuItemChecked;
                    mi.Unchecked += SearchEngineMenuItemUnchecked;

                    availableEngines.Items.Add(mi);
                }
            }

            if (languages.Items.Count == 0)
            {
                foreach (var lang in Languages.List)
                {
                    var mi = new MenuItem
                        {
                            Header           = new StackPanel { Orientation = Orientation.Horizontal },
                            IsCheckable      = true,
                            IsChecked        = ActiveLangs.Contains(lang.Key),
                            StaysOpenOnClick = true,
                            Tag              = lang.Key
                        };

                    (mi.Header as StackPanel).Children.Add(new Image
                        {
                            Source = new BitmapImage(new Uri("/RSTVShowTracker;component/Images/flag-" + lang.Key + ".png", UriKind.Relative)),
                            Width  = 16,
                            Height = 16,
                            Margin = new Thickness(3, -2, 0, 0)
                        });
                    (mi.Header as StackPanel).Children.Add(new Label
                        {
                            Content = lang.Value,
                            Padding = new Thickness(5, 0, 0, 0)
                        });

                    mi.Checked   += LanguageMenuItemChecked;
                    mi.Unchecked += LanguageMenuItemUnchecked;

                    languages.Items.Add(mi);
                }

                var mi2 = new MenuItem
                    {
                        Header           = new StackPanel { Orientation = Orientation.Horizontal },
                        IsCheckable      = true,
                        IsChecked        = ActiveLangs.Contains("null"),
                        StaysOpenOnClick = true,
                        Tag              = "null"
                    };

                (mi2.Header as StackPanel).Children.Add(new Image
                    {
                        Source = new BitmapImage(new Uri("/RSTVShowTracker;component/Images/unknown.png", UriKind.Relative)),
                        Width  = 16,
                        Height = 16,
                        Margin = new Thickness(3, -2, 0, 0)
                    });
                (mi2.Header as StackPanel).Children.Add(new Label
                    {
                        Content = "Unknown",
                        Padding = new Thickness(5, 0, 0, 0)
                    });

                mi2.Checked   += LanguageMenuItemChecked;
                mi2.Unchecked += LanguageMenuItemUnchecked;

                languages.Items.Add(mi2);
            }
        }

        /// <summary>
        /// Handles the Checked event of the SearchEngineMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchEngineMenuItemChecked(object sender, RoutedEventArgs e)
        {
            if (!Actives.Contains((sender as MenuItem).Tag as string))
            {
                Actives.Add((sender as MenuItem).Tag as string);

                Settings.Set("Active Subtitle Sites", Actives);
            }
        }

        /// <summary>
        /// Handles the Unchecked event of the SearchEngineMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchEngineMenuItemUnchecked(object sender, RoutedEventArgs e)
        {
            if (Actives.Contains((sender as MenuItem).Tag as string))
            {
                Actives.Remove((sender as MenuItem).Tag as string);

                Settings.Set("Active Subtitle Sites", Actives);
            }
        }

        /// <summary>
        /// Handles the Checked event of the LanguageMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void LanguageMenuItemChecked(object sender, RoutedEventArgs e)
        {
            if (!ActiveLangs.Contains((sender as MenuItem).Tag as string))
            {
                ActiveLangs.Add((sender as MenuItem).Tag as string);

                Settings.Set("Active Subtitle Languages", ActiveLangs);
            }
        }

        /// <summary>
        /// Handles the Unchecked event of the LanguageMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void LanguageMenuItemUnchecked(object sender, RoutedEventArgs e)
        {
            if (ActiveLangs.Contains((sender as MenuItem).Tag as string))
            {
                ActiveLangs.Remove((sender as MenuItem).Tag as string);

                Settings.Set("Active Subtitle Languages", ActiveLangs);
            }
        }

        /// <summary>
        /// Handles the Checked event of the filterResults control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void FilterResultsChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Filter Subtitles", true);
        }

        /// <summary>
        /// Handles the Unchecked event of the filterResults control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void FilterResultsUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Filter Subtitles", false);
        }

        /// <summary>
        /// Handles the Unchecked event of the appendLanguage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void AppendLanguageUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Append Language to Subtitle", false);
        }

        /// <summary>
        /// Handles the Checked event of the appendLanguage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void AppendLanguageChecked(object sender, RoutedEventArgs e)
        {
            Settings.Set("Append Language to Subtitle", true);
        }

        /// <summary>
        /// Initiates a search on this usercontrol.
        /// </summary>
        /// <param name="query">The query.</param>
        public void Search(string query)
        {
            Dispatcher.Invoke((Action)(() =>
                {
                    // cancel if one is running
                    if (_searching)
                    {
                        ActiveSearch.CancelAsync();
                        SubtitleSearchDone();
                    }

                    textBox.Text = query;
                    SearchButtonClick(null, null);
                }));
        }

        /// <summary>
        /// Handles the KeyUp event of the textBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.KeyEventArgs"/> instance containing the event data.</param>
        private void TextBoxKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SearchButtonClick(null, null);
            }
        }

        /// <summary>
        /// Handles the Click event of the searchButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchButtonClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text)) return;

            if (_searching)
            {
                ActiveSearch.CancelAsync();
                SubtitleSearchDone();
                return;
            }

            SubtitlesListViewItemCollection.Clear();

            textBox.IsEnabled    = false;
            searchButton.Content = "Cancel";

            ActiveSearch = new SubtitleSearch(ActiveSearchEngines, ActiveLangs, filterResults.IsChecked);

            ActiveSearch.SubtitleSearchDone          += SubtitleSearchDone;
            ActiveSearch.SubtitleSearchEngineNewLink += SubtitleSearchEngineNewLink;
            ActiveSearch.SubtitleSearchEngineDone    += SubtitleSearchEngineDone;
            ActiveSearch.SubtitleSearchEngineError   += SubtitleSearchEngineError;
            
            SetStatus("Searching for subtitles on " + (string.Join(", ", ActiveSearch.SearchEngines.Select(engine => engine.Name).ToArray())) + "...", true);

            _searching = true;

            ActiveSearch.SearchAsync(textBox.Text);

            Utils.Win7Taskbar(0, TaskbarProgressBarState.Normal);
        }
        
        /// <summary>
        /// Occurs when a subtitle search found a new link.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void SubtitleSearchEngineNewLink(object sender, EventArgs<Subtitle> e)
        {
            Dispatcher.Invoke((Action)(() =>
                {
                    lock (SubtitlesListViewItemCollection)
                    {
                        SubtitlesListViewItemCollection.Add(new SubtitleItem(e.Data));
                    }
                }));
        }

        /// <summary>
        /// Called when a subtitle search is done.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void SubtitleSearchEngineDone(object sender, EventArgs<List<SubtitleSearchEngine>> e)
        {
            if (!_searching)
            {
                return;
            }

            SetStatus("Searching for subtitles on " + (string.Join(", ", e.Data.Select(l => l.Name))) + "...", true);
            Utils.Win7Taskbar((int)((double)(ActiveSearch.SearchEngines.Count - e.Data.Count) / ActiveSearch.SearchEngines.Count * 100));
        }

        /// <summary>
        /// Called when a subtitle search is done on all engines.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void SubtitleSearchDone(object sender = null, EventArgs e = null)
        {
            if (!_searching)
            {
                return;
            }

            _searching   = false;
            ActiveSearch = null;

            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

            Dispatcher.Invoke((Action)(() =>
                {
                    textBox.IsEnabled    = true;
                    searchButton.Content = "Search";

                    if (SubtitlesListViewItemCollection.Count != 0)
                    {
                        SetStatus("Found " + Utils.FormatNumber(SubtitlesListViewItemCollection.Count, "subtitle") + "!");
                    }
                    else
                    {
                        SetStatus("Couldn't find any subtitles.");
                    }
                }));
        }

        /// <summary>
        /// Called when the subtitle search has encountered an unexpected error.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        public void SubtitleSearchEngineError(object sender, EventArgs<string, Exception> e)
        {
            if (e.Second is WebException || e.Second is CookComputing.XmlRpc.XmlRpcException)
            {
                // Exceptions such as
                // - The operation has timed out
                // - The remote server returned an error: (503) Server Unavailable.
                // - Unable to connect to the remote server
                // - Service Not Available (seen in XML-RPC, usually when OpenSubtitles is on vacation)
                // indicate that the server is either unreachable or is under heavy load,
                // and these problems are not parsing bugs, so they will be ignored.
                return;
            }

            MainWindow.Active.HandleUnexpectedException(e.Second);
        }

        /// <summary>
        /// Handles the ContextMenuOpening event of the listView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.ContextMenuEventArgs"/> instance containing the event data.</param>
        private void ListViewContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = true;

            if (listView.SelectedIndex == -1) return;

            var cm = new ContextMenu();
            ((FrameworkElement)e.Source).ContextMenu = cm;
            var sub = (Subtitle)listView.SelectedValue;

            var dls    = new MenuItem();
            dls.Header = "Download subtitle";
            dls.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/torrents.png")) };
            dls.Click += DownloadSubtitleClick;
            cm.Items.Add(dls);

            var dnv    = new MenuItem();
            dnv.Header = "Download subtitle near video";
            dnv.Icon   = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/RSTVShowTracker;component/Images/inbox-download.png")) };
            dnv.Click += DownloadSubtitleNearVideoClick;
            cm.Items.Add(dnv);

            foreach (var dlcm in Extensibility.GetNewInstances<SubtitleContextMenu>())
            {
                foreach (var dlcmi in dlcm.GetMenuItems(sub))
                {
                    var cmi    = new MenuItem();
                    cmi.Tag    = dlcmi;
                    cmi.Header = dlcmi.Name;
                    cmi.Icon   = dlcmi.Icon;
                    cmi.Click += (s, r) => ((ContextMenuItem<Subtitle>)cmi.Tag).Click(sub);
                    cm.Items.Add(cmi);
                }
            }

            TextOptions.SetTextFormattingMode(cm, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(cm, TextRenderingMode.ClearType);
            RenderOptions.SetBitmapScalingMode(cm, BitmapScalingMode.HighQuality);

            cm.IsOpen = true;
        }

        /// <summary>
        /// Handles the Click event of the DownloadSubtitle control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DownloadSubtitleClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var sub = (Subtitle)listView.SelectedValue;

            new SubtitleDownloadTaskDialog().Download(sub);
        }

        /// <summary>
        /// Handles the Click event of the DownloadSubtitle control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DownloadSubtitleNearVideoClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var sub = (Subtitle)listView.SelectedValue;

            try
            {
                var sf = FileNames.Parser.ParseFile(textBox.Text, null, false);

                if (sf.Success)
                {
                    var ep = sf.GetDatabaseEquivalent();

                    if (ep != null)
                    {
                        new SubtitleDownloadTaskDialog().DownloadNearVideo(sub, ep);
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else
                {
                    throw new Exception();
                }
            }
            catch
            {
                new TaskDialog
                    {
                        CommonIcon  = TaskDialogIcon.Stop,
                        Title       = "Mapping error",
                        Instruction = textBox.Text.Trim(),
                        Content     = "The query you've entered could not be mapped to an episode in the database. If this show is not on your list, add it, so the file search engine can find it."
                    }.Show();
            }
        }
    }
}
