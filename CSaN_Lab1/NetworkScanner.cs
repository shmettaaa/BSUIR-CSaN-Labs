using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CSaN_Lab1
{
    public class NetworkScanner
    {
        public NetworkDevice GetLocalComputerInfo()
        {
            NetworkInterface[] allInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            NetworkInterface selectedInterface = null;
            UnicastIPAddressInformation selectedIPv4 = null;

            bool interfaceFound = false;

            for (int i = 0; i < allInterfaces.Length && !interfaceFound; i++)
            {
                NetworkInterface nic = allInterfaces[i];

                if (nic.OperationalStatus != OperationalStatus.Up) 
                    continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) 
                    continue;
                if (!nic.Supports(NetworkInterfaceComponent.IPv4)) 
                    continue;

                IPInterfaceProperties ipProps = nic.GetIPProperties();

                bool hasGateway = false;
                for (int g = 0; g < ipProps.GatewayAddresses.Count; g++)
                {
                    GatewayIPAddressInformation gw = ipProps.GatewayAddresses[g];
                    if (gw.Address.AddressFamily == AddressFamily.InterNetwork &&
                        gw.Address.ToString() != "0.0.0.0")
                    {
                        hasGateway = true;
                        break;
                    }
                }

                if (!hasGateway) continue;

                UnicastIPAddressInformationCollection addresses = ipProps.UnicastAddresses;
                for (int j = 0; j < addresses.Count && !interfaceFound; j++)
                {
                    UnicastIPAddressInformation addr = addresses[j];
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        selectedInterface = nic;
                        selectedIPv4 = addr;
                        interfaceFound = true;
                    }
                }
            }

            PhysicalAddress macAddr = selectedInterface.GetPhysicalAddress();
            byte[] macBytes = macAddr.GetAddressBytes();
            string macString = "";
            for (int i = 0; i < macBytes.Length; i++)
            {
                if (i > 0) macString += "-";
                macString += macBytes[i].ToString("X2");
            }

            string hostName = Dns.GetHostEntry("127.0.0.1").HostName;

            return new NetworkDevice
            {
                IpAddress = selectedIPv4.Address.ToString(),
                MacAddress = macString,
                HostName = hostName
            };
        }

        public async Task<List<NetworkDevice>> ScanAllLocalNetworksAsync()
        {
            var devices = new List<NetworkDevice>();
            var localComputer = GetLocalComputerInfo();
            string localIp = localComputer.IpAddress;

            var allIps = new List<IPAddress>();

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up)
                    continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;
                if (!nic.Supports(NetworkInterfaceComponent.IPv4))
                    continue;

                IPInterfaceProperties ipProps = nic.GetIPProperties();
                UnicastIPAddressInformation ipv4 = null;
                foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipv4 = addr;
                        break;
                    }
                }
                if (ipv4 == null) continue;

                IPAddress myIp = ipv4.Address;
                IPAddress mask = ipv4.IPv4Mask;

                IPAddress networkAddress = GetNetworkAddress(myIp, mask);
                IPAddress broadcastAddress = GetBroadcastAddress(myIp, mask);

                List<IPAddress> ipRange = GetIpRange(networkAddress, broadcastAddress);

                foreach (IPAddress ip in ipRange)
                {
                    if (ip.ToString() != localIp)
                        allIps.Add(ip);
                }
            }
            const int MAX_CONCURRENT = 30;
            var semaphore = new SemaphoreSlim(MAX_CONCURRENT, MAX_CONCURRENT);
            var tasks = new List<Task<NetworkDevice>>();

            foreach (IPAddress ip in allIps)
            {
                tasks.Add(CheckOneIpWithSemaphoreAsync(ip, semaphore));
            }

            NetworkDevice[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach (var device in results)
            {
                if (device != null)
                {
                    devices.Add(device);
                }
            }

            return devices;
        }

        private async Task<NetworkDevice> CheckOneIpWithSemaphoreAsync(IPAddress ip, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                return await CheckOneIpAsync(ip).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task<NetworkDevice> CheckOneIpAsync(IPAddress ip)
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = await ping.SendPingAsync(ip, 1000);
                    if (reply.Status != IPStatus.Success)
                    {
                        return null;
                    }
                }
                string mac = GetMacFromArp(ip);
                string name = GetHostNameFromIp(ip);
                return new NetworkDevice
                {
                    IpAddress = ip.ToString(),
                    MacAddress = mac,
                    HostName = name
                };
            }
            catch
            {
                return null;
            }
        }

        private string GetMacFromArp(IPAddress ip)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "arp";
                process.StartInfo.Arguments = "-a";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                string[] lines = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string trimmed = line.Trim();
                    if (trimmed.Length > 0)
                    {
                        string[] parts = trimmed.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            string ipStr = parts[0];
                            string mac = parts[1];
                            if (ipStr == ip.ToString() && mac != "(incomplete)")
                            {
                                return mac;
                            }
                        }
                    }
                }
                return "Не удалось получить";
            }
            catch
            {
                return "Ошибка ARP";
            }
        }

        private string GetHostNameFromIp(IPAddress ip)
        {
            try
            {
                IPHostEntry entry = Dns.GetHostEntry(ip);
                return entry.HostName;
            }
            catch
            {
                return "Неизвестно";
            }
        }

        private IPAddress GetNetworkAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipBytes = address.GetAddressBytes();
            byte[] maskBytes = subnetMask.GetAddressBytes();
            byte[] networkBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
            }
            return new IPAddress(networkBytes);
        }

        private IPAddress GetBroadcastAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipBytes = address.GetAddressBytes();
            byte[] maskBytes = subnetMask.GetAddressBytes();
            byte[] broadcastBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
            }
            return new IPAddress(broadcastBytes);
        }

        private List<IPAddress> GetIpRange(IPAddress start, IPAddress end)
        {
            List<IPAddress> range = new List<IPAddress>();
            byte[] startBytes = start.GetAddressBytes();
            byte[] endBytes = end.GetAddressBytes();
            uint startVal = (uint)((startBytes[0] << 24) | (startBytes[1] << 16) | (startBytes[2] << 8) | startBytes[3]);
            uint endVal = (uint)((endBytes[0] << 24) | (endBytes[1] << 16) | (endBytes[2] << 8) | endBytes[3]);
            for (uint i = startVal; i <= endVal; i++)
            {
                byte b0 = (byte)((i >> 24) & 0xFF);
                byte b1 = (byte)((i >> 16) & 0xFF);
                byte b2 = (byte)((i >> 8) & 0xFF);
                byte b3 = (byte)(i & 0xFF);
                range.Add(new IPAddress(new byte[] { b0, b1, b2, b3 }));
            }
            return range;
        }

    }
}