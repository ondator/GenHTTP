using System.Collections.Generic;
using System.Linq;

using Xunit;

using GenHTTP.Api.Content;
using GenHTTP.Api.Protocol;

using GenHTTP.Modules.Basics;
using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.IO;
using GenHTTP.Modules.Layouting;

namespace GenHTTP.Testing.Acceptance.Modules.Controllers
{

    // ToDo: Complex content within a controller? e.g. a layout?

    public class RoutingTests
    {

        #region Supporting data structures

        public class RouteController
        {

            public IHandlerBuilder Appenders([FromPath] int one, [FromPath] string two, IHandler handler, IRequest request)
            {
                return new AppenderDependentHandlerBuilder();
            }

        }

        public class AppenderDependentHandlerBuilder : IHandlerBuilder
        {

            public IHandler Build(IHandler parent) => new AppenderDependentHandler(parent);

        }

        public class AppenderDependentHandler : IHandler
        {

            public IHandler Parent { get; }

            public AppenderDependentHandler(IHandler parent)
            {
                Parent = parent;
            }

            public IEnumerable<ContentElement> GetContent(IRequest request)
            {
                var root = this.GetRoot(request, false);

                yield return new ContentElement(root, "My File", ContentType.ApplicationForceDownload);
            }

            public IResponse? Handle(IRequest request)
            {
                return Content.From(GetContent(request).Select(c => c.Path).First())
                              .Build(this)
                              .Handle(request);
            }

        }

        #endregion

        #region Tests

        [Fact]
        public void TestAppenders()
        {
            using var runner = Setup();

            using var response = runner.GetResponse("/r/appenders/1/test/");

            Assert.Equal("/r/appenders/1/test/", response.GetContent());
        }

        #endregion

        #region Helpers

        private TestRunner Setup()
        {
            var layout = Layout.Create()
                               .AddController<RouteController>("r");

            return TestRunner.Run(layout);
        }

        #endregion

    }

}
