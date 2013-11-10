using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EncryptedType
{
    public interface IKeyServer
    {
        string GetKey(string KeyName);

        IList<string> Keys {get;}

        IDictionary<string, string> Map { get; }
    }
}
