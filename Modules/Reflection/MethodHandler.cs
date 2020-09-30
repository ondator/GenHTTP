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

using GenHTTP.Modules.Conversion;
using GenHTTP.Modules.Conversion.Providers;
using GenHTTP.Modules.Conversion.Providers.Forms;

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
    public class MethodHandler : IHandler
    {
        private static FormFormat _FormFormat = new FormFormat();

        #region Get-/Setters

        public IHandler Parent { get; }

        public MethodRouting Routing { get; }

        public MethodAttribute MetaData { get; }

        public MethodInfo Method { get; }

        private Func<object> InstanceProvider { get; }

        private Func<IRequest, IResponse?>? Precondition { get; }

        private Func<IRequest, IHandler, object?, IResponse?> ResponseProvider { get; }

        private SerializationRegistry Serialization { get; }

        #endregion

        #region Initialization

        public MethodHandler(IHandler parent, MethodInfo method, MethodRouting routing, Func<object> instanceProvider, MethodAttribute metaData,
            Func<IRequest, IResponse?>? precondition, Func<IRequest, IHandler, object?, IResponse?> responseProvider, SerializationRegistry serialization)
        {
            Parent = parent;

            Method = method;
            MetaData = metaData;
            InstanceProvider = instanceProvider;
            Serialization = serialization;

            ResponseProvider = responseProvider;
            Precondition = precondition;

            Routing = routing;
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

            return ResponseProvider(request, this, Invoke(request, arguments));
        }

        private object?[] GetArguments(IRequest request)
        {
            var targetParameters = Method.GetParameters();

            var targetArguments = new object?[targetParameters.Length];

            var sourceParameters = Routing.ParsedPath.Match(request.Target.GetRemaining().ToString());

            var bodyArguments = (targetParameters.Length > 0) ? _FormFormat.GetContent(request) : null;

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
                        targetArguments[i] = sourceArgument.Value.ConvertTo(par.ParameterType);
                        continue;
                    }

                    // is there a query parameter?
                    if (request.Query.TryGetValue(par.Name, out var queryValue))
                    {
                        targetArguments[i] = queryValue.ConvertTo(par.ParameterType);
                        continue;
                    }

                    // is there a parameter from the body?
                    if (bodyArguments != null)
                    {
                        if (bodyArguments.TryGetValue(par.Name, out var bodyValue))
                        {
                            targetArguments[i] = bodyValue.ConvertTo(par.ParameterType);
                            continue;
                        }
                    }

                    // assume the default value
                    continue;
                }
                else
                {
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

        #endregion

    }

}
