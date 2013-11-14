using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

using EncryptedType;

using NUnit.Framework;

using Raven;
using Raven.Client.Document;
using Newtonsoft.Json;

namespace EncryptedType.Tests
{
    [TestFixture]
    public class TestCelo
    {
        private string serverURL = "http://klaptop:1999";
        Raven.Client.IDocumentStore docStore;

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
            ((IEncryptedType)n).KeyServer = s;
            ((IEncryptedType)n).EncryptionKeys = s.Map;
            ((IEncryptedType)n).Integrity = n.IntegrityValue;
            n.SSN = "111-11-1111";
            Assert.AreNotEqual("111-11-1111", n.SSN);
        }

        [Test]
        public void TestDecryption()
        {
            var n = new EncTest();
            var s = new CeloClavis.TestServer();
            ((IEncryptedType)n).KeyServer = s;
            ((IEncryptedType)n).EncryptionKeys = s.Map;
            ((IEncryptedType)n).Integrity = n.IntegrityValue;
            n.SSN = "111-11-1111";
            Assert.AreEqual("111-11-1111", ((IEncryptedType)n).AsClear(() => n.SSN));
        }

        [Test]
        public void RavenStuff()
        {
            docStore = new Raven.Client.Document.DocumentStore() { Url = serverURL, DefaultDatabase = "Keys" };
            docStore.Initialize();
            var session = docStore.OpenSession();
            for(int loop = 1; loop <= 50; loop++)
            {
                string keyName = string.Format("Key{0}", loop);
                var newKey = new EncryptedType.Key() { Name = keyName, KeyValue = Guid.NewGuid().ToString() };
                session.Store(newKey, keyName);
            }
            session.SaveChanges();
        }

        [Test]
        public void TestRavenKeyServer()
        {
            var ks = new CeloClavis.RavenDBServer(serverURL, "Keys");
            var n = new EncTest();
            ((IEncryptedType)n).KeyServer = ks;
            ((IEncryptedType)n).EncryptionKeys.Add("SSN","Key1");
            //((IEncryptedType)n).Integrity = n.IntegrityValue;
            n.SSN = "111-11-1111";
            var asJson = JsonConvert.SerializeObject(n);
            using (var ds = new Raven.Client.Document.DocumentStore() { Url = serverURL, DefaultDatabase = "Test" })
            {
                ds.Initialize();
                var session = ds.OpenSession();
                session.Store(n, n.ID);
                session.SaveChanges();
                var test = session.Load<EncTest>(n.ID);
                Assert.AreEqual(n.SSN, test.SSN);
                Assert.AreEqual( ((IEncryptedType)n).AsClear(() => n.SSN), ((IEncryptedType)test).AsClear(() => test.SSN));
            }
        }
    }
}
