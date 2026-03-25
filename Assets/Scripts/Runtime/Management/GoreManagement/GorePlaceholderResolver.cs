using System;
using System.Collections.Generic;

namespace Tolik.RemakeSoF.Runtime.GoreManagement
{
    /// <summary>
    /// Resolves SoF2 gore template placeholders in surface and bolt names.
    /// SoF2 uses sided placeholders so that one gore_area definition covers both left and right.
    /// </summary>
    public static class GorePlaceholderResolver
    {
        /// <summary>
        /// Resolves all placeholders in a single string for the given side.
        /// </summary>
        /// <param name="template">Template string (e.g. "fingers_&lt;PS&gt;", "head_&lt;PL&gt;").</param>
        /// <param name="isRightSide">True = right side is primary, false = left side is primary.</param>
        /// <returns>Resolved string (e.g. "fingers_r" or "fingers_l").</returns>
        public static string Resolve(string template, bool isRightSide)
        {
            if (string.IsNullOrEmpty(template))
            {
                return template;
            }

            string primaryShort = isRightSide ? "r" : "l";
            string primaryLong = isRightSide ? "right" : "left";
            string oppositeShort = isRightSide ? "l" : "r";
            string oppositeLong = isRightSide ? "left" : "right";

            string result = template;
            result = result.Replace("<PS>", primaryShort, StringComparison.OrdinalIgnoreCase);
            result = result.Replace("<PL>", primaryLong, StringComparison.OrdinalIgnoreCase);
            result = result.Replace("<OS>", oppositeShort, StringComparison.OrdinalIgnoreCase);
            result = result.Replace("<OL>", oppositeLong, StringComparison.OrdinalIgnoreCase);

            return result;
        }

        /// <summary>
        /// Resolves all placeholders in a list of strings for the given side.
        /// Returns a new list with resolved strings.
        /// </summary>
        public static List<string> ResolveList(List<string> templates, bool isRightSide)
        {
            if (templates == null || templates.Count == 0)
            {
                return new List<string>();
            }

            List<string> resolved = new(templates.Count);
            foreach (string template in templates)
            {
                resolved.Add(Resolve(template, isRightSide));
            }

            return resolved;
        }
    }
}
