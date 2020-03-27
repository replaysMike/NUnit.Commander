namespace NUnit.Commander.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Truncate a string to a maximum length
        /// </summary>
        /// <param name="str"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static string MaxLength(this string str, int maxLength)
        {
            if(str?.Length > maxLength)
                return str.Substring(0, maxLength) + "...";
            return str;
        }
    }
}
