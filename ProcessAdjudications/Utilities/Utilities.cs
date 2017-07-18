using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Adjudications.Utilities
{
    sealed class Utilities
    {
        /// <summary>
        /// SHA256 Hash of the SSN
        /// </summary>
        /// <param name="ssn"></param>
        /// <returns></returns>
        public byte[] HashSSN(string ssn)
        {
            byte[] hashedFullSSN = null;

            SHA256 shaM = new SHA256Managed();

            ssn = ssn.Replace("-", string.Empty).Trim();

            //Using UTF8 because this only contains ASCII text
            if (ssn.Length == 9)
                hashedFullSSN = shaM.ComputeHash(Encoding.UTF8.GetBytes(ssn));

            shaM.Dispose();

            return hashedFullSSN;
        }

        public string GenerateDecryptedFilename(string encryptedFilename)
        {   
            return string.Concat(encryptedFilename, "-d.csv");
        }

        //This is not ideal.  Trying to cover all cases
        public void SendEMail(string body, string debugSubject = "")
        {
            string attachment = string.Empty;

            if (File.Exists(ConfigurationManager.AppSettings["ADJUDICATIONSSUMMARY"]))
                attachment = ConfigurationManager.AppSettings["ADJUDICATIONSSUMMARY"];

            using (EMail message = new EMail())
            {
                message.Send(ConfigurationManager.AppSettings["SUMMARYEMAIL"],
                         ConfigurationManager.AppSettings["SUMMARYEMAIL"],
                         "",
                         "",
                         debugSubject + ConfigurationManager.AppSettings["DEFAULTSUBJECT"] + " - " + DateTime.Now.ToString("MMMM dd, yyyy"),
                         body,
                         attachment,
                         ConfigurationManager.AppSettings["SMTPSERVER"],
                         true);
            }
        }

        public string GenerateEMailBody(string fileName, int totalAdjudications, string errors = "")
        {
            string template = File.ReadAllText(ConfigurationManager.AppSettings["SUMMARYTEMPLATE"]);

            template = template.Replace("[FILENAME]", Path.GetFileName(fileName));
            template = template.Replace("[TACOUNT]", totalAdjudications.ToString());
            template = template.Replace("[ERRORS]", errors);

            return template;
        }
    }
}