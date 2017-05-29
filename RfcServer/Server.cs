using System;
using System.Runtime.InteropServices;
using RFCCONNECTORLib;

namespace RfcServerExample
{
    class Server
    {        
        // ---------------
        // RFC Server data
        // ---------------
        //
        // RFC_DESTINATION must be set up in transaction SM59 as
        // "TCP/IP Connection" with "Registered Server Program"
        const string RFC_DESTINATION = "ZRFCCTEST";
        // Hostname of the SAP server we are registering with
        const string GATEWAY_HOST = "was-npl";
        // Gateway service (this is "sapgw" plus the system number)
        const string GATEWAY_SERVICE = "sapgw42";

        // ---------------
        // RFC Client Data
        // ---------------
        //
        // Client data is ONLY necessary for importing the function
        // definition once at startup
        const string SAPLOGON_ID = "NPL";
        const string CLIENT = "001";
        const string USER = "DEVELOPER";
        const string PW = "developer1";
        const string LANGUAGE = "EN";

        // our RFC server        
        protected NWRfcServer srv;

        // client session to import function definitions
        
        protected FunctionCall fn1;

        void ImportFunctions()
        {
            // import the function definitions we need later
            NWRfcSession session = new NWRfcSession();
            Console.WriteLine("Retrieving function definitions from remote system...");
            session.SystemID = SAPLOGON_ID;
            session.RfcSystemData.ConnectString = String.Format("SAPLOGON_ID={0}", SAPLOGON_ID);
            session.LogonData.Client = CLIENT;
            session.LogonData.User = USER;
            session.LogonData.Password = PW;
            session.LogonData.Language = LANGUAGE;
            session.Connect();
            if (session.Error == true)
            {
                Console.WriteLine("Error: " + session.ErrorInfo.Message);
            }
            fn1 = session.ImportCall("BAPI_FLIGHT_GETLIST", true);
            session.Disconnect();
        }

        void run()
        {
            srv = new NWRfcServer();
            srv.ProgramID = RFC_DESTINATION;
            srv.GatewayHost = GATEWAY_HOST;
            srv.GatewayService = GATEWAY_SERVICE;

            srv.Option["trace.file"] = "C:\\RfcServer.trc";

            ImportFunctions();
            srv.InstallFunction(fn1);

            // set up event handlers
            srv.IncomingCall += OnIncomingCall;
            srv.Logon += OnLogon;
            srv.ServerError += OnServerError;

            Console.WriteLine("Starting server...");
            srv.Serve();

            if (false && srv.Error == true)
            {
                Console.WriteLine("Error: Could not start server.");
                Console.WriteLine(srv.ErrorInfo.Message);
                return;
            }

            Console.WriteLine("Server running. Press key to stop...");
            Console.ReadLine();

            srv.Shutdown();


        }

        void OnIncomingCall(FunctionCall fn)
        {
            switch (fn.Function)
            {
                case "BAPI_FLIGHT_GETLIST":
                    string airline;
                    string dest_from;
                    string dest_to;
                    if (fn.Importing.HasKey("AIRLINE"))
                    {
                        airline = fn.Importing["AIRLINE"].value.ToString();
                    }
                    else
                    {
                        airline = "LH";
                    }

                    if (fn.Importing.HasKey("DEST_FROM"))
                    {
                        dest_from = fn.Importing["DEST_FROM"].value.ToString();
                    }
                    else
                    {
                        dest_from = "SFO";
                    }

                    if (fn.Importing.HasKey("DEST_TO"))
                    {
                        dest_to = fn.Importing["DEST_TO"].value.ToString();
                    }
                    else
                    {
                        dest_to = "JFK";
                    }

                    // add some static rows 
                    DateTime dt = DateTime.Now;
                    for (int i = 1; i <= 10; i++)
                    {
                        RfcFields r = fn.Tables["FLIGHT_LIST"].Rows.AddRow();
                        r["AIRLINEID"].value = airline;
                        r["AIRPORTFR"].value = dest_from;
                        r["AIRPORTTO"].value = dest_to;
                        r["CONNECTID"].value = i;
                        r["FLIGHTDATE"].value = dt.AddDays(i);
                    }
                    break;
                default:
                    // we don't know about this function, so we raise an exception
                    fn.RaiseException("SYSTEM_ERROR", "");
                    break;
            }
        }


        void OnLogon(RfcServerLogonInfo li)
        {
            if (li.User == "DEVELOPER")
            {
                li.RequestAllowed = true;
            }
            else
            {
                li.RequestAllowed = true;
            }
        }

        void OnServerError(RfcServerErrorInfo ei)
        {
            // log the error to console
            Console.WriteLine(ei.Message);
            // restart the server
            ei.Restart = true;
        }

        static void Main(string[] args)
        {
            Server self = new Server();
            self.run();
            Console.WriteLine("Server exited. Press key to leave...");
            Console.ReadLine();
        }

    }
}
