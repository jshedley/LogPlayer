using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSCloud.LogPlayer.Types
{
    public class ChangeLog<I>
        where I : struct
    {

        public ChangeLog() { }

        public ChangeLog(ChangeLog<I> clone)
        {
            ChangeLogId = clone.ChangeLogId;
            ObjectId = clone.ObjectId;
            FullTypeName = clone.FullTypeName;
            PropertySystemType = clone.PropertySystemType;
            Property = clone.Property;
            Value = clone.Value;
            ChangedBy = clone.ChangedBy;
            ChangedUtc = clone.ChangedUtc;
        }

        public Guid? ChangeLogId { get; set; }
        public I? ObjectId { get; set; }
        public string FullTypeName { get; set; }
        public string PropertySystemType { get; set; }
        public string Property { get; set; }
        public string Value { get; set; }
        public int? ChangedBy { get; set; }
        public DateTime ChangedUtc { get; set; } = DateTime.UtcNow;
    }
}
