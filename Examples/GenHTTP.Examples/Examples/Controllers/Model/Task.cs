namespace GenHTTP.Examples.Examples.Controllers.Model
{

    public class Task
    {

        #region Get-/Setters

        public int ID { get; }

        public string Title { get; }

        public bool Completed { get; }

        #endregion

        #region Initialization

        public Task(int id, string title, bool completed)
        {
            ID = id;
            Title = title;
            Completed = completed;
        }

        #endregion

    }

}
