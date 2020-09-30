using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using GenHTTP.Api.Content;
using GenHTTP.Api.Protocol;

using GenHTTP.Modules.Basics;
using GenHTTP.Modules.Conversion.Providers;
using GenHTTP.Modules.Reflection;

namespace GenHTTP.Modules.Controllers.Provider
{

    // ToDo: Enable routing (IRootPathAppender, IHandlerResolver)

    // ToDo: Enable proper content (test with sitemap)

    public class ControllerHandler<T> : IHandler where T : new()
    {
        private static readonly Regex EMPTY = new Regex("^(/|)$", RegexOptions.Compiled);

        #region Get-/Setters

        public IHandler Parent { get; }

        private MethodHandler Methods { get; }

        #endregion

        #region Initialization

        public ControllerHandler(IHandler parent, SerializationRegistry formats)
        {
            Parent = parent;

            var methods = new List<MethodProvider>(AnalyzeMethods(typeof(T), formats));

            Methods = new MethodHandler(this, methods);
        }

        private IEnumerable<MethodProvider> AnalyzeMethods(Type type, SerializationRegistry formats)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var annotation = method.GetCustomAttribute<ControllerActionAttribute>(true) ?? new MethodAttribute();

                var arguments = FindPathArguments(method);

                var path = DeterminePath(method, arguments);

                yield return new MethodProvider(this, method, path, () => new T(), annotation, Preconditions, GetResponse, formats);
            }
        }

        private Regex DeterminePath(MethodInfo method, List<string> arguments)
        {
            var pathArgs = string.Join('/', arguments.Select(a => a.ToParameter()));

            if (method.Name == "Index")
            {
                return pathArgs.Length > 0 ? new Regex($"^/{pathArgs}(/|)$", RegexOptions.Compiled) : EMPTY;
            }
            else
            {
                var path = $"^/{method.Name.ToLowerInvariant()}";

                return pathArgs.Length > 0 ? new Regex($"{path}/{pathArgs}(/|)$", RegexOptions.Compiled) : new Regex($"{path}(/|)$", RegexOptions.Compiled);
            }
        }

        private List<string> FindPathArguments(MethodInfo method)
        {
            var found = new List<string>();

            var parameters = method.GetParameters();

            foreach (var parameter in parameters)
            {
                if (parameter.GetCustomAttribute(typeof(FromPathAttribute), true) != null)
                {
                    if (!parameter.CheckSimple())
                    {
                        throw new InvalidOperationException("Parameters marked as 'FromPath' must be of a simple type (e.g. string or int)");
                    }

                    if (parameter.CheckNullable())
                    {
                        throw new InvalidOperationException("Parameters marked as 'FromPath' are not allowed to be nullable");
                    }

                    found.Add(parameter.Name);
                }
            }

            return found;
        }

        #endregion

        #region Functionality

        public IEnumerable<ContentElement> GetContent(IRequest request) => Methods.GetContent(request);

        public IResponse? Handle(IRequest request) => Methods.Handle(request);

        private IResponse? Preconditions(IRequest request)
        {
            if (request.HasType(RequestMethod.GET, RequestMethod.HEAD))
            {
                if (!request.Target.Path.TrailingSlash)
                {
                    return Redirect.To(request, $"{request.Target.Path}/")
                                   .Build(this)
                                   .Handle(request);
                }
            }

            return null;
        }

        private IResponse? GetResponse(IRequest request, object? result)
        {
            if (result == null)
            {
                return request.Respond().Status(ResponseStatus.NoContent).Build();
            }

            if (result is IHandlerBuilder handlerBuilder)
            {
                return handlerBuilder.Build(this).Handle(request);
            }

            if (result is IHandler handler)
            {
                return handler.Handle(request);
            }

            if (result is IResponseBuilder responseBuilder)
            {
                return responseBuilder.Build();
            }

            if (result is IResponse response)
            {
                return response;
            }

            throw new ProviderException(ResponseStatus.InternalServerError, "Result type of controller methods must be one of: IHandlerBuilder, IHandler, IResponseBuilder, IResponse");
        }

        #endregion

    }

}
