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
    [TestFixture]
    public class LogApplyerIntegrationInMemoryStore: LogApplyerIntegrationTests
    {
        private static string _uid = DateTime.UtcNow.Ticks.ToString();


        public LogApplyerIntegrationInMemoryStore()
        {
            this.Store = new InMemoryStore<int>(null);
        }
              
        

        [OneTimeSetUp()]
        public void provision()
        {
            this.Store.Provision().GetAwaiter().GetResult();
        }

      

    }
}
