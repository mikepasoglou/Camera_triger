using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;

using System.Text;
using System.Windows.Forms;
using IPS_ToolBox;
using System.Reflection;
using System.Configuration;
using System.Timers;

namespace Camera_triger
{
    public partial class Form1 : Form
    {
        private static string iocardport = string.Empty;
        private static int nonstopmode = 0;
        private static int openrelaytimes = 0;
        private IOCart myio;

        private System.Timers.Timer openrelayTimer;
        

        [Flags]
        private enum inEvents
        {
            ieNoneEven = 0x00,  // Κανένα event
            ieCarEntry1 = 0x01,  // Μπήκε αυτοκίνητο στο βρόγχο 1
            ieCarEntry2 = 0x01,  // Μπήκε αυτοκίνητο στο βρόγχο   2
            ieAutoinloop = 0x02,  // Πατήθηκε το κουμπί για την έκδοση εισιτηρίου
            ieAutoNOTinloop = 0x04,  // Εισήχθηκε κάρτα συνδρομητή στο μηχανήμα ανάγνωσης
            ieGotPlate = 0x08,  // Εγινε η αναγνώρισης της Πινακίδας
            ieBarrNOTVertical = 0x10,  //  η μπάρα  ΔΕΝ Ειναι σε κάθετη θέση 
            ieBarrVertical = 0x20,  //  ΜΠαρα  ΚΑΘΕΤΗ 
            iennnn = 0x40,   // Αυτο   στο loop του μηχανήματος της εξόδου
            ieBarrDown = 0x50 // new

        }


        [Flags]
        private enum inPorts
        {
            port_ = 0x00,
            port0 = 0x01, port1 = 0x02, port2 = 0x04, port3 = 0x08,
            port4 = 0x10, port5 = 0x20, port6 = 0x40, port7 = 0x80
        }

        private inPorts _ipCarLoop1;   // Θύρα Κάρτας ΙΟ που σηματοδοτεί την Υπαρξη Αυτοκινήτου στο Βρόχο   photocell 1


        public Form1()
        {
            InitializeComponent();
            _ipCarLoop1 = inPorts.port0;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                
                iocardport = ConfigurationManager.AppSettings["iocardport"];
                nonstopmode = Convert.ToInt16(ConfigurationManager.AppSettings["nonstop"]);
                openrelaytimes = Convert.ToInt16(ConfigurationManager.AppSettings["openrealytimes"]);
                myio = new IOCart(this, iocardport);
                openrelayTimer  = new System.Timers.Timer();
                

            }
            catch (Exception err)
            {

                Assorted.ErrorLog(MethodBase.GetCurrentMethod().ToString(), err.Message);
                MessageBox.Show("Params Init Problem");
                Application.Exit();
            }
        }


        public void Checkserialdata(byte[] comData)
        {

            byte inStat = (byte)(comData[3] ^ 0xFF);

            //inEvents[] currEvents = new inEvents[3];
            inEvents currEvents = new inEvents();
            currEvents = getEventType((inPorts)inStat);

             bool openrelay = false;

            if (currEvents == inEvents.ieCarEntry1)
            {
                openrelay = true;
                if (nonstopmode == 1)
                {
                    //StartTimer();
                    do
                    {
                        myio.PokeOutPort(1, 500);
                        System.Threading.Thread.Sleep(1000);
                    
                    }
                    while (openrelay == true);
                }
                else
                {
                    for (int k = 0; k < openrelaytimes; k++)
                    {
                        myio.PokeOutPort(1, 500);
                        System.Threading.Thread.Sleep(1000);
                    }


                }

            }
            else
            {
                //ResetTimer();
                openrelay = false;
            
            }
            



            


        }


        private inEvents getEventType(inPorts inStat)
        {
            inEvents currEvents = inEvents.ieNoneEven;

            
            if ((_ipCarLoop1 & inStat) == _ipCarLoop1)
                  currEvents |= inEvents.ieCarEntry1;
                


            return currEvents;
        }




        private void StartTimer() 
        {
            try
            {
                //Assorted.ErrorLog(MethodBase.GetCurrentMethod().ToString(), "iocartTimerBardown ON ");
                openrelayTimer.Elapsed += new ElapsedEventHandler(dotimerjob);
                openrelayTimer.Interval = 1000;
                openrelayTimer.AutoReset = true;
                openrelayTimer.Enabled = true;
            }
            catch (Exception err) { Assorted.ErrorLog(MethodBase.GetCurrentMethod().ToString(), err.Message); }

        }


        private void dotimerjob(object sender, ElapsedEventArgs e)  
        {
             myio.PokeOutPort(1, 500); 
            
        }


        private void ResetTimer()  
        {
            try
            {
                openrelayTimer.Elapsed -= dotimerjob;
                openrelayTimer.AutoReset = true;
                openrelayTimer.Enabled = false;
            }
            catch { }
        }

















    }
}
