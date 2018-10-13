using Esprima.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jint.Runtime
{
    public class RuntimeState
    {
        public Action<RuntimeState> method;
        public object arg;
        public object local = null;
        public object local2 = null;
        public object calleeReturnValue;
        public bool calleeReturned = false;
        public uint stage = 0;
        public bool interrupt = false;

        public RuntimeState(Action<RuntimeState> method, object arg, bool interrupt = false)
        {
            this.method = method;
            this.arg = arg;
            this.interrupt = interrupt;
        }

        public void Call()
        {
            this.method(this);
        }
    }
}
