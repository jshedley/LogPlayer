using JSCloud.LogPlayer.Types;

namespace JSCloud.LogPlayer.TestHarness
{
    internal class SimpleItem : IChangeLogTracked<int>
    {
        public int IntegerStandard { get; set; }
        public string StringStandard { get; set; }
        public long LongStandard { get; set; }
        public int? ObjectId { get; set ; }
    }
}