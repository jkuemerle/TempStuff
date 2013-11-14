using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using Raven;
using Raven.Client.Document;

using CeloCorvus;

namespace CeloCorvus.Tests
{
    [TestFixture]
    public class TestCeloCorvus
    {
        private string serverURL = "http://klaptop:1999";
        Raven.Client.IDocumentStore docStore;

        public class EncTest
        {
            public string ID { get; set; }
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

        //[Test]
        //public void TestRavenKeyServer()
        //{
        //    var ks = new CeloClavis.RavenDBServer(serverURL, "Keys");
        //    var n = new EncTest();
        //    ((IEncryptedType)n).KeyServer = ks;
        //    ((IEncryptedType)n).EncryptionKeys.Add("SSN", "Key1");
        //    //((IEncryptedType)n).Integrity = n.IntegrityValue;
        //    n.SSN = "111-11-1111";
        //    var asJson = JsonConvert.SerializeObject(n);
        //    using (var ds = new Raven.Client.Document.DocumentStore() { Url = serverURL, DefaultDatabase = "Test" })
        //    {
        //        ds.Initialize();
        //        var session = ds.OpenSession();
        //        session.Store(n, n.ID);
        //        session.SaveChanges();
        //        var test = session.Load<EncTest>(n.ID);
        //        Assert.AreEqual(n.SSN, test.SSN);
        //        Assert.AreEqual(((IEncryptedType)n).AsClear(() => n.SSN), ((IEncryptedType)test).AsClear(() => test.SSN));
        //    }
        //}


    }
}
