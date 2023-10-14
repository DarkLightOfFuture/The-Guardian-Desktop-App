using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.IO;

namespace guardian_wpf
{
    public partial class MainWindow : Window
    {
        //If paragraph is cited.
        bool isCited = false;
        //If apostrophe in quote is added.
        bool isApostropheAdded = false;
        //If quote is ended.
        bool isCitedEnd = false;
        //If after quote there is no after paragraph in live event.
        bool isLastParagraph = false;
        //If paragraph is list index.
        bool isLi = false;
        //If after paragraph is list index.
        bool isLiAfter = false;
        //Border of live event variable.
        Border liveBlockBorder = null;
        //Live event variable.
        internal TextBlock liveBlock = null;
        //Second TextBlock after articleBody.
        TextBlock articleDescription = null;
        /// <summary>Creates paragraphs in the article.</summary>
        /// <param name="linksParts">List with href and tag attributes.</param>
        /// <param name="linkIndex">Number of links in a paragraph.</param>
        /// <param name="loopLink">Index of a last used paragraph.</param>
        /// <returns><see cref="int"/> loopLink</returns>
        private int CreateTagP(TextBlock articleBody, string text, List<LinkParts> linksParts = null, int linkIndex = -1, int loopLink = -1)
        {
            //Main TextBlock of paragraph which styles it.
            articleDescription = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Justify,
                FontSize = 20,
                Foreground = Color("#FFCCC9D6")
            };

            //Applies HTML's special signs.
            text = text.Replace("<br>", "\n").Replace("&nbsp;", "\u00A0").Replace("&quot;", "\"").Replace("&apos;", "'")
                    .Replace("&amp;", null).Replace("&lt;", "<").Replace("&gt;", ">").Replace("<br>", "\n").Replace("<br incopy-break-type=\"webbreak\">", "\n")
                    .Replace("<sup>", string.Empty).Replace("</sup>", string.Empty);

            //Counts number of links inside paragraph.
            var textLoop = text;
            while (textLoop.Contains("</a>"))
            {
                var htmlEnd = textLoop.IndexOf("</a>");
                textLoop = textLoop.Substring(htmlEnd + 4, textLoop.Length - htmlEnd - 4);
                loopLink++;
            }

            //Builds a list.
            if (isLi)
            {
                text = String.Format($"  • {text}");
                isLi = false;
            }
            //Determines width of text
            var toLiveWidth = 0;
            if (isLive)
            {
                toLiveWidth = 50;
            }
            //Determines if piece of text is inside of <strong> tag.
            bool isStrong = false;
            //Determines if piece of text is inside of <em> tag.
            bool isEm = false;
            //Determines if piece of text is inside of <strike> tag.
            bool isStrike = false;
            //Applies paragraph to articleDescription where are links.
            if (linkIndex != -1 && !text.Contains("<span>"))
            {
                //Number of link in paragraph.
                var i = 0;
                //Text before link.
                string textA = null;
                //Formatted text after e.g. using of strong or em tag.
                var textFormatted = new TextBlock() { Margin = new Thickness(0, 0, 0, 10), TextWrapping = TextWrapping.Wrap, Width = 920 - toLiveWidth };
                //if (isLiAfter) { textFormatted.Padding = new Thickness(0, 0, 0, 100); isLiAfter = false; MessageBox.Show("ff"); }

                //Executes itself till every link will be added.
                while (i <= loopLink)
                {
                    var link = new Hyperlink();

                    //Sets text for textA and text when it's first stage of while loop.
                    if (textA == null)
                    {
                        textA = text.Substring(0, text.IndexOf("<a"));

                        if (textA.Length != 0 && textA[textA.Length - 1].ToString() != " " || textA.Length != 0 && textA[textA.Length - 1].ToString() != ".")
                        {
                            textA = string.Format($"{textA} ");
                        }

                        text = text.Substring(text.IndexOf("</a") + 4, text.Length - text.IndexOf("</a") - 4);
                    }


                    var formattedHTMLText = FormatHTMLText(textA, isEm, isStrong, isStrike, textFormatted);
                    textFormatted = formattedHTMLText.textFormatted;
                    isStrong = formattedHTMLText.isStrong; isEm = formattedHTMLText.isEm;


                    //If textA is before last link, not after it.
                    if (i < loopLink)
                    {
                        var href = linksParts.ElementAt(linkIndex + i).href;
                        var tag = linksParts.ElementAt(linkIndex + i).tag;

                        var decorationsCollection = new TextDecorationCollection();
                        var underLine = new TextDecoration { Pen = new Pen(Color("#39149e"), 2.5) };
                        decorationsCollection.Add(underLine);

                        ApplyLinkStyle(link, tag, isEm, isStrong);
                        link.MouseEnter += EnterLink;
                        link.MouseLeave += LeaveLink;
                        link.Click += OpenLink;
                        link.NavigateUri = new Uri(href);
                        link.Foreground = Color("#dfdbd2");
                        link.TextDecorations = decorationsCollection;
                    }
                    textFormatted.Inlines.Add(link);

                    i++;
                    //Changes position of textA in text and cuts off applied text from text.
                    if (i < loopLink)
                    {
                        var endingSigns = new List<string>() { " ", "(", "<" };
                        var openingSigns = new List<string>() { " ", ")", ">", ";", ";", ".", "," };
                        textA = text.Substring(0, text.IndexOf("<a"));

                        try
                        {
                            if (!endingSigns.Contains(textA[textA.Length - 1].ToString()))
                            {
                                textA = string.Format($"{textA} ");
                            }
                            else if (!openingSigns.Contains(textA[textA.Length - 1].ToString()))
                            {
                                textA = string.Format($" {textA}");
                            }
                        }
                        catch (Exception e) { }

                        text = text.Substring(text.IndexOf("</a>") + 4, text.Length - text.IndexOf("</a") - 4);
                    }
                    //If last link was added  and text after this left to add.
                    else
                    {
                        textA = text;
                    }
                }

                //If it's live article.
                if (isLive)
                {
                    liveBlock.Inlines.Add(textFormatted);
                }
                //If it's casual article.
                else
                {
                    articleDescription.Inlines.Add(textFormatted);
                }
            }
            //Applies paragraph in which there is link block.
            else if (text.Contains("<span>"))
            {
                //link's parts
                var href = linksParts.ElementAt(linkIndex).href;
                var tag = linksParts.ElementAt(linkIndex).tag;
                linkIndex = 1;

                //Deletes tags signs in link block.
                text = text.Replace("<span>Related: </span>", string.Empty)
                                .Replace("<span>Related:</span>", string.Empty).Replace("<span> Related: </span>", string.Empty);

                MouseEventHandler mouseEnter = EnterSpanArticle;
                MouseEventHandler mouseLeave = LeaveSpanArticle;
                RoutedEventHandler mouseClick = ClickSpanArticle;
                var link = CreateLinkBlock(tag, href, mouseEnter, mouseLeave, mouseClick, false);

                //If it's live article.
                if (isLive) { liveBlock.Inlines.Add(link); }
                //If it's casual article.
                else { articleDescription.Inlines.Add(link); }
            }
            //If text doesn't contains links.
            else if (text != null)
            {
                //Prevents being following paragraph in the same line like this one.
                if (text.Length < 60)
                {
                    text = String.Format($"{text}\t\t\t");
                }

                //If it's time tag, builds time info.
                if (text.Contains("<time") && !text.Contains("Updated <time"))
                {
                    isLive = true;
                    var dateBegin = text.IndexOf("<time datetime=\"") + 16;

                    var date = DateTime.Parse(text.Substring(dateBegin, 23)).AddHours(2);
                    var timeSpan = DateTime.Now - date;

                    string timeAgo = null;
                    if (timeSpan.TotalDays >= 365)
                    {
                        string plural = null;
                        if (Math.Floor(timeSpan.TotalDays / 365) > 1)
                        {
                            plural = "s";
                        }
                        else
                        {
                            plural = string.Empty;
                        }

                        timeAgo = String.Format($"{Math.Floor(timeSpan.TotalDays / 365).ToString()} year{plural} ago ");
                    }
                    else if (timeSpan.TotalDays >= 30)
                    {
                        string plural = null;
                        if (Math.Floor(timeSpan.TotalDays / 30) > 1)
                        {
                            plural = "s";
                        }
                        else
                        {
                            plural = string.Empty;
                        }

                        timeAgo = String.Format($"{Math.Floor(timeSpan.TotalDays / 30).ToString()} month{plural} ago ");
                    }
                    else if (timeSpan.TotalHours >= 24)
                    {
                        string plural = null;
                        if (timeSpan.TotalHours != 1)
                        {
                            plural = "s";
                        }
                        else
                        {
                            plural = string.Empty;
                        }

                        timeAgo = String.Format($"{Math.Floor(timeSpan.TotalHours / 24).ToString()} day{plural} ago ");
                    }
                    else if (timeSpan.TotalMinutes >= 60)
                    {
                        string plural = null;
                        if (timeSpan.TotalMinutes != 60)
                        {
                            plural = "s";
                        }
                        else
                        {
                            plural = string.Empty;
                        }

                        timeAgo = String.Format($"{Math.Floor(timeSpan.TotalMinutes / 60)} hour{plural} ago");
                    }
                    else if (timeSpan.TotalMinutes < 60 && timeSpan.TotalMinutes != 0)
                    {
                        string plural = null;
                        if (timeSpan.TotalMinutes != 1)
                        {
                            plural = "s";
                        }
                        else
                        {
                            plural = string.Empty;
                        }

                        timeAgo = String.Format($"{Math.Floor(timeSpan.TotalMinutes / 1)} minute{plural} ago ");
                    }
                    else if (timeSpan.TotalSeconds < 60)
                    {
                        string plural = null;
                        if (timeSpan.TotalSeconds != 1)
                        {
                            plural = "s";
                        }
                        else
                        {
                            plural = string.Empty;
                        }

                        timeAgo = String.Format($"{timeSpan.TotalSeconds} second{plural} ago ");
                    }

                    var finalText = String.Format($"{timeAgo} •  {date}");
                    var textSpan = new TextBlock(new Run(finalText)) { FontSize = 13, Margin = new Thickness(0, 0, 0, 25) };

                    var liveBlockSchema = new TextBlock
                    {
                        Background = Color("#2b0a50"),
                        TextWrapping = TextWrapping.Wrap,
                        Width = 900,
                        Padding = new Thickness(15, 10, 15, 20),
                    };
                    liveBlockBorder = new Border
                    {
                        CornerRadius = new CornerRadius(10),
                        BorderThickness = new Thickness(0, 5, 0, 0),
                        BorderBrush = Color("#1430ad"),
                        Margin = new Thickness(10, 0, 0, 15)
                    };

                    //If it's first liveBlock.
                    if (liveBlock == null)
                    {
                        liveBlock = liveBlockSchema;
                        liveBlock.Inlines.Add(textSpan);
                        liveBlock.Inlines.Add(new LineBreak());
                    }
                    else
                    {
                        articleDescription.Inlines.Add(liveBlockBorder);

                        liveBlockBorder.Child = liveBlock;
                        liveBlock = liveBlockSchema;
                        liveBlock.Inlines.Add(textSpan);
                        liveBlock.Inlines.Add(new LineBreak());
                    }
                }
                //If it's time info about update. 
                else if (text.Contains("<time")) { }
                //If it's ordinary paragraph or quoting.
                else
                {
                    double liAfter = 0;
                    if (isLiAfter)
                    {
                        liAfter = 12.5;
                        isLiAfter = false;
                    }

                    var textFormattedTB = new TextBlock
                    {
                        Margin = new Thickness(0, 0, 0, 10 - liAfter),
                        TextWrapping = TextWrapping.Wrap,
                        Width = 920 - toLiveWidth
                    };

                    //If it's quote inside liveBlock.
                    if (isCited)
                    {
                        var citedText = new TextBlock() { TextWrapping = TextWrapping.Wrap };

                        if (isLastParagraph) { citedText.Margin = new Thickness(0, 0, 0, 10); }
                        else if (isCitedEnd) { citedText.Margin = new Thickness(0, 0, 0, 14); }

                        //If apostrophe wasn't added.
                        if (!isApostropheAdded)
                        {
                            var apostrophe = new System.Windows.Controls.Image
                            {
                                Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons\\apostrophe.png"))),
                                Width = 48,
                                Height = 40,
                                Margin = new Thickness(-3, -7, -2.5, -10)
                            };
                            citedText.Inlines.Add(apostrophe);

                            isApostropheAdded = true;
                        }

                        isLastParagraph = false;

                        var textFormatted = FormatHTMLText(text);
                        textFormatted.FontStyle = FontStyles.Italic;
                        textFormatted.FontSize = 18.5;
                        citedText.Inlines.Add(textFormatted);
                        if (isLive) { liveBlock.Inlines.Add(citedText); }
                        else
                        {
                            citedText.Margin = new Thickness(0, 20, 0, 20);
                            articleDescription.Inlines.Add(citedText);
                        }

                        //If quote ends.
                        if (isCitedEnd)
                        {
                            isCitedEnd = isCited = isApostropheAdded = false;
                        }
                    }
                    //If it's ordinary paragraph.
                    else
                    {
                        var textFormatted = new TextBlock();
                        var formattedHTMLText = FormatHTMLText(text, isEm, isStrong, isStrike, textFormatted);
                        textFormatted = formattedHTMLText.textFormatted;
                        isStrong = formattedHTMLText.isStrong; isEm = formattedHTMLText.isEm;

                        textFormattedTB.Inlines.Add(textFormatted);

                        //If it's live article.
                        if (isLive) { liveBlock.Inlines.Add(textFormattedTB); }
                        //If it's ordinary article.
                        else { articleDescription.Inlines.Add(textFormattedTB); }
                    }
                }
            }
            articleBody.Inlines.Add(articleDescription);
            return loopLink;
        }

        bool was = false;
        ///<summary>Applies HTML style.</summary>
        private FormattedHTMLText FormatHTMLText(string text, bool isEm, bool isStrong, bool isStrike, TextBlock textFormatted)
        {

            textFormatted.TextWrapping = TextWrapping.Wrap;
            //Piece of text inside of tag or whole text.
            var content = "";

            var openingTags = CreateTagsList();

            var endingTags = new List<string>();
            foreach (var tag in openingTags)
            {
                endingTags.Add(string.Format($"{tag.Substring(0, 1)}/{tag.Substring(1, tag.Length - 1)}"));
            }

            var formattingTags = CreateFormattingTagsList(isStrong, isEm, isStrike);

            //Styles text, bit by bit.
            for (int x = 0; x < text.Length && !endingTags.Contains(text.Substring(x, text.Length - x).Trim()); x++)
            {
                //If text contains formatting tags.
                var isFormatted = false;
                for (var i = 0; i < 5; i++)
                {
                    if (text.Contains(openingTags.ElementAt(i)) || text.Contains(endingTags.ElementAt(i))) { isFormatted = true; }
                }

                if (isFormatted)
                {
                    var result = CheckHTMLText(formattingTags, x, text, content);
                    formattingTags = result.tags;
                    x = result.x;
                    content = result.content;

                    if (formattingTags.ElementAt(0).isTag || formattingTags.ElementAt(4).isTag) { isStrong = true; }
                    else if (!formattingTags.ElementAt(0).isTag && !formattingTags.ElementAt(4).isTag) { isStrong = false; }

                    if (formattingTags.ElementAt(1).isTag || formattingTags.ElementAt(3).isTag) { isEm = true; }
                    else if (!formattingTags.ElementAt(1).isTag && !formattingTags.ElementAt(3).isTag) { isEm = false; }

                    if (formattingTags.ElementAt(2).isTag) { isStrike = true; }
                    else if (!formattingTags.ElementAt(2).isTag) { isStrike = false; }


                    try
                    {
                        if (x == text.Length - 1 || text[x + 1].ToString() == "<")
                        {
                            var bitSpan = new Span(new Run(content));
                            if (isStrong) { bitSpan.FontWeight = FontWeights.Bold; }

                            if (isEm) { bitSpan.FontStyle = FontStyles.Italic; }

                            if (isStrike) { bitSpan.TextDecorations = TextDecorations.Strikethrough; }

                            textFormatted.Inlines.Add(bitSpan);
                            content = "";
                        }
                    }
                    catch (Exception e) { }

                }
                else
                {
                    textFormatted.Inlines.Add(text);
                    break;
                }
            }

            return new FormattedHTMLText { isEm = isEm, isStrong = isStrong, textFormatted = textFormatted };
        }

        ///<summary>Applies HTML style.</summary>
        private Span FormatHTMLText(string text)
        {
            //Element to return.
            var textFormatted = new Span();

            var isStrong = false;
            var isEm = false;
            var isStrike = false;
            //Piece of text inside of tag or whole text.
            var content = "";

            var openingTags = CreateTagsList();

            var endingTags = new List<string>();
            foreach (var tag in openingTags)
            {
                endingTags.Add(string.Format($"{tag.Substring(0, 1)}/{tag.Substring(1, tag.Length - 1)}"));
            }

            var formattingTags = CreateFormattingTagsList(isStrong, isEm, isStrike);

            //Styles text, bit by bit.
            for (int x = 0; x < text.Length && !endingTags.Contains(text.Substring(x, text.Length - x).Trim()); x++)
            {
                //If text contains formatting tags.
                var isFormatted = false;
                for (var i = 0; i < 5; i++)
                {
                    if (text.Contains(openingTags.ElementAt(i)) || text.Contains(endingTags.ElementAt(i))) { isFormatted = true; }
                }

                if (isFormatted)
                {
                    var result = CheckHTMLText(formattingTags, x, text, content);
                    formattingTags = result.tags;
                    x = result.x;
                    content = result.content;

                    if (formattingTags.ElementAt(0).isTag || formattingTags.ElementAt(4).isTag) { isStrong = true; }
                    else if (!formattingTags.ElementAt(0).isTag && !formattingTags.ElementAt(4).isTag) { isStrong = false; }

                    if (formattingTags.ElementAt(1).isTag || formattingTags.ElementAt(3).isTag) { isEm = true; }
                    else if (!formattingTags.ElementAt(1).isTag && !formattingTags.ElementAt(3).isTag) { isEm = false; }

                    if (formattingTags.ElementAt(2).isTag) { isStrike = true; }
                    else if (!formattingTags.ElementAt(2).isTag) { isStrike = false; }

                    try
                    {
                        if (x == text.Length - 1 || text[x + 1].ToString() == "<")
                        {
                            var bitSpan = new Span(new Run(content));
                            if (isStrong) { bitSpan.FontWeight = FontWeights.Bold; }

                            if (isEm) { bitSpan.FontStyle = FontStyles.Italic; }

                            if (isStrike) { bitSpan.TextDecorations = TextDecorations.Strikethrough; }

                            textFormatted.Inlines.Add(bitSpan);
                            content = "";
                        }
                    }
                    catch (Exception e) { }

                }
                else
                {
                    textFormatted.Inlines.Add(text);
                    break;
                }
            }

            return textFormatted;
        }

        /// <summary>Creates list of tags with their determining bool isTag, openning and ending tags.</summary>
        private List<TagAttributes> CreateFormattingTagsList(bool isStrong, bool isEm, bool isStrike)
        {
            var formattingTags = new List<TagAttributes>();

            formattingTags.Add(new TagAttributes { openningTag = "<b>", endingTag = "</b>", isTag = isStrong });
            formattingTags.Add(new TagAttributes { openningTag = "<i>", endingTag = "</i>", isTag = isEm });
            formattingTags.Add(new TagAttributes { openningTag = "<s>", endingTag = "</s>", isTag = isStrike });
            formattingTags.Add(new TagAttributes { openningTag = "<em>", endingTag = "</em>", isTag = isEm });
            formattingTags.Add(new TagAttributes { openningTag = "<strong>", endingTag = "</strong>", isTag = isStrong });

            return formattingTags;
        }

        ///<summary>Checks if in html text there are formatting tags and allows use formatting styles in application text.</summary>
        private FormattedHTMLTags CheckHTMLText(List<TagAttributes> tags, int x, string text, string content)
        {
            try
            {
                while (text.Substring(x, 4).Contains("</s>") || text.Substring(x, 4).Contains("</b>") || text.Substring(x, 4).Contains("</i>") ||
                    text.Substring(x, 5).Contains("</em>") || text.Substring(x, 9).Contains("</strong>"))
                {
                    foreach (var tag in tags)
                    {
                        var tagLength = tag.endingTag.Length;

                        if (x + tagLength <= text.Length && text.Substring(x, tagLength).Contains(tag.endingTag))
                        {
                            tag.isTag = false;

                            try { x += tagLength; }
                            catch (Exception e) { break; }
                        }
                    }
                }
            }
            catch (Exception e) { }
            try
            {
                while (text.Substring(x, 3).Contains("<s>") || text.Substring(x, 3).Contains("<b>") || text.Substring(x, 3).Contains("<i>") ||
                    text.Substring(x, 4).Contains("<em>") || text.Substring(x, 8).Contains("<strong>"))
                {
                    foreach (var tag in tags)
                    {
                        var tagLength = tag.openningTag.Length;

                        if (x + tagLength <= text.Length && text.Substring(x, tagLength).Contains(tag.openningTag))
                        {
                            tag.isTag = true;

                            try { x += tagLength; }
                            catch (Exception e) { break; }
                        }
                    }
                }
            }
            catch (Exception e) { }

            try
            {
                content += text[x].ToString();
            }
            catch (Exception e) { }

            return new FormattedHTMLTags { tags = tags, content = content, x = x };
        }

        ///<summary>Creates tags list useful in formatting html text.</summary>
        private List<string> CreateTagsList()
        {
            var tags = new List<string>() { "<s>", "<b>", "<i>", "<em>", "<strong>" };

            return tags;
        }

        /// <summary>Applies style of a link.</summary>
        private void ApplyLinkStyle(Hyperlink link, string text, bool isEm, bool isStrong)
        {
            //Applies HTML's special signs.
            text = text.Replace("<br>", "\n").Replace("&nbsp;", "\u00A0").Replace("&quot;", "\"").Replace("&apos;", "'")
                    .Replace("&amp;", null).Replace("&lt;", "<").Replace("&gt;", ">").Replace("<br>", "\n").Replace("<br incopy-break-type=\"webbreak\">", "\n")
                    .Replace("<sup>", string.Empty).Replace("</sup>", string.Empty);

            if (text.Contains("<em>"))
            {
                isEm = true;
                text = text.Replace("<em>", string.Empty).Replace("</em>", string.Empty);
            }
            if (text.Contains("<strong>"))
            {
                isStrong = true;
                text = text.Replace("<strong>", string.Empty).Replace("</strong>", string.Empty);
            }
            text = text.Trim();

            var content = "";
            for (var x = 0; x < text.Length; x++)
            {
                content += text[x].ToString();
                if (text[x].ToString() == " " || text[x].ToString() == "@" || x == text.Length - 1)
                {
                    var textBlock = new TextBlock(new Run(content));

                    if (isEm) { textBlock.FontStyle = FontStyles.Italic; }
                    if (isStrong) { textBlock.FontWeight = FontWeights.Bold; }

                    link.Inlines.Add(textBlock);
                    content = "";
                }
            }
        }

        /// <summary>Applies style of a link in block.</summary>
        private TextBlock ApplyLinkStyle(string text)
        {
            text = text.Trim();

            if (text.Contains("|"))
            {
                text = text.Substring(0, text.IndexOf("|"));
            }

            var textBlock = new TextBlock(new Run(text))
            {
                TextWrapping = TextWrapping.Wrap,
                FontWeight = FontWeights.Bold
            };

            return textBlock;
        }

        ///<summary>Creates a text for title and then adds it in the article.</summary>
        private void CreateTagTitle(ResponseArticle article)
        {
            string text = article.response.content.webTitle;

            Tags tags = null;
            if (article.response.content.tags.Count > 0 && article.response.content.tags.ElementAt(0) != null)
            {
                tags = article.response.content.tags.ElementAt(0);
            }

            if (text.Contains("|") && text.Length - text.IndexOf("|") <= 40)
            {
                text = text.Substring(0, text.IndexOf("|"));
            }

            var articleTitle = new TextBlock
            {
                Name = "title",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 40,
                Margin = new Thickness(10, 0, 10, 0),
                FontWeight = FontWeights.Bold,
                Foreground = Color("#FFCCC9D6"),
                Text = text
            };

            articleTitle.MouseLeftButtonDown += CopyTitle;
            articleTitle.MouseEnter += EnterTitle;
            articleTitle.MouseLeave += LeaveTitle;
            articleTitle.Inlines.Add(new LineBreak());

            if (tags != null)
            {
                var authorName = new TextBlock(new Run(tags.webTitle))
                {
                    FontWeight = FontWeights.Normal,
                    Foreground = Color("#4b0875"),
                };
                articleTitle.Inlines.Add(authorName);
            }

            articleView.Children.Add(articleTitle);
        }

        ///<summary>Creates a header and then adds it in the article.</summary>
        private void CreateTagH(TextBlock articleBody, List<string> hTagsContent, int hTagAmount, int hTagIndex)
        {
            var liveWidth = 0;
            var marginTop = 0;
            if (!isLive) { liveWidth = 50; }
            if (isLive) { marginTop = 15; }

            for (int x = hTagIndex; x < hTagAmount + hTagIndex; x++)
            {
                bool isEm = false;
                bool isStrong = false;

                var text = hTagsContent.ElementAt(x);

                if (text.Contains("<em>"))
                {
                    isEm = true;
                    text = text.Replace("<em>", string.Empty).Replace("</em>", string.Empty);
                }
                if (text.Contains("<strong>"))
                {
                    isStrong = true;
                    text = text.Replace("<strong>", string.Empty).Replace("</strong>", string.Empty);
                }

                var articleHeader = new TextBlock
                {
                    Margin = new Thickness(0, 15 - marginTop, 0, 2.5),
                    Width = 970 - liveWidth,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Left,
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Foreground = Color("#FFCCC9D6"),
                    Text = text
                };

                //Set Margins for group of headers/Sets margin for header.
                if (!isLive && hTagAmount > 1)
                {
                    if (x == hTagIndex)
                    {
                        articleHeader.Margin = new Thickness(0, 15, 0, 0);
                    }
                    else if (x > hTagIndex && x != hTagIndex + hTagAmount)
                    {
                        articleHeader.Margin = new Thickness(0, 0, 0, 0);
                    }
                    else if (x == hTagIndex + hTagAmount)
                    {
                        articleHeader.Margin = new Thickness(0, 0, 0, 2.5);
                    }
                }

                if (isStrong) { articleHeader.FontWeight = FontWeights.Black; }
                if (isEm) { articleHeader.FontStyle = FontStyles.Italic; }

                if (isLive)
                {
                    liveBlock.Inlines.Add(articleHeader);
                    liveBlock.Inlines.Add(new LineBreak());
                }
                else
                {
                    if (articleDescription == null)
                    {
                        articleDescription = new TextBlock
                        {
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Justify,
                            FontSize = 20,
                            Foreground = Color("#FFCCC9D6")
                        };
                    }

                    articleDescription.Inlines.Add(articleHeader);
                }
            }
        }

        /// <summary>Sets thumbnail in the article.</summary>
        /// <param name="thumbnailBase">Thumbnail URL.</param>
        private void CreateTagImg(TextBlock articleBody, Results article)
        {
            System.Windows.Controls.Image thumbnailIMG = null;
            var space = new TextBlock(new LineBreak());

            if (article.fields.thumbnail != null)
            {
                string thumbnail = article.fields.thumbnail;
                var source = new BitmapImage(new Uri(thumbnail));

                space.Padding = new Thickness(0, 0, 0, 25);

                thumbnailIMG = new System.Windows.Controls.Image
                {
                    Width = 940,
                    Height = 560,
                    Source = source,
                    Stretch = Stretch.Fill,
                    Margin = new Thickness(-10, 0, -10, -17.5)
                };
            }

            System.Windows.Controls.Image author = null;
            var topMargin = 100;
            try
            {
                author = new System.Windows.Controls.Image
                {
                    Source = new BitmapImage(new Uri(article.tags.ElementAt(0).bylineLargeImageUrl)),
                    Width = 240,
                    Height = 200,
                    Stretch = Stretch.Fill,
                    Margin = new Thickness(660, -210, 0, -10)
                };
            }
            catch (Exception e)
            {
                topMargin = 10;
            }

            var bar = new System.Windows.Controls.Image
            {
                Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons\\bar.png"))),
                Width = 940,
                Height = 50,
                Stretch = Stretch.Fill,
                Margin = new Thickness(-10, topMargin, -10, 0)
            };

            articleBody.Inlines.Add(bar);
            if (author != null) { articleBody.Inlines.Add(author); }

            var trailText = string.Format($"{article.fields.trailText}.");
            trailText = trailText.Replace("<br>", String.Empty);
            var trailTextTB = new TextBlock()
            {
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Justify,
                Margin = new Thickness(0, 10, 0, 10),
                FontSize = 20,
                Foreground = Color("#FFCCC9D6")
            };
            var formattedHTMLText = FormatHTMLText(trailText, false, false, false, trailTextTB);
            trailTextTB = formattedHTMLText.textFormatted;

            var bar2 = new System.Windows.Controls.Image
            {
                Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons\\barSecond.png"))),
                Width = 940,
                Height = 2.25,
                Stretch = Stretch.Fill,
                Margin = new Thickness(-10, 0, -10, 10)
            };

            articleBody.Inlines.Add(trailTextTB);
            articleBody.Inlines.Add(bar2);

            var articleDate = ArticleDate(DateTime.Parse(article.webPublicationDate).AddHours(1));
            var articleDateTextBlock = new TextBlock(new Run(articleDate))
            {
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Justify,
                Margin = new Thickness(0, 0, 300, 10),
                FontSize = 15,
                Foreground = Color("#FFCCC9D6")
            };
            articleBody.Inlines.Add(articleDateTextBlock);

            var bar3 = new System.Windows.Controls.Image
            {
                Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons\\barSecond.png"))),
                Width = 940,
                Height = 2.25,
                Stretch = Stretch.Fill,
                Margin = new Thickness(-10, 0, -10, 10)
            };
            if (thumbnailIMG != null) { articleBody.Inlines.Add(thumbnailIMG); }
            articleBody.Inlines.Add(bar3);
            articleBody.Inlines.Add(space);

            string ArticleDate(DateTime date)
            {
                var monthNames = new List<string>()
                { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

                var year = date.Year;
                var month = monthNames[date.Month - 1];
                var dayName = date.DayOfWeek.ToString().Substring(0, 3);
                var day = date.Day.ToString();
                var time = date.TimeOfDay.ToString();

                return string.Format($"{time} {dayName} {day} {month} {year}");
            }
        }

        /// <summary>Sets image in the article.</summary>
        /// <param name="thumbnailBase">Thumbnail URL.</param>
        private void CreateTagImg(TextBlock articleBody, List<string> imagesUrl, int loopSize, int imageIndex)
        {
            try
            {
                for (int x = imageIndex; x < imageIndex + loopSize; x++)
                {
                    var source = new BitmapImage(new Uri(imagesUrl.ElementAt(x)));

                    var img = new System.Windows.Controls.Image
                    {
                        Width = 930,
                        Height = 560,
                        Source = source,
                        Stretch = Stretch.Fill
                    };

                    //Sets margins for group of images/Sets margin for image.
                    if (x == imageIndex && x != imageIndex + loopSize - 1)
                    {
                        img.Margin = new Thickness(0, 10, 0, 0);
                    }
                    else if (x == imageIndex)
                    {
                        img.Margin = new Thickness(0, 10, 0, 10);
                    }
                    else if (x != imageIndex && x != imageIndex + loopSize - 1)
                    {
                        img.Margin = new Thickness(0, 5, 0, 0);
                    }
                    else
                    {
                        img.Margin = new Thickness(0, 5, 0, 10);
                    }


                    if (liveBlock != null)
                    {
                        liveBlock.Inlines.Add(img);
                    }
                    else
                    {
                        articleDescription.Inlines.Add(img);
                    }
                }
            }
            catch (ArgumentNullException e)
            { }
        }

        internal class FormattedHTMLTags
        {
            public List<TagAttributes> tags { get; set; } 
            public int x { get; set; }
            public string content { get; set; }
        }

        //Tags like <strong>, <em>, <s> and similar to it.
        internal class TagAttributes
        {
            public string openningTag { get; set; }
            public string endingTag { get; set; }
            public bool isTag { get; set; }
        }
    }
}
