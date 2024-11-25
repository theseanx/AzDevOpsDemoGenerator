namespace RestAPI.Extractor
{
    public class ProjectProperties
    {
        public class Value
        {
            public string name { get; set; }
            public string value { get; set; }
        }

        public class Properties
        {
            public int count { get; set; }
            public IList<Value> value { get; set; }

            public string TypeClass { get; set; }
        }


    }
}
