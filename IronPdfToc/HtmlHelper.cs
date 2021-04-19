using HtmlAgilityPack;
using System.Collections.Generic;

namespace IronPdfToc
{
    public static class HtmlHelper
    {
        public static HtmlDocument GetHtmlDocumentFromMarkup(string htmlMarkup)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlMarkup);

            return htmlDoc;
        }

        /*public static void CycleThroughHtmlDoc(HtmlNode htmlNode)
        {
            foreach(var element in htmlNode.ChildNodes)
            {
                if (element.NodeType != HtmlNodeType.Element)
                    continue;

                if (element.ChildNodes.Count > 0)
                {
                    CycleThroughHtmlDoc(element);
                }

                Console.WriteLine($"html node: {element.Name}, content: {element.InnerText}");
            }
        }*/

        public static void UnfoldHtmlNodeToList(HtmlNode htmlNode, ref List<HtmlNode> htmlNodesPlainList)
        {
            if (htmlNodesPlainList == null)
                htmlNodesPlainList = new List<HtmlNode>();

            if (htmlNode.Name != "#document")
                htmlNodesPlainList.Add(htmlNode);

            foreach (var childNode in htmlNode.ChildNodes)
            {
                if (childNode.NodeType != HtmlNodeType.Element)
                    continue;

                UnfoldHtmlNodeToList(childNode, ref htmlNodesPlainList);
            }
        }

        public static List<HtmlNode> GetPlainNodesList(HtmlDocument htmlDocument)
        {
            var htmlNodesPlainList = new List<HtmlNode>();

            UnfoldHtmlNodeToList(htmlDocument.DocumentNode, ref htmlNodesPlainList);

            return htmlNodesPlainList;
        }

        public static List<HtmlNode> GetHtmlNodesListFromMarkup(string htmlMarkup)
        {
            var htmlDoc = GetHtmlDocumentFromMarkup(htmlMarkup);
            return GetPlainNodesList(htmlDoc);
        }

        public static readonly string[] HTML_HEADERS = { "h1", "h2", "h3", "h4", "h5", "h6" };
    }
}
