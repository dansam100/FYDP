using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SampleServer
{
    public class Server
    {
        private ClientListener clientListener;

        public Server()
        {
            this.clientListener = new ClientListener();
            this.clientListener.DataReceived += new DataReceivedHandler(clientListener_DataReceived);
        }


        public void Start()
        {
            bool exit = false;

            while (!exit)
            {
                string input = Console.ReadLine();
                switch (input)
                {
                    case "stop":
                        clientListener.Stop();
                        exit = true;
                        break;
                    default:
                        break;
                }
            }
        }


        void clientListener_DataReceived(string response)
        {
            Console.WriteLine("Received: " + response);
        }
    }
}
