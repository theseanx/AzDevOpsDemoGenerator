namespace RestAPI.Viewmodel.Extractor
{
    public class BuildDefinitionResponse
    {
        public class Build
        {
            public int count { get; set; }
            public Value[] value { get; set; }
        }
        public class Value
        {
            public int id { get; set; }
        }
    }
}
