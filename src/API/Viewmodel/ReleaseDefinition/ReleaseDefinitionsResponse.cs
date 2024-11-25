namespace RestAPI.Viewmodel.ReleaseDefinition
{
    public class ReleaseDefinitionsResponse
    {
        public class Release
        {
            public int count { get; set; }
            public Value[] value { get; set; }
        }
        public class Value
        {
            public int id { get; set; }
            public string name { get; set; }
        }
    }
}
