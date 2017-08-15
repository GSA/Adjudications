using System;
using System.Configuration;

namespace Adjudications
{
    class AdjudicationEMails
    {
        //Reference to logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //suitability object
        private static Suitability.SendNotification sendNotification;

        //ctor
        public AdjudicationEMails() { }

        /// <summary>
        /// Send notification using suitability dll
        /// </summary>
        /// <param name="id"></param>
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
            //catch all exceptions and log message
            catch (Exception ex)
            {
                log.Error("E-Mailing: " + ex.Message + " - " + ex.InnerException);
                //throw;
            }
        }
    }
}