namespace RestAPI.Viewmodel.Extractor
{
    public class GetINumIteration
    {
        public class Attributes
        {
            public DateTime? startDate { get; set; }
            public DateTime? finishDate { get; set; }
            public string timeFrame { get; set; }
        }

        public class Value
        {
            public string id { get; set; }
            public string name { get; set; }
            public string path { get; set; }
            public Attributes attributes { get; set; }
            public string url { get; set; }
        }

        public class Iterations
        {
            public int count { get; set; }
            public IList<Value> value { get; set; }
        }


    }
}
