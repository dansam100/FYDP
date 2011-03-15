using UnityEngine;
using System.Collections;
using System.IO.Ports;
using System.Threading;

/// MouseLook rotates the transform based on the 3DM-GX1 IMU Euler angles.
/// Minimum and Maximum values can be used to constrain the possible rotation

[AddComponentMenu("Camera-Control/Mouse Look")]
public class MouseLookIMU : MonoBehaviour {
    //Flags        
    bool useRelativeAngles = true; //When true, startup angles are captured and used as reference for 0 degrees
	const bool USE_IMU = true; //true = IMU, false = Mouse
	
    //Serial Port
    string portName = "COM8";
    SerialPort _port;

    //Rotation    
    float initialPitch, initialYaw, initialRoll;
    Quaternion originalRotation;

    //IMU Communication
    const int RESPONSE_BUFF_LEN = 11;
    byte[] IMU_CMD = new byte[] { 0x0E };
    byte[] responseBuffer = new byte[RESPONSE_BUFF_LEN];
    float convertFactor = (360.0f/65536.0f);
	int bytesRead = 0; 
	int maxStartupReads = 100;

	//Threads
	Thread serialPortThread;
    Object serPortLock = new Object();

	
	//Mouse related variables
	enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
	RotationAxes axes = RotationAxes.MouseXAndY;
	float sensitivityX = 15F;
	float sensitivityY = 15F;

	float minimumX = -360F;
	float maximumX = 360F;

	float minimumY = -60F;
	float maximumY = 60F;

	float rotationX = 0F;
	float rotationY = 0F;
		
	//
	// Start Functions
	//	
    void Start ()
    {
		//
        //Rigid body stuff
        //
        // Make the rigid body not change rotation
        if (rigidbody) {
            rigidbody.freezeRotation = true;
		}
        originalRotation = transform.localRotation;
		
		if (USE_IMU) {
			StartIMU();
		}
		else {
			StartMouse();
		}
    }
	
	void StartMouse()
	{
		//Currently empty
	}
	
	void StartIMU()
	{
		//
        //Serial port set up
        //
		serialPortThread = new Thread(serialPortThreadEntry);
        //serialPortThread.IsBackground= true;
        serialPortThread.Start();
        Debug.Log("serialPortThread started.");
		
        //
        //Find startup angles, if applicable
        //

        if (useRelativeAngles)
        {
            float nPitch = 0.0f, nYaw = 0.0f, nRoll = 0.0f;
            int i = 0;

            //Read angles until got a reading or max reads reached
            while (i++ < maxStartupReads && !m3dmg_getEulerAngles(ref nRoll, ref nPitch, ref nYaw)) {
				System.Threading.Thread.Sleep(10);
			}

            initialRoll = nRoll;
            initialYaw = nYaw;
            initialPitch = nPitch;

            Debug.Log("initialPitch = " +nPitch + " initialRoll = " + nRoll + " initialYaw = " + nYaw);
        }
        else 
        {
            initialRoll = 0.0f;
            initialYaw = 0.0f;
            initialPitch = 0.0f;
        }	
	}
	
	//
	//Update functions
	//
	void Update ()
    {
		if (USE_IMU) {
			UpdateIMU();
		}
		else {
			UpdateMouse();
		}
    }
	
	void UpdateIMU()
	{
		float finalRoll = 0.0f, finalPitch = 0.0f, finalYaw = 0.0f;
        float nPitch = 0.0f, nYaw = 0.0f, nRoll = 0.0f;
        
        if (m3dmg_getEulerAngles(ref nRoll, ref nPitch, ref nYaw))
        {
            finalRoll = nRoll - initialRoll;
            finalPitch = -(nPitch - initialPitch);
            finalYaw = nYaw - initialYaw;
            //Debug.Log("pitch = " +finalPitch + " roll = " + finalRoll + " yaw = " + finalYaw);
        }
        else
        {
            Debug.Log("Failed UpdateIMU().");
            return;
        }
        
        Quaternion xQuaternion = Quaternion.AngleAxis (finalYaw, Vector3.up);
        Quaternion yQuaternion = Quaternion.AngleAxis (finalPitch, Vector3.left);
        Quaternion zQuaternion = Quaternion.AngleAxis (finalRoll, Vector3.forward);
        
        transform.localRotation = originalRotation * xQuaternion * yQuaternion * zQuaternion;
	}
	
	void UpdateMouse()
	{
		if (axes == RotationAxes.MouseXAndY)
		{
			// Read the mouse input axis
			rotationX += Input.GetAxis("Mouse X") * sensitivityX;
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;

			rotationX = ClampAngle (rotationX, minimumX, maximumX);
			rotationY = ClampAngle (rotationY, minimumY, maximumY);
			
			Quaternion xQuaternion = Quaternion.AngleAxis (rotationX, Vector3.up);
			Quaternion yQuaternion = Quaternion.AngleAxis (rotationY, Vector3.left);
			
			transform.localRotation = originalRotation * xQuaternion * yQuaternion;
		}
		else if (axes == RotationAxes.MouseX)
		{
			rotationX += Input.GetAxis("Mouse X") * sensitivityX;
			rotationX = ClampAngle (rotationX, minimumX, maximumX);

			Quaternion xQuaternion = Quaternion.AngleAxis (rotationX, Vector3.up);
			transform.localRotation = originalRotation * xQuaternion;
		}
		else
		{
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
			rotationY = ClampAngle (rotationY, minimumY, maximumY);

			Quaternion yQuaternion = Quaternion.AngleAxis (rotationY, Vector3.left);
			transform.localRotation = originalRotation * yQuaternion;
		}
	} 
	
	//This thread is responsible for keeping the serial port active
	//TODO: Test this thread's functionality
	void serialPortThreadEntry ()
    {
		for (;;) {
            System.Threading.Thread.Sleep(100);
            lock (serPortLock) {
                if (_port == null) {
					Debug.Log("Serial port thread starting a new _port");
                    _port = new SerialPort(portName, 38400, Parity.None, 8, StopBits.One);
                    _port.Open();
                }

                if (_port.IsOpen) {
					//Debug.Log("Serial port thread: _port open, nothing to do.");
                    continue;
                }
                else {
                    try {
						Debug.Log("Serial port thread: _port NOT open, trying to close and open.");
                        _port.Close();
                        _port.Open();
                    }
                    catch (System.Exception e) {
                        Debug.Log("serialPortThreadEntry() subsequent port opening failed: " + e.ToString());
                    }
                }
            } //lock
        } //for
    }
	
	//
	// Util functions
	//
    public static float ClampAngle (float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp (angle, min, max);
    }
    
	//This function reads data from the IMU and updates the roll/pitch/yaw passed in.
	//Note that when communicating with serial, there's a chance that not all the data from the IMU will be read at once.
	//Rather than looping until all data is read inside this function, the reading of partial data is distributed across different calls of this function.
	//This is done to avoid a bottleneck whereby frame updates will not continue until a full buffer from the IMU is read
    bool m3dmg_getEulerAngles(ref float nRoll, ref float nPitch, ref float nYaw)
    {
		//Need to read a full buffer, clear any existing data in buffer
		if (bytesRead == 0) {
			System.Array.Clear(responseBuffer, 0, RESPONSE_BUFF_LEN);
		}

        lock (serPortLock) {
            if (_port == null) return false;
            if (_port.IsOpen == false) return false;

            try {
				_port.Write(IMU_CMD, 0, 1);
                bytesRead += _port.Read(responseBuffer, bytesRead, RESPONSE_BUFF_LEN - bytesRead);
            }
            catch (System.Exception e) {
                Debug.Log("Exception in m3dmg_getEulerAngles: " + e.ToString());
				if (_port != null) {
					try {
						_port.Close();
					} catch { }
					_port.Dispose();
					_port = null;
				}
                return false;
            }
        }
		
		if (bytesRead == RESPONSE_BUFF_LEN) {
			//Ready to process data. Reset byte read counts.
			bytesRead = 0;
		}
		else {
			//Still more bytes to read, can't process yet so return.
			Debug.Log("Bytes read so far = " + bytesRead + ", bytes left to read = " + (RESPONSE_BUFF_LEN - bytesRead));
			return false;
		}
        //string toDebug = "";
        //for (int i = 1; i <= 6; ++i) toDebug += "(" + i + ")" + responseBuffer[i] + " "; 
        //Debug.Log(toDebug);

		//Process buffer -> verify checksum and update roll/pitch/yaw
        if (!checksumOkay(ref responseBuffer, RESPONSE_BUFF_LEN)) {
                Debug.Log("Resceived bad checksum from IMU, ignoring data");
                return false;
        }
        nRoll = convert2int(responseBuffer[1], responseBuffer[2])* convertFactor;
        nPitch = convert2int(responseBuffer[3], responseBuffer[4]) * convertFactor;
        nYaw = convert2int(responseBuffer[5], responseBuffer[6]) * convertFactor;
        Debug.Log("pitch = " + System.Math.Round(nPitch, 2) + " roll = " + System.Math.Round(nRoll, 2) + " yaw = " + System.Math.Round(nYaw, 2));
        return true;
    }
    
    short convert2short(byte byte1, byte byte2)
    {
        short x =0;
        x = (short)((byte1&0xFF)*256 + (byte2&0xFF));
        return x;
    }
   

    int convert2int(byte byte1, byte byte2)
    {
        int x =0;
        x = (byte1&0xFF)*256 + (byte2&0xFF);
        return x;
    }

    bool checksumOkay(ref byte[] buff, int buffLen)
    {
        int calcedCS = 0;

        //
        //Calculate checksum
        //

        if (buffLen < 4)
            return false;

        //Sum up all the bytes starting with command byte, UP TO checksum bytes
        calcedCS = buff[0] & 0xFF;                  //command byte
		
        for (int i=1; i < buffLen - 2; i += 2) {            //Rest of bytes
            calcedCS += convert2int(buff[i], buff[i+1]);
        }
        calcedCS &= 0xFFFF; 

        //
        //Compare to received checksum
        //
        return (calcedCS == convert2int(buff[buffLen-2], buff[buffLen-1]));

    }
}

