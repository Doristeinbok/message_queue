using System;


namespace DQM 
{
    class ClassSMS : ClassMessageDevice
    {
        public ClassSMS(QueueManageDBDataSet.Tbl_MessageQueueRow row) : base(row)
        {}

        /// <summary>
        /// checks if phone number is in a valid length
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns>true if phoneNumber is valid, false otherwise</returns>
        public override bool isValidAddress(string phoneNumber)
        {
            string trimmedNumber = phoneNumber.Trim();
            if (trimmedNumber.Length < 9 || trimmedNumber.Length > 10)
            {
                LastSendException += phoneNumber + " has " + trimmedNumber.Length + "digits" + ", ";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Check if first letter of SendFrom field is a Latin letter
        /// and check if all letters are Latin letters or digits
        /// </summary>
        /// <param name="sendFrom"></param>
        /// <returns>true if SendFrom is valid</returns>
        public override bool isValidFromAddress(string sendFrom)
        {
            if (!((sendFrom[0] >= 'A' && sendFrom[0] <= 'Z') ||
                 (sendFrom[0] >= 'a' && sendFrom[0] <= 'z')))
                return false;

            foreach(char c in sendFrom)
            {
                if(!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')))
                {
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// calls a web service to send SMS message
        /// </summary>
        /// <returns>true if message delivery has succeeded, and false if message delivery has failed</returns>
        public override bool send()
        {
            try
            {
                if (AttemptsLeft == 0)
                {
                    SendTo = SendToOwner;
                }
                else if (AttemptsLeft == -1)
                {
                    SendTo = Properties.Settings.Default.phoneAdmin;
                    string addToSubject = "failed delivery message. ";
                    Subject = addToSubject + "original subject: " + Subject;
                }

                WS.Service1 serviceSMS = Mod.CreateWSMaaleSer1();
                
                string response = serviceSMS.SendSMS(Subject, SendTo, SendFrom);
                if(response != GOOD_RESPONSE_FROM_SENDER)
                {
                    LastSendException += response + ", ";
                    return IsSuccessSent = false;
                }

                return IsSuccessSent = true;
            }
            catch (Exception ex)
            {
                LastSendException += ex.ToString() + ", ";
                return IsSuccessSent = false;
            }
        }
    }
}
