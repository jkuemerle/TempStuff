using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using Raven;
using Raven.Client.Document;

using CeloCorvus;

using EncryptedType;

namespace CeloCorvus.Tests
{
    [TestFixture]
    public class TestCeloCorvus
    {
        private string serverURL = "http://klaptop:1999";
        Raven.Client.IDocumentStore docStore;

        [RavenEncryptedType]
        [RavenSeekableType]
        public class EncTest
        {
            public string ID { get; set; }
            [RavenEncryptedValue]
            [RavenSeekableValue]
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
        public void TestRavenWithKeyServer()
        {
            var ks = new CeloClavis.RavenDBServer(serverURL, "Keys");
            var n = new EncTest();
            ((IEncryptedType)n).KeyServer = ks;
            ((IEncryptedType)n).EncryptionKeys.Add("SSN", "Key1");
            ((IEncryptedType)n).Integrity = n.IntegrityValue;
            n.SSN = "111-11-1111";
            using (var ds = new Raven.Client.Document.DocumentStore() { Url = serverURL, DefaultDatabase = "Test" })
            {
                ds.Initialize();
                var session = ds.OpenSession();
                session.Store(n, n.ID);
                session.SaveChanges();
                var test = session.Load<EncTest>(n.ID);
                ((IEncryptedType)test).KeyServer = ks;
                ((IEncryptedType)test).Integrity = n.IntegrityValue;
                Assert.AreEqual(n.ID, test.ID);
                Assert.AreEqual(n.SSN, test.SSN);
                string nSSN = ((IEncryptedType)n).AsClear(() => n.SSN).ToString();
                string tSSN = ((IEncryptedType)test).AsClear(() => test.SSN).ToString();
                Assert.AreEqual(nSSN, tSSN );
            }
        }


    }
}
