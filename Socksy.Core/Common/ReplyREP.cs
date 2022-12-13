namespace Socksy.Core.Common;

public enum ReplyREP
{
    Succeeded = 0,
    General_socks5_server_failure = 1,
    Connection_not_allowed_by_ruleset = 2,
    Network_unreachable = 3,
    Host_unreachable = 4,
    Connection_refused = 5,
    TTL_expired = 6,
    Command_not_supported = 7,
    Address_type_not_supported = 8,
    UNASSIGNED = 9
}