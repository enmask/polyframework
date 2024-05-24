using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;


// Usage:
// Draw data:
// 1. Have the server call SendDrawData() in Draw() once per frame to send draw data to the network.
// 2. Have each client call SetReceivedDrawData() to store new draw data as it's received from the network.
// 3. Have each client call GetReceivedDrawData() in Draw() once per frame to get the latest draw data.

// Client input:
// 1. Have each client call SendClientInput() in Update() to send its player input to the network.
// 2. Have the server call AddReceivedClientInput() to store new input data as it's received from the network.
// 3. Have the server call GetReceivedClientsInput() to get all player input data that has been received from the network
//    since the last call to GetClientsInput().
//    Note the 's' in GetReceivedClientsInput(), which indicates that there can be input from multiple clients.

namespace PolyNetworking
{

    public class Networking
    {
        private static bool isServer;
        private static bool networkIsAvailable = false;

        // TODO: Don't hard code IPv4 addess and subnet mask. Instead, get them from the system or ask the user for them.
        // Below values were found by:
        // 1. Connecting an ethernet cable between two computers.
        // 2. Running ipconfig in a command prompt, and in the output looking at the IPv4 address of the ethernet adapter.
        // 3. Looking at the IPv4 address of the ethernet adapter.
        private const string IPv4_ADDRESS = "169.254.244.29";
        private const string SUBNET_MASK = "255.255.0.0";
        private const int DRAWDATA_PORT = 11001;
        private const int CLIENTSINPUT_PORT = 11002;
        private const int MAX_DRAWDATA_QUEUE_SIZE = 3;

        private static IPAddress BROADCAST_ADDRESS = CalculateBroadcastAddress(IPAddress.Parse(IPv4_ADDRESS), IPAddress.Parse(SUBNET_MASK));
        //private static string drawData = "";
        private static ConcurrentQueue<string> drawData = new ConcurrentQueue<string>();
        private static ConcurrentQueue<string> clientsInput = new ConcurrentQueue<string>();
        private static Socket socket;
        private static IPEndPoint ep;

        public static void StartNetworking(bool isServer)
        {
            Networking.isServer = isServer;

            CheckNetworkAvailability();

            if (!networkIsAvailable)
            {
                Debug.WriteLine("Nätverk är inte tillgängligt.");
                return;
            }

            var listenThread = new Thread(Listener);
            listenThread.Name = "NetworkListener";
            // Make the listener a background thread, so it will die when the main thread dies.
            listenThread.IsBackground = true;
            listenThread.Start();

            socket = new Socket(AddressFamily.InterNetwork,
                                SocketType.Dgram,
                                ProtocolType.Udp);

            socket.EnableBroadcast = true;
            if (isServer)
                ep = new IPEndPoint(BROADCAST_ADDRESS, DRAWDATA_PORT);
            else
                ep = new IPEndPoint(BROADCAST_ADDRESS, CLIENTSINPUT_PORT);
        }

        public static bool IsNetworkAvailable()
        {
            return networkIsAvailable;
        }

        private static void CheckNetworkAvailability()
        {
            networkIsAvailable = false;  // Start by assuming the network is not available.

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Ensure the network interface is 'up' and is of type Ethernet.
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    bool isVirtual = false;  // Flag to detect if the interface is virtual.
                    string description = ni.Description.ToLower();

                    // List of known descriptions for virtual adapters.
                    string[] virtualIdentifiers = { "virtual", "hyper-v", "vbox", "virtualbox", "vmnet", "vmware", "pseudo" };

                    foreach (var identifier in virtualIdentifiers)
                    {
                        if (description.Contains(identifier))
                        {
                            isVirtual = true;
                            break;
                        }
                    }

                    if (!isVirtual)  // If it's not a virtual adapter, proceed.
                    {
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                networkIsAvailable = true;  // Network is available through a physical Ethernet connection.
                                break;
                            }
                        }
                    }
                }
                if (networkIsAvailable)  // Break out of the loop once a valid network is found.
                {
                    break;
                }
            }
            Debug.WriteLine($"Network availability check: {networkIsAvailable}");
        }

        public static void SendDrawData(string dData)
        {
            if (!networkIsAvailable)
            {
                //Debug.WriteLine("Försöker sända data när nätverket inte är tillgängligt.");
                return;
            }

            byte[] sendbuf = Encoding.UTF8.GetBytes(dData);

            Core.Tools.Log("\n\n: Time: " + System.DateTime.Now.ToString("HHmmssfff") +
                           "  SendDrawData: Server sends dData to network: <" + dData + ">");

            socket.SendTo(sendbuf, ep);
        }

        public static void SendClientInput(string cInput)
        {
            byte[] sendbuf = Encoding.UTF8.GetBytes(cInput);

            Core.Tools.Log("\n\n: Time: " + System.DateTime.Now.ToString("HHmmssfff") +
                           "  SendClientInput: Client sends cInput to network: <" + cInput + ">");

            socket.SendTo(sendbuf, ep);
        }

        /* Obsolete, allow any queue size
        public static void SetReceivedDrawData(string dData)
        {
            drawData.Enqueue(dData);

            Core.Tools.Log("\n\n: Time: " + System.DateTime.Now.ToString("HHmmssfff") +
                              "  SetReceivedDrawData: Client added dData to queue: <" + dData + ">, Count=" + drawData.Count);
        }
        */

        /* New, allow only MAX_DRAWDATA_QUEUE_SIZE elements in the queue */
        //private static ConcurrentQueue<DrawData> drawDataQueue = new ConcurrentQueue<DrawData>();

        public static void SetReceivedDrawData(string dData) {
            drawData.Enqueue(dData);
            // Säkerställ att köns storlek inte överstiger N
            while (drawData.Count > MAX_DRAWDATA_QUEUE_SIZE) {
                drawData.TryDequeue(out _);
            }
        }

        /**/



        public static void AddReceivedClientInput(string cInput)
        {
            clientsInput.Enqueue(cInput);

            Core.Tools.Log("\n\n: Time: " + System.DateTime.Now.ToString("HHmmssfff") +
                                          "  AddReceivedClientInput: Server added cInput to queue: <" + cInput + ">, Count=" + clientsInput.Count);
        }

        /* Obsolete, picks the newest and throws away the rest
        public static string GetReceivedDrawData()
        {
            string latestData = null;
            string data = null;

            // Empties the queue until the last element
            while (drawData.TryDequeue(out data))
            {
                Core.Tools.Log("\n\n: Time: " + System.DateTime.Now.ToString("HHmmssfff") +
                               "  GetReceivedDrawData: Client gets from queue: <" + data + ">");
                latestData = data;
            }

            return latestData;
        }
        */

        /* New version, just picks the oldest */
        public static string GetReceivedDrawData() {
            if (drawData.TryDequeue(out string oldestData))
            {
                Core.Tools.Log("\n\n: Time: " + System.DateTime.Now.ToString("HHmmssfff") +
                               "  GetReceivedDrawData: Client gets from queue: <" + oldestData + ">");
                return oldestData;
            }
            return null;
        }
        /**/


        public static string GetReceivedClientsInput()
        {
            string earliestData = null;
            clientsInput.TryDequeue(out earliestData);

            Core.Tools.Log("\n\n: Time: " + System.DateTime.Now.ToString("HHmmssfff") +
                           "  GetReceivedClientsInput: Server gets from queue: <" + earliestData + ">");

            return earliestData;
        }

        public static string SerializeMessagePrefix(string senderId)
        {
            string timestamp = System.DateTime.Now.ToString("HHmmssfff");
            return timestamp + ";" + senderId;
        }

        public static string JoinMessageSections(string sections1, string sections2)
        {
            return sections1 + "#" + sections2;
        }

        /*
        public static string JoinMessageValues(string values1, string values2)
        {
            return values1 + ";" + values2;
        }
        */

        public static string JoinMessageValues(params string[] values)
        {
            return string.Join(";", values);
        }

        static void Listener()
        {
            UdpClient listener;
            if (isServer)
                listener = new UdpClient(CLIENTSINPUT_PORT);
            else
                listener = new UdpClient(DRAWDATA_PORT);

            try
            {
                while (true)
                {
                    IPEndPoint groupEP;
                    if (isServer)
                        groupEP = new IPEndPoint(IPAddress.Any, CLIENTSINPUT_PORT);
                    else
                        groupEP = new IPEndPoint(IPAddress.Any, DRAWDATA_PORT);
                    byte[] bytes = listener.Receive(ref groupEP);

                    if (isServer)
                    {
                        string msg = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                        Core.Tools.Log("\n\nTime: " + System.DateTime.Now.ToString("HHmmssfff") +
                                       "  Listener: Server receives: <" + msg + ">");
                        AddReceivedClientInput(msg);
                    }
                    else
                    {
                        string msg = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                        Core.Tools.Log("\n\nTime: " + System.DateTime.Now.ToString("HHmmssfff") +
                                       "  Listener: Client receives: <" + msg + ">");
                        SetReceivedDrawData(msg);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static IPAddress CalculateBroadcastAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();
            byte[] broadcastAddress = new byte[ipAdressBytes.Length];

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                // Perform a bitwise OR operation on each byte
                broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }

            return new IPAddress(broadcastAddress);
        }

    } // End of class Networking
} // End of namespace PolyNetworking