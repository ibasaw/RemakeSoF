using System.Text.RegularExpressions;

namespace Tolik.RemakeSoF.Runtime
{
    public static class MyUtils
    {
        /// <summary>
        /// Validates if the given string is a valid email address
        /// </summary>
        /// <param name="email">Email string to validate</param>
        /// <returns>True if valid email format, false otherwise</returns>
        public static bool IsValidEmail(string email)
        {
            try
            {
                // Simple email validation regex
                var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
                return emailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }
    }
}
