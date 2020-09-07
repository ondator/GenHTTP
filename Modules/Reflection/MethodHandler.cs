using System.Collections.Generic;
using System.IO;
using System.Linq;

using GenHTTP.Api.Content;
using GenHTTP.Api.Protocol;
using GenHTTP.Api.Routing;

using GenHTTP.Modules.Basics;

namespace GenHTTP.Modules.Reflection
{

    public class MethodHandler : IHandler
    {

        #region Get-/Setters

        public IHandler Parent { get; }

        private List<MethodProvider> Methods { get; }

        #endregion

        #region Initialization

        public MethodHandler(IHandler parent, List<MethodProvider> methods)
        {
            Parent = parent;
            Methods = methods;
        }

        #endregion

        #region Functionality

        public IResponse? Handle(IRequest request)
        {
            var methods = FindProviders(request.Target.GetRemaining().ToString());

            if (methods.Any())
            {
                var matchingMethods = methods.Where(m => m.MetaData.SupportedMethods.Contains(request.Method)).ToList();

                if (matchingMethods.Count == 1)
                {
                    return matchingMethods.First().Handle(request);
                }
                else if (methods.Count > 1)
                {
                    throw new ProviderException(ResponseStatus.BadRequest, $"There are multiple methods matching '{request.Target.Path}'");
                }
                else
                {
                    throw new ProviderException(ResponseStatus.MethodNotAllowed, $"There is no method of a matching request type");
                }
            }

            return null;
        }

        public IEnumerable<ContentElement> GetContent(IRequest request)
        {
            foreach (var method in Methods.Where(m => m.MetaData.SupportedMethods.Contains(new FlexibleRequestMethod(RequestMethod.GET))))
            {
                var parts = new List<string>(this.GetRoot(request.Server.Handler, false).Parts);

                WebPath path;

                if (method.ParsedPath == null)
                {
                    path = new WebPath(parts, true);
                }
                else
                {
                    parts.Add(method.ParsedPath.ToString());
                    path = new WebPath(parts, false);
                }

                yield return new ContentElement(path, Path.GetFileName(path.ToString()), path.ToString().GuessContentType() ?? ContentType.ApplicationForceDownload, null);
            }
        }

        private List<MethodProvider> FindProviders(string path) => Methods.Where(m => m.ParsedPath?.IsMatch(path) ?? false).ToList();

        #endregion

    }

}
