using FontAwesome.WPF;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace guardian_wpf
{
    public partial class MainWindow : Window
    {
        ///<summary>Animates a tile of article preview while mouse is over it.</summary>
        private void ArticleMouseEnter(object sender, MouseEventArgs e)
        {
            var articleBorder = (Border)sender;
            var articleButton = (Button)articleBorder.FindName("articleButton");
            articleBorder.Background = Color("#FF580773");
            articleButton.Background = Color("#FF580773");
            AnimationMain(0.01, articleBorder);
        }
        ///<summary>Animates a tile of article preview while mouse isn't over it.</summary>
        private void ArticleMouseLeave(object sender, MouseEventArgs e)
        {
            var articleBorder = (Border)sender;
            var articleButton = (Button)articleBorder.FindName("articleButton");
            articleBorder.Background = Color("#2D033B");
            articleButton.Background = Color("#2D033B");
            AnimationMain(0.005, articleBorder);
        }
        bool wasLeft = false;


        ///<summary>Displays popup and extends its opacity.</summary>
        private async void PopupEnter(object sender, MouseEventArgs e)
        {
            wasLeft = false;
            await Task.Delay(1500);

            var obj = (Button)sender;
            var objName = obj.Name;


            if (!navBarFullyExtended || objName == "queryBtn" || objName == "sectionBtn")
            {
                if (obj.IsMouseOver)
                {
                    string popupText = "";
                    switch (objName)
                    {
                        case "queryBtn": popupText = "Ordinary search/link"; break;
                        case "sectionBtn": popupText = "Search by section"; break;
                        case "navbarBtn1": popupText = "Economy"; break;
                        case "navbarBtn2": popupText = "Law"; break;
                        case "navbarBtn3": popupText = "Opinion"; break;
                        case "navbarBtn4": popupText = "Politics"; break;
                        case "navbarBtn5": popupText = "Health"; break;
                    }

                    popupTextBlock.Text = popupText;
                    popup.IsOpen = true;

                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
                    timer.Tick += Animation;
                    timer.Start();

                    void Animation(object sender, EventArgs e)
                    {
                        if (!wasLeft && popupTextBlockBorder.Opacity < 1)
                        {
                            popupTextBlockBorder.Opacity += 0.25;
                        }
                        else
                        {
                            timer.Stop();
                        }
                    }
                }
            }
        }

        /// <summary>Disappears popup and reduces its opacity.</summary>
        private void PopupLeave(object sender, MouseEventArgs e)
        {
            wasLeft = true;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
            timer.Tick += Animation;
            timer.Start();

            void Animation(object sender, EventArgs e)
            {
                if (popupTextBlockBorder.Opacity > 0)
                {
                    popupTextBlockBorder.Opacity -= 0.25;
                }
                else
                {
                    timer.Stop();
                    popup.IsOpen = false;
                }
            }
        }


        //Helps in animation of aritcles previews.
        bool isDone = false;
        ///<summary>Extends or narrows a tile of article preview.</summary>
        private void AnimationMain(double numb, Border articleBorder)
        {
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(numb);
            timer.Tick += Animation;
            timer.Start();
            void Animation(object sender, EventArgs e)
            {
                if (articleBorder.Padding.Bottom > 1 && articleBorder.IsMouseOver)
                {
                    articleBorder.Padding = new Thickness(articleBorder.Padding.Left, articleBorder.Padding.Top, articleBorder.Padding.Right, articleBorder.Padding.Bottom - 1);
                }
                else if (articleBorder.IsMouseOver)
                {
                    isDone = true;
                    timer.Stop();
                }
                else if (articleBorder.Padding.Bottom < 10 && !articleBorder.IsMouseOver)
                {
                    articleBorder.Padding = new Thickness(articleBorder.Padding.Left, articleBorder.Padding.Top, articleBorder.Padding.Right, articleBorder.Padding.Bottom + 1);
                }
                else
                {
                    isDone = false;
                    timer.Stop();
                }
            }
        }


        /// <summary>Changes a colour of a link while entering.</summary>
        private void EnterLink(object sender, MouseEventArgs e)
        {
            var link = (Hyperlink)sender;
            link.TextDecorations.First().Pen = new Pen(Color("#4217b8"), 2.5);
        }

        /// <summary>Changes a colour of a link while leaving.</summary>
        private void LeaveLink(object sender, MouseEventArgs e)
        {
            var link = (Hyperlink)sender;
            link.TextDecorations.First().Pen = new Pen(Color("#39149e"), 2.5);
        }


        /// <summary>Changes a colour of a link block while entering.</summary>
        private void EnterSpanArticle(object sender, MouseEventArgs e)
        {
            var border = (Border)sender;
            var textBlock = (TextBlock)border.Child;

            border.Background = textBlock.Background = Color("#24056d");
        }

        /// <summary>Changes a colour of a link block while leaving.</summary>
        private void LeaveSpanArticle(object sender, MouseEventArgs e)
        {
            var border = (Border)sender;
            var textBlock = (TextBlock)border.Child;

            border.Background = textBlock.Background = Color("#200461");
        }

        /// <summary>Changes a colour of a link block while clicking it.</summary>
        private async void ClickSpanArticle(object sender, RoutedEventArgs e)
        {
            restOfBody = null;

            var link = (Hyperlink)sender;
            var linkBorder = link.Inlines.OfType<InlineUIContainer>().Select(x => x.Child).First() as Border;
            var linkTextBlock = (TextBlock)linkBorder.Child;

            linkBorder.Background = linkTextBlock.Background = Color("#24056d");

            try
            {
                var linkApi = ConvertLinkToApi(link.NavigateUri.ToString());
                var client = new HttpClient();
                var response = await client.GetAsync(linkApi);
                var article = JsonConvert.DeserializeObject<ResponseArticle>(await response.Content.ReadAsStringAsync());
                if (article.response.content == null)
                {
                    throw new NullReferenceException();
                }

                ShowArticlePlaceHolder.Visibility = Visibility.Visible;
                articleScrollViewer.Visibility = Visibility.Hidden;

                await Task.Delay(1);
                await BuildArticle(article);
            }
            catch (NullReferenceException x)
            {
                Process.Start(new ProcessStartInfo(link.NavigateUri.ToString()) { UseShellExecute = true });
            }
            catch (HttpRequestException x)
            {
                Process.Start(new ProcessStartInfo(link.NavigateUri.ToString()) { UseShellExecute = true });
            }

            await Task.Delay(100);
            linkBorder.Background = linkTextBlock.Background = Color("#1f1063");
        }


        /// <summary>Changes a colour of article from related content while entering.</summary>
        private void EnterRelatedContentBlock(object sender, MouseEventArgs e)
        {
            var border = (Border)sender;
            var textBlock = (TextBlock)border.Child;
            border.Background = textBlock.Background = Color("#302f38");
        }

        /// <summary>Changes a colour of article from related content while leaving.</summary>
        private void LeaveRelatedContentBlock(object sender, MouseEventArgs e)
        {
            var border = (Border)sender;
            var textBlock = (TextBlock)border.Child;
            border.Background = textBlock.Background = Color("#292830");
        }

        /// <summary>Changes a colour of article from related content while clicking it.</summary>
        private void ClickRelatedContentBlock(object sender, RoutedEventArgs e)
        {
            restOfBody = null;

            var link = (Hyperlink)sender;
            var linkBorder = link.Inlines.OfType<InlineUIContainer>().Select(x => x.Child).First() as Border;
            var linkTextBlock = (TextBlock)linkBorder.Child;

            linkBorder.Background = linkTextBlock.Background = Color("#2c2b33");

            RestOfMethod();
            BackToEnter();

            async void BackToEnter()
            {
                await Task.Delay(100);
                linkBorder.Background = linkTextBlock.Background = Color("#292830");
            }
            async void RestOfMethod()
            {
                try
                {
                    var linkApi = ConvertLinkToApi(link.NavigateUri.ToString());
                    var client = new HttpClient();
                    var response = await client.GetAsync(linkApi);
                    var article = JsonConvert.DeserializeObject<ResponseArticle>(await response.Content.ReadAsStringAsync());
                    if (article.response.content == null)
                    {
                        throw new NullReferenceException();
                    }

                    ShowArticlePlaceHolder.Visibility = Visibility.Visible;
                    articleScrollViewer.Visibility = Visibility.Hidden;

                    await Task.Delay(1);
                    await BuildArticle(article);
                }
                catch (NullReferenceException x)
                {
                    Process.Start(new ProcessStartInfo(link.NavigateUri.ToString()) { UseShellExecute = true });
                }
                catch (HttpRequestException x)
                {
                    Process.Start(new ProcessStartInfo(link.NavigateUri.ToString()) { UseShellExecute = true });
                }
            }
        }


        private void EnterTitle(object sender, MouseEventArgs e)
        {
            var title = (TextBlock)sender;
            title.Cursor = Cursors.Hand;
        } //***********************************************

        ///<summary>Changes cursor while leaves a title.</summary>
        private void LeaveTitle(object sender, MouseEventArgs e)
        {
            var title = (TextBlock)sender;
            title.Cursor = Cursors.Arrow;
        }


        //Determines whether navbar width is maxWidth.
        bool navBarFullyExtended;
        //Changes degree of a navBar.
        double degreeAmount = 0;
        ///<summary>Extends NavBar's width</summary>
        private void NavBarExtendFunc(object sender, RoutedEventArgs e)
        {
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.02);
            timer.Tick += animation;
            timer.Start();

            void animation(object sender, EventArgs e)
            {
                var degree = new RotateTransform(degreeAmount);

                if (!navBarFullyExtended)
                {
                    navbar.Width += 20;
                    degree.Angle += 15;
                    degreeAmount = degree.Angle;
                    navbarImg.RenderTransform = degree;

                    if (navbar.Width == 180)
                    {
                        navBarFullyExtended = true;
                        timer.Stop();
                    }
                }
                else
                {
                    navbar.Width -= 20;
                    degree.Angle -= 15;
                    degreeAmount = degree.Angle;
                    navbarImg.RenderTransform = degree;

                    if (navbar.Width == 60)
                    {
                        navBarFullyExtended = false;
                        timer.Stop();
                    }
                }
            }
        }


        /// <summary>Creates a refresh icon.</summary>
        /// <returns><see cref="Border"/></returns>
        Border CreateRefreshIcon(double margin = 0)
        {
            var refreshIcon = new ImageAwesome()
            {
                Icon = FontAwesomeIcon.Refresh,
                Foreground = Color("#dad9db"),
                Spin = true,
                SpinDuration = 1,
                Width = 30,
                Height = 30
            };
            var refreshIconBorder = new Border()
            {
                Name = "spinnerBorder",
                Child = refreshIcon,
                Width = 30,
                BorderThickness = new Thickness(0, 10, 0, 10),
                BorderBrush = Color("#18122B"),
                Style = this.FindResource("spinnerBorder") as Style
            };

            if (margin != 0)
            {
                refreshIconBorder.Margin = new Thickness(margin - 102.5, 20, margin, 20);
            }

            return refreshIconBorder;
        }
    }
}
