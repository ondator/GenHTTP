using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using GenHTTP.Api.Content;
using GenHTTP.Api.Protocol;
using GenHTTP.Api.Routing;

using GenHTTP.Modules.Basics;

namespace GenHTTP.Modules.Reflection
{

    public class MethodCollection : IHandler, IHandlerResolver, IRootPathAppender
    {

        #region Get-/Setters

        public IHandler Parent { get; }

        public List<MethodHandler> Methods { get; }

        #endregion

        #region Initialization

        public MethodCollection(IHandler parent, IEnumerable<Func<IHandler, MethodHandler>> methodFactories)
        {
            Parent = parent;

            Methods = methodFactories.Select(factory => factory(this))
                                     .ToList();
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
                var parts = new List<string>(this.GetRoot(request, false).Parts);

                WebPath path;

                if (method.Routing.ParsedPath == null)
                {
                    path = new WebPath(parts, true);
                }
                else
                {
                    parts.Add(method.Routing.ParsedPath.ToString());
                    path = new WebPath(parts, false);
                }

                yield return new ContentElement(path, Path.GetFileName(path.ToString()), path.ToString().GuessContentType() ?? ContentType.ApplicationForceDownload, null);
            }
        }

        private List<MethodHandler> FindProviders(string path) => Methods.Where(m => m.Routing.ParsedPath?.IsMatch(path) ?? false).ToList();

        public IHandler? Find(string segment)
        {
            return Methods.Where(m => m.Routing.Segment == segment)
                          .FirstOrDefault();
        }

        public void Append(PathBuilder path, IRequest request, IHandler? child = null)
        {
            path.TrailingSlash(true);

            var handler = Methods.Where(m => m == child)
                                 .FirstOrDefault();

            if (handler != null)
            {
                var match = handler.Routing.ParsedPath.Match(request.Target.GetRemaining().ToString());
                                
                if (match.Success)
                {
                    path.Preprend(new PathBuilder(match.Value).Build());
                }
            }
        }

        #endregion

    }

}
