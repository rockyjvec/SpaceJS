using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Interop;
using Sandbox.ModAPI;
using Jint.Native;

using SpaceJS.Api.Blocks;

namespace SpaceJS.Api.Blocks
{
    public sealed class BlocksInstance : ObjectInstance
    {
        private SpaceJS.Block js;
        IMyGridTerminalSystem grid;

        private BlocksInstance(Jint.Engine engine) : base(engine, "Blocks")
        {
        }

        public static BlocksInstance CreateObject(Jint.Engine engine)
        {
            var e = new BlocksInstance(engine);
            e.Extensible = true;
            e.Prototype = engine.Object.PrototypeObject;

            return e;
        }

        public void Configure(SpaceJS.Block js)
        {
            this.js = js;

            grid = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(js.cb.CubeGrid);

            FastAddProperty("get", new ClrFunctionInstance(Engine, "get", GetBlock), true, false, true);
        }

        public JsValue GetBlock(JsValue obj, JsValue[] arguments)
        {
            if (arguments.Length < 1)
                return null;
            var name = TypeConverter.ToString(arguments.At(0));
            var block = grid.GetBlockWithName(name);
            if (block == null)
                return null;

            var b = BlockInstance.CreateObject(this.Engine, block);
            b.Configure(js);

            return b;
        }
    }
}
