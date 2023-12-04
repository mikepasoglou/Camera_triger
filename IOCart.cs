using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Reflection;
using IPS_ToolBox;

namespace Camera_triger
{
    public class IOCart
    {
        // Constants
        private const int PACKET_SIZE = 6;

        // Private Variables
        //private BoxOffice _caller;
        private Form1 _caller;
        private SerialPort _comPort;
        private List<byte> oldBfr = new List<byte>();

        // Private Methods
        private byte[] StrToByteArray(string respData)
        {
            string[] response = respData.Split('|');
            byte[] aData = new byte[response.Length - 1];

            for (int i = 0; i < response.Length - 1; i++)
                aData[i] = Convert.ToByte(response[i]);

            return aData;
        }



        private string ByteArrayToStr(byte[] ByteArray)
        {
            string str = string.Empty;
            for (int i = 0; i < ByteArray.Length; i++)
            {
                str += ByteArray[i].ToString() + " ";
            }
            return str;
        }

        private bool procData(byte[] newBfr, ref string response)
        {
           // List<byte> oldBfr = new List<byte>();   //   new 30/052017  ********************************************************
            bool funcResult = true;

            try
            {
                string respData = String.Empty;
                int packCntr = 1;
                int upperLim = packCntr * PACKET_SIZE;

                oldBfr.AddRange(newBfr);

                byte[] sumData = oldBfr.ToArray();

                oldBfr.Clear();

                for (int i = 0; i < sumData.Length; i++)
                {
                    if ((upperLim - 1) < sumData.Length)
                    {
                        respData += sumData[i].ToString() + "|";
                    }
                    else
                    {
                        for (int j = i; j < sumData.Length; j++)
                            oldBfr.Add(sumData[j]);

                        break;
                    }

                    if (i == (upperLim - 1))
                    {
                        packCntr++;
                        upperLim = packCntr * PACKET_SIZE;

                        response = respData;

                        respData = String.Empty;
                    }
                }

                int oldbfrsize = oldBfr.Count;
                if (oldbfrsize != 6 || oldbfrsize != 12 || oldbfrsize != 18 || oldbfrsize != 24 || oldbfrsize != 30)
                {
                    oldBfr = new List<byte>();
                    Assorted.ErrorLog("oldBfr_DEL", oldbfrsize.ToString() );
                }




            }
            catch (Exception err) 
            { 
                Assorted.ErrorLog(MethodBase.GetCurrentMethod().ToString(), err.Message);
                funcResult = false;
            
            }

            return funcResult;
        }

        // Constructors
        //public IOCart(BoxOffice caller)
         public IOCart(Form1  caller, string comport)
        {
            _caller = caller;

            try
            {
                _comPort = new SerialPort();

                _comPort.PortName = comport;
                _comPort.BaudRate = 9600;
                _comPort.Parity = Parity.None;
                _comPort.DataBits = 8;
                _comPort.StopBits = StopBits.One;
                _comPort.Handshake = Handshake.None;
                _comPort.RtsEnable = true;
                _comPort.ReceivedBytesThreshold = PACKET_SIZE;

                if (!_comPort.IsOpen)
                    _comPort.Open();

                _comPort.DiscardInBuffer();
                _comPort.DiscardOutBuffer();

                _comPort.DataReceived += new SerialDataReceivedEventHandler(OnDataReceived);
            }
            catch (Exception err)
            { Assorted.ErrorLog(MethodBase.GetCurrentMethod().ToString(), err.Message); }
        }

        // Destructors
        ~IOCart()
        {
            try
            {
                if (_comPort.IsOpen)
                    _comPort.Close();
            }
            catch { }

            if (_comPort != null)
                _comPort.Dispose();
        }


        void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {

           int bfrSize = _comPort.BytesToRead;
           byte[] buffer = new byte[bfrSize];
           string response = String.Empty;

           _comPort.Read(buffer, 0, bfrSize);

           Assorted.ErrorLog("IOData", ByteArrayToStr(buffer));

           if (procData(buffer, ref response))
           {
               //Assorted.ErrorLog("procData", response );
               if (response != String.Empty)
               {
                   byte[] respData = StrToByteArray(response);

                   if (respData[0] == 0x44)
                       _caller.Checkserialdata(respData);
               }
           }
          
            


        }



        // Interface to the Outside World
        public void OpenAllOut()
        {
            byte[] cmdStruct = new byte[3];

            cmdStruct[0] = 0x41;
            cmdStruct[1] = 0x48;
            cmdStruct[2] = 0x0A;

            _comPort.Write(cmdStruct, 0, cmdStruct.Length);
        }

        public void CloseAllOut()
        {
            byte[] cmdStruct = new byte[3];

            cmdStruct[0] = 0x41;
            cmdStruct[1] = 0x4C;
            cmdStruct[2] = 0x0A;

            _comPort.Write(cmdStruct, 0, cmdStruct.Length);
        }

        public void PokeOutPort(int currOut,int msdelay)
        {
            if ((currOut >= 0) && (currOut <= 7))
            {
                byte[] cmdStruct = new byte[3];

                cmdStruct[0] = 0x53;
                cmdStruct[1] = Convert.ToByte(Convert.ToChar(currOut.ToString()));
                cmdStruct[2] = 0x0A;

                _comPort.Write(cmdStruct, 0, cmdStruct.Length);

                Thread.Sleep(msdelay);
                cmdStruct[0] = 0x43;

                _comPort.Write(cmdStruct, 0, cmdStruct.Length);
            }
        }



        public void OpenPort(int currOut)
        {
            if ((currOut >= 0) && (currOut <= 7))
            {
                byte[] cmdStruct = new byte[3];

                cmdStruct[0] = 0x53;
                cmdStruct[1] = Convert.ToByte(Convert.ToChar(currOut.ToString()));
                cmdStruct[2] = 0x0A;

                _comPort.Write(cmdStruct, 0, cmdStruct.Length);
            }
        }

        public void CloseOutPort(int currOut)
        {
            if ((currOut >= 0) && (currOut <= 7))
            {
                byte[] cmdStruct = new byte[3];

                cmdStruct[0] = 0x43;
                cmdStruct[1] = Convert.ToByte(Convert.ToChar(currOut.ToString()));
                cmdStruct[2] = 0x0A;

                _comPort.Write(cmdStruct, 0, cmdStruct.Length);
            }
        }




        public byte GetInStatus()
        {
            byte[] cmdStruct = new byte[3];

            cmdStruct[0] = 0x52;
            cmdStruct[1] = 0x4D;
            cmdStruct[2] = 0x0A;

            _comPort.Write(cmdStruct, 0, cmdStruct.Length);

            byte inStatus = 0x00;
            byte[] comAnswer = new byte[6];

            _comPort.DataReceived -= OnDataReceived;

            try
            {
                for (int i = 0; i > comAnswer.Length - 1; i++)
                    comAnswer[i] = Convert.ToByte(_comPort.ReadByte());

                inStatus = (byte)(comAnswer[3] ^ 0xFF);
            }
            catch { }
            finally { _comPort.DataReceived += OnDataReceived; }

            return inStatus;
        }

        public byte GetOutStatus()
        {
            byte[] cmdStruct = new byte[3];

            cmdStruct[0] = 0x52;
            cmdStruct[1] = 0x4D;
            cmdStruct[2] = 0x0A;

            _comPort.Write(cmdStruct, 0, cmdStruct.Length);

            byte outStatus = 0x00;
            byte[] comAnswer = new byte[6];

            _comPort.DataReceived -= OnDataReceived;

            try
            {
                for (int i = 0; i > comAnswer.Length - 1; i++)
                    comAnswer[i] = Convert.ToByte(_comPort.ReadByte());

                outStatus = comAnswer[2];
            }
            catch { }
            finally { _comPort.DataReceived += OnDataReceived; }

            return outStatus;
        }

        public byte[] GetIOStatus()
        {
            byte[] cmdStruct = new byte[3];

            cmdStruct[0] = 0x52;
            cmdStruct[1] = 0x4D;
            cmdStruct[2] = 0x0A;

            _comPort.Write(cmdStruct, 0, cmdStruct.Length);

            byte[] ioStatus = new byte[2];
            byte[] comAnswer = new byte[6];

            _comPort.DataReceived -= OnDataReceived;

            try
            {
                for (int i = 0; i > comAnswer.Length - 1; i++)
                    comAnswer[i] = Convert.ToByte(_comPort.ReadByte());

                ioStatus[0] = (byte)(comAnswer[3] ^ 0xFF);
                ioStatus[1] = comAnswer[2];
            }
            catch { }
            finally { _comPort.DataReceived += OnDataReceived; }

            return ioStatus;
        }

        public void DisplayAll()
        {
            byte[] cmdStruct = new byte[3];

            cmdStruct[0] = 0x44;
            cmdStruct[1] = 0x41;
            cmdStruct[2] = 0x0A;

            _comPort.Write(cmdStruct, 0, cmdStruct.Length);
        }

        public void ClearINData()
        {
            _comPort.DiscardInBuffer();

        }


        public void fakeAction()
        {
            OpenAllOut();

            //Thread.Sleep(_caller.AppSettings.PortDelay);
            Thread.Sleep(1000);
            CloseAllOut();
        }


        public void ResetIO()
        {
            byte[] cmdStruct = new byte[3];

            cmdStruct[0] = 0x52;
            cmdStruct[1] = 0x53;
            cmdStruct[2] = 0x0A;

            _comPort.Write(cmdStruct, 0, cmdStruct.Length);
        }


        public bool IsBarOpened(int curInPort, int waittime)  
        {

            bool funcresult = false;

            _comPort.DataReceived -= OnDataReceived;


            byte respByte = 0x00;
            byte[] comAnswer = new byte[6];

            _comPort.DiscardInBuffer();         //   new ********************************************  ???  SOS
            Thread.Sleep(waittime);

           
            DisplayAll();
            try
            {
                int i = 0;
                do
                {
                    respByte = Convert.ToByte(_comPort.ReadByte());
                    comAnswer[i] = respByte;
                    i++;

                } while ((respByte != 3) || (i < 6));  // to teleyteaio byte eiani 3 



                byte inStat = (byte)(comAnswer[3] ^ 0xFF);

                if ((curInPort & inStat) != curInPort)  //   ok σε αυτή την πέριπτωση το limit switch θα δώσει σήμα (beep)  όταν η μπάρα είναι κάτω 
               // if ((curInPort & inStat) == curInPort)  //   test σε αυτή την πέριπτωση το limit switch θα δώσει σήμα (beep)  όταν η μπάρα είναι επάνω (επαφη normal close )
                {
                    funcresult = true;  //     
                }



                /*
                if ((comAnswer[0] != 68) || (comAnswer[1] != 65) || (comAnswer[5] != 3))
                {
                    Assorted.ErrorLog("DErr", ByteArrayToStr(comAnswer));
                    funcresult = false;
                }
                else
                {
                    byte inStat = (byte)(comAnswer[3] ^ 0xFF);

                    //if ((curInPort & inStat) != curInPort)  //   ok σε αυτή την πέριπτωση το limit switch θα δώσει σήμα (beep)  όταν η μπάρα είναι κάτω 
                    if ((curInPort & inStat) == curInPort)  //   test σε αυτή την πέριπτωση το limit switch θα δώσει σήμα (beep)  όταν η μπάρα είναι επάνω (επαφη normal close )
                    {
                        funcresult = true;  //     Η Function επιστρέφει true όταν η μπαρα ΔΕΝ είναι οριζόντια - δηλ αρχίζει και ανεβαίνει
                    }

                }
                */
            }
            catch
            {
                return funcresult;
            }
            finally
            {
                _comPort.DataReceived += OnDataReceived;
            }
            return funcresult;


        }




        public void GetTemperature()
        {
            byte[] cmdStruct = new byte[3];

            cmdStruct[0] = 0x54;
            cmdStruct[1] = 0x43;
            cmdStruct[2] = 0x0A;

            _comPort.Write(cmdStruct, 0, cmdStruct.Length);
        }











        /*
        public bool IsBarOpened(int curInPort, int waittime)  // add 15/05/2016
        {

            bool funcresult = false;

            _comPort.DataReceived -= OnDataReceived;
            _comPort.DiscardInBuffer();
            _comPort.DiscardOutBuffer();


            byte respByte = 0x00;
            byte[] comAnswer = new byte[6];

            Thread.Sleep(waittime);
            DisplayAll();
            try
            {
                int i = 0;
                do
                {
                    respByte = Convert.ToByte(_comPort.ReadByte());
                    comAnswer[i] = respByte;
                    i++;

                } while ((respByte != 3) || (i < 6));  // to teleyteaio byte eiani 3 


                if ((comAnswer[0] != 68) || (comAnswer[1] != 65) || (comAnswer[5] != 3)) //DA
                    funcresult = false;
                else
                {
                    byte inStat = (byte)(comAnswer[3] ^ 0xFF);

                    if ((curInPort & inStat) == curInPort)  //   απο την μπαρα έρχεται οτι είναι καθετη
                        funcresult = true;  //     Η Function επιστρέφει true όταν η μπαρα ΕΙΝΑΙ Καθετη
                }
            }
            catch
            {
                return funcresult;
            }
            finally
            {
                _comPort.DataReceived += OnDataReceived;
            }

            return funcresult;


        }


        */

















    }
}

















