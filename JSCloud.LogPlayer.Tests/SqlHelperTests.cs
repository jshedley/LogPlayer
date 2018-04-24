using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSCloud.LogPlayer.Tests
{
    [TestFixture]
    public class SqlHelperTests
    {

        [TestCase(typeof(int), SqlDbType.Int, Description = "Int >> Int")]
        [TestCase(typeof(float), SqlDbType.Real, Description = "Float >> Real")]
        [TestCase(typeof(double), SqlDbType.Float, Description = "Double >> Float")]
        [TestCase(typeof(Guid), SqlDbType.UniqueIdentifier, Description = "Guid >> Uniqueidentifier")]
        public void TestTypeOf(Type type, SqlDbType expectedType)
        {
            Assert.AreEqual(expectedType, SqlHelper.GetDbType(type));
        }

        [TestCase(Description = "Generic Mapping")]

        public void TestGeneric()
        {
            Assert.AreEqual(SqlDbType.UniqueIdentifier, SqlHelper.GetDbType<Guid>());
        }

        [TestCase(Description = "Invalid Mapping")]

        public void TestInvalidMapping()
        {
            TestDelegate testDelegate = () => SqlHelper.GetDbType<DateTime>();
            Assert.That(testDelegate, Throws.TypeOf<ArgumentException>());
        }

    }
}
