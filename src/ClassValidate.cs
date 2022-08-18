using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DQM
{
    static class ClassValidate
    {
        static public bool IsValidate(QueueManageDBDataSet.Tbl_MessageQueueRow row)
        {
            return (ClassValidate.isValidAddresses(row.requestType, row.requestFrom, row.requestTo, row.requestToOwner)
                        && ClassValidate.isNotNull(row));
        }

        static public bool isNotNull(QueueManageDBDataSet.Tbl_MessageQueueRow row)
        {
            if(row.requestTime != null && isTypeExists(row.requestType) && row.requestStatus == (int)DQM.Status.RECORDED
                && row.requestExpiration != null && row.requestToken != null && row.requestFrom != null && row.requestTo != null)
            {
                return true;
            }
            return false;
        }

        static public bool isTypeExists(int requestType)
        {
            foreach(int enumValue in Enum.GetValues(typeof(DQM.Type)))
            {
                if (requestType == enumValue) return true;
            }
            return false;
        }

        static public bool isValidAddresses(int type, string emailFrom, string emailTo, string emailToOwner)
        {
            switch (type)
            {
                case (1):
                    if(IsValidEmail(emailFrom) && IsValidEmail(emailTo) && IsValidEmail(emailToOwner))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case (2):
                    if (IsValidPhoneNumber(emailFrom) && IsValidPhoneNumber(emailTo) && IsValidPhoneNumber(emailToOwner))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case (3):
                    if (IsValidPhoneNumber(emailFrom) && IsValidPhoneNumber(emailTo) && IsValidPhoneNumber(emailToOwner))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
            }
            return false;
        }

        static public bool IsValidEmail(string email)
        {
            string[] emailArr = email.Split(',');
            foreach(string singleMail in emailArr)
            {
                var trimmedEmail = singleMail.Trim();
                if (trimmedEmail.EndsWith("."))
                {
                    return false;
                }
                    //var addr = new System.Net.Mail.MailAddress(trimmedEmail); 
                    //if (addr.Address != trimmedEmail)
                    //    return false;
            }
            return true;
        }

        static public bool IsValidPhoneNumber(string number)
        {

            var stringNumber = "123";
            int numericValue;
            bool isNumber = int.TryParse(stringNumber, out numericValue);
            //string trimmedSms = number.Trim();
            //if (trimmedSms.Length != 12)
            //{
            //    return false;
            //}
            return true;
        }

        static public void limitToMaxRecipients(ClassMessageDevice messageDevice, int maxRecipients)
        {
            if (messageDevice.SendTo.Split(',').Length > maxRecipients)
            {
                messageDevice.SendTo = messageDevice.SendTo.Substring(0, messageDevice.SendTo.IndexOf(messageDevice.SendTo.Split(',')[maxRecipients])-1);
            }
        }
        static public bool isValidTime(ClassMessageDevice messageDevice)
        {
            if (DateTime.Compare(messageDevice.ExpirationTime, DateTime.Now) <= 0)
            {
                return false;
            }
            return true;
        }
    }
}
