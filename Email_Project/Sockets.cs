using System.Net.Sockets;
using System.Text;
using System.Net;
using System;

namespace Email_Project
{
    public class Sockets
    {
        //Komut gönder ve sunucudan gelen cevabı al.
        public string KomutGonder(string sqlCommand)
        {
            byte[] bytes = new byte[1024];

            try
            {
                // Sunucuya bağlan.  
                // Host IP'sini al.  
                // Şimdilik localhost bağlanıldı. (127.0.0.1)
                IPHostEntry host = Dns.GetHostEntry("localhost");
                IPAddress ipAddress = host.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

                // TCP/IP soket oluştur.    
                Socket sender = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    //EndPoint'e bağlan.
                    sender.Connect(remoteEP);

                    Console.WriteLine("Socket connected to {0}",
                        sender.RemoteEndPoint.ToString());

                    //String'i byte'a çevir.  
                    byte[] byte_SqlCommand = Encoding.ASCII.GetBytes(sqlCommand);

                    //Gönder. 
                    int bytesSent = sender.Send(byte_SqlCommand);

                    //Cevabı al.  
                    int bytesRec = sender.Receive(bytes);
                    
                    //Soketi kapat.  
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();

                    //Cevabı dön.
                    return Encoding.ASCII.GetString(bytes, 0, bytesRec);

                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                    return null;
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                    return null;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                    return null;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

    }
}
