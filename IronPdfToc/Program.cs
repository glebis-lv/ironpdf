using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using IronPdf;

namespace IronPdfToc
{
    internal class Program
    {
        private static List<TitleDto> _titles = new List<TitleDto> {new TitleDto{ Title = "1st chapter main title", Indentation=  0 },
            new TitleDto{ Title = "1st chapter 2nd level title", Indentation= 1 },
            new TitleDto{ Title = "1st chapter 3rd level title", Indentation=2 },
            new TitleDto{ Title = "2nd chapter main title", Indentation = 0 },
            new TitleDto{ Title = "2nd chapter 2nd level title", Indentation=  1 },
            new TitleDto{ Title = "2nd chapter 3rd level title" , Indentation= 2 } };

        public class TitleDto
        {
            public string Title { get; set; }
            public int Indentation { get; set; }
        }

        private static readonly ChapterInfo Chapter1 = new ChapterInfo
        {
            Title = "Intro",
            Content = @$"
                <h1>{_titles[0].Title}</h1>
                <div>This is the intro of the page</div>

                <div style='page-break-after: always;'>&nbsp;</div>
                    <div style='page-break-after: always;'>&nbsp;</div>

                <h2>{_titles[1].Title}</h2>

                <div style='page-break-after: always;'>&nbsp;</div>
                    <div style='page-break-after: always;'>&nbsp;</div>

                <h3>{_titles[2].Title}</h3>
                <div style='page-break-after: always;'>&nbsp;</div>
                <div style='page-break-after: always;'>&nbsp;</div>"
        };

        private static readonly ChapterInfo Chapter2 = new ChapterInfo
        {
            Title = "Person list",
            Content = @$"
                <h1>{_titles[3].Title}</h1>
                <div>A lot of persons over here...</div>
                <div style='page-break-after: always;'>&nbsp;</div>
                <div style='page-break-after: always;'>&nbsp;</div>
                <h2>{_titles[4].Title}</h2>
                <div style='page-break-after: always;'>&nbsp;</div>
                <div style='page-break-after: always;'>&nbsp;</div>
                <div style='page-break-after: always;'>&nbsp;</div>
                <h3>{_titles[5].Title}</h3>
                <div style='page-break-after: always;'>&nbsp;</div>"
        };

        private static void Main()
        {
            var htmlToPdfRenderer = CreateRenderer();
            var chapterInfos = new List<ChapterInfo>
            {
                new ChapterInfo
                {
                    Title = "great chapter 1",
                    Content = Chapter1.Content
                },
                new ChapterInfo
                {
                    Title = "great chapter 2",
                    Content = Chapter2.Content
                }
            };

            var pdfDocuments = new List<PdfDocument>();

            foreach (var chapterInfo in chapterInfos)
            {
                var startPage = pdfDocuments.Sum(d => d.PageCount)
                    + GetTableOfContentsPagesCount(HtmlHelper.GetHtmlNodesListFromMarkup(chapterInfo.Content), CreateTocRenderer());
                var pdfDocument = CreatePdfDocument(htmlToPdfRenderer, chapterInfo.Title, chapterInfo.Content, startPage);
                pdfDocuments.Add(pdfDocument);
            }

            var toc = CreateToCDocument(CreateTocRenderer(), pdfDocuments);
            pdfDocuments.Insert(0, toc);

            var mergedDocument = PdfDocument.Merge(pdfDocuments);
            mergedDocument.SaveAs("HtmlToPDF.pdf");
        }

        private static List<TitleDto> FindTitles(string pageAllText)
        {
            return _titles.Where(t => pageAllText.Contains(t.Title)).ToList();
        }

        private static List<TitleDto> FindTitles(List<HtmlNode> htmlNodes)
        {
            return htmlNodes.Where(node => HtmlHelper.HTML_HEADERS.Contains(node.Name))
                .Select(node => new TitleDto { Title = node.InnerText, Indentation = GetHeaderIndentation(node.Name) })
                .ToList();
        }

        private static int GetTableOfContentsPagesCount(List<HtmlNode> htmlNodes, HtmlToPdf htmlToPdfRenderer)
        {
            var titles = FindTitles(htmlNodes);
            var html = default(string);
            foreach (var tocItem in titles)
            {
                html += $"<h{tocItem.Indentation + 1}>{tocItem.Title} ... {0}</h{tocItem.Indentation + 1}>";
            }

            return htmlToPdfRenderer.RenderHtmlAsPdf(html).PageCount;
        }

        private static int GetHeaderIndentation(string headerName)
        {
            Regex regex = new Regex(@"^h([1-6]{1})$");
            Match match = regex.Match(headerName);

            if (match.Success)
                return int.Parse(match.Groups[1].Value);

            return 0;
        }


        private static List<HtmlNode> FindPageNodes(string pageText, List<HtmlNode> htmlNodes)
        {
            var pageNodes = new List<HtmlNode>();
            foreach (var htmlNode in htmlNodes)
            {
                if (pageText.StartsWith(htmlNode.InnerHtml))
                {
                    pageNodes.Add(htmlNode);
                    pageText = pageText.Substring(htmlNode.InnerHtml.Length);
                }
                else
                    break;
            }

            return pageNodes;
        }

        private static PdfDocument CreatePdfDocument(HtmlToPdf htmlToPdfRenderer,
            string pdfDocumentName,
            string html,
            int startingPage)
        {
            htmlToPdfRenderer.PrintOptions.FirstPageNumber = startingPage;

            var pdfDocument = htmlToPdfRenderer.RenderHtmlAsPdf(html);
            var htmlNodes = HtmlHelper.GetHtmlNodesListFromMarkup(html);
            htmlNodes = htmlNodes.Where(node => node.InnerText != "&nbsp;").ToList();
            for (var a = 0; a < pdfDocument.Pages.Count; a++)
            {
                var pageText = pdfDocument.ExtractTextFromPage(a);
                var pageHtmlNodes = FindPageNodes(pageText, htmlNodes);
                if (!pageHtmlNodes.Any())
                    continue;

                htmlNodes = htmlNodes.GetRange(pageHtmlNodes.Count, htmlNodes.Count - pageHtmlNodes.Count);
                var titles = FindTitles(pageHtmlNodes);

                foreach (var title in titles)
                {
                    pdfDocument.BookMarks.AddBookMarkAtStart(title.Title, a, title.Indentation);
                }
            }

            return pdfDocument;
        }

        private static PdfDocument CreateToCDocument(HtmlToPdf htmlToPdfRenderer, List<PdfDocument> pdfDocuments)
        {
            var tocItems = new List<TableOfContents>();
            var startPage = default(int);
            foreach (var pdfDocument in pdfDocuments)
            {
                foreach (var bookMark in pdfDocument.BookMarks.BookMarkList.OrderBy(b => b.PageIndex))
                {
                    tocItems.Add(new TableOfContents { Title = bookMark.Text, PageNumber = bookMark.PageIndex + 1 + startPage, IndentLevel = bookMark.IndentLevel });
                }
                startPage += pdfDocument.PageCount;
            }

            var html = default(string);
            foreach (var tocItem in tocItems)
            {
                html += $"<h{tocItem.IndentLevel + 1}>{tocItem.Title} ... {tocItem.PageNumber}</h{tocItem.IndentLevel + 1}>";
            }

            return htmlToPdfRenderer.RenderHtmlAsPdf(html);
        }

        private static HtmlToPdf CreateRenderer()
        {
            var renderer = new HtmlToPdf();
            renderer.PrintOptions.MarginTop = 50; //millimeters
            renderer.PrintOptions.MarginBottom = 50;
            renderer.PrintOptions.CssMediaType = PdfPrintOptions.PdfCssMediaType.Print;
            renderer.PrintOptions.EnableJavaScript = false;
            renderer.PrintOptions.RenderDelay = 100;
            renderer.PrintOptions.Header = new SimpleHeaderFooter
            {
                CenterText = "{pdf-title}",
                DrawDividerLine = true,
                FontSize = 16
            };
            renderer.PrintOptions.Footer = new SimpleHeaderFooter
            {
                LeftText = "{date} {time}",
                RightText = "Page {page}",
                DrawDividerLine = true,
                FontSize = 14
            };
            return renderer;
        }

        private static HtmlToPdf CreateTocRenderer()
        {
            var renderer = new HtmlToPdf();
            renderer.PrintOptions.MarginTop = 50; //millimeters
            renderer.PrintOptions.MarginBottom = 50;
            renderer.PrintOptions.CssMediaType = PdfPrintOptions.PdfCssMediaType.Print;
            renderer.PrintOptions.EnableJavaScript = false;
            renderer.PrintOptions.RenderDelay = 100;
            return renderer;
        }

        private class ChapterInfo
        {
            public string Title { get; set; }
            public string Content { get; set; }
        }

        private class TableOfContents
        {
            public int PageNumber { get; set; }
            public string Title { get; set; }
            public int IndentLevel { get; set; }
        }
    }
}