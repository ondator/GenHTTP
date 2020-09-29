using System.Collections.Generic;
using System.Linq;

using GenHTTP.Api.Content;
using GenHTTP.Api.Content.Templating;
using GenHTTP.Api.Protocol;

using GenHTTP.Modules.Basics;
using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.IO;
using GenHTTP.Modules.Scriban;

namespace GenHTTP.Playground
{

    public class BookController
    {
        private static readonly List<Book> _Books = new List<Book>()
        {
            new Book() { ID = 1, Title = "Lord of the Rings", Read = true },
            new Book() { ID = 2, Title = "Der weiße Drache", Read = true }
        };

        public IHandlerBuilder Index()
        {
            return ModScriban.Page(Data.FromResource("BookList.html"), (r, h) => new ViewModel(r, h, _Books))
                             .Title("Books");
        }

        public IHandlerBuilder Create()
        {
            return ModScriban.Page(Data.FromResource("Editor.html"), (r, h) => new ViewModel(r, h))
                             .Title("Create Book");
        }

        [ControllerAction(RequestMethod.POST)]
        public IHandlerBuilder Create(Book book)
        {
            book.ID = _Books.Select(b => b.ID).Max() + 1;

            _Books.Add(book);

            return Redirect.To("../"); // ToDo: Routing-Redirect, e.g. Redirect.To("{index}", handler)
        }

    }

}
