using System.Collections.Generic;

namespace RestAPI.Viewmodel.Extractor
{

    public class Value
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string isDefault { get; set; }
    }

    public class TeamList
    {
        public IList<Value> value { get; set; }
    }
}
