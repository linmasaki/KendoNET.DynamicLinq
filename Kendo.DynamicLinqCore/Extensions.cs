using System;
using System.Globalization;

namespace Kendo.DynamicLinqCore
{
    public static class Extensions
    {
        public static string ToTitleCase(this TextInfo textInfo, string str)
        {
            var tokens = str.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                tokens[i] = token.Substring(0, 1).ToUpper() + token.Substring(1);
            }

            return string.Join(" ", tokens);
        }
    }
}
