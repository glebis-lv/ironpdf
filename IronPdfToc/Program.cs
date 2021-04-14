using System.Collections.Generic;
using System.Linq;
using IronPdf;

namespace IronPdfToc
{
    internal class Program
    {
        private static readonly ChapterInfo Chapter1 = new ChapterInfo
        {
            Title = "Intro",
            Content = @"
<h1>Introduction</h1>
<div>This is the intro of the page</div>


    <div style='page-break-after: always;'>&nbsp;</div>

another intro page


    <div style='page-break-after: always;'>&nbsp;</div>

and another
"
        };

        private static readonly ChapterInfo Chapter2 = new ChapterInfo
        {
            Title = "Person list",
            Content = @"
<h1>All of the persons</h1>
<div>A lot of persons over here...</div>
<div style='page-break-after: always;'>&nbsp;</div>
really a lot
<div style='page-break-after: always;'>&nbsp;</div>
of persons!
"
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
                var startPage = pdfDocuments.Sum(d => d.PageCount) + 1;
                var pdfDocument = CreatePdfDocument(htmlToPdfRenderer, chapterInfo.Title, chapterInfo.Content, startPage);
                pdfDocuments.Add(pdfDocument);
            }

            var toc = CreateToCDocument(CreateTocRenderer(), pdfDocuments);
            pdfDocuments.Insert(0, toc);

            var mergedDocument = PdfDocument.Merge(pdfDocuments);
            mergedDocument.SaveAs("HtmlToPDF.pdf");
        }

        private static PdfDocument CreatePdfDocument(HtmlToPdf htmlToPdfRenderer,
            string pdfDocumentName,
            string html,
            int startingPage)
        {
            htmlToPdfRenderer.PrintOptions.FirstPageNumber = startingPage;

            var pdfDocument = htmlToPdfRenderer.RenderHtmlAsPdf(html);
            for (var a = 0; a < pdfDocument.Pages.Count; a++)
            {
                pdfDocument.BookMarks.AddBookMarkAtStart($"{pdfDocumentName}'s bookmark of page ", a, a);
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