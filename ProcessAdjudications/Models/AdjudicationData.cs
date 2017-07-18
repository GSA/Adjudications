using System;

namespace Adjudications.Models
{
    /// <summary>
    /// Need to add in the other hidden fields that are updated with adjudications (ie: access type, it access, naci, nac values)
    /// </summary>
    class AdjudicationData
    {
        /// <summary>
        /// Person ID
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// First Name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Middle Name
        /// </summary>
        public string MiddleName { get; set; }

        /// <summary>
        /// Last Name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Suffix
        /// </summary>
        public string Suffix { get; set; }

        /// <summary>
        /// SSN
        /// </summary>
        public string SSN { get; set; }

        /// <summary>
        /// Hashed SSN
        /// </summary>
        public byte[] HashedSSN { get; set; }

        /// <summary>
        /// Date Of Entry
        /// </summary>
        public DateTime? DateOfEntry { get; set; }

        /// <summary>
        /// Less Than 3 Years In U.S.
        /// </summary>
        public string LessThan3YearsUS { get; set; }

        /// <summary>
        /// Should we update the Port Of Entry?
        /// </summary>
        public bool UpdatePortOfEntryDate { get; set; }

        /// <summary>
        /// Investigation type that is to be set in the GCIMS Table
        /// </summary>
        public string InvestigationType { get; set; }

        /// <summary>
        /// Investigation date that is to be set in the CGIMS Table
        /// </summary>
        public DateTime? InvestigationDate { get; set; }
        
        /// <summary>
        /// Holds the status based on what is returned from the database after processing the person
        /// </summary>
        public string AdjudicationStatus { get; set; }

        /// <summary>
        /// Whether or not an e-mail was sent
        /// </summary>
        public bool EMailRequested { get; set; }

        /// <summary>
        /// Holds the investigation data
        /// </summary>
        public Investigation Investigation { get; set; }
    }
}