using UnityEngine;
using System.Collections;

using System.Net.Sockets;
using System.Net;
using System;
using System.Threading;

//This script is responsible for accepting data from the hall effect sensor and potentiometer and applying it to the vehicle.
// Notes:
// - The potentiometer and hall effect sensor are "clients"
// - client IDs are stored in enum tcpClientIndices
// - each client has a TcpConnection struct associated with it in the tcpConnections array
// - each client has a thread associated with it in the tcpThreads array
// - the threads are used only for listening for clients (sensors) during startup. They quit after connection is received. When accepting a client,
//   the thread finishes initialization of the client's TcpConnection struct (TcpClient and NetworkStream objects).
// - receiving actual data from the client is done by Service(), which is called on each Update() call.

public class Movement : MonoBehaviour {

	Quaternion originalRotation;
	
	//Forward/backward movement
	private const float maxSpeed = 5.0f;
	private float currSpeed = 0.0f;
	
	//Handlebar/FrontWheel object and rotation
	private GameObject handlebarObj, frontRightWheelObj, frontLeftWheelObj;
	private WheelCollider frontRightWheelColl, frontLeftWheelColl, rearRightWheelColl, rearLeftWheelColl;
	private const float maxRotateSpeed = 500.0f;
    private const float torquePerSpeedLevel = 50.0f;
	private float currRot = 0.0f;
	
	//TCP Comm (potentiometer, hall effect sensor)
	enum tcpClientIndices { Potentiometer = 0, HallEffect = 1 } //When adding new clients, don't forget to add port in Start()
	protected const string serverIP = "127.0.0.1";
	protected const int potPort = 9191;
	protected const int hallEffPort = 9192;
    protected int[] tcpPorts;
	
    struct TcpConnection
    {
        public int clientID;
        public TcpClient client;
        public TcpListener listener;
        public NetworkStream stream;
    }
    TcpConnection[] tcpConnections;

    //Threads
    Thread[] tcpThreads;

    ~Movement()
    {
        Debug.Log("Movement destructor called.");
        
        foreach (tcpClientIndices client in Enum.GetValues(typeof(tcpClientIndices))) {
            if(tcpConnections[(int)client].stream != null) {
                tcpConnections[(int)client].stream.Close();
            }

            if(tcpConnections[(int)client].client != null) {
                tcpConnections[(int)client].client.Close();
            }

            if(tcpConnections[(int)client].listener != null) {
                tcpConnections[(int)client].listener.Stop();
            }
        }
    }

	// Use this for initialization
	void Start () {
		originalRotation = transform.localRotation;

        //
        //Get needed object references		
        //
		handlebarObj = GameObject.Find("Handlebar");
		frontRightWheelObj = GameObject.Find("FrontRightWheel");
		frontLeftWheelObj = GameObject.Find("FrontLeftWheel");
		frontRightWheelColl = GameObject.Find ("FrontRightWheelCollider").GetComponent<WheelCollider>();
		frontLeftWheelColl = GameObject.Find ("FrontLeftWheelCollider").GetComponent<WheelCollider>();

        //
        //Setup TCP Comm and Initialize arrays
        //
		IPAddress ServerIPAddress = IPAddress.Parse(serverIP);
        int numTcpClients = Enum.GetValues(typeof(tcpClientIndices)).Length;

        //Tcp Comm Objects arrays
        tcpPorts = new int[numTcpClients];
        tcpPorts[(int)tcpClientIndices.Potentiometer] = 9191;
        tcpPorts[(int)tcpClientIndices.HallEffect] = 9192;

        tcpConnections = new TcpConnection[numTcpClients];

        //Threading arrays
        tcpThreads = new Thread[numTcpClients];
        ThreadStart[] threadStarters = new ThreadStart[numTcpClients];

        //Initialize TCP structures and start threads
        foreach (tcpClientIndices client in Enum.GetValues(typeof(tcpClientIndices))) {
            //tcp connection struct
            tcpConnections[(int)client].clientID = (int)client;
            tcpConnections[(int)client].client = null;
            tcpConnections[(int)client].listener = new TcpListener(ServerIPAddress, tcpPorts[(int)client]);
            tcpConnections[(int)client].stream = null;

            //Thread setup
            tcpThreads[(int)client] = new Thread (tcpCommThreadEntry);
            tcpThreads[(int)client].IsBackground = true;
            tcpThreads[(int)client].Start((int)client);
        }
	}

    private void tcpCommThreadEntry (object data)
    {
        int clientID= (int)data;
        Debug.Log("tcpCommThreadEntry... Client = " + clientID);
		try
		{
            try {
                tcpConnections[clientID].listener.Start();
            }
            catch (System.Exception e) {
                Debug.Log("Failed to start tcp listener for client " + clientID + " error: " + e.ToString()); 
            }
			//blocks until a client has connected to the server
			tcpConnections[clientID].client = tcpConnections[clientID].listener.AcceptTcpClient();
			Debug.Log("Client found");
			
			tcpConnections[clientID].stream = tcpConnections[clientID].client.GetStream();
        }
        catch (Exception e) {
            Debug.Log("Stopped listening, error: " + e.ToString());
            return;
        }

		Debug.Log("Client " + clientID + " found");
    }
	
	/// <summary>
	/// Handle client requests and receives
	/// </summary>
	private string Service(ref TcpConnection connection)
	{
		//receives and sends messages
		if (connection.client != null)
		{
			try
			{
				// Get a stream object for reading and writing
				if (connection.client.Connected && connection.stream.DataAvailable)
				{
					int length;
					// Buffer for reading data
					Byte[] bytes = new Byte[64];
					string data = null;

					// Loop to receive all the data sent by the client.
					length = connection.stream.Read(bytes, 0, bytes.Length); //TODO: Find out exactly how many bytes to read per message

					// Translate data bytes to a ASCII string.
					data = System.Text.Encoding.ASCII.GetString(bytes, 0, bytes.Length);
					data = data.TrimEnd('\0');
					return data;
				}
			}
			catch (System.Exception e)
			{
				Debug.Log("Failed in Service() client " + connection.clientID + ", err: " + e.ToString());
			}
		}
		return "-500";
	}
	
	void updateFromPotentiometer()
	{
		float rotX = 0.0f;
		string rotXStr = Service(ref tcpConnections[(int)tcpClientIndices.Potentiometer]);
        Debug.Log("rotXStr = " + rotXStr);
		
		try {
			rotX = float.Parse(rotXStr);
		}
		catch {
			Debug.Log("Got invalid potentiometer value:  " + rotXStr);
		}	
		
		if(rotX == -500.0f){
			return;
		}
		
		//Max user can turn handle bar is +/-90 degrees
		if (rotX < -90.0f) rotX = -90.0f;
		if (rotX > 90.0f) rotX = 90.0f;
		currRot = rotX;
	}
	
	void updateFromHallEffect()
	{
		float speed = 0.0f;
		string speedStr = Service(ref tcpConnections[(int)tcpClientIndices.HallEffect]);
        Debug.Log("speedStr = " + speedStr);
		
		try {
			speed = float.Parse(speedStr);
		}
		catch {
			Debug.Log("Got invalid hall effect value:  " + speedStr);
		}
		
		if(speed == -500.0f){
			return;
		}
		
		if (speed > maxSpeed) speed = maxSpeed;
		if (speed < -maxSpeed) speed = -maxSpeed;
		currSpeed = speed;
	}
	
	// Update is called once per frame
	void Update () {
		//
		//Read sensor data
		//
		updateFromPotentiometer();
		updateFromHallEffect();
		
		//
		//Rotate handle bar - for visual purpose only
		//
		Quaternion xQuaternion = Quaternion.AngleAxis(currRot, Vector3.up);
		handlebarObj.transform.localRotation = originalRotation * xQuaternion;
		frontRightWheelObj.transform.localRotation = originalRotation * xQuaternion;
		frontLeftWheelObj.transform.localRotation = originalRotation * xQuaternion;
		
		//
		//Move bike	
		//
		frontRightWheelColl.motorTorque = torquePerSpeedLevel * currSpeed;
		frontLeftWheelColl.motorTorque = torquePerSpeedLevel * currSpeed;

		frontRightWheelColl.steerAngle = currRot;
		frontLeftWheelColl.steerAngle = currRot;
		//Debug.Log("Torque = " + frontRightWheelColl.motorTorque + " angle = " + frontRightWheelColl.steerAngle);
	}
}

