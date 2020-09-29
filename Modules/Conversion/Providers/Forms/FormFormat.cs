using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;

using GenHTTP.Api.Content;
using GenHTTP.Api.Protocol;

using GenHTTP.Modules.Basics;

namespace GenHTTP.Modules.Conversion.Providers.Forms
{

    public class FormFormat : ISerializationFormat
    {
        private static readonly Type[] EMPTY_CONSTRUCTOR = new Type[0];

        private static readonly object[] EMPTY_ARGS = new object[0];

        public async Task<object> Deserialize(Stream stream, Type type)
        {
            using var reader = new StreamReader(stream);

            var content = await reader.ReadToEndAsync();

            var query = HttpUtility.ParseQueryString(content);

            var constructor = type.GetConstructor(EMPTY_CONSTRUCTOR);
            
            if (constructor == null)
            {
                throw new ProviderException(ResponseStatus.InternalServerError, $"Instance of type '{type}' cannot be constructed as there is no parameterless constructor");
            }

            var result = constructor.Invoke(EMPTY_ARGS);

            foreach (var key in query.AllKeys)
            {
                var value = query[key];

                if (!string.IsNullOrWhiteSpace(value))
                {
                    var property = type.GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                    if (property != null)
                    {
                        property.SetValue(result, Convert.ChangeType(value, property.PropertyType));
                    }
                    else
                    {
                        var field = type.GetField(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                        if (field != null)
                        {
                            field.SetValue(result, Convert.ChangeType(value, field.FieldType));
                        }
                    }
                }
            }

            return result;
        }

        public IResponseBuilder Serialize(IRequest request, object response)
        {
            return request.Respond()
                          .Content(new FormContent(response.GetType(), response))
                          .Type(ContentType.ApplicationWwwFormUrlEncoded);
        }

    }

}
