using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using GenHTTP.Api.Content;
using GenHTTP.Api.Protocol;

using GenHTTP.Modules.Conversion.Providers;

namespace GenHTTP.Modules.Reflection
{

    /// <summary>
    /// Allows to invoke a function on a service oriented resource.
    /// </summary>
    /// <remarks>
    /// This provider analyzes the target method to be invoked and supplies
    /// the required arguments. The result of the method is analyzed and
    /// converted into a HTTP response.
    /// </remarks>
    public class MethodProvider : IHandler
    {

        #region Get-/Setters

        public IHandler Parent { get; }

        /// <summary>
        /// The path of the method, converted into a regular
        /// expression to be evaluated at runtime.
        /// </summary>
        public Regex ParsedPath { get; }

        public MethodAttribute MetaData { get; }

        private MethodInfo Method { get; }

        private Func<object> InstanceProvider { get; }

        private Func<IRequest, IResponse?>? Precondition { get; }

        private Func<IRequest, object?, IResponse?> ResponseProvider { get; }

        private SerializationRegistry Serialization { get; }

        #endregion

        #region Initialization

        public MethodProvider(IHandler parent, MethodInfo method, Regex path, Func<object> instanceProvider, MethodAttribute metaData,
            Func<IRequest, IResponse?>? precondition, Func<IRequest, object?, IResponse?> responseProvider, SerializationRegistry serialization)
        {
            Parent = parent;

            Method = method;
            MetaData = metaData;
            InstanceProvider = instanceProvider;
            Serialization = serialization;

            ResponseProvider = responseProvider;
            Precondition = precondition;

            ParsedPath = path;
        }

        #endregion

        #region Functionality

        public IResponse? Handle(IRequest request)
        {
            var arguments = GetArguments(request);

            if (Precondition != null)
            {
                var result = Precondition(request);

                if (result != null)
                {
                    return result;
                }
            }

            return ResponseProvider(request, Invoke(request, arguments));
        }

        private object?[] GetArguments(IRequest request)
        {
            var targetParameters = Method.GetParameters();

            var targetArguments = new object?[targetParameters.Length];

            var sourceParameters = ParsedPath.Match(request.Target.GetRemaining().ToString());

            for (int i = 0; i < targetParameters.Length; i++)
            {
                var par = targetParameters[i];

                // request
                if (par.ParameterType == typeof(IRequest))
                {
                    targetArguments[i] = request;
                    continue;
                }

                // handler
                if (par.ParameterType == typeof(IHandler))
                {
                    targetArguments[i] = this;
                    continue;
                }

                // input stream
                if (par.ParameterType == typeof(Stream))
                {
                    if (request.Content == null)
                    {
                        throw new ProviderException(ResponseStatus.BadRequest, "Request body expected");
                    }

                    targetArguments[i] = request.Content;
                    continue;
                }

                if (par.CheckSimple())
                {
                    // is there a named parameter?
                    var sourceArgument = sourceParameters.Groups[par.Name];

                    if (sourceArgument.Success)
                    {
                        targetArguments[i] = ChangeType(sourceArgument.Value, par.ParameterType);
                        continue;
                    }

                    // is there a query parameter?
                    if (request.Query.TryGetValue(par.Name, out var value))
                    {
                        targetArguments[i] = ChangeType(value, par.ParameterType);
                        continue;
                    }

                    // assume the default value
                    continue;
                }
                else
                {
                    // ToDo: form encoding

                    // deserialize from body
                    var deserializer = Serialization.GetDeserialization(request);

                    if (deserializer == null)
                    {
                        throw new ProviderException(ResponseStatus.UnsupportedMediaType, "Requested format is not supported");
                    }

                    if (request.Content == null)
                    {
                        throw new ProviderException(ResponseStatus.BadRequest, "Request body expected");
                    }

                    targetArguments[i] = Task.Run(async () => await deserializer.Deserialize(request.Content, par.ParameterType)).Result;
                    continue;
                }
            }

            return targetArguments;
        }

        private object? Invoke(IRequest request, object?[] arguments)
        {
            try
            {
                return Method.Invoke(InstanceProvider(), arguments);
            }
            catch (TargetInvocationException e)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                return null; // nop
            }
        }

        public IEnumerable<ContentElement> GetContent(IRequest request) => Enumerable.Empty<ContentElement>();

        private object? ChangeType(string value, Type type)
        {
            if (string.IsNullOrEmpty(value) && Nullable.GetUnderlyingType(type) != null)
            {
                return null;
            }

            try
            {
                var actualType = Nullable.GetUnderlyingType(type) ?? type;

                if (type.IsEnum)
                {
                    return Enum.Parse(actualType, value);
                }

                return Convert.ChangeType(value, actualType);
            }
            catch (Exception e)
            {
                throw new ProviderException(ResponseStatus.BadRequest, $"Unable to convert value '{value}' to type '{type}'", e);
            }
        }

        #endregion

    }

}
