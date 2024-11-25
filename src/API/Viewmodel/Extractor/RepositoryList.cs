namespace RestAPI.Viewmodel.Extractor
{
    public class RepositoryList
    {
        public class Project
        {
            public string id { get; set; }
            public string name { get; set; }
        }

        public class Value
        {
            public string id { get; set; }
            public string name { get; set; }
        }

        public class Repository
        {
            public int count { get; set; }
            public IList<Value> value { get; set; }
        }
    }
}
