namespace RestAPI.Viewmodel.Importer
{
    public class IncludeSubAreas
    {
        public class Root
        {
            public string defaultValue { get; set; }
            public List<Value> values { get; set; }
        }

        public class Value
        {
            public string value { get; set; }
            public bool includeChildren { get; set; }
        }
    }


}
