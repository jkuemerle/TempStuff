using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Celo;

using NUnit.Framework;

namespace Celo.Tests
{
    [TestFixture]
    public class TestCelo
    {

        [EncryptedType]
        public class EncTest
        {
            public string ID { get; set; }
            [EncryptedValue]
            public string SSN { get; set; }

            public string IntegrityValue()
            {
                return this.ID;
            }

            public EncTest()
            {
                this.ID = Guid.NewGuid().ToString();
            }
        }

        [Test]
        public void TestAspect()
        {
            var n = new EncTest();
            var s = new CeloClavis.TestServer();
            ((ICelo)n).KeyServer = s;
            ((ICelo)n).EncryptionKeys = s.Map;
            //((ICelo)n).Integrity = n.IntegrityValue;
            //((ICelo)n).EncryptionKeys.Add("SSN", "Key1");
            n.SSN = "111-11-1111";
            Assert.AreEqual("111-11-1111", n.SSN);
        }


    }
}
