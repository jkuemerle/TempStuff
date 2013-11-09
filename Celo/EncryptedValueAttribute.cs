using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

using PostSharp;
using PostSharp.Serialization;
using PostSharp.Aspects;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Advices;

namespace Celo
{
    [PSerializable]
    [AspectTypeDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, typeof(EncryptedTypeAttribute))]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class EncryptedValueAttribute : LocationInterceptionAspect, IInstanceScopedAspect
    {

        private string propname;

        public override void CompileTimeInitialize(PostSharp.Reflection.LocationInfo targetLocation, AspectInfo aspectInfo)
        {
            propname = targetLocation.Name;
        }

        [ImportMember("EncryptedValues", IsRequired = true)]
        public Property<IDictionary<string, string>> EncryptedValuesStore;

        [ImportMember("EncryptionKeys", IsRequired = true)]
        public Property<IDictionary<string, string>> EncryptionKeysStore;

        [ImportMember("KeyServer", IsRequired = true)]
        public Property<IKeyServer> KeyServer;

        [ImportMember("Integrity", IsRequired = false)]
        public Property<Func<string>> IntegrityFunction;
        public object CreateInstance(AdviceArgs adviceArgs) { return this.MemberwiseClone(); }

        public void RuntimeInitializeInstance() { }

        public override void OnSetValue(LocationInterceptionArgs args)
        {
            if(EncryptionKeysStore.Get().ContainsKey(propname))
            {
                string keyName = EncryptionKeysStore.Get()[propname];
                var encrypted = Encrypt(args.Value.ToString(), KeyServer.Get().GetKey(keyName));
                if (null != EncryptedValuesStore)
                    if (!EncryptedValuesStore.Get().ContainsKey(propname))
                        EncryptedValuesStore.Get().Add(propname, encrypted);
                    else
                        EncryptedValuesStore.Get()[propname] = encrypted;
            }
        }

        public override void OnGetValue(LocationInterceptionArgs args)
        {
            if(EncryptionKeysStore.Get().ContainsKey(propname) && EncryptedValuesStore.Get().ContainsKey(propname))
            {
                string keyName = EncryptionKeysStore.Get()[propname];
                args.Value = Decrypt(EncryptedValuesStore.Get()[propname], KeyServer.Get().GetKey(keyName));
            }
        }

        private string Encrypt(string Data, string KeyValue)
        {
            if (null != this.IntegrityFunction.Get())
                Data = AddHMAC(Data, this.IntegrityFunction.Get()); 
            var val = System.Text.UnicodeEncoding.Unicode.GetBytes(Data);
            var iv = new byte[new System.Security.Cryptography.AesManaged().BlockSize / 8].FillWithEntropy();
            byte[] key = new Rfc2898DeriveBytes(KeyValue, iv).GetBytes(new System.Security.Cryptography.AesManaged().KeySize / 8);
            byte[] encrypted;
            var crypt = new System.Security.Cryptography.AesManaged() { IV = iv, Key = key,   Mode = System.Security.Cryptography.CipherMode.CBC };
            using (var encrypter = crypt.CreateEncryptor())
            {
                using (var to = new MemoryStream())
                {
                    using (var writer = new CryptoStream(to, encrypter, CryptoStreamMode.Write))
                    {
                        writer.Write(val, 0, val.Length);
                        writer.FlushFinalBlock();
                        encrypted = to.ToArray();
                    }
                }
            }
            return string.Format("{0}|{1}",Convert.ToBase64String(iv), Convert.ToBase64String(encrypted));
        }

        private string AddHMAC(string Data, Func<string> Integrity)
        {
            var retVal = Data;
            retVal = string.Format("{0}\0{1}", Data, ComputeHMAC(Data,Integrity));
            return retVal;
        }
        private bool VerifyHMAC(string Data, Func<string> Integrity)
        {
            try
            {
            var values = Data.Split('\0');
            if (values.Length < 2)
                return false;
            return values[1] == ComputeHMAC(values[0], Integrity);
            }
            catch(Exception ex)
            {
                return false;
            }
        }
        private static string ComputeHMAC(string Data, Func<string> Integrity)
        {
            var hmac = new HMACSHA512() { Key = Encoding.Unicode.GetBytes(Integrity.Invoke()) };
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.Unicode.GetBytes(Data)));
        }

        private string Decrypt(string Data, string KeyValue)
        {
            string retVal = null;
            var vals = Data.Split('|');
            if(vals.Length > 1)
            {
                var iv = Convert.FromBase64String(vals[0]);
                byte[] key = new Rfc2898DeriveBytes(KeyValue, iv).GetBytes(new System.Security.Cryptography.AesManaged().KeySize / 8);
                var encrypted = Convert.FromBase64String(vals[1]);
                byte[] decrypted;
                int decryptedByteCount = 0;
                var crypt = new System.Security.Cryptography.AesManaged() { IV = iv, Key = key, Mode = System.Security.Cryptography.CipherMode.CBC };
                using (var cipher = crypt.CreateDecryptor())
                {
                    using (var from = new MemoryStream(encrypted))
                    {
                        using (var reader = new CryptoStream(from, cipher, CryptoStreamMode.Read))
                        {
                            decrypted = new byte[encrypted.Length];
                            decryptedByteCount = reader.Read(decrypted, 0, decrypted.Length);
                        }
                    }
                }
                retVal = Encoding.Unicode.GetString(decrypted, 0, decryptedByteCount);
            }
            if(null != this.IntegrityFunction.Get())
            {
                var values = retVal.Split('\0');
                if(values.Length < 2)
                    retVal = null;
                if(null != retVal && VerifyHMAC(retVal, this.IntegrityFunction.Get()))
                    retVal = values[0];
                else
                    retVal = null;
            }
            return retVal;
        }
    }
}
