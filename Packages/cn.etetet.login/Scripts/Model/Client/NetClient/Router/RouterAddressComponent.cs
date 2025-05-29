using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class RouterAddressComponent: Entity, IAwake<string>
    {
        public AddressFamily AddressFamily { get; set; }
        public string Address;
        public List<string> Realms { get; set; } = new();
        public List<string> Routers { get; set; } = new();
        public int RouterIndex;
    }
}