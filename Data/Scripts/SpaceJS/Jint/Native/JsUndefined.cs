﻿using System;
using Jint.Runtime;

namespace Jint.Native
{
    public sealed class JsUndefined : JsValue, IEquatable<JsUndefined>
    {
        internal JsUndefined() : base(Types.Undefined)
        {
        }

        public override object ToObject()
        {
            return null;
        }

        public override string ToString()
        {
            return "undefined";
        }

        public override bool Equals(JsValue obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (!(obj is JsUndefined))
            {
                return false;
            }

            return Equals(obj as JsUndefined);
        }

        public bool Equals(JsUndefined other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            return true;
        }
    }
}