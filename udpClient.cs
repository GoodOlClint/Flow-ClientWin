using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace Flow_ClientWin
{
    [Serializable]
    public class udpPacket
    {
        public udpMessageType Type { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Message { get; set; }

        public udpPacket(udpMessageType type)
        { this.Type = type; }

        public udpPacket(udpMessageType type, string message)
        {
            this.Type = type;
            this.Message = message;
        }
    }

    public enum udpMessageType
    {
        Announce = 1,
        Greet = 2,
        Goodbye = 3,
        FindLeader = 4,
        LeaderReply = 5,
        Heartbeat = 6
    }

    public class udpPeer
    {
        public IPAddress Address { get; set; }
        public DateTime LastHeartBeat { get; set; }
        public string ClientID { get; set; }
    }

    public static class udpClient
    {
        private static List<udpPeer> peers;
        private static List<IPAddress> myIPs;
        private static Timer heartbeatTimer;
        public static void Start()
        {
            myIPs = Dns.GetHostAddresses(Dns.GetHostName()).ToList();
            peers = new List<udpPeer>();
            Listen();
            Broadcast(new udpPacket(udpMessageType.Announce, Properties.Settings.Default.ClientID));

            RecevedSignal += udpClient_RecevedSignal;
            FindLeader(0);

            //AutoResetEvent autoEvent = new AutoResetEvent(false);
            int timeout = Properties.Settings.Default.Heartbeat * 1000;
            heartbeatTimer = new Timer(sendHeartbeat, null, timeout,timeout);
        }
        private static void sendHeartbeat(object stateInfo)
        {
            Broadcast(new udpPacket(udpMessageType.Heartbeat, Properties.Settings.Default.ClientID));
            int timeout = Properties.Settings.Default.PeerTimeout;
            peers.RemoveAll(p => DateTime.UtcNow.Subtract(p.LastHeartBeat).TotalSeconds > timeout);
        }

        private static IPEndPoint udpLeader;
        private static bool isLeader = false;
        static void udpClient_RecevedSignal(udpPacket message, IPEndPoint Sender)
        {
            if (myIPs.Contains(Sender.Address))
            { return; }

            switch (message.Type)
            {
                case udpMessageType.LeaderReply:
                    udpLeader = Sender;
                    break;
                case udpMessageType.FindLeader:
                    if (isLeader)
                        Broadcast(new udpPacket(udpMessageType.LeaderReply, Properties.Settings.Default.ClientID));
                    break;
                case udpMessageType.Announce:
                case udpMessageType.Greet:
                case udpMessageType.Heartbeat:
                    udpPeer peer = (from p in peers
                                    where p.Address.Equals(Sender.Address)
                                    where p.ClientID == message.Message
                                    select p).FirstOrDefault();
                    if (peer == null)
                    {
                        udpPeer p = new udpPeer();
                        p.Address = Sender.Address;
                        p.ClientID = message.Message;
                        p.LastHeartBeat = DateTime.UtcNow;
                        if (message.Type == udpMessageType.Announce)
                        { Broadcast(new udpPacket(udpMessageType.Greet, Properties.Settings.Default.ClientID)); }
                        peers.Add(p);
                    }
                    else
                    { peer.LastHeartBeat = DateTime.UtcNow; }
                    Console.Title = "Peer Count: " + peers.Count;
                    break;
            }
        }

        private static void FindLeader(int attempt)
        {
            if (attempt < 5)
            {
                Broadcast(new udpPacket(udpMessageType.FindLeader));
                Thread.Sleep(3000);
                if (udpLeader == null)
                { FindLeader(++attempt); }
            }
            else
            {
                Broadcast(new udpPacket(udpMessageType.LeaderReply));
                isLeader = true;
                udpLeader = null;
            }
        }

        public delegate void ReceivedSignalDelegate(udpPacket Message, IPEndPoint Sender);
        public static event ReceivedSignalDelegate RecevedSignal;

        private static readonly UdpClient udp = new UdpClient(Properties.Settings.Default.Port);

        public static void Listen()
        { udp.BeginReceive(Receive, new object()); }

        public static void Broadcast(udpPacket Message)
        {
            Message.TimeStamp = DateTime.UtcNow;
            byte[] bytes = ObjectToByteArray(Message);
            using (UdpClient client = new UdpClient())
            {
                IPEndPoint ip = new IPEndPoint(IPAddress.Broadcast, Properties.Settings.Default.Port);
                client.Send(bytes, bytes.Length, ip);
                client.Close();
            }
        }

        private static void Receive(IAsyncResult ar)
        {

            IPEndPoint ip = new IPEndPoint(IPAddress.Any, 15000);
            byte[] bytes = udp.EndReceive(ar, ref ip);
            udpPacket message = (udpPacket)ByteArrayToObject(bytes);
            Listen();
            if (RecevedSignal != null)
            { RecevedSignal(message, ip); }
        }

        // Convert an object to a byte array
        private static byte[] ObjectToByteArray(Object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        private static Object ByteArrayToObject(byte[] arrBytes)
        {
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(arrBytes, 0, arrBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = binForm.Deserialize(memStream);
                return obj;
            }
        }
    }
}
