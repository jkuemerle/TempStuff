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

namespace EncryptedType
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

        //[ImportMember("EncryptedValues", IsRequired = true)]
        //public Property<IDictionary<string, string>> EncryptedValuesStore;

        //[ImportMember("EncryptionKeys", IsRequired = true)]
        //public Property<IDictionary<string, string>> EncryptionKeysStore;

        [ImportMember("KeyServer", IsRequired = true)]
        public Property<IKeyServer> KeyServer;

        [ImportMember("Integrity", IsRequired = false)]
        public Property<Func<string>> IntegrityFunction;

        [ImportMember("Encrypt", IsRequired = true, Order=ImportMemberOrder.AfterIntroductions)]
        public Func<string,string,string> Encrypt;

        [ImportMember("Decrypt", IsRequired = true, Order = ImportMemberOrder.AfterIntroductions)]
        public Func<string,string, string> Decrypt;

        [ImportMember("GetEncryptedValues", IsRequired = true, Order = ImportMemberOrder.AfterIntroductions)]
        public Func<IDictionary<string, string>> GetEncryptedValues;

        [ImportMember("GetEncryptionKeys", IsRequired = true, Order = ImportMemberOrder.AfterIntroductions)]
        public Func<IDictionary<string, string>> GetEncryptionKeys;


        public object CreateInstance(AdviceArgs adviceArgs) { return this.MemberwiseClone(); }

        public void RuntimeInitializeInstance() { }

        public override void OnSetValue(LocationInterceptionArgs args)
        {
            if(GetEncryptionKeys().ContainsKey(propname))
            {
                string keyName = GetEncryptionKeys()[propname];
                var encrypted = Encrypt(args.Value.ToString(), KeyServer.Get().GetKey(keyName));
                if (null != GetEncryptedValues())
                    if (!GetEncryptedValues().ContainsKey(propname))
                        GetEncryptedValues().Add(propname, encrypted);
                    else
                        GetEncryptedValues()[propname] = encrypted;
            }
        }

        public override void OnGetValue(LocationInterceptionArgs args)
        {
            if (GetEncryptedValues().ContainsKey(propname))
                args.Value = GetEncryptedValues()[propname];
        }

    }
}
