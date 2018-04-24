using JSCloud.LogPlayer.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSCloud.LogPlayer.Tests
{
    public abstract class LogApplyerIntegrationTests
    {
        public IStore<int> Store { get; set; }        
    }
}
