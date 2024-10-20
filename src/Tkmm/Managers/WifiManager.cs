using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Tkmm.Managers;

public class Connman
{
    public struct WifiNetworkInfo
    {
        public string Ssid { get; set; }
        public string NetId { get; set; }
        public bool Connected { get; set; }
        public bool SavedPassword { get; set; }
        public string Passphrase { get; set; }
    }

    private static string macAddress;
    private static string ipAddress;
    private static string netmask;
    private static string gateway;

    public static event Action NetworkDetailsChanged;

    public static string MacAddress
    {
        get => macAddress;
        private set
        {
            if (macAddress != value)
            {
                macAddress = value;
                NetworkDetailsChanged?.Invoke();
            }
        }
    }

    public static string IpAddress
    {
        get => ipAddress;
        private set
        {
            if (ipAddress != value)
            {
                ipAddress = value;
                NetworkDetailsChanged?.Invoke();
            }
        }
    }

    public static string Netmask
    {
        get => netmask;
        private set
        {
            if (netmask != value)
            {
                netmask = value;
                NetworkDetailsChanged?.Invoke();
            }
        }
    }

    public static string Gateway
    {
        get => gateway;
        private set
        {
            if (gateway != value)
            {
                gateway = value;
                NetworkDetailsChanged?.Invoke();
            }
        }
    }

    public static void RetrieveMacAddress()
    {
        using (var output = ExecuteCommand("ip link show wlan0"))
        {
            string result = output.ReadToEnd();
            var match = Regex.Match(result, @"link/ether (\S+)");
            MacAddress = match.Success ? match.Groups[1].Value.ToUpper() : string.Empty;
        }
    }

    public class ConnmanT
    {
        public WifiNetworkScan Scan;
        public string Command = new string(new char[300]);
        public bool ConnmanctlWidgetsSupported;
    }

    public class WifiNetworkScan
    {
        public WifiNetworkInfo[] NetList;
        public DateTime ScanTime;
    }

    public static ConnmanT ConnmanctlInit()
    {
        var connman = new ConnmanT
        {
            Scan = new WifiNetworkScan
            {
                NetList = Array.Empty<WifiNetworkInfo>()
            }
        };
        return connman;
    }

    private static bool IsDefault(WifiNetworkInfo netinfo)
    {
        return string.IsNullOrEmpty(netinfo.Ssid) && string.IsNullOrEmpty(netinfo.NetId) && !netinfo.Connected && !netinfo.SavedPassword && string.IsNullOrEmpty(netinfo.Passphrase);
    }

    public static void ConnmanctlFree(ConnmanT connman)
    {
        if (connman.Scan.NetList != null)
            connman.Scan.NetList = null;
    }

    public static bool ConnmanctlStart(ConnmanT connman)
    {
        return true;
    }

    public static void ConnmanctlStop(ConnmanT connman)
    {
    }

    public static void ConnmanctlRefreshServices(ConnmanT connman)
    {
        if (connman == null || connman.Scan == null)
        {
            Trace.WriteLine("Connman or Scan is null.");
            return;
        }
        RetrieveMacAddress();

        using (var servFile = ExecuteCommand("connmanctl services"))
        {
            if (connman.Scan.NetList != null)
                connman.Scan.NetList = null;

            string line;
            while ((line = servFile.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("wifi_"))
                    continue;

                var entry = new WifiNetworkInfo();
                var list = line.Substring(4).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                entry.Ssid = string.Join(" ", list, 0, list.Length - 1);
                entry.NetId = list[list.Length - 1];
                entry.Connected = line[2] == 'R' || line[2] == 'O';
                entry.SavedPassword = File.Exists(Path.Combine(CONNMAN_DIR, entry.NetId, "settings"));

                if (entry.Connected)
                {
                    GetNetworkDetails(entry.NetId);
                }

                if (entry.NetId.StartsWith("wifi_"))
                {
                    var netList = new List<WifiNetworkInfo>(connman.Scan.NetList ?? Array.Empty<WifiNetworkInfo>());
                    netList.Add(entry);
                    connman.Scan.NetList = netList.ToArray();
                }
            }
        }
    }

    private static void GetNetworkDetails(string netId)
    {
        using (var detailFile = ExecuteCommand($"connmanctl services {netId}"))
        {
            string detailLine;
            while ((detailLine = detailFile.ReadLine()) != null)
            {
                if (detailLine.Contains("Ethernet ="))
                {
                    MacAddress = ExtractValue(detailLine, "Address");
                }
                else if (detailLine.Contains("IPv4 ="))
                {
                    IpAddress = ExtractValue(detailLine, "Address");
                    Netmask = ExtractValue(detailLine, "Netmask");
                    Gateway = ExtractValue(detailLine, "Gateway");
                }
            }
        }
    }

    private static string ExtractValue(string line, string key)
    {
        var startIndex = line.IndexOf(key + "=");
        if (startIndex == -1) return "N/A";

        startIndex += key.Length + 1;
        var endIndex = line.IndexOf(',', startIndex);
        if (endIndex == -1) endIndex = line.IndexOf(']', startIndex);

        var extractedValue = endIndex == -1 ? string.Empty : line.Substring(startIndex, endIndex - startIndex).Trim();
        return string.IsNullOrEmpty(extractedValue) ? "N/A" : extractedValue;
    }

    public static void ConnmanctlScan(ConnmanT connman)
    {
        ExecuteCommand("connmanctl scan wifi");
        connman.Scan.ScanTime = DateTime.Now;
        ConnmanctlRefreshServices(connman);
    }

    public static WifiNetworkScan ConnmanctlGetSsids(ConnmanT connman)
    {
        return connman.Scan;
    }

    public static bool ConnmanctlSsidIsOnline(ConnmanT connman, int i)
    {
        return connman.Scan.NetList != null && i < connman.Scan.NetList.Length && connman.Scan.NetList[i].Connected;
    }

    public static bool ConnmanctlConnectionInfo(ConnmanT connman, out WifiNetworkInfo netinfo)
    {
        netinfo = new WifiNetworkInfo();
        if (connman.Scan.NetList == null)
            return false;

        foreach (var net in connman.Scan.NetList)
        {
            if (net.Connected)
            {
                netinfo = net;
                return true;
            }
        }

        return false;
    }

    public static bool ConnmanctlConnectSsid(ConnmanT connman, WifiNetworkInfo netinfo)
    {
        if (connman == null)
        {
            Trace.WriteLine("Connman is null.");
            return false;
        }

        if (IsDefault(netinfo))
        {
            Trace.WriteLine("Network info is in its default state.");
            return false;
        }

        var netid = netinfo.NetId;
        if (string.IsNullOrEmpty(netid))
        {
            Trace.WriteLine("NetId is null or empty.");
            return false;
        }

        var settingsDir = Path.Combine(CONNMAN_DIR, netid);
        var settingsPath = Path.Combine(settingsDir, "settings");

        try
        {
            Directory.CreateDirectory(settingsDir);

            if (!netinfo.SavedPassword)
            {
                using (var settingsFile = new StreamWriter(settingsPath))
                {
                    settingsFile.WriteLine($"[{netid}]");
                    settingsFile.WriteLine($"Name={netinfo.Ssid}");
                    settingsFile.WriteLine("SSID=" + (BitConverter.ToString(Encoding.UTF8.GetBytes(netinfo.Ssid)).Replace("-", "")).ToLower());
                    settingsFile.WriteLine("Favorite=true");
                    settingsFile.WriteLine("AutoConnect=true");
                    settingsFile.WriteLine($"Passphrase={netinfo.Passphrase}");
                    settingsFile.WriteLine("IPv4.method=dhcp");
                }

                ExecuteCommand("systemctl restart connman.service");
            }
            else if (!File.Exists(settingsPath))
            {
                ExecuteCommand("systemctl restart connman.service");
                return false;
            }

            connman.Command = $"connmanctl connect {netinfo.NetId}";
            ExecuteCommand(connman.Command);
            ConnmanctlRefreshServices(connman);

            if (connman.Scan?.NetList == null)
            {
                return false;
            }

            for (int i = 0; i < connman.Scan.NetList.Length; i++)
            {
                var net = connman.Scan.NetList[i];
                if (net.NetId == netid)
                {
                    if (!net.Connected)
                    {
                        net.SavedPassword = false;
                        File.Delete(settingsPath);
                        connman.Scan.NetList[i] = net;
                    }
                    return net.Connected;
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error in ConnmanctlConnectSsid: {ex.Message}");
            return false;
        }

        return false;
    }

    public static bool ConnmanctlDisconnectSsid(ConnmanT connman, WifiNetworkInfo netinfo)
    {
        connman.Command = $"connmanctl disconnect {netinfo.NetId} 2>&1";
        ExecuteCommand(connman.Command);
        ConnmanctlRefreshServices(connman);
        return true;
    }

    public static bool ConnmanctlForgetSsid(ConnmanT connman, WifiNetworkInfo netinfo)
    {
        var netid = netinfo.NetId;
        var settingsDir = Path.Combine(CONNMAN_DIR, netid);

        if (Directory.Exists(settingsDir))
        {
            bool isConnectedNetwork = netinfo.Connected;

            if (isConnectedNetwork)
            {
                ExecuteCommand("systemctl stop connman.service");
                Directory.Delete(settingsDir, true);
                ExecuteCommand("systemctl start connman.service");
            } else {
                Directory.Delete(settingsDir, true);
            }
            
            Trace.WriteLine($"Settings for SSID {netinfo.Ssid} have been removed.");
            ConnmanctlRefreshServices(connman);
            return true;
        }
        else
        {
            Trace.WriteLine($"No settings found for SSID {netinfo.Ssid}.");
            return false;
        }
    }

    public static void ConnmanctlGetConnectedSsid(ConnmanT connman, StringBuilder ssid, int bufferSize)
    {
        if (bufferSize < 1) return;

        connman.Command = "connmanctl services | grep wifi_ | grep \"^..\\(R\\|O\\)\" | awk '{for (i=2; i<NF; i++) printf $i \" \"; print \"\"}'";
        using (var commandFile = ExecuteCommand(connman.Command))
        {
            var line = commandFile.ReadLine()?.TrimEnd('\n');
            if (!string.IsNullOrEmpty(line))
            {
                ssid.Append(line);
            }
        }
    }

    public static void ConnmanctlGetConnectedServiceName(ConnmanT connman, StringBuilder serviceName, int bufferSize)
    {
        if (bufferSize < 1) return;

        connman.Command = "connmanctl services | grep wifi_ | grep \"^..\\(R\\|O\\)\" | awk '{print $NF}'";
        using (var commandFile = ExecuteCommand(connman.Command))
        {
            var line = commandFile.ReadLine()?.TrimEnd('\n');
            if (!string.IsNullOrEmpty(line))
            {
                serviceName.Clear().Append(line);
            }
        }
    }

    private static StreamReader ExecuteCommand(string command)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes("")));
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        return process.StandardOutput;
    }

    private const string CONNMAN_DIR = "/storage/.cache/connman/";
}