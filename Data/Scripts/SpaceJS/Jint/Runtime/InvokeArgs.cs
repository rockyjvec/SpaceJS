using Jint.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jint.Runtime
{
    public class InvokeArgs
    {
        public JsValue value;
        public object thisObject;
        public object[] arguments;

        public InvokeArgs(JsValue value, object thisObject, object[] arguments)
        {
            this.value = value;
            this.thisObject = thisObject;
            this.arguments = arguments;
        }
    }
}
