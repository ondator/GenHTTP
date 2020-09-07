using System.Linq;

using GenHTTP.Api.Content;
using GenHTTP.Api.Protocol;

using GenHTTP.Modules.Core;
using GenHTTP.Modules.Scriban;
using GenHTTP.Modules.Controllers;

using GenHTTP.Examples.Examples.Controllers.Model;

namespace GenHTTP.Examples.Examples.Controllers.Controller
{

    public class TaskController
    {
        private readonly Repository _Repository = new Repository();

        public IHandlerBuilder Index()
        {
            return View("TaskList", _Repository.Tasks);
        }

        public IHandlerBuilder Create()
        {
            return View("NewTask");
        }

        [ControllerAction(RequestMethod.POST)]
        public IHandlerBuilder Create(Task task)
        {
            var id = _Repository.Tasks.Max(t => t.ID) + 1;

            var model = new Task(id, task.Title, task.Completed);

            _Repository.Tasks.Add(model);

            return Redirect.To("../"); // ToDo
        }

        private IHandlerBuilder View(string name, object? model = null) // ToDo: move into Scriban etc as an extension?
        {
            return ModScriban.Page(Data.FromResource($"{name}.html"), (r, h) => new ObjectModel(r, h, model, name));
        }

    }

}
