using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq.Expressions;

namespace Celo
{
    public interface ICelo
    {
        IDictionary<string, string> EncryptionKeys { get; set; }

        IKeyServer KeyServer { get; set; }

        Func<string> Integrity {get; set;}

        object ClearText(string PropertyName);

    }
}

namespace System
{
    public static class Extensions
    {
        public static object AsClear<T>(this T Item, Expression<Func<object>> Property) where T : Celo.ICelo
        {
            MemberExpression memberExpression = null;
            if (Property.Body.NodeType == ExpressionType.Convert)
            {
                memberExpression = ((UnaryExpression)Property.Body).Operand as MemberExpression;
            }
            else if (Property.Body.NodeType == ExpressionType.MemberAccess)
            {
                memberExpression = Property.Body as MemberExpression;
            }

            if (memberExpression == null)
            {
                throw new ArgumentException("Not a member access", "expression");
            }
            var propName = (memberExpression.Member as PropertyInfo).Name;
            return ((Celo.ICelo)Item).ClearText(propName);
        }
    }
}
