using GenHTTP.Api.Content;
using GenHTTP.Api.Content.Templating;
using GenHTTP.Api.Protocol;

namespace GenHTTP.Examples.Examples.Controllers
{

    /// <summary>
    /// ToDo: Specify
    /// </summary>
    public class ObjectModel : PageModel
    {

        #region Get-/Setters

        public object? Data { get; }

        #endregion

        #region Initialization

        public ObjectModel(IRequest request, IHandler handler, object? data = null, string? title = null) : base(request, handler)
        {
            Data = data;
            Title = title;
        }

        #endregion

    }

}
