using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EncryptedType
{
    public interface ISeekableType
    {
        IDictionary<string, string> HashedValues { set; get; }

        int Iterations { get; set; }

        string HashValue(string Value);

    }
}
