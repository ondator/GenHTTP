using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using GenHTTP.Api.Content;
using GenHTTP.Api.Protocol;
using GenHTTP.Api.Routing;

using GenHTTP.Modules.Basics;
using GenHTTP.Modules.Conversion.Providers;
using GenHTTP.Modules.Reflection;

namespace GenHTTP.Modules.Controllers.Provider
{

    // ToDo: Enable proper content (test with sitemap), with "ContentHint" for path variables?

    public class ControllerHandler<T> : IHandler, IHandlerResolver where T : new()
    {
        private static readonly MethodRouting EMPTY = new MethodRouting("^(/|)$", null);

        #region Get-/Setters

        public IHandler Parent { get; }

        private MethodCollection Provider { get; }

        #endregion

        #region Initialization

        public ControllerHandler(IHandler parent, SerializationRegistry formats)
        {
            Parent = parent;

            Provider = new MethodCollection(this, AnalyzeMethods(typeof(T), formats));
        }

        private IEnumerable<Func<IHandler, MethodHandler>> AnalyzeMethods(Type type, SerializationRegistry formats)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var annotation = method.GetCustomAttribute<ControllerActionAttribute>(true) ?? new MethodAttribute();

                var arguments = FindPathArguments(method);

                var path = DeterminePath(method, arguments);

                yield return (parent) => new MethodHandler(parent, method, path, () => new T(), annotation, Preconditions, GetResponse, formats);
            }
        }

        private MethodRouting DeterminePath(MethodInfo method, List<string> arguments)
        {
            var pathArgs = string.Join('/', arguments.Select(a => a.ToParameter()));

            if (method.Name == "Index")
            {
                return pathArgs.Length > 0 ? new MethodRouting($"^/{pathArgs}(/|)$", null) : EMPTY;
            }
            else
            {
                var name = method.Name.ToLowerInvariant();

                var path = $"^/{name}";

                return pathArgs.Length > 0 ? new MethodRouting($"{path}/{pathArgs}(/|)$", name) : new MethodRouting($"{path}(/|)$", name);
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

        public IEnumerable<ContentElement> GetContent(IRequest request) => Provider.GetContent(request);

        public IResponse? Handle(IRequest request) => Provider.Handle(request);

        private IResponse? Preconditions(IRequest request)
        {
            if (request.HasType(RequestMethod.GET, RequestMethod.HEAD))
            {
                if (!request.Target.Path.TrailingSlash)
                {
                    return Redirect.To($"{request.Target.Path}/")
                                   .Build(this)
                                   .Handle(request);
                }
            }

            return null;
        }

        private IResponse? GetResponse(IRequest request, IHandler methodProvider, object? result)
        {
            if (result == null)
            {
                return request.Respond().Status(ResponseStatus.NoContent).Build();
            }

            if (result is IHandlerBuilder handlerBuilder)
            {
                return handlerBuilder.Build(methodProvider).Handle(request);
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

        public IHandler? Find(string segment)
        {
            if (segment == "{controller}")
            {
                return this;
            }

            if (segment == "{index}")
            {
                return Provider.Methods.Where(m => m.Method.Name == "Index" && m.MetaData.SupportedMethods.Contains(new FlexibleRequestMethod(RequestMethod.GET)))
                                       .FirstOrDefault();
            }

            return null;
        }

        #endregion

    }

}
