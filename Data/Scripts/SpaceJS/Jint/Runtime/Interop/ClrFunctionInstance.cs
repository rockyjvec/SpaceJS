﻿using System;
using Jint.Native;
using Jint.Native.Function;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;

namespace Jint.Runtime.Interop
{
    /// <summary>
    /// Wraps a Clr method into a FunctionInstance
    /// </summary>
    public sealed class ClrFunctionInstance : FunctionInstance, IEquatable<ClrFunctionInstance>
    {
        private readonly Func<JsValue, JsValue[], JsValue> _func;

        public ClrFunctionInstance(
            Engine engine,
            string name,
            Func<JsValue, JsValue[], JsValue> func,
            int length = 0,
            PropertyFlag lengthFlags = PropertyFlag.AllForbidden) : base(engine, name, null, null, false)
        {
            _func = func;

            Prototype = engine.Function.PrototypeObject;
            Extensible = true;

            _length = new PropertyDescriptor(length, lengthFlags);
        }

        public override JsValue Call(JsValue thisObject, JsValue[] arguments)
        {
            try
            {
                var result = _func(thisObject, arguments);
                return result;
            }
            catch (InvalidCastException)
            {
                ExceptionHelper.ThrowTypeError(Engine);
                return null;
            }
        }
        
        public override bool Equals(JsValue obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (!(obj is ClrFunctionInstance))
            {
                return false;
            }

            return Equals(obj as ClrFunctionInstance);
        }

        public bool Equals(ClrFunctionInstance other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (_func == other._func)
            {
                return true;
            }
            
            return false;
        }
    }
}
