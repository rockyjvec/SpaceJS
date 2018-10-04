using System;
using Jint.Runtime;

namespace Jint.Native
{
    public sealed class JsNull : JsValue, IEquatable<JsNull>
    {
        internal JsNull() : base(Types.Null)
        {
        }

        public override object ToObject()
        {
            return null;
        }

        public override string ToString()
        {
            return "null";
        }

        public override bool Equals(JsValue obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (!(obj is JsNull))
            {
                return false;
            }

            return Equals(obj as JsNull);
        }

        public bool Equals(JsNull other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            return true;
        }
    }
}