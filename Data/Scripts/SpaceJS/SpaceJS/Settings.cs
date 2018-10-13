using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceJS
{
    public class Settings
    {
        // The maximum number of steps spread accross all blocks so server should never slow down.
        // higher = scripts run faster, server hit harder
        public static uint maxStepsPerTick = 10000;

        // The maximum number of action event listeners that can be created per block
        public static uint maxEventActions = 50;
    }
}
