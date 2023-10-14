using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media.Animation;
using System.IO;

namespace guardian_wpf
{
    public partial class MainWindow : Window
    {
        //Button which reloades section.
        Button checkNetworkStatusBtn = null;
        //Information about error.
        TextBlock errorInfoTB = null;
        //page in guardianapis data.
        int page = 1;
        //Stores articles which are in section.
        List<Results> sectionArticles = new List<Results>();
        //Store old articles (important during animation of loading photos of articles previews).
        List<Results> lastItemSource = new List<Results>();
        ///<summary>Gets articles data for corresponding section.</summary>
        async Task GetArticles()
        {
            if (spinner != null)
            {
                mainScrollViewer.IsEnabled = false;
            }

            var current_section = System.IO.File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "current_section.txt"));
            var navbarBtn = (Button)this.FindName(current_section);

            var part1 = "https://content.guardianapis.com/search?section=";
            var part2AndApiKey = "&page-size=20&api-key=dcc2192a-81e0-45c4-b847-322e8da0ffac";
            var part3 = "&show-fields=thumbnail,body,trailText&show-tags=contributor";

            var client = new HttpClient();
            if (!isAllBtnsEnable)
            {
                noResults.Visibility = Visibility.Hidden;
                navbarBtn.IsEnabled = false;

                switch (navbarBtn.Name)
                {
                    case "navbarBtn1":
                        await GetResponse($"{part1}business&page={page}{part2AndApiKey}{part3}");
                        break;
                    case "navbarBtn2":
                        await GetResponse($"{part1}law&page={page}{part2AndApiKey}{part3}");
                        break;
                    case "navbarBtn3":
                        await GetResponse($"{part1}commentisfree&page={page}{part2AndApiKey}{part3}");
                        break;
                    case "navbarBtn4":
                        await GetResponse($"{part1}politics&page={page}{part2AndApiKey}{part3}");
                        break;
                    case "navbarBtn5":
                        await GetResponse($"{part1}lifeandstyle&page={page}&subsection=health-and-wellbeing{part2AndApiKey}{part3}");
                        break;
                }
            }
            else
            {
                await GetResponse($"https://content.guardianapis.com/search?q={searchQuery}&page={page}{part2AndApiKey}{part3}");
            }

            async Task GetResponse(string link)
            {
                var client = new HttpClient();
                try
                {
                    var response = await client.GetAsync(link);
                    ConfigureArticles(response);

                    Main.Children.Remove(checkNetworkStatusBtn);
                    Main.Children.Remove(errorInfoTB);
                    checkNetworkStatusBtn = null;
                }
                //If internet connection was lost.
                catch (Exception e)
                {
                    if (e.Message.Contains("443"))
                    {
                        if (checkNetworkStatusBtn == null)
                        {
                            MouseButtonEventHandler func = ReloadSection;

                            TryLoadAgain(func);

                            async void ReloadSection(object sender, MouseButtonEventArgs e)
                            {
                                await GetResponse(link);
                                mainScrollViewer.Visibility = Visibility.Visible;
                            }
                        }
                    }
                    else
                    {
                        HttpResponseMessage response = null;
                        ConfigureArticles(response);
                    }
                }
            }
        }

        /// <summary>Builds <see cref="List"/> sectionArticles and truncate titles of articles if it's need.</summary>
        async Task ConfigureArticles(HttpResponseMessage response)
        {
            if (page == 1) { mainScrollViewer.ScrollToTop(); }

            Response articles = null;
            if (response != null)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                articles = JsonConvert.DeserializeObject<Response>(responseData);
            }

            noResults.Visibility = Visibility.Hidden;

            //Truncate title of article if title consist of more than 15 words.
            articles = TruncateTitle(articles);

            foreach (var article in articles.response.results)
            {
                sectionArticles.Add(article);
            }

            if (articles.response.results.Count() == 0) { noResults.Visibility = Visibility.Visible; }

            if (sectionArticles.Count > 20)
            {
                for (int y = 0; y < sectionArticles.Count - 20; y++)
                {
                    lastItemSource.Add(sectionArticles.ElementAt(y));
                }
            }


            articlesPanel.ItemsSource = null;
            articlesPanel.ItemsSource = sectionArticles;

            //--- Basic conditions to show articles section. ---
            MainPlaceHolder.Visibility = Visibility.Hidden;
            WrapPanelArticles.Visibility = Visibility.Visible;
            //--------------------------------------------------

            Response TruncateTitle(Response articles)
            {
                foreach (var article in articles.response.results)
                {
                    try
                    {
                        article.truncatedWebTitle = article.webTitle;

                        var authorIndex = article.webTitle.IndexOf("|");

                        if (authorIndex != -1)
                        {
                            article.truncatedWebTitle = article.webTitle = article.webTitle.Substring(0, authorIndex);
                        }

                        var titleSplit = article.truncatedWebTitle.Split(" ");
                        string articleTitle = "";

                        if (titleSplit.Length > 15)
                        {
                            if (titleSplit.Length != 16)
                            {
                                articleTitle = article.truncatedWebTitle.Substring(0, article.webTitle.IndexOf(titleSplit.ElementAt(15)) + titleSplit.ElementAt(15).Length);
                            }
                            else
                            {
                                articleTitle = article.truncatedWebTitle;
                            }

                            var articleTitleFinal = String.Format($"{articleTitle}...");
                            article.truncatedWebTitle = articleTitleFinal;
                        }
                    }
                    catch (Exception e) { }
                }
                return articles;
            }
        }

        //Loading icon for section.
        Border spinner = null;
        /// <summary>Loads another articles when scrolled to the bottom boundary of main page.</summary>
        private void LoadAnotherArticles(object sender, ScrollChangedEventArgs e)
        {
            //If main site is scrolled to down, previous articles are loaded and spinner isn't added.
            if (e.VerticalOffset == mainScrollViewer.ScrollableHeight && newSection == false && spinner == null)
            {
                //Change number of page. Important during downloading data from api.
                page++;

                //Creates animated refresh icon.
                spinner = CreateRefreshIcon(667.5);

                //Adds refresh icon on the bottom of articles' tiles.
                WrapPanelArticles.Children.Add(spinner);

                //Creates animated move of refresh icon from down to top.
                var translateTransform = new TranslateTransform();
                spinner.RenderTransform = translateTransform;
                var animation = new DoubleAnimation()
                { From = 90, To = 0, Duration = TimeSpan.FromSeconds(0.3) };
                translateTransform.BeginAnimation(TranslateTransform.YProperty, animation);

                //Timer for animated move after which it's deleted.
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.3) };
                timer.Tick += DeleteSpinner;
                timer.Start();

                async void DeleteSpinner(object sender, EventArgs e)
                {
                    GetArticles();
                    timer.Stop();
                }
            }
            //If first articles of section are loading.
            else
            {
                newSection = false;
            }
        }

        //Prevents loading of second section page while section is just opening.
        bool newSection = true;
        ///<summary>Changes section on the main site.</summary>
        private async void ChangeSection(object sender, RoutedEventArgs e)
        {
            Main.Children.Remove(checkNetworkStatusBtn);
            Main.Children.Remove(errorInfoTB);
            checkNetworkStatusBtn = null;

            isAllBtnsEnable = false;

            //Removes loading icon of section if during change of section were loading articles in other section.
            if (spinner != null)
            {
                WrapPanelArticles.Children.Remove(spinner);
                spinner = null;
            }

            MainPlaceHolder.Visibility = Visibility.Visible;

            //Checks last chosen section.
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "current_section.txt");
            var CurrentSection = System.IO.File.ReadAllText(path);

            for (int i = 1; i < 6; i++)
            {
                var btn = (Button)this.FindName(String.Format($"navbarBtn{i}"));
                btn.IsEnabled = true;
            }

            var sectionBtn = (Button)sender;
            sectionBtn.IsEnabled = false;

            //Sets data about chosen section.
            System.IO.File.WriteAllText(path, sectionBtn.Name);

            //--- Basic conditions to load section. ---
            articlesPanel.ItemsSource = null;
            lastItemSource.Clear();
            sectionArticles.Clear();
            mainScrollViewer.ScrollToVerticalOffset(0);
            page = 1;
            newSection = true;
            //----------------------------------------

            GetArticles();
        }

        ///<summary>Loads and animates article preview image placeholder.</summary>
        private void LoadedArticlePreview(object sender, RoutedEventArgs e)
        {
            //Tile of article.
            var rectangle = (System.Windows.Shapes.Rectangle)sender;
            //Image of article preview.
            var articleImage = (ImageBrush)rectangle.FindName("articleImage");
            //Placeholder of image of article preview.
            var imagePlaceHolder = (System.Windows.Shapes.Rectangle)rectangle.FindName("imagePlaceHolder");

            //If article preview hasn't thumbnail.
            if (articleImage.ImageSource == null)
            {
                //Determines if opacity of imagePlaceHolder is 1 (max).
                bool isFull = true;

                var timer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(0.05) };
                timer.Tick += ChangeOpacity;
                timer.Start();

                //When article preview hasn't thumbnail. (Animation)
                void ChangeOpacity(object sender, EventArgs e)
                {
                    if (articleImage.ImageSource == null)
                    {
                        if (imagePlaceHolder.Opacity < 0.95 && isFull == false)
                        {
                            imagePlaceHolder.Opacity += 0.05;
                        }
                        else if (isFull == false)
                        {
                            imagePlaceHolder.Opacity += 0.05;
                            isFull = true;
                        }
                        else if (imagePlaceHolder.Opacity > 0.05 && isFull == true)
                        {
                            imagePlaceHolder.Opacity -= 0.05;
                        }
                        else
                        {
                            imagePlaceHolder.Opacity -= 0.05;
                            isFull = false;
                        }
                    }
                }
            }
            //If article preview thumbnail was loaded before current page which is loading.
            else if (lastItemSource.Count != 0 && lastItemSource.FirstOrDefault(x => x.fields.thumbnail == articleImage.ImageSource.ToString()) != null)
            {
                imagePlaceHolder.Visibility = Visibility.Hidden;
            }
            //If article preview thumbnail is loading for first time.
            else
            {
                //Determines if opacity of imagePlaceHolder is 1 (max).
                bool isFull = true;

                var timer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(0.05) };
                timer.Tick += ChangeOpacity;
                timer.Start();

                //Animation of loading article preview thumbnail
                void ChangeOpacity(object sender, EventArgs e)
                {

                    if (imagePlaceHolder.Opacity != 0.05 && isFull == true)
                    {
                        imagePlaceHolder.Opacity -= 0.05;
                    }
                    else
                    {
                        imagePlaceHolder.Opacity -= 0.05;
                        isFull = false;

                        //Prevents showing placeholder on loaded images.
                        imagePlaceHolder.Visibility = Visibility.Hidden;
                    }
                }
            }
        }

        /// <summary>When size of WrapPanelArticlesSection changed, after loading another articles in section, removes animation of loading spinner.</summary>
        private void SizeChangedArticlesPanel(object sender, SizeChangedEventArgs e)
        {
            if (spinner != null)
            {
                WrapPanelArticles.Children.Remove(spinner);
                mainScrollViewer.IsEnabled = true;
                spinner = null;
            }
        }
    }
}
