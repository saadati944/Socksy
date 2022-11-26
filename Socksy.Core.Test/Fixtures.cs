using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socksy.Core.Test
{
    internal static class Fixtures
    {
        private static int _lastTempPort = 54678;

        public static IPEndPoint GetLocalendpointWithTemplatePortNumber()
        {
            return new IPEndPoint(IPAddress.Loopback, _lastTempPort++);
        }
    }
}
