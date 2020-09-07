using GenHTTP.Api.Content;

using GenHTTP.Modules.Core;
using Ctrl = GenHTTP.Modules.Controllers;

using GenHTTP.Examples.Examples.Controllers.Controller;

namespace GenHTTP.Examples.Examples.Controllers
{

    public static class ControllerExample
    {

        public static IHandlerBuilder Create()
        {
            return Layout.Create()
                         .Fallback(Ctrl.Controller.From<TaskController>()); // ToDo: investigate?
        }

    }

}
