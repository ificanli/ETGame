using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace ET
{
	public static class NetworkHelper
	{
		public static string[] GetAddressIPs()
		{
			List<string> list = new List<string>();
			foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (networkInterface.NetworkInterfaceType != NetworkInterfaceType.Ethernet)
				{
					continue;
				}
				foreach (UnicastIPAddressInformation add in networkInterface.GetIPProperties().UnicastAddresses)
				{
					list.Add(add.Address.ToString());
				}
			}
			return list.ToArray();
		}
		
		// 优先获取IPV4的地址
		public static IPAddress GetHostAddress(string hostName)
		{
			IPAddress[] ipAddresses = Dns.GetHostAddresses(hostName);
			IPAddress returnIpAddress = null;
			foreach (IPAddress ipAddress in ipAddresses)
			{
				returnIpAddress = ipAddress;
				if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
				{
					return ipAddress;
				}
			}
			return returnIpAddress;
		}
		
		public static IPEndPoint ToIPEndPoint(string host, int port)
		{
			return new IPEndPoint(IPAddress.Parse(host), port);
		}

		public static IPEndPoint ToIPEndPoint(string address)
		{
			int index = address.LastIndexOf(':');
			string host = address.Substring(0, index);
			string p = address.Substring(index + 1);
			int port = int.Parse(p);
			return ToIPEndPoint(host, port);
		}

		public static int IPStringToInt(string ipString)
		{
			IPAddress ipAddress = IPAddress.Parse(ipString);
			byte[] bytes = ipAddress.GetAddressBytes();
			if (bytes.Length != 4)
			{
				throw new ArgumentException("Only IPv4 addresses are supported");
			}
			return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
		}

		public static string IntToIPString(int ipInt)
		{
			byte[] bytes = new byte[4];
			bytes[0] = (byte)((ipInt >> 24) & 0xFF);
			bytes[1] = (byte)((ipInt >> 16) & 0xFF);
			bytes[2] = (byte)((ipInt >> 8) & 0xFF);
			bytes[3] = (byte)(ipInt & 0xFF);
			return new IPAddress(bytes).ToString();
		}
        
        public static Address IPEndPointToAddress(IPEndPoint ipEndPoint)
        {
            int ip = IPStringToInt(ipEndPoint.Address.ToString());
            int port = ipEndPoint.Port;
            return new Address(ip, port);
        }
        
        public static IPEndPoint AddressToIPEndPoint(Address address)
        {
            string ipString = IntToIPString(address.IP);
            return new IPEndPoint(IPAddress.Parse(ipString), address.Port);
        }

		public static void SetSioUdpConnReset(Socket socket)
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return;
			}

			const uint IOC_IN = 0x80000000;
			const uint IOC_VENDOR = 0x18000000;
			const int SIO_UDP_CONNRESET = unchecked((int)(IOC_IN | IOC_VENDOR | 12));

			socket.IOControl(SIO_UDP_CONNRESET, new[] { Convert.ToByte(false) }, null);
		}
	}
}
