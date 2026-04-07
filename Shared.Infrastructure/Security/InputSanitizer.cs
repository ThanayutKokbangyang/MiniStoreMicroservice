using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Security
{
    public class InputSanitizer
    {
        public static string SanitizeHtml(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            // Remove HTML tags
            input = System.Text.RegularExpressions.Regex.Replace(input, @"<[^>]+>", string.Empty);
            // Remove script-related content
            input = System.Text.RegularExpressions.Regex.Replace(input, @"javascript\s*:", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            // Encode special characters
            input = System.Net.WebUtility.HtmlEncode(input);

            return input;
        }

        public static string SanitizeSql(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            // Note: Dapper ใช้ parameterized queries อยู่แล้ว
            // นี่คือ defense-in-depth layer เพิ่มเติม
            input = input.Replace("'", "''");
            input = input.Replace(";", string.Empty);
            input = input.Replace("--", string.Empty);
            input = input.Replace("/*", string.Empty);
            input = input.Replace("*/", string.Empty);

            return input;
        }
    }
}
