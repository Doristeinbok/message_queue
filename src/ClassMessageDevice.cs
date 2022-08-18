using System;
using System.Collections.Generic;

namespace DQM
{
    /// <summary>
    /// An abstract class that holds: 
    /// * variables
    /// * 3 implemented methods: isValid, writeToLog, and limitToMaxRecipients
    /// * 2 abstract methods: isValidAddress and send
    /// </summary>
    public abstract class ClassMessageDevice
    {
        public readonly string GOOD_RESPONSE_FROM_SENDER = "OK";

        public int AttemptsLeft { get; set; }
        public DateTime RequestTime { get; set; }
        public string SendFrom { get; set; }

        string m_SendTo;
        public string SendTo {
            get
            {
                return m_SendTo;
            }
            set
            {
                m_SendTo = value;
                limitToMaxRecipients(20);

                SendToList.Clear();
                string[] arr = m_SendTo.Split(',');
                int len = arr.Length;
                foreach (string item in arr)
                {
                    SendToList.Add(item);
                }
            }
        }
        public List<string> SendToList;
        public string SendToOwner { get; set; }
        public Type RequestType { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public Status RequestStatus { get; set; }
        public DateTime ExpirationTime { get; set; }
        public bool IsSuccessSent { get; set; }
        public string Mac { get; set; }
        public int RowId { get; set; }
        public Guid RequestToken { get; set; }
        public string LastSendException { get; set; }

        public QueueManageDBDataSet.Tbl_MessageQueueRow Row { get; set; } //use only for updating the sql table (requestAttemptsNum update for example)

        //constructor
        public ClassMessageDevice(QueueManageDBDataSet.Tbl_MessageQueueRow row)
        {
            Subject = row.requestSubject;
            Body = row.requestBody;
            ExpirationTime = row.requestExpiration;
            RequestStatus = (Status)row.requestStatus;
            Mac = row.requestMAC;
            RowId = row.RowID;
            RequestToken = row.requestToken;
            SendToList = new List<string>();
            SendTo = row.requestTo;
            SendFrom = row.requestFrom;
            SendToOwner = row.requestToOwner == null ? row.requestTo : row.requestToOwner;
            RequestTime = row.requestTime;
            AttemptsLeft = row.requestAttemptsNum;
            RequestType = (Type)row.requestType;
            IsSuccessSent = false;
            LastSendException = string.Empty;
        }

        /// <summary>
        /// Check if message request is valid
        /// </summary>
        /// <returns>true if valid, flase if not</returns>
        public bool isValid()
        {
            return isValidAddresses() && isNotNull() && isTypeExists() && isValidTime();
        }

        public void writeToLog(ClassLog log)
        {
            string messageToLog = string.Empty;
            string outcomeMessage = IsSuccessSent ? "message was sent successfully" : "sending message was failed   ";

            messageToLog = string.Format("{0,-30} {1,-10} {2,-10} {3,-30} {4,-30} {5,-30} {6,-30} {7,-30} {8,-10} {9,-10} {10,-10}",
                   RequestTime, RequestType, RequestStatus, SendFrom, SendTo, SendToOwner, Mac, Subject, ExpirationTime, outcomeMessage, LastSendException);

            log.Write(messageToLog);
        }

        public void limitToMaxRecipients(int maxRecipients)
        {
            if (SendTo.Split(',').Length > maxRecipients)
            {
                SendTo = SendTo.Substring(0, SendTo.IndexOf(SendTo.Split(',')[maxRecipients]) - 1);
                LastSendException += ", You have exceeded the maximum number of recipients";
            }
        }

        public bool isValidAddresses()
        {
            return isValidFromAddress(SendFrom) && isValidAddressListTo() && isValidAddress(SendToOwner);
        }

        /// <summary>
        /// check if SendTo List field is valid
        /// if it has only one address, it will use isValidAddress method directly
        /// </summary>
        /// <returns>true if atleast one address is valid, false for invalid of all addresses</returns>

        public bool isValidAddressListTo()
        {
            int len = SendToList.Count;
            if(len == 1)
            {
                return isValidAddress(SendTo);
            }

            for(int i = 0;  i < len; ++i)
            {
                if(!isValidAddress(SendToList[i]))
                {
                    SendToList.RemoveAt(i--);
                    --len;
                }
            }
            SendTo = String.Join (",", SendToList.ToArray());
            if(len == 0) return false;
            return true;
        }

        public abstract bool isValidAddress(string address);
        public abstract bool send();

        /// <summary>
        /// check if SendFrom field is valid
        /// if not override, it will use the same isValidAddress method like SendTo and SendToOwner
        /// </summary>
        /// <param name="sendFrom"></param>
        /// <returns>true for valid, false for invalid</returns>
        public virtual bool isValidFromAddress(string sendFrom)
        {
            return isValidAddress(sendFrom);
        }

        public bool isNotNull()
        {
            if (RequestTime != null && isTypeExists() && RequestStatus == DQM.Status.RECORDED
                && ExpirationTime != null && RequestToken != null && SendFrom != null && SendTo != null)
            {
                return true;
            }

            return false;
        }

        public bool isTypeExists()
        {
            foreach (Type enumValue in Enum.GetValues(typeof(DQM.Type)))
            {
                if (RequestType == enumValue) return true;
            }
            LastSendException += "message delivery type doen't exist" + ", ";
            return false;
        }

        public bool isValidTime()
        {
            if (DateTime.Compare(ExpirationTime, DateTime.Now) <= 0)
            {
                LastSendException += "message request is expired" + ", ";
                return false;
            }
            return true;
        }

    }
}
