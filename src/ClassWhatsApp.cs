using System;
using System.Net; //for WebRequest
using System.IO; //for StreamWriter
using Newtonsoft.Json;
using System.Collections.Generic;


namespace DQM 
{
    class ClassWhatsApp : ClassMessageDevice
    {
        /// <summary>
        /// A class that handles WhatsApp request
        /// inherits from ClassMessageDevice
        /// </summary>
        private new readonly int GOOD_RESPONSE_FROM_SENDER = 200;
        public string newSendTo = string.Empty;

        public ClassWhatsApp(QueueManageDBDataSet.Tbl_MessageQueueRow row) : base(row)
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
        /// this method create and perform a POST request to whatsapp cloud api
        /// if neede, it ctreates multiple POST requests for multiple recipients
        /// </summary>
        /// <returns>return true if messae sending has succeeded, and false if message sending was failed</returns>
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

                bool allWAisSuccessSent = true;

                //Handle multiple recipients

                foreach(string recipient in SendToList)
                {
                    var url = Properties.Settings.Default.WhatsAppUrl;
                    var httpRequest = (HttpWebRequest)WebRequest.Create(url);
                    httpRequest.Method = "POST";
                    httpRequest.Headers["Authorization"] = "Bearer " + Properties.Settings.Default.WhatsApphttpRequestPassword;
                    httpRequest.ContentType = "application/json";

                    //create Json file for Post request
                    Parameter p1 = new Parameter();
                    p1.type = "text";
                    p1.text = Subject;

                    //Componenet can be header, body, or button
                    Component c1 = new Component();
                    c1.type = "header";
                    c1.parameters[0] = p1;

                    //Parameter can be text, currency, date_time, image, document, or video
                    Parameter p2 = new Parameter();
                    p2.type = "text";
                    p2.text = Body;

                    Component c2 = new Component();
                    c2.type = "body";
                    c2.parameters[0] = p2;

                    Language lang1 = new Language();
                    lang1.code = "en_US"; // Language supported: https://developers.facebook.com/docs/whatsapp/api/messages/message-templates#

                    Template t1 = new Template();
                    t1.name = "dqm_message"; // general explanation on template in code: https://developers.facebook.com/docs/whatsapp/cloud-api/guides/send-message-templates#text-based

                    t1.components[0] = c1;
                    t1.components[1] = c2;
                    t1.language = lang1;

                    WhatsAppPostData data = new WhatsAppPostData();
                    data.messaging_product = "whatsapp";

                    data.type = "template";
                    data.template = t1;

                    data.to = recipient;
                    string json = JsonConvert.SerializeObject(data);

                    using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
                    {
                        streamWriter.Write(json);
                    }
                    var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                    }

                    bool isCurrentSuccessSent = ((int)httpResponse.StatusCode == GOOD_RESPONSE_FROM_SENDER);

                    if (!isCurrentSuccessSent)
                    {
                        //add recipient to new string and later send to SendTo field in database table
                        newSendTo += recipient + ";";
                    }

                    // IsSuccessSent is true only if messagese where sent to all recipients 
                    allWAisSuccessSent &= isCurrentSuccessSent;
                }

                if (!string.IsNullOrEmpty(newSendTo))
                {
                    //newSendTo = newSendTo.Remove(newSendTo.Length - 1);
                    SendTo = newSendTo;
                    return IsSuccessSent = false;
                }

                //if (!allWAisSuccessSent)
                //{
                //    newSendTo = newSendTo.Remove(newSendTo.Length - 1);
                //    SendTo = newSendTo;
                //    return IsSuccessSent = false;
                //}
                return IsSuccessSent = true;

            } catch (Exception ex)
            {
                LastSendException += "Exception from WhatsApp Class" + ", ";
                LastSendException += ex.ToString() + ", ";
                return IsSuccessSent = false;
            }
        }



        // ******************* Helpers Methods *******************



        private void RemoveRecipient(string[] recipientArr, int i)
        {
            SendTo = string.Empty;
            List<string> recipientList = new List<string>(recipientArr);
            recipientList.RemoveAt(i);
            recipientArr = recipientList.ToArray();
            int len = recipientArr.Length;
            for (int j = 0; j < len; ++j)
            {
                SendTo += recipientArr[j] + ",";
            }
            SendTo = SendTo.Remove(SendTo.Length - 1);
        }
    }
}
