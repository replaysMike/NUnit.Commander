using AnyConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NUnit.Commander.Display
{
    public static class StackTracePrettify
    {
        public static ColorTextBuilder Format(string text, ColorScheme colorScheme)
        {
            try
            {
                var builder = new ColorTextBuilder();
                var colors = typeof(IColorScheme).GetPublicProperties();
                var formatted = StackTraceFormatter.FormatHtml(text, new StackTraceHtmlFragments
                {
                    BeforeType = $"<{nameof(colorScheme.DarkDefault)}>",
                    AfterType = $"</{nameof(colorScheme.DarkDefault)}>",
                    BeforeParameters = $"<{nameof(colorScheme.DarkDefault)}>",
                    AfterParameters = $"</{nameof(colorScheme.DarkDefault)}>",
                    BeforeFile = $"<{nameof(colorScheme.DarkDuration)}>",
                    AfterFile = $"</{nameof(colorScheme.DarkDuration)}>",
                    BeforeMethod = $"<{nameof(colorScheme.Duration)}>",
                    AfterMethod = $"</{nameof(colorScheme.Duration)}>",
                    BeforeLine = $"<{nameof(colorScheme.Error)}>",
                    AfterLine = $"</{nameof(colorScheme.Error)}>",
                }, false);

                // indent
                formatted = formatted.Replace(Environment.NewLine, $"{Environment.NewLine}  ");

                var chunk = string.Empty;
                var startIndex = -1;
                var endIndex = -1;

                // todo: this is horribly inefficient, but I will readdress later
                foreach (var c in formatted)
                {
                    chunk += c;
                    foreach (var colorProperty in colors)
                    {
                        var colorStartTag = $"<{colorProperty.Name}>";
                        var colorEndTag = $"</{colorProperty.Name}>";

                        startIndex = chunk.IndexOf(colorStartTag);
                        if (startIndex >= 0)
                        {
                            // add everything before this point
                            if (startIndex > 0)
                                builder.Append(chunk.Substring(0, startIndex), colorScheme.Default);
                            chunk = chunk.Substring(startIndex + colorStartTag.Length, chunk.Length - colorStartTag.Length - startIndex);
                        }
                        endIndex = chunk.IndexOf(colorEndTag);
                        if (endIndex >= 0)
                        {
                            // add everything before this point
                            builder.Append(chunk.Substring(0, endIndex), colorScheme.GetColor(colorProperty.Name));
                            chunk = chunk.Substring(endIndex + colorEndTag.Length, chunk.Length - colorEndTag.Length - endIndex);
                        }
                    }
                }

                return builder;
            }
            catch (Exception)
            {
                // something went wrong
                return ColorTextBuilder.Create.Append(text);
            }
        }

        private static PropertyInfo[] GetPublicProperties(this Type type)
        {
            if (type.IsInterface)
            {
                var propertyInfos = new List<PropertyInfo>();

                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);
                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetInterfaces())
                    {
                        if (considered.Contains(subInterface)) continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeProperties = subType.GetProperties(
                        BindingFlags.FlattenHierarchy
                        | BindingFlags.Public
                        | BindingFlags.Instance);

                    var newPropertyInfos = typeProperties
                        .Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos.ToArray();
            }

            return type.GetProperties(BindingFlags.FlattenHierarchy
                | BindingFlags.Public | BindingFlags.Instance);
        }
    }
}
