using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace SampleServer
{
    public delegate void DataReceivedHandler(string response);

    public class ClientListener
    {
        private TcpListener tcpListener;
        private Thread listenThread;
        private ThreadFactory threadFactory;

        private ASCIIEncoding ascii;
        public event DataReceivedHandler DataReceived;

        protected const string serverIP = "127.0.0.1";
        protected const int port = 9191;

        public ClientListener()
        {
            ascii = new ASCIIEncoding();
            
            // Set the TcpListener on port.
            IPAddress IPAddress = IPAddress.Parse(serverIP);

            threadFactory = new ThreadFactory(5);
            this.tcpListener = new TcpListener(IPAddress, port);
            tcpListener.Start();

            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.IsBackground = true;
            this.listenThread.Name = "ListenerThread";
            this.listenThread.Start();
        }

        ~ClientListener()
        {
            tcpListener.Stop();
        }


        private void ListenForClients()
        {
            try
            {
                while (true)
                {
                    //blocks until a client has connected to the server
                    TcpClient client = this.tcpListener.AcceptTcpClient();
                    Console.WriteLine("Client connected!\nServicing...");

                    //create a thread to handle communication with connected client
                    Thread clientThread = threadFactory.CreateThread(Start);
                    clientThread.Start(client);
                }
            }
            catch (Exception e) { Console.WriteLine("Stop listening\n {0}", e); };
        }

        /// <summary>
        /// Stop a client thread once the client is lost.
        /// </summary>
        /// <param name="clientname"></param>
        public void Stop(string clientname)
        {
            threadFactory.KillThread(clientname);
        }

        public void Stop()
        {
            this.tcpListener.Stop();
        }

        /// <summary>
        /// Handle client requests and receives
        /// </summary>
        private void Start(object clientobj)
        {
            //receives and sends messages
            try
            {
                TcpClient tcpClient = (TcpClient)clientobj;
                NetworkStream stream = tcpClient.GetStream();
                byte[] bytes;
                string data;
                while(true){
                    // Get a stream object for reading and writing
                    if (tcpClient.Connected && stream.DataAvailable)
                    {
                        bytes = new byte[64];

                        // Loop to receive all the data sent by the client.
                        stream.Read(bytes, 0, bytes.Length);

                        // Translate data bytes to a ASCII string.
                        ///data = System.Text.Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                        //data = data.TrimEnd('\0');
                        data = System.BitConverter.ToDouble(bytes, 0).ToString();
                        
                        //bubble the response to the application level
                        if(DataReceived != null)
                            this.DataReceived(data);
                    }
                }
            }
            catch (SocketException e){
                Console.WriteLine("SocketException: {0}", e);
            }
            catch (Exception e){
                Console.WriteLine(e);
            }
        }
    }
}
