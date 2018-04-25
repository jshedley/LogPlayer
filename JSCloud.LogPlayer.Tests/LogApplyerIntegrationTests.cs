using JSCloud.LogPlayer.Store;
using JSCloud.LogPlayer.Types;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSCloud.LogPlayer.Tests
{
    public abstract class LogApplyerIntegrationTests
    {
        public IStore<int> Store { get; set; }

        [TestCase(1, "2", 3, 4, 5, TestName = "As the system inserting a single entry into the store")]
        public async Task InsertSingleTestData(int integerStandard, string stringStandard, long longStandard, int? integerNullable, long? longNullable)
        {
            var changeLog = new ChangeLog<int>()
            {
                ChangeLogId = null,
                ObjectId = 1,
                ChangedBy = null,
                Property = "integerStandard",
                ChangedUtc = DateTime.UtcNow,
                Value = integerStandard.ToString(),
                FullTypeName = "JSCloud.LogPlayer.Tests.SimpleItem",
                PropertySystemType = "System.Int32"
            };
            var result = await this.Store.StoreAsync(changeLog);
            Assert.IsTrue(result.ChangeLogId.HasValue);

        }


        [TestCase(1, "2", 3, 4, 5, TestName = "As the system inserting multiple entry into the store and force an exception")]
        public async Task InsertBulkTestDataAndForceAnException(int integerStandard, string stringStandard, long longStandard, int? integerNullable, long? longNullable)
        {
            ICollection<ChangeLog<int>> changeLogs = new LinkedList<ChangeLog<int>>();
            for (int i = 0; i < 100; i++)
            {
                var changeLog = new ChangeLog<int>()
                {
                    ChangeLogId = null,
                    ChangedBy = null,
                    ObjectId = null,
                    Property = "integerStandard",
                    ChangedUtc = DateTime.UtcNow,
                    Value = integerStandard.ToString(),
                    FullTypeName = "JSCloud.LogPlayer.Tests.SimpleItem2 AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                    PropertySystemType = "System.Int32"
                };
                changeLogs.Add(changeLog);
            }

            if (Store is MicrosoftSqlStore<int> || Store is LogApplyerIntegrationInMemoryStoreWithSqlBaseStore)
            {
                TestDelegate testDelegate = () => this.Store.StoreAsync(changeLogs).GetAwaiter().GetResult();
                Assert.That(testDelegate, Throws.TypeOf<InvalidOperationException>());

                testDelegate = () => this.Store.StoreAsync(changeLogs.ElementAt(0)).GetAwaiter().GetResult();
                Assert.That(testDelegate, Throws.TypeOf<Exception>());

                changeLogs.ElementAt(0).ChangeLogId = Guid.NewGuid();
                testDelegate = () => this.Store.StoreAsync(changeLogs.ElementAt(0)).GetAwaiter().GetResult();
                Assert.That(testDelegate, Throws.TypeOf<ArgumentException>());

                testDelegate = () => this.Store.StoreAsync(changeLogs).GetAwaiter().GetResult();
                Assert.That(testDelegate, Throws.TypeOf<System.ArgumentException>());
            }
        }


        [TestCase(1, "2", 3, 4, 5, TestName = "As the system inserting multiple entry into the store")]
        public async Task InsertBulkTestData(int integerStandard, string stringStandard, long longStandard, int? integerNullable, long? longNullable)
        {
            ICollection<ChangeLog<int>> changeLogs = new LinkedList<ChangeLog<int>>();
            for (int i = 0; i < 100; i++)
            {
                var changeLog = new ChangeLog<int>()
                {
                    ChangeLogId = null,
                    ObjectId = i,
                    ChangedBy = null,
                    Property = "integerStandard",
                    ChangedUtc = DateTime.UtcNow,
                    Value = integerStandard.ToString(),
                    FullTypeName = "JSCloud.LogPlayer.Tests.SimpleItem2",
                    PropertySystemType = "System.Int32"
                };
                changeLogs.Add(changeLog);
            }
            var results = await this.Store.StoreAsync(changeLogs);

            var changes = await this.Store.GetChangesAsync(null, "JSCloud.LogPlayer.Tests.SimpleItem2");
            Assert.AreEqual(results.Count, changes.Count);

            changes = await this.Store.GetChangesAsync(changeLogs.ElementAt(0).ObjectId, "JSCloud.LogPlayer.Tests.SimpleItem2");
            Assert.IsTrue(changes.Count == 1);
            Assert.AreEqual(changeLogs.ElementAt(0).ChangeLogId, changes.ElementAt(0).ChangeLogId);

            changeLogs = new LinkedList<ChangeLog<int>>();
            for (int i = 0; i < 100; i++)
            {
                var changeLog = new ChangeLog<int>()
                {
                    ChangeLogId = null,
                    ChangedBy = null,
                    Property = "integerStandard",
                    ChangedUtc = DateTime.UtcNow,
                    Value = integerStandard.ToString(),
                    FullTypeName = "JSCloud.LogPlayer.Tests.SimpleItem",
                    PropertySystemType = "System.Int32"
                };
                changeLogs.Add(changeLog);
            }
            var resultsDifferetntType = await this.Store.StoreAsync(changeLogs);

            Assert.AreEqual(100, resultsDifferetntType.Count); //This will ensure that the types are filtered correctly



        }

    }
}
