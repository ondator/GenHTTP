using System.Collections.Generic;

namespace GenHTTP.Examples.Examples.Controllers.Model
{

    public class Repository
    {
        private static readonly List<Task> _Tasks = new List<Task>()
        {
            { new Task(1, "Compile and run the example project", true) },
            { new Task(2, "Create your own project with the GenHTTP SDK", false) }
        };

        #region Get-/Setters

        public List<Task> Tasks => _Tasks;

        #endregion

    }

}
