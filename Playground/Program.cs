using GenHTTP.Engine;

using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.Layouting;
using GenHTTP.Modules.Practices;

using GenHTTP.Playground;

namespace Playground
{

    public static class Program
    {

        public static int Main(string[] args)
        {
            var layout = Layout.Create()
                               .AddController<BookController>("books");

            return Host.Create()
                       .Handler(layout)
                       .Console()
                       .Defaults()
                       .Development()
                       .Run();
        }

    }

}
