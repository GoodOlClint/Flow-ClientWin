using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Flow_ClientWin
{
    class Program
    {
        static void Main(string[] args)
        {
            udpClient.RecevedSignal += udpClient_RecevedSignal;
            udpClient.Start();
            Console.ReadLine();
            udpClient.Broadcast(new udpPacket(udpMessageType.Goodbye));
        }

        static void udpClient_RecevedSignal(udpPacket obj, IPEndPoint sender)
        {
            Console.WriteLine("{0} sent {1} at {2}: '{3}'", sender.Address, obj.Type, obj.TimeStamp, obj.Message );
        }
    }
}
