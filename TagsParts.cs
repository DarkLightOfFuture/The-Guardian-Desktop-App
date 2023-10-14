using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace guardian_wpf
{
    public class LinkParts
    {
        public string href { get; set; }
        public string tag { get; set; }
    }

    public class ImgParts
    {
        public List<string> imagesUrl { get; set; }
        public List<int> imgIndexList { get; set; }
    } 

    public class HParts
    {
        public List<int> hTags { get; set; }
        public List<string> hTagsContent { get; set; }
    }

    public class BlockQuoteParts
    {
        public List<int> blockQuoteTags { get; set; }
        public List<int> blockQuoteEndingTags { get; set; }
    }

    public class FormattedHTMLText
    {
        public bool isStrong { get; set; }
        public bool isEm { get; set; }
        public TextBlock textFormatted { get; set; }
    }
}
