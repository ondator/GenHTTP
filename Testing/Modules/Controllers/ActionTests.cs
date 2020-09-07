using System.Net;
using System.IO;

using Xunit;

using GenHTTP.Api.Content;
using GenHTTP.Api.Protocol;

using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.IO;
using GenHTTP.Modules.Layouting;

namespace GenHTTP.Testing.Acceptance.Modules.Mvc
{

    public class ActionTests
    {

        #region Supporting data structures

        public class Model
        {

            public string? Field { get; set; }
                            
        }

        public class TestController
        {

            public IHandlerBuilder Index()
            {
                return Content.From("Hello World!");
            }

            public IHandlerBuilder Action(int? query)
            {
                return Content.From(query?.ToString() ?? "Action");
            }

            public IHandlerBuilder Action([FromPath] int id)
            {
                return Content.From(id.ToString());
            }

            public IHandlerBuilder Action(int three, [FromPath] int one, [FromPath] int two)
            {
                return Content.From((one + two + three).ToString());
            }

            [ControllerAction(RequestMethod.POST)]
            public IHandlerBuilder Action(Model data)
            {
                return Content.From(data.Field ?? "no content");
            }

            public void Void() { }
              
        }

        #endregion

        #region Tests

        [Fact]
        public void TestIndex()
        {
            using var runner = GetRunner();

            using var response = runner.GetResponse("/t/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello World!", response.GetContent());
        }

        [Fact]
        public void TestAction()
        {
            using var runner = GetRunner();

            using var response = runner.GetResponse("/t/action/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Action", response.GetContent());
        }

        [Fact]
        public void TestActionWithQuery()
        {
            using var runner = GetRunner();

            using var response = runner.GetResponse("/t/action/?query=0815");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("815", response.GetContent());
        }

        [Fact]
        public void TestActionWithBody()
        {
            using var runner = GetRunner();

            var request = runner.GetRequest("/t/action/");
            request.Method = "POST";

            using (var input = new StreamWriter(request.GetRequestStream()))
            {
                input.Write("{ \"field\": \"FieldData\" }");
            }

            using var response = runner.GetResponse(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("FieldData", response.GetContent());
        }

        [Fact]
        public void TestActionWithParameter()
        {
            using var runner = GetRunner();

            using var response = runner.GetResponse("/t/action/4711/");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("4711", response.GetContent());
        }

        [Fact]
        public void TestActionWithBadParameter()
        {
            using var runner = GetRunner();

            using var response = runner.GetResponse("/t/action/string/");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public void TestActionWithMixedParameters()
        {
            using var runner = GetRunner();

            using var response = runner.GetResponse("/t/action/1/2/?three=3");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("6", response.GetContent());
        }

        [Fact]
        public void TestActionWithNoResult()
        {
            using var runner = GetRunner();

            using var response = runner.GetResponse("/t/void/");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        // ToDo: Test non matching route

        #endregion

        #region Helpers

        private TestRunner GetRunner()
        {
            return TestRunner.Run(Layout.Create().AddController<TestController>("t"));
        }

        #endregion

    }

}
