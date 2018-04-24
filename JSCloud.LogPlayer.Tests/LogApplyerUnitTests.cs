using System;
using System.Collections.Generic;
using System.Diagnostics;
using JSCloud.LogPlayer.Types;
using NUnit.Framework;

namespace JSCloud.LogPlayer.Tests
{
    [TestFixture]
    public class LogApplyerUnitTests
    {
        [TestCase(1, "2", 3, null, null, TestName =
            "As the system applying logs without overriting values does it work with null values")]
        [TestCase(1, "2", 3, 4, 5, TestName =
            "As the system applying logs without overriting values does it work without null values")]
        public void SimpleBuild(int integerStandard, string stringStandard, long longStandard, int? integerNullable,
            long? longNullable)
        {
            IChangeLogPlayer<SimpleItem, int> logPlayer = new ChangeLogPlayer<SimpleItem, int>();
            var timer = new Stopwatch();
            timer.Start();
            for (var i = 0; i < 1000; i++)
            {
                IList<ChangeLog<int>> changes = new List<ChangeLog<int>>();
                changes.Add(new ChangeLog<int>
                {
                    ChangeLogId = null,
                    ChangedBy = null,
                    Property = "IntegerStandard",
                    ChangedUtc = DateTime.UtcNow,
                    Value = integerStandard.ToString(),
                    FullTypeName = "JSCloud.LogPlayer.Tests.SimpleItem",
                    PropertySystemType = "System.Int32"
                });
                changes.Add(new ChangeLog<int>
                {
                    ChangeLogId = null,
                    ChangedBy = null,
                    Property = "StringStandard",
                    ChangedUtc = DateTime.UtcNow,
                    Value = stringStandard,
                    FullTypeName = "JSCloud.LogPlayer.Tests.SimpleItem",
                    PropertySystemType = "System.String"
                });
                changes.Add(new ChangeLog<int>
                {
                    ChangeLogId = null,
                    ChangedBy = null,
                    Property = "LongStandard",
                    ChangedUtc = DateTime.UtcNow,
                    Value = longStandard.ToString(),
                    FullTypeName = "JSCloud.LogPlayer.Tests.SimpleItem",
                    PropertySystemType = "System.Int64"
                });
                changes.Add(new ChangeLog<int>
                {
                    ChangeLogId = null,
                    ChangedBy = null,
                    Property = "IntegerNullable",
                    ChangedUtc = DateTime.UtcNow,
                    Value = integerNullable?.ToString(),
                    FullTypeName = "JSCloud.LogPlayer.Tests.SimpleItem",
                    PropertySystemType = "System.Int32"
                });
                changes.Add(new ChangeLog<int>
                {
                    ChangeLogId = null,
                    ChangedBy = null,
                    Property = "LongNullable",
                    ChangedUtc = DateTime.UtcNow,
                    Value = longNullable?.ToString(),
                    FullTypeName = "JSCloud.LogPlayer.Tests.SimpleItem",
                    PropertySystemType = "System.Int32"
                });

                var SimpleItem = logPlayer.RebuildFromLogs(changes);
                Assert.AreEqual(integerStandard, SimpleItem.IntegerStandard);
            }

            timer.Stop();
            Debug.WriteLine($"{timer.ElapsedMilliseconds}ms elapsed");

            Assert.IsTrue(timer.ElapsedMilliseconds < 1000);
        }

        [TestCase(1, "2", 3, null, null, TestName =
            "As the system passing in the incorrect type, I expect an argument exception.")]
        public void CheckForTypeSafety(int integerStandard, string stringStandard, long longStandard,
            int? integerNullable, long? longNullable)
        {
            IChangeLogPlayer<SimpleItem, int> logPlayer = new ChangeLogPlayer<SimpleItem, int>();
            TestDelegate testDelegate = () => logPlayer.Apply(new ChangeLog<int>(), null);
            Assert.That(testDelegate, Throws.TypeOf<ArgumentException>());
        }


        [TestCase(1, 1000, TestName =
            "As the system running a series of changes, I want to ensure the final value are correct and of speed.")]
        public void SequencialChanges(int finalInt, int iterations)
        {
            IChangeLogPlayer<SimpleItem, int> logPlayer = new ChangeLogPlayer<SimpleItem,int>();
            ICollection<ChangeLog<int>> changes = new LinkedList<ChangeLog<int>>();
            changes.Add(new ChangeLog<int>
            {
                Property = "IntegerStandard",
                ChangedUtc = DateTime.UtcNow.AddSeconds(5),
                Value = finalInt.ToString(),
                FullTypeName = "JSCloud.LogPlayer.Tests.SimpleItem",
                PropertySystemType = "System.Int32"
            });

            for (var i = 0; i < iterations; i++)
                changes.Add(new ChangeLog<int>
                {
                    Property = "IntegerStandard",
                    ChangedUtc = DateTime.UtcNow,
                    Value = i.ToString(),
                    ObjectId = i,
                    FullTypeName = "JSCloud.LogPlayer.Tests.SimpleItem",
                    PropertySystemType = "System.Int32"
                });

            var timer = new Stopwatch();
            timer.Start();
            var SimpleItem = logPlayer.RebuildFromLogs(changes);
            timer.Stop();
            Assert.AreEqual(finalInt, SimpleItem.IntegerStandard);
            Debug.WriteLine($"{timer.ElapsedMilliseconds}ms elapsed");

            Assert.IsTrue(timer.ElapsedMilliseconds <= iterations);
        }

        [TestCase(1, 1000, TestName =
           "As the system running a series of changes, I want the system to decide on my changes")]
        public void DetectChanges(int finalInt, int iterations)
        {
            var simpleItemSource = new SimpleItem() { IntegerNullable = 1, StringStandard = "Hello World", LongNullable = null, ObjectId = 1 } ;
            var simpleItemDestination = new SimpleItem() { IntegerNullable = 2, StringStandard = "Hello World 2", LongNullable = 3, ObjectId = 1 } ;

            IChangeLogPlayer<SimpleItem, int> logPlayer = new ChangeLogPlayer<SimpleItem, int>();

            var changes = logPlayer.CalculateChanges(simpleItemSource, simpleItemDestination, 1);

            bool integerNullableFound = false;
            bool stringStandardFound = false;
            bool objectIdFound = false;
            bool longNullableFoundFalse = false;

            foreach(var change in changes)
            {
                integerNullableFound = integerNullableFound == true || (change.Property == "IntegerNullable" && change.Value == "1");
                stringStandardFound = stringStandardFound == true || (change.Property == "StringStandard" && change.Value == "Hello World");
                objectIdFound = objectIdFound == true || (change.Property == "ObjectId");
                longNullableFoundFalse = longNullableFoundFalse == true || (change.Property == "LongNullable" && string.IsNullOrEmpty(change.Value));
            }

            Assert.IsTrue(integerNullableFound);
            Assert.IsTrue(stringStandardFound);
            Assert.IsTrue(longNullableFoundFalse);
            Assert.IsTrue(!objectIdFound);

            TestDelegate testDelegate = () => logPlayer.CalculateChanges(null, null, 1);
            Assert.That(testDelegate, Throws.TypeOf<ArgumentNullException>());


        }

    }
}