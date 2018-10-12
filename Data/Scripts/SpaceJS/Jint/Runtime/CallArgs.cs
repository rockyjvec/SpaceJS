using Jint.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runtime
{
    public class CallArgs
    {
        public JsValue thisObject;
        public JsValue[] arguments;

        public CallArgs(JsValue thisObject, JsValue[] arguments)
        {
            this.thisObject = thisObject;
            this.arguments = arguments;
        }
    }
}
