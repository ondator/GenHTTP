using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using GenHTTP.Api.Protocol;

namespace GenHTTP.Modules.Conversion.Providers.Forms
{

    public class FormContent : IResponseContent
    {

        #region Get-/Setters

        public ulong? Length => null;

        private Type Type { get; }

        private object Data { get; }

        #endregion

        #region Initialization

        public FormContent(Type type, object data)
        {
            Type = type;
            Data = data;
        }

        #endregion

        #region Functionality

        public async Task Write(Stream target, uint bufferSize)
        {
            using var writer = new StreamWriter(target, Encoding.UTF8, (int)bufferSize, true);

            var query = HttpUtility.ParseQueryString(string.Empty);

            foreach (var property in Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = property.GetValue(Data);

                if (value != null)
                {
                    query[property.Name] = value.ToString();
                }
            }

            foreach (var property in Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = property.GetValue(Data);

                if (value != null)
                {
                    query[property.Name] = value.ToString();
                }
            }

            var replaced = query.ToString()
                                .Replace("+", "%20")
                                .Replace("%2b", "+");

            await writer.WriteAsync(replaced);
        }

        #endregion

    }

}
