using System;
using System.Configuration;

namespace Adjudications
{
    class AdjudicationEMails
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static Suitability.SendNotification sendNotification;

        public AdjudicationEMails() { }

        //Need a try catch here!
        public void SendNotice(int id)
        {
            try
            {
                sendNotification = new Suitability.SendNotification(
                                    ConfigurationManager.AppSettings["DEFAULTEMAIL"],
                                    id,
                                    ConfigurationManager.ConnectionStrings["GCIMS"].ToString(),
                                    ConfigurationManager.AppSettings["SMTPSERVER"],
                                    ConfigurationManager.AppSettings["ONBOARDINGLOCATION"]);

                sendNotification.SendAdjudicationNotification();
            }
            catch (Exception ex)
            {
                log.Error("E-Mailing: " + ex.Message + " - " + ex.InnerException);
                //throw;
            }
            
        }
    }
}