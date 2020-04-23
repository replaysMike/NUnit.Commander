using MoreLinq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace NUnit.Commander.Display
{

    partial class StackTraceHtmlFragments : IStackTraceFormatter<string>
    {
        public string BeforeType { get; set; }
        public string AfterType { get; set; }
        public string BeforeMethod { get; set; }
        public string AfterMethod { get; set; }
        public string BeforeParameterType { get; set; }
        public string AfterParameterType { get; set; }
        public string BeforeParameterName { get; set; }
        public string AfterParameterName { get; set; }
        public string BeforeFile { get; set; }
        public string AfterFile { get; set; }
        public string BeforeLine { get; set; }
        public string AfterLine { get; set; }
        public string BeforeFrame { get; set; }
        public string AfterFrame { get; set; }
        public string BeforeParameters { get; set; }
        public string AfterParameters { get; set; }

        string IStackTraceFormatter<string>.Text(string text, bool encodeEntities) => string.IsNullOrEmpty(text) ? string.Empty : encodeEntities ? WebUtility.HtmlEncode(text) : text;
        string IStackTraceFormatter<string>.Type(string markup) => BeforeType + markup + AfterType;
        string IStackTraceFormatter<string>.Method(string markup) => BeforeMethod + markup + AfterMethod;
        string IStackTraceFormatter<string>.ParameterType(string markup) => BeforeParameterType + markup + AfterParameterType;
        string IStackTraceFormatter<string>.ParameterName(string markup) => BeforeParameterName + markup + AfterParameterName;
        string IStackTraceFormatter<string>.File(string markup) => BeforeFile + markup + AfterFile;
        string IStackTraceFormatter<string>.Line(string markup) => BeforeLine + markup + AfterLine;
        string IStackTraceFormatter<string>.BeforeFrame => BeforeFrame ?? string.Empty;
        string IStackTraceFormatter<string>.AfterFrame => AfterFrame ?? string.Empty;
        string IStackTraceFormatter<string>.BeforeParameters => BeforeParameters ?? string.Empty;
        string IStackTraceFormatter<string>.AfterParameters => AfterParameters ?? string.Empty;
    }

    partial interface IStackTraceFormatter<T>
    {
        T Text(string text, bool encodeEntities);
        T Type(T markup);
        T Method(T markup);
        T ParameterType(T markup);
        T ParameterName(T markup);
        T File(T markup);
        T Line(T markup);
        T BeforeFrame { get; }
        T AfterFrame { get; }
        T BeforeParameters { get; }
        T AfterParameters { get; }
    }

    static partial class StackTraceFormatter
    {
        static readonly StackTraceHtmlFragments DefaultStackTraceHtmlFragments = new StackTraceHtmlFragments();

        public static string FormatHtml(string text, IStackTraceFormatter<string> formatter, bool encodeEntities)
        {
            return string.Concat(Format(text, formatter ?? DefaultStackTraceHtmlFragments, encodeEntities));
        }

        public static IEnumerable<T> Format<T>(string text, IStackTraceFormatter<T> formatter, bool encodeEntities)
        {
            Debug.Assert(text != null);

            var frames = StackTraceParser.Parse
                (
                    text,
                    encodeEntities,
                    (idx, len, txt) => new
                    {
                        Index = idx,
                        End = idx + len,
                        Text = txt,
                        Markup = formatter.Text(txt, encodeEntities),
                    },
                    (t, m) => new
                    {
                        Type = new { t.Index, t.End, Markup = formatter.Type(t.Markup) },
                        Method = new { m.Index, m.End, Markup = formatter.Method(m.Markup) }
                    },
                    (t, n) => new
                    {
                        Type = new { t.Index, t.End, Markup = formatter.ParameterType(t.Markup) },
                        Name = new { n.Index, n.End, Markup = formatter.ParameterName(n.Markup) }
                    },
                    (p, ps) => new { List = p, Parameters = ps.ToArray() },
                    (f, l) => new
                    {
                        File = f.Text.Length > 0
                             ? new { f.Index, f.End, Markup = formatter.File(f.Markup) }
                             : null,
                        Line = l.Text.Length > 0
                             ? new { l.Index, l.End, Markup = formatter.Line(l.Markup) }
                             : null,
                    },
                    (f, tm, p, fl) =>
                        from tokens in new[]
                        {
                            new[]
                            {
                                new { f.Index, End = f.Index, Markup = formatter.BeforeFrame },
                                tm.Type,
                                tm.Method,
                                new { p.List.Index, End = p.List.Index, Markup = formatter.BeforeParameters },
                            },
                            from pe in p.Parameters
                            from e in new[] { pe.Type, pe.Name }
                            select e,
                            new[]
                            {
                                new { Index = p.List.End, p.List.End, Markup = formatter.AfterParameters },
                                fl.File,
                                fl.Line,
                                new { Index = f.End, f.End, Markup = formatter.AfterFrame },
                            },
                        }
                        from token in tokens
                        where token != null
                        select token
                );

            return
                from token in Enumerable.Repeat(new { Index = 0, End = 0, Markup = default(T) }, 1)
                                        .Concat(from tokens in frames from token in tokens select token)
                                        .Pairwise((prev, curr) => new { Previous = prev, Current = curr })
                from m in new[]
                {
                    formatter.Text(text.Substring(token.Previous.End, token.Current.Index - token.Previous.End), encodeEntities),
                    token.Current.Markup,
                }
                select m;
        }
    }
}
