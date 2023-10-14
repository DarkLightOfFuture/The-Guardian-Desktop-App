using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;
using System;

namespace guardian_wpf
{
    public partial class MainWindow : Window
    {
        //-----Methods related with program's windows actions.-----

        ///<summary>Closes the program.</summary>
        private void CloseFunc(object sender, RoutedEventArgs e)
        {
            Close();
        }
        ///<summary>Minimizes the window of program.</summary>
        private void MinimizeFunc(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        ///<summary>Drags the program.</summary>
        private void DragFunc(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }


        //----------------Search bar methods.----------------

        //If query button section on search bar is chosen.
        bool isQueryBtnChosen = true;
        //If section button section on search bar is chosen.
        bool isSectionBtnChosen = false;
        private void QueryBtnClick(object sender, RoutedEventArgs e)
        {
            isQueryBtnChosen = sectionBtn.IsEnabled = true;

            isSectionBtnChosen = queryBtn.IsEnabled = false;
        }

        private void SectionBtnClick(object sender, RoutedEventArgs e)
        {
            isSectionBtnChosen = queryBtn.IsEnabled = true;

            isQueryBtnChosen = sectionBtn.IsEnabled = false;
        }

        //If none of sections is chosen.
        bool isAllBtnsEnable = false;
        //Query typed in search bar.
        string searchQuery = "";
        private void SearchBarPreviewKeyDown(object sender, KeyEventArgs e)
        {
            restOfBody = null;

            var bannedSigns = new List<string>() { "{", "}", "[", "]", "(", ")" };

            //If search bar and Enter key are clicked.
            if (isClickedSearchGlass && searchBtn.IsEnabled && searchBar.Text != "" || e?.Key == Key.Enter && searchBtn.IsEnabled && searchBar.Text != "")
            {
                isClickedSearchGlass = false;

                //To avoid error related with signs in bannnedSigns list.
                bool notAllowed = false;
                foreach (var bannedSign in bannedSigns)
                {
                    if (searchBar.Text.Contains(bannedSign))
                    {
                        notAllowed = true;
                    }
                }
                if (!notAllowed) { SearchResult(); }

                noResults.Visibility = Visibility.Hidden;
            }

            async void SearchResult()
            {
                var apiKey = "dcc2192a-81e0-45c4-b847-322e8da0ffac";

                if (isQueryBtnChosen)
                {
                    var client = new HttpClient();

                    //If it's link to article.
                    if (searchBar.Text.Contains("https://content.guardianapis.com") || searchBar.Text.Contains("https://www.theguardian.com"))
                    {
                        searchBtn.IsEnabled = false;
                        backToMain.Visibility = Visibility.Hidden;
                        string link = null;

                        try
                        {
                            //If link wasn't converted to guardianapis.
                            if (searchBar.Text.Contains("https://www.theguardian.com"))
                            {
                                link = ConvertLinkToApi(searchBar.Text);
                            }
                            else
                            {
                                link = searchBar.Text;
                            }

                            var response = await client.GetAsync(link);
                            var responseData = await response.Content.ReadAsStringAsync();

                            //If Link is wrong.
                            if (responseData.Contains("The requested resource could not be found.")) { throw new ArgumentOutOfRangeException(); }

                            var article = JsonConvert.DeserializeObject<ResponseArticle>(responseData);

                            ShowArticlePlaceHolder.Visibility = ShowArticle.Visibility = Visibility.Visible;
                            Main.Visibility = Visibility.Hidden;
                            articleView.Children.Clear();

                            if (article.response.content == null || article.response.content.fields.thumbnail == null) { throw new ArgumentOutOfRangeException(); }

                            await Task.Delay(1);
                            BuildArticle(article);

                            if (isLive)
                            {
                                RefreshLiveArticle(article.response.content.webUrl);
                            }
                            searchBtn.IsEnabled = true;
                            backToMain.Visibility = Visibility.Visible;

                            //Clears text of search bar.
                            Keyboard.ClearFocus();
                            searchBar.Text = "";
                        }
                        //If article is not supported.
                        catch (ArgumentOutOfRangeException)
                        {
                            searchBtn.IsEnabled = true;

                            if (linksApi.Count > 0) { linksApi.RemoveAt(linksApi.Count() - 1); }

                            //When it's closing not loaded article
                            Main.Children.Remove(checkNetworkStatusBtn);
                            Main.Children.Remove(errorInfoTB);
                            checkNetworkStatusBtn = null;

                            SolveInvalidArticle();

                            searchBtn.IsEnabled = true;

                            //Clears text of search bar.
                            Keyboard.ClearFocus();
                            searchBar.Text = "";
                        }
                        //If there is no internet connection.
                        catch (Exception e)
                        {
                            MouseButtonEventHandler func = Reconnect;

                            TryLoadAgain(func, true);

                            async void Reconnect(object sender, MouseButtonEventArgs e)
                            {
                                try
                                {
                                    var client = new HttpClient();
                                    var response = await client.GetAsync(link);
                                    var article = JsonConvert.DeserializeObject<ResponseArticle>(await response.Content.ReadAsStringAsync());

                                    await Task.Delay(1);
                                    checkNetworkStatusBtn.IsEnabled = false;

                                    try
                                    {
                                        if (article.response.content.fields.thumbnail == null) { throw new ArgumentOutOfRangeException(); }

                                        BuildArticle(article);

                                        searchBtn.IsEnabled = true;
                                        backToMain.Visibility = Visibility.Visible;

                                        //Clears text of search bar.
                                        Keyboard.ClearFocus();
                                        searchBar.Text = "";
                                    }
                                    //If article is not supported.
                                    catch (ArgumentOutOfRangeException)
                                    {
                                        //When it's closing not loaded article
                                        Main.Children.Remove(checkNetworkStatusBtn);
                                        Main.Children.Remove(errorInfoTB);
                                        checkNetworkStatusBtn = null;

                                        SolveInvalidArticle();
                                        searchBtn.IsEnabled = true;
                                    }
                                }
                                catch (Exception f) { }
                            }
                        }
                    }
                    //If it's simple query.
                    else
                    {
                        string link = null;

                        try
                        {
                            searchQuery = searchBar.Text;
                            link = string.Format($"https://content.guardianapis.com/search?q={searchBar.Text}&page-size=20&show-fields=thumbnail,body,trailText&show-tags=contributor&api-key={apiKey}");
                            var response = await client.GetAsync(link);

                            for (int i = 1; i < 6; i++)
                            {
                                var btn = (Button)this.FindName(String.Format($"navbarBtn{i}"));
                                btn.IsEnabled = true;
                            }

                            sectionArticles.Clear();
                            articleView.Children.Clear();
                            linksApi.Clear();
                            ShowArticle.Visibility = MainPlaceHolder.Visibility = backToMain.Visibility = Visibility.Hidden;
                            Main.Visibility = Visibility.Visible;
                            page = 1;
                            isAllBtnsEnable = true;
                            await ConfigureArticles(response);

                            //Clears text of search bar.
                            Keyboard.ClearFocus();
                            searchBar.Text = "";
                        }
                        //If there is no internet connection.
                        catch (Exception e)
                        {
                            MouseButtonEventHandler func = ReloadSection;

                            TryLoadAgain(func);

                            async void ReloadSection(object sender, MouseButtonEventArgs e)
                            {
                                try
                                {
                                    var response = await client.GetAsync(link);

                                    for (int i = 1; i < 6; i++)
                                    {
                                        var btn = (Button)this.FindName(String.Format($"navbarBtn{i}"));
                                        btn.IsEnabled = true;
                                    }

                                    sectionArticles.Clear();
                                    articleView.Children.Clear();
                                    linksApi.Clear();
                                    ShowArticle.Visibility = MainPlaceHolder.Visibility = backToMain.Visibility = Visibility.Hidden;
                                    Main.Visibility = Visibility.Visible;
                                    page = 1;
                                    isAllBtnsEnable = true;
                                    await ConfigureArticles(response);

                                    //Removes checkNetworkStatusBtn.
                                    Main.Children.Remove(checkNetworkStatusBtn);
                                    Main.Children.Remove(errorInfoTB);
                                    checkNetworkStatusBtn = null;

                                    mainScrollViewer.Visibility = Visibility.Visible;

                                    //Clears text of search bar.
                                    Keyboard.ClearFocus();
                                    searchBar.Text = "";
                                }
                                catch (Exception f) { }
                            }
                        }
                    }
                }
                else if (isSectionBtnChosen)
                {
                    string link = null;

                    try
                    {
                        if (searchBar.Text.Contains("https://content.guardianapis.com/search?section="))
                        {
                            link = searchBar.Text;
                        }
                        else
                        {
                            link = string.Format($"https://content.guardianapis.com/search?section={searchBar.Text}&show-fields=body,thumbnail,trailText&show-tags=contributor&page=1&page-size=20&api-key={apiKey}");
                        }

                        var client = new HttpClient();
                        var response = await client.GetAsync(link);

                        for (int i = 1; i < 6; i++)
                        {
                            var btn = (Button)this.FindName(String.Format($"navbarBtn{i}"));
                            btn.IsEnabled = true;
                        }

                        sectionArticles.Clear();
                        articleView.Children.Clear();
                        linksApi.Clear();
                        ShowArticle.Visibility = MainPlaceHolder.Visibility = backToMain.Visibility = Visibility.Hidden;
                        Main.Visibility = Visibility.Visible;
                        page = 1;
                        isAllBtnsEnable = true;
                        await ConfigureArticles(response);

                        //Clears text of search bar.
                        Keyboard.ClearFocus();
                        searchBar.Text = "";
                    }
                    //If there is no internet connection.
                    catch (Exception e)
                    {
                        MouseButtonEventHandler func = ReloadSection;

                        TryLoadAgain(func);

                        async void ReloadSection(object sender, MouseButtonEventArgs e)
                        {
                            try
                            {
                                var client = new HttpClient();
                                var response = await client.GetAsync(link);

                                for (int i = 1; i < 6; i++)
                                {
                                    var btn = (Button)this.FindName(String.Format($"navbarBtn{i}"));
                                    btn.IsEnabled = true;
                                }

                                sectionArticles.Clear();
                                articleView.Children.Clear();
                                linksApi.Clear();
                                ShowArticlePlaceHolder.Visibility = ShowArticle.Visibility = backToMain.Visibility = MainPlaceHolder.Visibility = Visibility.Hidden;
                                Main.Visibility = Visibility.Visible;
                                page = 1;
                                await ConfigureArticles(response);

                                //Removes checkNetworkStatusBtn.
                                Main.Children.Remove(checkNetworkStatusBtn);
                                Main.Children.Remove(errorInfoTB);
                                checkNetworkStatusBtn = null;

                                mainScrollViewer.Visibility = Visibility.Visible;

                                //Clears text of search bar.
                                Keyboard.ClearFocus();
                                searchBar.Text = "";
                            }
                            catch (Exception f) { }
                        }
                    }
                }
            }
        }

        //If it's clicked search glass on right corner of search bar.
        bool isClickedSearchGlass = false;
        private void SearchBarClick(object sender, RoutedEventArgs e)
        {
            isClickedSearchGlass = true;
            KeyEventArgs f = null;

            //Calls function, normally called by keyboard, by clicking of glass icon.
            SearchBarPreviewKeyDown(sender, f);
        }


        //---Methods executed during lost of internet connection or when article is not supported.---

        /// <summary>Displays "Unsupported article." with ShowArticlePlaceHolder when article isn't available to load.</summary>
        private async void SolveInvalidArticle(bool isOpenArticle = false)
        {
            ShowArticlePlaceHolder.Visibility = ShowArticle.Visibility = Visibility.Visible;
            articleScrollViewer.Visibility = Main.Visibility = RelatedArticlesGrid.Visibility = Visibility.Hidden;

            MessageBox.Show("Unsupported article.");

            //By this you can close not loaded article.
            if (wasReconnected)
            {
                linksApi.RemoveAt(linksApi.Count() - 1);
                wasReconnected = false;
            }

            //--- Basic conditions to prevent empty screen of article view. ---
            if (linksApi.Count() == 0 || isOpenArticle)
            {
                Main.Visibility = mainScrollViewer.Visibility = WrapPanelArticles.Visibility = Visibility.Visible;
                ShowArticle.Visibility = backToMain.Visibility = Visibility.Hidden;

                //When it's closing not loaded article
                Main.Children.Remove(checkNetworkStatusBtn);
                Main.Children.Remove(errorInfoTB);
                checkNetworkStatusBtn = null;
            }
            else
            {
                ShowArticlePlaceHolder.Visibility = Visibility.Visible;

                restOfBody = null;

                mainScrollViewer.Visibility = Visibility.Visible;

                var client = new HttpClient();
                var response = await client.GetAsync(linksApi.ElementAt(linksApi.Count() - 1));
                var article = JsonConvert.DeserializeObject<ResponseArticle>(await response.Content.ReadAsStringAsync());

                //When it's closing not loaded article
                Main.Children.Remove(checkNetworkStatusBtn);
                Main.Children.Remove(errorInfoTB);
                checkNetworkStatusBtn = null;

                await Task.Delay(1);
                BuildArticle(article);
                linksApi.RemoveAt(linksApi.Count() - 1);
            }
            //-----------------------------------------------------------------

            if (isOpenArticle && linksApi.Count() > 0) { linksApi.RemoveAt(linksApi.Count() - 1); }
        }

        /// <summary>It allows reload not loaded (by internet causation) article/section of articles.</summary>
        /// <param name="func">Proper function which will be execute by checkNetworkStatusBtn.</param>
        /// <param name="isOpenedFromMain">When article was open from section.</param>
        private void TryLoadAgain(MouseButtonEventHandler func, bool isOpenedFromMain = false)
        {
            if (checkNetworkStatusBtn == null)
            {
                //--- Basic conditions to load connection issue screen. ---
                mainScrollViewer.Visibility = Visibility.Hidden;
                Main.Visibility = Visibility.Visible;
                if (isOpenedFromMain) { backToMain.Visibility = Visibility.Visible; }
                else { ShowArticle.Visibility = Visibility.Hidden; }
                //---------------------------------------------------------

                if (isOpenedFromMain)
                {
                    linksApi.Add("");
                    wasReconnected = true;
                }

                var fontName = "Font Awesome 6 Free Solid";
                var fontFamily = (FontFamily)Application.Current.FindResource("FontAwesomeSolid");
                checkNetworkStatusBtn = new Button()
                {
                    Width = 100,
                    Height = 100,
                    Content = new TextBlock(new Run("↻")) { FontFamily = new FontFamily(fontName), FontSize = 80 },
                    Margin = new Thickness(1020, -80, 0, 0),
                    Style = this.FindResource("ReconnectBtn") as Style
                };
                checkNetworkStatusBtn.PreviewMouseLeftButtonDown += func;

                Main.Children.Add(checkNetworkStatusBtn);

                MainPlaceHolder.Visibility = articleScrollViewer.Visibility = Visibility.Hidden;

                errorInfoTB = new TextBlock
                {
                    FontSize = 76,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = 1090,
                    FontWeight = FontWeights.Bold,
                    Foreground = Color("#211057"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(35, -100, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                };
                errorInfoTB.Inlines.Add(new Span(new Run(" ")) { FontFamily = fontFamily });
                errorInfoTB.Inlines.Add(new Run("Connection lost... Try later."));
                Main.Children.Add(errorInfoTB);
            }
        }


        //---Methods related with link's actions - openning, converting to api format.--- 

        ///<summary>Opens a link.</summary>
        private async void OpenLink(object sender, RoutedEventArgs e)
        {
            var link = (Hyperlink)sender;
            link.TextDecorations.First().Pen = new Pen(Color("#33128f"), 2.5);
            //Gets back to default color of underline.
            BackToEnter();
            var linkApi = link.NavigateUri.ToString();

            try
            {
                linkApi = ConvertLinkToApi(linkApi);
                var client = new HttpClient();
                var response = await client.GetAsync(linkApi);
                var article = JsonConvert.DeserializeObject<ResponseArticle>(await response.Content.ReadAsStringAsync());
                if (article.response.content == null)
                {
                    throw new NullReferenceException();
                }

                //--- Basic conditions to open an article. ---
                articleScrollViewer.Visibility = Visibility.Hidden;
                ShowArticlePlaceHolder.Visibility = Visibility.Visible;
                //--------------------------------------------

                //Used to show placeholder of article.
                Task.Delay(1);
                await BuildArticle(article);
            }
            //If it's http error.
            catch (HttpRequestException x)
            {
                Process.Start(new ProcessStartInfo(link.NavigateUri.ToString()) { UseShellExecute = true });
            }
            //If it's null error.
            catch (NullReferenceException y)
            {
                Process.Start(new ProcessStartInfo(link.NavigateUri.ToString()) { UseShellExecute = true });
            }

            async void BackToEnter()
            {
                await Task.Delay(100);
                link.TextDecorations.First().Pen = new Pen(Color("#4217b8"), 2.5);
            }
        }

        /// <summary>Converts web URL to API URL</summary>
        /// <returns><see cref="String"/> link</returns>
        private string ConvertLinkToApi(string link)
        {
            return String.Format($"https://content.guardianapis.com{link.Substring(27, link.Length - 27)}?show-tags=contributor&show-fields=body,thumbnail,trailText&show-related=true&api-key=dcc2192a-81e0-45c4-b847-322e8da0ffac");
        }

        //Takes amount of live events in live article to check if there has been something changed after update.
        internal int eventsCount = 0;
        //Prevents cloning of single live article in linksApi (history of articles important for BackToMain Func).
        internal bool isFirstTime = true;
        //Responsibles for refreshing live article.
        DispatcherTimer confirmationTimer;
        ///<summary>Opens article's link from clipboard</summary>
        //private void OpenCopiedLink(object sender, KeyEventArgs e)
        //{
        //    //---- prerequisites ----
        //    //Deletes refreshing timer of article if it runs (isn't null).
        //    confirmationTimer?.Stop();
        //    //Deletes confirmation button of refreshing article.
        //    ShowArticle.Children.Remove(confirmationBtn);
        //    //By this last empty live event is deleted.
        //    liveBlock = null;
        //    eventsCount = 0;
        //    isLive = false;
        //    isFirstTime = true;
        //    restOfBody = null;
        //    //-----------------------


        //    OpenArticle();

        //    async void OpenArticle()
        //    {
        //        var liveLink = Clipboard.GetText();
        //        //Converts ordinary guardian link into guardianapis link.
        //        var link = ConvertLinkToApi(liveLink);

        //        var client = new HttpClient();
        //        try
        //        {
        //            var response = await client.GetAsync(link);

        //            //If there is no access to data, throws an exception.
        //            if (response.StatusCode.ToString() == "Forbidden")
        //            {
        //                throw new Exception();
        //            }

        //            var article = JsonConvert.DeserializeObject<ResponseArticle>(await response.Content.ReadAsStringAsync());

        //            //--- Basic conditions to open an article. ---
        //            Main.Visibility = articleScrollViewer.Visibility = Visibility.Hidden;
        //            ShowArticle.Visibility = ShowArticlePlaceHolder.Visibility = Visibility.Visible;
        //            articleScrollViewer.ScrollToTop();
        //            //--------------------------------------------

        //            var body = article.response.content.fields.body;
        //            //Counts total number of live events in body.
        //            for (int i = 0; i < body.Length; i++)
        //            {
        //                try
        //                {
        //                    if (body.Substring(i, 3).Contains("<ti") && body[i - 2].ToString() != "d")
        //                    {
        //                        eventsCount++;
        //                    }
        //                }
        //                catch (Exception f) { }
        //            }

        //            try
        //            {
        //                //Used to show placeholder of article.
        //                await Task.Delay(1);
        //                await BuildArticle(article);
        //            }
        //            catch (Exception e)
        //            {

        //            }

        //            //If it's live article, then it loads refresh method for article.
        //            if (isLive)
        //            {
        //                RefreshLiveArticle(liveLink);
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            SolveInvalidArticle();
        //        }
        //    }
        //}


        //------------Others------------

        ///<summary>Copies text of a title.</summary>
        private void CopyTitle(object sender, MouseButtonEventArgs e)
        {
            var title = (TextBlock)sender;
            Clipboard.SetText(title.Text);
            title.Cursor = Cursors.Arrow;
        }

        /// <summary>Converts string code to a colour.</summary>
        /// <returns><see cref="SolidColorBrush"/></returns>
        private SolidColorBrush Color(string colour)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colour));
        }

        /// <summary>Backs to main site or previous article if was.</summary>
        private void BackToMain(object sender, RoutedEventArgs e)
        {
            //Prevents errors related with loading articles from history.
            backToMain.Visibility = Visibility.Hidden;
            //---- Conditions to open previous article. ----
            confirmationTimer?.Stop();
            ShowArticle.Children.Remove(confirmationBtn);
            RelatedArticlesGrid.Visibility = Visibility.Hidden;
            liveBlock = null;
            isLive = false;
            articleView.Children.Clear();
            //----------------------------------------------

            //When it's closing not loaded article
            Main.Children.Remove(checkNetworkStatusBtn);
            Main.Children.Remove(errorInfoTB);
            checkNetworkStatusBtn = null;

            if (linksApi.Count != 0)
            {
                BackToPrevious(linksApi);

                if (linksApi.Count == 0)
                {
                    //--- Conditions to open section of articles ---
                    backToMain.Visibility = Visibility.Hidden;
                    Main.Visibility = mainScrollViewer.Visibility = WrapPanelArticles.Visibility = Visibility.Visible;
                    //----------------------------------------------
                }
            }

            async Task BackToPrevious(List<string> linksApi)
            {
                try
                {
                    restOfBody = null;
                    linksApi.RemoveAt(linksApi.Count - 1);

                    if (linksApi.Count() > 0)
                    {
                        var client = new HttpClient();
                        var response = await client.GetAsync(linksApi[linksApi.Count - 1]);
                        var article = JsonConvert.DeserializeObject<ResponseArticle>(await response.Content.ReadAsStringAsync());

                        //--- Basic conditions to open an article. ---
                        Main.Visibility = articleScrollViewer.Visibility = Visibility.Hidden;
                        ShowArticle.Visibility = ShowArticlePlaceHolder.Visibility = Visibility.Visible;
                        articleScrollViewer.ScrollToTop();
                        //--------------------------------------------

                        await Task.Delay(1);
                        await BuildArticle(article);

                        linksApi.RemoveAt(linksApi.Count - 1);
                    }
                }
                catch (Exception e)
                {
                    MouseButtonEventHandler func = Reconnect;

                    TryLoadAgain(func);

                    async void Reconnect(object sender, MouseButtonEventArgs e)
                    {
                        try
                        {
                            var client = new HttpClient();

                            var response = await client.GetAsync(linksApi[linksApi.Count - 1]);
                            var article = JsonConvert.DeserializeObject<ResponseArticle>(await response.Content.ReadAsStringAsync());

                            await BuildArticle(article);

                            linksApi.RemoveAt(linksApi.Count - 1);
                        }
                        catch (Exception f) { MessageBox.Show(linksApi.Count().ToString()); }
                    }
                }
            }
        }
    }
}