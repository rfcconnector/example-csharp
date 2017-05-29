using System;
using System.Runtime.InteropServices;
using RFCCONNECTORLib;
namespace TableReaderExample
{
    class Program
    {
        static protected ISession GetNwRfcSession(string saplogon_id = "NPL")
        {
            NWRfcSession session = new NWRfcSession();
            // for a full list of possible options, see http://rfcconnector.com/documentation/kb/0008/
            session.RfcSystemData.ConnectString = String.Format("SAPLOGON_ID={0}", saplogon_id);
            return session;
        }

        /// <summary>
        /// Create a session through SOAP. This does not require any additional libraries, but
        /// you need to enable the service /sap/bc/soap/rfc/ in transaction SICF.
        /// 
        /// The port number is usually SAP system number + 8000.
        /// </summary>
        /// <param name="host">hostname of the SAP server</param>
        /// <param name="port">port of the SAP server</param>
        /// <returns></returns>
        static protected ISession GetSOAPSession(string host = "nplhost", int port = 8042)
        {
            SoapSession soapsession = new SoapSession();
            soapsession.HttpSystemData.Host = host;
            soapsession.HttpSystemData.Port = port;
            return soapsession;
        }

        /// <summary>
        /// Connect through classic RFC. This is not recommended any more, and should be used
        /// only in legacy situations, where it is not possible to upgrade SAPGUI.
        /// 
        /// This mode can only be used with SAP GUI 7.40 or older.
        /// </summary>
        /// <param name="saplogon_id">target system (as defined in SAP Logon)</param>
        /// <returns></returns>
        static protected ISession GetRfcSession(string saplogon_id = "NPL")
        {
            RfcSession rfcsession = new RfcSession();
            rfcsession.RfcSystemData.ConnectString = String.Format("SAPLOGON_ID={0}", saplogon_id);
            return (ISession)rfcsession;
        }

        /// <summary>
        /// Log on to the SAP system with the given parameters, and check errors
        /// </summary>
        /// <param name="session">session instance</param>
        /// <returns>true for success, false if there were errors</returns>
        static protected bool Logon(ISession session, string client = "001", string user = "", string password = "", string language = "EN")
        {
            // set up logon data
            session.LogonData.Client = client;
            session.LogonData.User = user;
            session.LogonData.Password = password;
            session.LogonData.Language = language;

            // enable trace file, see http://rfcconnector.com/documentation/kb/0004/
            session.set_Option("trace.file", "C:\\tracefile.txt");

            // connect and check error
            session.Connect();

            if (session.Error == true)
            {
                Console.WriteLine(session.ErrorInfo.Message);
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Use the TableReader to read the SAP table SFLIGHT with RFC_READ_TABLE
        /// 
        /// For limitations of RFC_READ_TABLE and possible solutions, please refer to
        /// http://rfcconnector.com/documentation/kb/0007/
        /// </summary>
        /// <param name="session"></param>
        static void ReadTable(ISession session)
        {
            TableReader tr;

            tr = session.GetTableReader("SFLIGHT");
            tr.Fields.Add("CARRID");
            tr.Fields.Add("CONNID");
            tr.Fields.Add("FLDATE");
            tr.Query.Add("CARRID EQ 'LH'");
            tr.Read(0, 0);
            foreach (RfcFields row in tr.Rows)
            {
                Console.WriteLine(row["CONNID"].value + " " + row["FLDATE"].value + " " + row["CARRID"].value);
            }

        }

        static void Main(string[] args)
        {
            try
            {
                ISession session = GetNwRfcSession();
                if (Logon(session, "001", "DEVELOPER", "developer1", "EN"))
                {
                    ReadTable(session);
                }
            }
            catch (COMException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            // wait for user input
            Console.WriteLine("press any key to exit");
            Console.ReadLine();
        }
    }
}
