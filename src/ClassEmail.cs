using System;
using System.Net; //for NetworkCredential class
using System.Net.Mail; //for sneding email

namespace DQM
{
    class ClassEmail : ClassMessageDevice
    {
        public ClassEmail(QueueManageDBDataSet.Tbl_MessageQueueRow row) : base(row)
        {}

        /// <summary>
        /// Checks if email is in a valid stracture
        /// </summary>
        /// <param name="email"></param>
        /// <returns>true if email stractue is valid, false otherwise</returns>
        public override bool isValidAddress(string email)
        {
            string[] emailArr = email.Split(',');
            foreach (string singleMail in emailArr)
            {
                var trimmedEmail = singleMail.Trim();
                if (trimmedEmail.EndsWith(".") || !trimmedEmail.Contains("@"))
                {
                    LastSendException += email + " is not a valid email" + ", ";
                    return false;
                }

                var addr = new MailAddress(trimmedEmail); 
                if (addr.Address != trimmedEmail)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// sends email using SMTP (Simple Mail Transfer Protocol)
        /// </summary>
        /// <returns>true if messae delivery has succeeded, and false if message sending has failed</returns>
        public override bool send()
        {
            try
            {
                MailMessage mailMessage = new MailMessage();
                if(AttemptsLeft == 0)
                {
                    SendTo = SendToOwner;
                }
                else if(AttemptsLeft == -1)
                {
                    SendTo = Properties.Settings.Default.emailAdmin;
                    string addToSubject = "failed delivery message. ";
                    Subject = addToSubject + ": " + Subject;
                }
                mailMessage.To.Add(SendTo);
                mailMessage.From = new MailAddress(SendFrom);
                mailMessage.Subject = Subject;
                mailMessage.Body = Body;

                SmtpClient smtpEmail = new SmtpClient();

                smtpEmail.Host = Properties.Settings.Default.emailHost;
                smtpEmail.Port = Int32.Parse(Properties.Settings.Default.emailPort);
                var userName = Properties.Settings.Default.emailUserName;
                string password = Properties.Settings.Default.emailPassword;

                NetworkCredential networkCredential = new NetworkCredential(userName, password);

                smtpEmail.Credentials = networkCredential;
                smtpEmail.EnableSsl = false;
                smtpEmail.Send(mailMessage);

                return IsSuccessSent = true;
            }
            catch (SmtpFailedRecipientsException ex)
            {
                LastSendException += ex.ToString() + ", ";
                return IsSuccessSent = false;
            }
        }
    }
}
