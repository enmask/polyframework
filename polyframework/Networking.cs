using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;


// Usage:
// Draw data:
// 1. Have the server call SendDrawData() in Draw() once per frame to send draw data to the network.
// 2. Have each client call SetReceivedDrawData() to store new draw data as it's received from the network.
// 3. Have each client call GetDrawData() in Draw() once per frame to get the latest draw data.

// Client input:
// 1. Have each client call SendClientInput() in Update() to send its player input to the network.
// 2. Have each client call AddReceivedClientInput() to store new input data as it's received from the network.
// 3. Have each client call GetClientsInput() to get all player input data that has been received from the network
//    since the last call to GetClientsInput().
//    Note the 's' in GetClientsInput(), which indicates that there can be input from multiple clients.

namespace PolyNetworking
{

    public class Networking
    {
        private static bool isServer;

        // TODO: Don't hard code IPv4 addess and subnet mask. Instead, get them from the system or ask the user for them.
        // Below values were found by:
        // 1. Connecting an ethernet cable between two computers.
        // 2. Running ipconfig in a command prompt, and in the output looking at the IPv4 address of the ethernet adapter.
        // 3. Looking at the IPv4 address of the ethernet adapter.
        private const string IPv4_ADDRESS = "169.254.244.29";
        private const string SUBNET_MASK = "255.255.0.0";
        private const int DRAWDATA_PORT = 11001;
        private const int CLIENTSINPUT_PORT = 11002;

        private static IPAddress BROADCAST_ADDRESS = CalculateBroadcastAddress(IPAddress.Parse(IPv4_ADDRESS), IPAddress.Parse(SUBNET_MASK));
        private static string drawData = "";
        private static string clientsInput = "";
        private static Socket socket;
        private static IPEndPoint ep;

        public static void StartNetworking(bool isServer)
        {
            Networking.isServer = isServer;
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

        public static void SendDrawData(string dData)
        {
            byte[] sendbuf = Encoding.UTF8.GetBytes(dData);
            socket.SendTo(sendbuf, ep);
        }

        public static void SendClientInput(string dData)
        {
            byte[] sendbuf = Encoding.UTF8.GetBytes(dData);
            socket.SendTo(sendbuf, ep);
        }

        public static void SetReceivedDrawData(string dData)
        {
            drawData = dData;
        }
        
        public static void AddReceivedClientInput(string dData)
        {
            drawData += dData;
        }

        public static string GetReceivedDrawData()
        {
            string s = drawData;
            // The drawData is now consumed, and should not be gotten again.
            drawData = "";
            return s;
        }

        public static string GetReceivedClientsInput()
        {
            Debug.WriteLine("GetReceivedClientsInput gets (and clears): <" + clientsInput + ">");

            string s = clientsInput;
            // The clientsInput is now consumed, and should not be gotten again.
            clientsInput = "";
            return s;
        }

        static void Listener()
        {
            Debug.WriteLine("Listener called");
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
                        AddReceivedClientInput(Encoding.UTF8.GetString(bytes, 0, bytes.Length));
                    else
                        SetReceivedDrawData(Encoding.UTF8.GetString(bytes, 0, bytes.Length));
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