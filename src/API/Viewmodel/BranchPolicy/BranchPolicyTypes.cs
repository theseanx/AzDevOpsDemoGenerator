namespace RestAPI.Viewmodel.BranchPolicy
{
    public class BranchPolicyTypes
    {
        public class Links
        {
            public Self self { get; set; }
        }

        public class PolicyTypes
        {
            public int count { get; set; }
            public List<Value> value { get; set; }
        }

        public class Self
        {
            public string href { get; set; }
        }

        public class Value
        {
            public string description { get; set; }
            public Links _links { get; set; }
            public string id { get; set; }
            public string url { get; set; }
            public string displayName { get; set; }
        }
    }
}
