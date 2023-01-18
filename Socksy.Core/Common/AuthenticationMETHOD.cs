using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socksy.Core.Common
{
    public enum AuthenticationMETHOD
    {
        NO_AUTHENTICATION_REQUIRED = 0x00,
        GSSAPI = 0x01,
        USERNAME_PASSWORD = 0x02,
        to_X_7F_IANA_ASSIGNED = 0x03,
        to_X_FE_RESERVED_FOR_PRIVATE_METHODS = 0x80,
        NO_ACCEPTABLE_METHODS = 0xFF
    }
}
