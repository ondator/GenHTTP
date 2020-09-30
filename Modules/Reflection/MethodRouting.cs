using System.Text.RegularExpressions;

namespace GenHTTP.Modules.Reflection
{

    public class MethodRouting
    {

        #region Get-/Setters

        /// <summary>
        /// The path of the method, converted into a regular
        /// expression to be evaluated at runtime.
        /// </summary>
        public Regex ParsedPath { get; }

        public string? Segment { get; }

        #endregion

        #region Initialization

        public MethodRouting(string pathExpression, string? segment)
        {
            ParsedPath = new Regex(pathExpression, RegexOptions.Compiled);
            Segment = segment;
        }

        #endregion

    }

}
