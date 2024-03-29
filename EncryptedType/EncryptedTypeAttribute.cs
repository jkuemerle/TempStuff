﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

using PostSharp;
using PostSharp.Serialization;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Dependencies;

namespace EncryptedType
{
    [PSerializable]
    [AttributeUsage(AttributeTargets.Class)]
    [AspectTypeDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, typeof(EncryptedValueAttribute))]
    [IntroduceInterface(typeof(IEncryptedType), OverrideAction = InterfaceOverrideAction.Ignore)]
    public class EncryptedTypeAttribute : InstanceLevelAspect, IEncryptedType
    {
        [IntroduceMember(IsVirtual = false, OverrideAction = MemberOverrideAction.OverrideOrFail, Visibility = PostSharp.Reflection.Visibility.Public)]
        public IDictionary<string, string> EncryptedValues { set; get; }

        [IntroduceMember(IsVirtual = false, OverrideAction = MemberOverrideAction.OverrideOrFail, Visibility = PostSharp.Reflection.Visibility.Public)]
        public IDictionary<string, string> EncryptionKeys { set; get; }

        [IntroduceMember(IsVirtual=false,OverrideAction=MemberOverrideAction.OverrideOrFail, Visibility=PostSharp.Reflection.Visibility.Public)]
        public IKeyServer KeyServer { get; set; }

        [IntroduceMember(IsVirtual = true, OverrideAction = MemberOverrideAction.OverrideOrFail, Visibility = PostSharp.Reflection.Visibility.Public)]
        public Func<string> Integrity { get; set; }


        [IntroduceMember(IsVirtual = false, OverrideAction = MemberOverrideAction.OverrideOrFail, Visibility = PostSharp.Reflection.Visibility.Public)]
        public IDictionary<string, string> GetEncryptedValues()
        {
            return this.EncryptedValues;
        }

        [IntroduceMember(IsVirtual = false, OverrideAction = MemberOverrideAction.OverrideOrFail, Visibility = PostSharp.Reflection.Visibility.Public)]
        public IDictionary<string, string> GetEncryptionKeys()
        {
            return this.EncryptionKeys;
        }

        public override void RuntimeInitialize(Type type)
        {
            EncryptedValues = new Dictionary<string, string>();
            EncryptionKeys = new Dictionary<string, string>();
        }

        [IntroduceMember(IsVirtual = false, OverrideAction = MemberOverrideAction.OverrideOrFail, Visibility = PostSharp.Reflection.Visibility.Public)]
        public object ClearText(string PropertyName)
        {
            if (EncryptionKeys.ContainsKey(PropertyName) && EncryptedValues.ContainsKey(PropertyName))
            {
                string keyName = EncryptionKeys[PropertyName];

                return Decrypt(EncryptedValues[PropertyName], KeyServer.GetKey(keyName));
            }
            return null;
        }

        [IntroduceMember(IsVirtual = false, OverrideAction = MemberOverrideAction.OverrideOrFail, Visibility = PostSharp.Reflection.Visibility.Public)]
        public string Encrypt(string Data, string KeyValue)
        {
            if (null != this.Integrity)
                Data = AddHMAC(Data, this.Integrity);
            var val = System.Text.UnicodeEncoding.Unicode.GetBytes(Data);
            var iv = new byte[new System.Security.Cryptography.AesManaged().BlockSize / 8].FillWithEntropy();
            byte[] key = new Rfc2898DeriveBytes(KeyValue, iv).GetBytes(new System.Security.Cryptography.AesManaged().KeySize / 8);
            byte[] encrypted;
            var crypt = new System.Security.Cryptography.AesManaged() { IV = iv, Key = key, Mode = System.Security.Cryptography.CipherMode.CBC };
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
            return string.Format("{0}\0{1}", Convert.ToBase64String(iv), Convert.ToBase64String(encrypted));
        }

        private string AddHMAC(string Data, Func<string> Integrity)
        {
            var retVal = Data;
            retVal = string.Format("{0}\0{1}", Data, ComputeHMAC(Data, Integrity));
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
            catch (Exception ex)
            {
                return false;
            }
        }
        private static string ComputeHMAC(string Data, Func<string> Integrity)
        {
            var hmac = new HMACSHA512() { Key = Encoding.Unicode.GetBytes(Integrity.Invoke()) };
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.Unicode.GetBytes(Data)));
        }

        [IntroduceMember(IsVirtual = false, OverrideAction = MemberOverrideAction.OverrideOrFail, Visibility = PostSharp.Reflection.Visibility.Public)]
        public string Decrypt(string Data, string KeyValue)
        {
            string retVal = null;
            var vals = Data.Split('\0');
            if (vals.Length > 1)
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
            if (null != this.Integrity)
            {
                var values = retVal.Split('\0');
                if (values.Length < 2)
                    retVal = null;
                if (null != retVal && VerifyHMAC(retVal, this.Integrity))
                    retVal = values[0];
                else
                    retVal = null;
            }
            return retVal;
        }

    }
}
