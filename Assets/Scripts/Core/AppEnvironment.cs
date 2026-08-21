namespace NAS.Core
{
    // Named AppEnvironment, not Environment, to avoid colliding with System.Environment.
    public enum AppEnvironment
    {
        Local,
        ApiDomain,

        // Same nginx HTTPS proxy as ApiDomain, addressed by raw LAN IP
        // instead of the api.nas.test hostname - for physical-device testing
        // on a network where nothing resolves that hostname (e.g. a phone
        // hotspot without dnsmasq's device-DNS override set up). Needs its
        // own ApiSettings asset since the IP changes per network/hotspot -
        // update that asset's _baseUrl when the LAN IP changes, same as the
        // dnsmasq LAN IP is per-network.
        ApiIp
    }
}
