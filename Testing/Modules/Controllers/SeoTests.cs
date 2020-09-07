using System.Net;

using Xunit;

using GenHTTP.Api.Content;
using GenHTTP.Api.Protocol;

using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.IO;
using GenHTTP.Modules.Layouting;

namespace GenHTTP.Testing.Acceptance.Modules.Mvc
{

    public class SeoTests
    {

        #region Supporting data structures

        public class TestController
        {

            public IHandlerBuilder Action()
            {
                return Content.From("Action");
            }

            [ControllerAction(RequestMethod.DELETE)]
            public IHandlerBuilder Action([FromPath] int id) 
            {
                return Content.From(id.ToString());
            }

        }

        #endregion

        #region Tests

        /// <summary>
        /// As the developer of a web application, I don't want the MCV framework to generate duplicate content
        /// by accepting upper case letters in action names.
        /// </summary>
        [Fact]
        public void TestActionCasingMatters()
        {
            using var runner = GetRunner();

            using var response = runner.GetResponse("/t/Action/");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        /// <summary>
        /// As the developer of a web application, I expected the framework to handle trailing slashes for me
        /// so that I don't create duplicate content by mistake.
        /// </summary>
        [Fact]
        public void TestActionWithoutTrailingSlashRedirects()
        {
            using var runner = GetRunner();

            using var response = runner.GetResponse("/t/action");

            Assert.Equal(HttpStatusCode.Moved, response.StatusCode);
        }

        /// <summary>
        /// For other methods than GET and HEAD, trailing slash handling is not required and would introduce
        /// unnecessary redirects for sloppy developers.
        /// </summary>
        [Fact]
        public void TestNoRedirectionWithOtherMethods()
        {
            using var runner = GetRunner();

            var request = runner.GetRequest("/t/action/4711");
            request.Method = "DELETE";

            using var response = runner.GetResponse(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("4711", response.GetContent());
        }

        #endregion

        #region Helpers

        private TestRunner GetRunner()
        {
            return TestRunner.Run(Layout.Create().AddController<TestController>("t"));
        }

        #endregion

    }

}
