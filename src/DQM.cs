using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;
using System.Security.Principal;
using System.Transactions;

namespace DQM
{
    public enum Type
    {
        EMAIL = 1,
        SMS = 2,
        WHATSAPP = 3,
        GSLAB = 50,
        ERROR = 404
    }
    public enum Status
    {
        RECORDED = 1,
        DONE = 2,
        POSTPONED = 3,
        EXPIRED = 4,
        CANCELED = 5
    }
    public class ClassDQM
    {

        private const int NO_ATTEMPTS_LEFT = -2; //-2 to send owner and administrator (0 is the end of regular sending)



        [SqlFunction(DataAccess = DataAccessKind.Read)]

        static public int DQM_RunOnce()
        {
            return DQM_Run(false); // to enable no parameter call from CLR (true for production, false for testing)
        }


        /// <summary>
        /// The main method of the application
        /// </summary>
        /// <returns>Number of times that a message request was send successfully</returns>
      
        //[SqlFunction(TableDefinition = "forename nvarchar(50)", FillRowMethodName = "TestFillRow", DataAccess = DataAccessKind.Read)]

        static public int DQM_Run(bool RunAsCLR)
        {
            int nSuccess = 0;
            WindowsImpersonationContext impersonatedUser = null;    // used only when running as CLR function

            var dsDQM = new QueueManageDBDataSet();
            var daQueue = new QueueManageDBDataSetTableAdapters.Tbl_MessageQueueTableAdapter();
            daQueue.FillWithFilter(dsDQM.Tbl_MessageQueue);

            //ClassLog log = new ClassLog();
            ClassLog log = ClassLog.getInstance(); //creating ClassLog ones (singleton pattern)
            
            foreach (QueueManageDBDataSet.Tbl_MessageQueueRow row in dsDQM.Tbl_MessageQueue)
            {
                Console.WriteLine("date = " + row.requestTime);

                //creating object
                ClassMessageDevice messagingMethod;

                switch ((Type)row.requestType)
                {
                    case Type.EMAIL:
                        messagingMethod = new ClassEmail(row);
                        break;
                    case Type.SMS:
                        messagingMethod = new ClassSMS(row);
                        break;
                    case Type.WHATSAPP:
                        messagingMethod = new ClassWhatsApp(row);
                        break;
                    default:
                        messagingMethod = new ClassEmail(row);
                        break;
                }

                //checking validation of messaging request
                bool validatedUpdate = row.requestValidated;
                if (!validatedUpdate)
                {
                    if (messagingMethod.isValid())
                    {
                        validatedUpdate = true;
                    }
                    else
                    {
                        //request is not valid => update table and return
                        try
                        {
                            using (System.Transactions.TransactionScope trans = new System.Transactions.TransactionScope())
                            {
                                daQueue.updateMessageQueue(messagingMethod.RowId, messagingMethod.AttemptsLeft, (int)Status.CANCELED,
                                                        false, messagingMethod.SendTo, "request is not valid");

                                trans.Complete();
                            }

                            messagingMethod.writeToLog(log);
                            return nSuccess;
                        }
                        catch
                        {
                            messagingMethod.writeToLog(log);
                            return nSuccess;
                        }
                    }
                }


                //send message
                bool isSent = messagingMethod.send();
                if (isSent)
                {
                    nSuccess++;
                    messagingMethod.RequestStatus = Status.DONE;
                }
                else
                {
                    messagingMethod.AttemptsLeft--;
                    if (!messagingMethod.isValidTime() || messagingMethod.AttemptsLeft <= NO_ATTEMPTS_LEFT)
                    {
                        messagingMethod.RequestStatus = Status.EXPIRED;
                    }
                }

                messagingMethod.writeToLog(log);

                if (RunAsCLR)
                {
                    //impersonatedUser = null;
                    WindowsIdentity clientId = null;
                    clientId = SqlContext.WindowsIdentity;
                    impersonatedUser = clientId.Impersonate();
                }

                //update table
                try
                {
                    //ORIG: using (System.Transactions.TransactionScope trans = new System.Transactions.TransactionScope())
                    using (System.Transactions.TransactionScope trans = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeOption.Suppress)) // SUPRESS BY ELI
                    {
                        daQueue.updateMessageQueue(messagingMethod.RowId,
                            messagingMethod.AttemptsLeft,
                            (int)messagingMethod.RequestStatus,
                            validatedUpdate,
                            messagingMethod.SendTo,
                            messagingMethod.LastSendException);

                        trans.Complete();
                    }
                }
                catch
                {
                    messagingMethod.RequestStatus = Status.RECORDED;
                }
                finally
                {
                    if (RunAsCLR)
                        // Undo impersonation. 
                        if (impersonatedUser != null)
                            impersonatedUser.Undo();
                }
            }

            return nSuccess;
        }
    }
}

