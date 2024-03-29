﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CeloClavis
{
    public class TestServer : EncryptedType.IKeyServer
    {
        public IList<string> Keys 
        {  get 
            {
                return new string[] { "Key1", "Key2" }.ToList();
            }
        }

        public string GetKey(string KeyName)
        {
            switch (KeyName.Trim().ToUpperInvariant())
            {
                case "KEY1" :
                    return "1234";
                    break;
                case "KEY2" : 
                    return "5678";
                    break;
                default : 
                    return null;
                    break;
            }
        }

        public IDictionary<string,string> Map
        {
            get
            {
                var retVal = new Dictionary<string,string>();
                retVal.Add("SSN","Key1");
                return retVal;
            }
        }
    }
}
