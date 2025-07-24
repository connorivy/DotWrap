using System;

namespace DotWrap.Generator.Builders;

public static class XmlParser
{
    public static string? ParseSummary(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return null;
        }
        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(xml);
            return doc.Root?.Element("summary")?.Value.Trim();
        }
        catch
        {
            return null;
        }
    }

    public static string? ParseReturns(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return null;
        }
        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(xml);
            return doc.Root?.Element("returns")?.Value.Trim();
        }
        catch
        {
            return null;
        }
    }

    public static string? ParseParamComment(string? xml, string paramName)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return null;
        }
        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(xml);
            return doc
                .Root?.Elements("param")
                .FirstOrDefault(e => e.Attribute("name")?.Value == paramName)
                ?.Value.Trim();
        }
        catch
        {
            return null;
        }
    }
}
