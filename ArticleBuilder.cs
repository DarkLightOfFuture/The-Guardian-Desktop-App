using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;
using System.IO;
using System.Drawing.Text;

namespace guardian_wpf
{
    public partial class MainWindow : Window
    {
        //Prevents duplication of article in linksApi.
        internal bool wasReconnected = false;
        /// <summary>Animates a tile of article preview after click and opens an article</summary>
        private async void OpenArticle(object sender, RoutedEventArgs e)
        {
            var senderArticle = sender;

            //Article preview button.
            var articleButton = (Button)sender;
            //Article preview button's border.
            var articleBorder = (Border)articleButton.FindName("articleBorder");

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.02);
            timer.Tick += AnimationWithArticle;
            timer.Start();

            async void AnimationWithArticle(object sender, EventArgs e)
            {
                if (articleBorder.Padding.Bottom < 8 && !isDone)
                {
                    articleBorder.Background = Color("#2D033B");
                    articleButton.Background = Color("#2D033B");
                    articleBorder.Padding = new Thickness(articleBorder.Padding.Left, articleBorder.Padding.Top, articleBorder.Padding.Right, articleBorder.Padding.Bottom + 3);
                }
                else if (!isDone)
                {
                    isDone = true;
                }
                else if (articleBorder.Padding.Bottom > 3)
                {
                    articleBorder.Background = Color("#FF580773");
                    articleButton.Background = Color("#FF580773");
                    articleBorder.Padding = new Thickness(articleBorder.Padding.Left, articleBorder.Padding.Top, articleBorder.Padding.Right, articleBorder.Padding.Bottom - 3);
                }
                else
                {
                    isDone = false;
                    timer.Stop();
                }
            }

            try
            {
                if (!wasReconnected)
                {
                    //--- Basic conditions to open an article. ---
                    Main.Visibility = Visibility.Hidden;
                    ShowArticle.Visibility = ShowArticlePlaceHolder.Visibility = Visibility.Visible;
                    //--------------------------------------------
                }

                //Used to show placeholder of article.
                await Task.Delay(1);
                if (isLive) { isFirstTime = true; }

                //Important for check index of article.
                int index = -1;
                var articleTitle = (TextBlock)articleButton.FindName("articleTitle");
                int minLength = -1;

                try
                {
                    foreach (var articleType in sectionArticles)
                    {
                        if (minLength == -1 || minLength > articleType.webTitle.Length)
                        {
                            minLength = articleType.webTitle.Length;
                        }
                    }
                    index = sectionArticles.FindIndex(x => x.webTitle.Substring(0, minLength) == articleTitle.Text.Substring(0, minLength));

                    var link = ConvertLinkToApi(sectionArticles.ElementAt(index).webUrl);
                    var client = new HttpClient();
                    var response2 = await client.GetAsync(link);

                    if (response2.StatusCode.ToString() == "Forbidden")
                    {
                        throw new Exception();
                    }

                    var article = JsonConvert.DeserializeObject<ResponseArticle>(await response2.Content.ReadAsStringAsync());

                    //--- Basic conditions to open an article. ---
                    Main.Visibility = articleScrollViewer.Visibility = Visibility.Hidden;
                    ShowArticle.Visibility = ShowArticlePlaceHolder.Visibility = Visibility.Visible;
                    articleScrollViewer.ScrollToTop();
                    //--------------------------------------------

                    if (article.response.content.fields.thumbnail == null) { throw new ArgumentOutOfRangeException(); }

                    await Task.Delay(1);
                    await BuildArticle(article);
                    wasReconnected = false;
                    if (isLive)
                    {
                        RefreshLiveArticle(sectionArticles.ElementAt(index).webUrl);
                    }
                }
                //If article is not supported.
                catch (ArgumentOutOfRangeException f)
                {
                    try
                    {
                        var client = new HttpClient();
                        var response = client.GetAsync("https://www.google.pl");

                        SolveInvalidArticle(true);
                        searchBtn.IsEnabled = true;
                    }
                    catch (Exception g) { }
                }
            }
            //If there is no internet connection.
            catch (Exception f)
            {
                if (checkNetworkStatusBtn != null) { checkNetworkStatusBtn.IsEnabled = true; }
                MouseButtonEventHandler func = Reconnect;

                TryLoadAgain(func, true);

                async void Reconnect(object sender, MouseButtonEventArgs e)
                {
                    checkNetworkStatusBtn.IsEnabled = false;
                    wasReconnected = true;
                    OpenArticle(senderArticle, null);
                }
            }
        }

        //List of opened articles.
        List<string> linksApi = new List<string>();
        /// <summary>Closes previous article and opens a new one.</summary>
        private async Task BuildArticle(ResponseArticle article)
        {
            //Clears value of events count after previous article.
            eventsCount = 0;

            //If article was reload and tried to load from search bar.
            if (checkNetworkStatusBtn != null) { checkNetworkStatusBtn.IsEnabled = false; }
            Keyboard.ClearFocus();
            searchBar.Text = "";

            //--- Basic conditions to close/open an article whithout any problem. ---
            articleScrollViewer.ScrollToTop();
            //Clears variable which stores live event.
            liveBlock = null;
            isLive = false;
            //Clears view of article after previous one.
            articleView.Children.Clear();
            //--- Basic conditions to close/open an article whithout any problem. ---

            //When it's closing not loaded article
            Main.Children.Remove(checkNetworkStatusBtn);
            Main.Children.Remove(errorInfoTB);
            checkNetworkStatusBtn = null;

            //By this you can close not loaded article.
            if (wasReconnected)
            {
                linksApi.RemoveAt(linksApi.Count() - 1);
                wasReconnected = false;
            }

            var body = article.response.content.fields.body;

            //Counts number of live events in article.
            for (int i = 0; i < body.Length; i++)
            {
                try
                {
                    if (body.Substring(i, 3).Contains("<ti") && body[i - 2].ToString() != "d")
                    {
                        eventsCount++;
                    }
                }
                catch (Exception f) { }
            }

            //History of opened articles.
            try
            {
                if (isFirstTime || !isLive)
                {
                    linksApi.Add(ConvertLinkToApi(article.response.content.webUrl));
                }
            }
            //If article isn't supported.
            catch (NullReferenceException e)
            {
                SolveInvalidArticle();
            }

            //Loads related articles previews in left column of article view.
            relatedContentTB.Text = null;
            for (var x = 0; x < 5; x++)
            {
                if (x != article.response.relatedContent.Count())
                {
                    //Text of link block.
                    var tag = article.response.relatedContent.ElementAt(x).webTitle;

                    var titleSplit = tag.Split(" ");
                    string articleTitle = null;

                    if (titleSplit.Length > 13)
                    {
                        if (titleSplit.ElementAt(13) != "|" && titleSplit.ElementAt(13) != ".")
                        {
                            articleTitle = tag.Substring(0, tag.IndexOf(titleSplit.ElementAt(13)) + titleSplit.ElementAt(13).Length);
                        }
                        else if (titleSplit.Length > 13)
                        {
                            articleTitle = tag.Substring(0, tag.IndexOf(titleSplit.ElementAt(14)) + titleSplit.ElementAt(14).Length);
                        }
                        else
                        {
                            articleTitle = tag;
                        }

                        var articleTitleFinal = String.Format($"{articleTitle}...");
                        tag = articleTitleFinal;
                    }

                    MouseEventHandler mouseEnter = EnterRelatedContentBlock;
                    MouseEventHandler mouseLeave = LeaveRelatedContentBlock;
                    RoutedEventHandler mouseClick = ClickRelatedContentBlock;
                    var link = CreateLinkBlock(tag, article.response.relatedContent.ElementAt(x).webUrl, mouseEnter, mouseLeave, mouseClick);

                    relatedContentTB.Inlines.Add(link);
                }
                else { break; }
            }

            var htmlDocument = new HtmlDocument();

            var articleBody = new TextBlock()
            {
                TextAlignment = TextAlignment.Justify,
                TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness(10, 0, 10, 0)
            };

            ComputeLastIndexOfBody(htmlDocument, 20, body);
            CreateArticle(article, articleBody, htmlDocument);

            //Puts ShowMore Button if there is more content.
            if (isShowMoreBtn)
            {
                var showMoreButton = new Button()
                {
                    Content = "Show more",
                    Foreground = Color("#c9c6d1"),
                    Style = this.FindResource("ShowMoreBtn") as Style,
                    Margin = new Thickness(0, 20, 0, 40)
                };
                showMoreButton.Click += ShowMoreLiveEvents;

                articleView.Children.Add(showMoreButton);
                isShowMoreBtn = false;
            }

            //--- Conditions to open article. ---
            articleScrollViewer.Visibility = RelatedArticlesGrid.Visibility = ShowArticle.Visibility = backToMain.Visibility = Visibility.Visible;
            ShowArticlePlaceHolder.Visibility = Main.Visibility = Visibility.Hidden;
            //----------------------------------
        }

        string restOfBody = null;
        bool isShowMoreBtn = false;
        bool isLive = false;
        bool isEnd = false;
        /// <summary>Deletes showMoreButton then loads another 20 liveEvents and adds new showMoreButton if <see cref="bool"/> isLive = true, adds new showMoreButton.</summary>
        private async void ShowMoreLiveEvents(object sender, RoutedEventArgs e)
        {
            var isInternetConnection = true;
            try
            {
                var client = new HttpClient();
                var response = await client.GetAsync("https://content.guardianapis.com/search?api-key=dcc2192a-81e0-45c4-b847-322e8da0ffac");
            }
            catch (Exception f) { isInternetConnection = false; }

            if (isInternetConnection)
            {
                //showMoreBtn from BuildArticle/ShowMoreLiveEvents Func.
                var btn = (Button)sender;
                //Variable in which is store refresh icon.
                Border spinnerBorder = null;

                //Gets instance of StackPanel which contains refresh icon.
                if ((btn.Parent is StackPanel parent1))
                {
                    //Removes from StackPanel showMoreBtn (sender).
                    parent1.Children.Remove(btn);
                    spinnerBorder = CreateRefreshIcon();
                    //Adds refresh icon to instance of StackPanel in which are articles tiles.
                    parent1.Children.Add(spinnerBorder);
                    //Creates animation of move from bottom to top for refresh icon.
                    AnimationSpinner((Border)parent1.Children[parent1.Children.IndexOf(spinnerBorder)]);

                    void AnimationSpinner(Border border)
                    {
                        TranslateTransform translateTransform = new TranslateTransform();
                        border.RenderTransform = translateTransform;
                        DoubleAnimation animation = new DoubleAnimation { From = 20, To = 0, Duration = TimeSpan.FromSeconds(0.2) };
                        translateTransform.BeginAnimation(TranslateTransform.YProperty, animation);
                    }
                }

                //Wrapper around native html.
                var htmlDocument = new HtmlDocument();

                //Main textblock of article.
                var articleBody = new TextBlock()
                {
                    TextAlignment = TextAlignment.Justify,
                    TextWrapping = TextWrapping.Wrap,
                    Padding = new Thickness(10, 0, 10, 0)
                };

                ComputeLastIndexOfBody(htmlDocument, 50);

                CreateArticle(null, articleBody, htmlDocument, true);

                //Deletes showMoreBtn and adds new live events. If there is more live events, adds showMoreBtn.
                StopRefresh(spinnerBorder, articleBody, isShowMoreBtn);

                isShowMoreBtn = false;

                async void StopRefresh(Border border, TextBlock articleBody, bool isShowMoreBtn)
                {
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.5) };
                    timer.Tick += Animation;
                    timer.Start();

                    async void Animation(object sender, EventArgs e)
                    {
                        //Gets instance of StackPanel which contains refresh button.
                        if (border.Parent is StackPanel parent)
                        {
                            //Removes refresh button.
                            parent.Children.Remove(border);
                        }

                        //Adds new live blocks.
                        articleView.Children.Add(articleBody);

                        //If there is more live blocks, adds showMoreBtn.
                        if (isShowMoreBtn)
                        {
                            var showMoreButton = new Button()
                            {
                                Content = "Show more",
                                Foreground = Color("#c9c6d1"),
                                Style = this.FindResource("ShowMoreBtn") as Style,
                                Margin = new Thickness(0, 20, 0, 40)
                            };

                            showMoreButton.Click += ShowMoreLiveEvents;
                            articleView.Children.Add(showMoreButton);
                        }

                        timer.Stop();
                    }
                }
            }
            else
            {
                MessageBox.Show("Lost connection... Try later.");
            }
        }

        //Confirmation button used to refresh an article.
        Button confirmationBtn = null;
        /// <summary>Refresh live article.</summary>
        /// <param name="liveLink">Ordinary link to guardian article.</param>
        private void RefreshLiveArticle(string liveLink)
        {
            isFirstTime = false;

            confirmationTimer = new DispatcherTimer() { Interval = TimeSpan.FromMinutes(3) };
            confirmationTimer.Tick += CreateUpdateBtn;
            confirmationTimer.Start();

            async void CreateUpdateBtn(object sender, EventArgs e)
            {
                //Prevents errors after refresh of article.
                restOfBody = null;

                ResponseArticle article = null;

                try
                {
                    var client = new HttpClient();
                    var link = ConvertLinkToApi(liveLink);
                    var response = await client.GetAsync(link);

                    //If there is no access to data, throws an exception.
                    if (response.StatusCode.ToString() == "Forbidden")
                    {
                        throw new Exception();
                    }

                    article = JsonConvert.DeserializeObject<ResponseArticle>(await response.Content.ReadAsStringAsync());
                }
                catch (Exception f) { }

                var body = article.response.content.fields.body;
                //Used to compare with eventsCount to determine if article was changed.
                var currentEventsCount = 0;
                //Counts total number of live events in body of refreshed article.
                for (int i = 0; i < body.Length; i++)
                {
                    try
                    {
                        if (body.Substring(i, 3).Contains("<ti") && body[i - 2].ToString() != "d")
                        {
                            currentEventsCount++;
                        }
                    }
                    catch (Exception f) { }
                }

                //If refreshed data of article has new live events creates button with ability to refresh article.
                if (currentEventsCount > eventsCount)
                {
                    ShowArticle.Children.Remove(confirmationBtn);


                    confirmationBtn = new Button() { Style = this.FindResource("researchBtn") as Style };

                    var contentBtn = new TextBlock() { Foreground = Color("#dad9db"), FontSize = 17, TextAlignment = TextAlignment.Center };

                    contentBtn.Inlines.Add(new Span(new Run("↻ ")) { FontFamily = new FontFamily("Font Awesome 6 Free Solid") });
                    contentBtn.Inlines.Add(new Run(string.Format($"{currentEventsCount - eventsCount} new updates at article.")));

                    confirmationBtn.Content = contentBtn;

                    confirmationBtn.PreviewMouseLeftButtonDown += RefreshArticle;

                    eventsCount = 0;

                    //Sets confirmation button is second column of ShowArticle Grid.
                    Grid.SetColumn(confirmationBtn, 1); ShowArticle.Children.Add(confirmationBtn);

                    async void RefreshArticle(object sender, MouseEventArgs e)
                    {
                        //--- Basic conditions to refresh an article. ---
                        articleScrollViewer.ScrollToTop();
                        articleScrollViewer.Visibility = Visibility.Hidden;
                        ShowArticlePlaceHolder.Visibility = Visibility.Visible;
                        //-----------------------------------------------

                        //Used to show placeholder of article.
                        await Task.Delay(1);
                        await BuildArticle(article);

                        ShowArticle.Children.Remove(confirmationBtn);
                    }
                }
            }
        }

        /// <summary>Puts in article view all attributes, beginning from title ending on headers.</summary>
        private void CreateArticle(ResponseArticle article, TextBlock articleBody, HtmlDocument htmlDocument, bool isShowMoreLiveEvents = false)
        {
            //Takes from body - paragraphs.
            var bodyParagraphs = htmlDocument.DocumentNode.Descendants("p").ToList();

            //Takes from body attributes of links (href, tag).
            var linksParts = GetElementsOfTagA(htmlDocument, bodyParagraphs);

            //Takes from body attributes of H tags (content, index of paragraph before which it will be add).
            var hParts = GetElementsOfTagH(htmlDocument, bodyParagraphs);
            var hTags = hParts.hTags;
            var hTagsContent = hParts.hTagsContent;

            //Takes from body attributes of img tags (src, index of paragraph before which it will be add).
            var imgParts = GetElementsOfTagImg(htmlDocument, bodyParagraphs, hTags);
            var imagesUrl = imgParts.imagesUrl;
            var imgIndexList = imgParts.imgIndexList;

            //Takes from body attributes of ordinary blockquote tags (index of first paragraph in blockquote, index of last paragraph in blockquote).
            var blockQuoteParts = GetElementsOfTagBlockQuote(htmlDocument, bodyParagraphs);
            var blockQuoteTags = blockQuoteParts.blockQuoteTags;
            var blockQuoteEndingTags = blockQuoteParts.blockQuoteEndingTags;

            //Takes from body indices of paragraph after which are twitter blockquote tags.
            var twitterQuoteIndexList = GetElementsOfTwitterQuote(htmlDocument, bodyParagraphs);

            //Takes from body indices of paragraph after which are li tags.
            var liTags = GetElementsOfTagLi(htmlDocument, bodyParagraphs);


            //Index of link in links.
            var linkIndex = 0;
            //Index of headline in hTags.
            var hIndex = 0;
            //Index of image in imgIndexList.
            var imageIndex = 0;
            for (var index = 0; index < bodyParagraphs.Count(); index++)
            {
                //Builds title and thumbnail of article.
                if (index == 0 && !isShowMoreLiveEvents)
                {
                    CreateTagTitle(article);
                    CreateTagImg(articleBody, article.response.content);
                }

                var paragraph = bodyParagraphs.ElementAt(index);

                //Determines if paragraph is in <li> tag.
                if (liTags.Contains(index))
                {
                    isLi = true;

                    if (index + 1 < bodyParagraphs.Count() && liTags.Contains(index + 1))
                    {
                        isLiAfter = true;
                    }
                }

                //Creates h tag.
                if (hTags.Contains(index))
                {
                    var hTagAmount = hTags.Where(x => x == index).Count();
                    CreateTagH(articleBody, hTagsContent, hTagAmount, hIndex);
                    hIndex += hTagAmount;
                }

                //If true, builds image, or images if they are next to each other. (applies only to images after thumbnail)
                if (imgIndexList.Contains(index))
                {
                    //Amount of images next to each other if amount > 1.
                    int imagesNumb = imgIndexList.Where(x => x == index).Count();

                    CreateTagImg(articleBody, imagesUrl, imagesNumb, imageIndex);

                    imageIndex += imagesNumb;
                }

                //Paragraph's text.
                var text = paragraph.InnerHtml;
                //If true, builds paragraph with link.
                if (text.Contains("<a"))
                {
                    //Amount of links in this paragraph.
                    var loopLink = 0;

                    if (twitterQuoteIndexList.Contains(index))
                    {
                        if (isLive)
                        {
                            liveBlock.Inlines.Add(new TextBlock(new LineBreak()));
                        }
                        else
                        {
                            articleBody.Inlines.Add(new TextBlock(new LineBreak()));
                        }
                    }

                    loopLink = CreateTagP(articleBody, text, linksParts, linkIndex, loopLink);
                    linkIndex += loopLink;
                }
                //Else, builds ordinary paragraph.
                else
                {
                    //If paragraph is begining of quote.
                    if (blockQuoteTags.Contains(index))
                    {
                        isCited = true;

                        if (blockQuoteEndingTags.Contains(index))
                        {
                            isCitedEnd = true;
                        }
                    }
                    //If paragraph is ending of quote.
                    else if (blockQuoteEndingTags.Contains(index))
                    {
                        isCitedEnd = true;
                    }

                    CreateTagP(articleBody, text);
                }

                //By this can be add last live block.
                if (restOfBody == null && index == bodyParagraphs.Count() - 1)
                {
                    var time = "<time datetime=\"1970-01-01T00:00:00.000Z\">0.00am <span class=\"timezone\">BST</span></time>";
                    CreateTagP(articleBody, time);
                }
            }

            if (!isShowMoreLiveEvents)
            {
                articleView.Children.Add(articleBody);
            }
        }

        /// <returns><see cref="List"/> imagesUrl and <see cref="List"/> imgIndexList</returns>
        private ImgParts GetElementsOfTagImg(HtmlDocument htmlDocument, System.Collections.Generic.List<HtmlNode> bodyParagraphs, List<int> hTags)
        {
            var images = htmlDocument.DocumentNode.SelectNodes("//img");
            var bodyImages = htmlDocument.DocumentNode.Descendants("img");

            var imagesUrl = new List<string>();
            if (htmlDocument.DocumentNode.SelectNodes("//img") != null)
            {
                foreach (var image in images)
                {
                    imagesUrl.Add(image.Attributes["src"].Value);
                }
            }

            var imgIndexList = new List<int>();
            for (int x = 0; x < bodyImages.Count(); x++)
            {
                var element = bodyParagraphs.IndexOf(bodyParagraphs.First(y => y.StreamPosition > bodyImages.ElementAt(x).StreamPosition));

                try
                {
                    if (element == -1)
                    {
                        element = bodyParagraphs.Count;
                    }
                }
                catch (Exception e) { }

                imgIndexList.Add(element);
            }

            return new ImgParts { imagesUrl = imagesUrl, imgIndexList = imgIndexList };
        }
        ///<returns><see cref="List"/> linksParts</returns>
        private List<LinkParts> GetElementsOfTagA(HtmlDocument htmlDocument, System.Collections.Generic.List<HtmlNode> bodyParagraphs)
        {
            var linksParts = new List<LinkParts>();

            if (htmlDocument.DocumentNode.SelectNodes("//p//a") != null)
            {
                var links = htmlDocument.DocumentNode.SelectNodes("//p//a").ToList();
                foreach (var link in links)
                {
                    var linkParts = new LinkParts { href = link.Attributes["href"].Value, tag = link.InnerHtml };
                    linksParts.Add(linkParts);
                }
            }

            return linksParts;
        }
        ///<returns><see cref="HParts"/></returns>
        private HParts GetElementsOfTagH(HtmlDocument htmlDocument, System.Collections.Generic.List<HtmlNode> bodyParagraphs)
        {
            var hTags = new List<int>();
            var hTagsContent = new List<string>();
            for (int c = 1; c <= 6; c++)
            {
                foreach (var hTag in htmlDocument.DocumentNode.Descendants(String.Format($"h{c}")))
                {
                    try
                    {
                        hTags.Add(bodyParagraphs.IndexOf(bodyParagraphs.First(x => x.StreamPosition > hTag.StreamPosition)));
                    }
                    catch (Exception e) { }

                    hTagsContent.Add(hTag.InnerHtml);
                }
            }

            return new HParts { hTags = hTags, hTagsContent = hTagsContent };
        }
        ///<returns><see cref="BlockQuoteParts"/></returns>
        private BlockQuoteParts GetElementsOfTagBlockQuote(HtmlDocument htmlDocument, System.Collections.Generic.List<HtmlNode> bodyParagraphs)
        {
            var blockQuoteTags = new List<int>();
            var blockQuoteEndingTags = new List<int>();

            foreach (var blockQuoteTag in htmlDocument.DocumentNode.Descendants("blockquote"))
            {
                if (blockQuoteTag.Attributes["class"]?.Value?.Contains("quoted") == true || blockQuoteTag.Attributes["class"] == null)
                {
                    blockQuoteTags.Add(bodyParagraphs.IndexOf(bodyParagraphs.First(x => x.StreamPosition > blockQuoteTag.StreamPosition)));
                }

                blockQuoteEndingTags.Add(bodyParagraphs.IndexOf(bodyParagraphs.Last(x => blockQuoteTag.StreamPosition + blockQuoteTag.OuterHtml.Length > x.StreamPosition)));
            }

            return new BlockQuoteParts { blockQuoteTags = blockQuoteTags, blockQuoteEndingTags = blockQuoteEndingTags };
        }
        ///<returns><see cref="List"/></returns>
        private List<int> GetElementsOfTwitterQuote(HtmlDocument htmlDocument, System.Collections.Generic.List<HtmlNode> bodyParagraphs)
        {
            var twitterQuotes = htmlDocument.DocumentNode.Descendants("blockquote").Where(x => x.Attributes["class"]?.Value == "twitter-tweet").ToList();
            var twitterQuotesTags = new List<int>();

            foreach (var twitterQuote in twitterQuotes)
            {
                var index = bodyParagraphs.IndexOf(bodyParagraphs.First(x => twitterQuote.StreamPosition < x.StreamPosition));
                twitterQuotesTags.Add(index);
            }

            return twitterQuotesTags;
        }
        ///<returns><see cref="List"/></returns>
        private List<int> GetElementsOfTagLi(HtmlDocument htmlDocument, System.Collections.Generic.List<HtmlNode> bodyParagraphs)
        {
            var liTags = new List<int>();

            foreach (var element in htmlDocument.DocumentNode.Descendants("li"))
            {
                var index = bodyParagraphs.IndexOf(bodyParagraphs.First(x => x.StreamPosition > element.StreamPosition));
                liTags.Add(index);
            }

            return liTags;
        }

        /// <summary>Creates link block with its attributes and events which are put in arguments of function.</summary>
        ///<param name="tag">Tag of link</param>
        ///<param name="href">Href of link</param>
        ///<param name="mouseEnter">Event executed when mouse is over link block.</param>
        ///<param name="mouseLeave">Event executed when mouse isn't over link block.</param>
        ///<param name="mouseClick">Event executed after mouse click.</param>
        ///<param name="isRelatedContent">Determines is it article from related content or from inside of article content. (True by default)</param>
        private Hyperlink CreateLinkBlock(string tag, string href, MouseEventHandler mouseEnter, MouseEventHandler mouseLeave, RoutedEventHandler mouseClick, bool isRelatedContent = true)
        {
            var fontFamily = (FontFamily)Application.Current.FindResource("FontAwesomeSolid");

            //Text after title of link block.
            var leftArrow = new TextBlock(new Run("\n "))
            {
                FontFamily = fontFamily
            };
            var readMore = new TextBlock(new Run("Read more"));

            var tagStyled = ApplyLinkStyle(tag);

            Thickness tagStyledBorderPadding, tagStyledBorderMargin, tagStyledBorderBorder;
            int linkFontSize;
            string tagStyledBorderBackground, tagStyledBorderBorderBrush;
            if (!isRelatedContent)
            {
                readMore.Margin = leftArrow.Margin = new Thickness(0, -15, 0, 0);
                tagStyledBorderPadding = new Thickness(15, 15, 20, 10);
                tagStyledBorderMargin = new Thickness(0, 10, 100, 10);
                tagStyledBorderBorder = new Thickness(7.5, 0, 0, 0);
                linkFontSize = 20;
                tagStyledBorderBackground = "#200461";
                tagStyledBorderBorderBrush = "#4c31c0";
            }
            else
            {
                tagStyled.Width = 190;
                tagStyledBorderPadding = new Thickness(5, 5, 5, 5);
                tagStyledBorderMargin = new Thickness(25, 10, 0, 0);
                tagStyledBorderBorder = new Thickness(0, 2.5, 0, 0);
                linkFontSize = 15;
                tagStyledBorderBackground = "#292830";
                tagStyledBorderBorderBrush = "#a3a3a3";
            }

            tagStyled.Inlines.Add(new LineBreak()); tagStyled.Inlines.Add(leftArrow); tagStyled.Inlines.Add(readMore);

            //Whole link block style.
            var link = new Hyperlink()
            {
                FontSize = linkFontSize,
                FontWeight = FontWeights.Bold,
                Foreground = Color("#FFCCC9D6")
            };
            var tagStyledBorder = new Border
            {
                Padding = tagStyledBorderPadding,
                Background = Color(tagStyledBorderBackground),
                Child = (UIElement)tagStyled,
                Margin = tagStyledBorderMargin,
                BorderThickness = tagStyledBorderBorder,
                BorderBrush = Color(tagStyledBorderBorderBrush)
            };
            link.TextDecorations = null;
            link.NavigateUri = new Uri(href);
            link.Inlines.Add(tagStyledBorder);

            //Link's events.
            tagStyledBorder.MouseEnter += mouseEnter;
            tagStyledBorder.MouseLeave += mouseLeave;
            link.Click += mouseClick;

            return link;
        }

        /// <summary>Computes last index of (fiftyth / last) live event in body and then loads, truncated to this index, body/restOfBody in htmlDocument which is HTML wrapper.</summary>
        /// <param name="htmlDocument">HTML wrapper</param>
        /// <param name="pageSize">How many live events must be loaded this time.</param>
        /// <param name="body">HTML body (important only during first loading of live article)</param>
        /// <returns><see cref="HtmlDocument"/></returns>
        private void ComputeLastIndexOfBody(HtmlDocument htmlDocument, int pageSize, string body = null)
        {
            //Live events count.
            var liveEvents = 0;
            //HTML body carriage, by which you can determine length of text which contains 50 live events.
            string subBody = null;
            //If live events left to show and it's live article.
            if (restOfBody != null && restOfBody.Contains("<time datetime=") || body != null && body.Contains("<time"))
            {
                //Index of next "<time datetime=".
                var index = -1;
                //Total length of body which contains 50 live events or less if there is no more them.
                var textTotalIndex = 0;
                //Index of next "</time>".
                var endTagIndex = 0;

                //Text after this one which contains live events. Used to determines another live blocks until amount of 50 or if there is no more.
                string checkedBody;
                if (restOfBody != null)
                {
                    checkedBody = restOfBody;
                }
                else
                {
                    checkedBody = body;
                }
                //Executes itself till reach 50 live events or if there is no more live events then it breaks.
                while (liveEvents <= pageSize)
                {
                    if (checkedBody.Contains("<time datetime="))
                    {
                        index = checkedBody.IndexOf("<time datetime=");
                        if (!checkedBody.Substring(index - 8, 13).Contains("Updated <time"))
                        {
                            liveEvents++;
                            endTagIndex = checkedBody.IndexOf("</time>") + 7;
                        }
                        else
                        {
                            endTagIndex = checkedBody.IndexOf("</time>") + 7;
                        }

                        textTotalIndex += checkedBody.Substring(0, endTagIndex).Length;
                        checkedBody = checkedBody.Substring(endTagIndex, checkedBody.Length - endTagIndex);
                    }
                    else
                    {
                        break;
                    }
                }

                //If amount of live events is 50 then it adds 50th's body to textTotalIndex.
                if (liveEvents == pageSize)
                {
                    textTotalIndex += checkedBody.IndexOf("<time");
                }

                //If left live events to show.
                if (restOfBody != null && restOfBody.Substring(textTotalIndex, restOfBody.Length - textTotalIndex).Contains("<time datetime="))
                {
                    subBody = restOfBody.Substring(0, textTotalIndex);
                    restOfBody = restOfBody.Substring(textTotalIndex, restOfBody.Length - textTotalIndex);
                }
                //If in body there is no more live events to show.
                else if (body == null)
                {
                    subBody = restOfBody;
                    restOfBody = null;
                }
                //If it's beginning of live article.
                else
                {
                    subBody = body.Substring(0, textTotalIndex);
                    restOfBody = body.Substring(textTotalIndex, body.Length - textTotalIndex);
                }

                //If there is more live events to show and must be add showMoreBtn.
                if (restOfBody != null && restOfBody.Length != 0)
                {
                    isShowMoreBtn = true;
                }
            }
            else
            {
                subBody = body;
            }

            //Loads body with (first {pageSize}/last <{pageSize}) live events.
            htmlDocument.LoadHtml(subBody);
        }
    }
}
