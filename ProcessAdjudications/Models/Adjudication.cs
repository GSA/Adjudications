using System;

namespace Adjudications.Models
{
    class Adjudication
    {
        /// <summary>
        /// Investigation type that is to be set in the GCIMS Table
        /// </summary>
        public string InvestigationType { get; set; }

        /// <summary>
        /// Using this since I will not know which date to place in the parameters for the query once the data is passed over.
        /// Might be able to do something different later.
        /// </summary>
        public string InvestigationDate { get; set; }

        /// <summary>
        /// Column C
        /// </summary>
        public string FirstName { get; set; } 
       
        /// <summary>
        /// Column D
        /// </summary>
        public string MiddleName { get; set; }

        /// <summary>
        /// Column B
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Column  E
        /// </summary>
        public string Suffix { get; set; }

        /// <summary>
        /// Column  G
        /// </summary>
        public string SSN { get; set; }

        /// <summary>
        /// Column H
        /// </summary>
        public string InvestigationType1 { get; set; }

        /// <summary>
        /// Column I
        /// </summary>
        public string InvestigationType2 { get; set; }

        /// <summary>
        /// Column J
        /// </summary>
        public string InvestigationType3 { get; set; }

        /// <summary>
        /// Column T
        /// </summary>
        public string InterimAdjudication1 { get; set; }

        /// <summary>
        /// Column U
        /// </summary>
        public string InterimAdjudication2 { get; set; }

        /// <summary>
        /// Column V
        /// </summary>
        public string InterimAdjudication3 { get; set; }

        /// <summary>
        /// Column W
        /// </summary>
        public string InterimActionTakenIfNotClearedToEOD1 { get; set; }

        /// <summary>
        /// Column X
        /// </summary>
        public string InterimActionTakenIfNotClearedToEOD2 { get; set; }

        /// <summary>
        /// Column Y
        /// </summary>
        public string InterimActionTakenIfNotClearedToEOD3 { get; set; }

        /// <summary>
        /// Column Z
        /// </summary>
        public DateTime? InterimAdjudicationDate1 { get; set; }

        /// <summary>
        /// Column AA
        /// </summary>
        public DateTime? InterimAdjudicationDate2 { get; set; }

        /// <summary>
        /// Column AB
        /// </summary>
        public DateTime? InterimAdjudicationDate3 { get; set; }

        /// <summary>
        /// Column AF
        /// </summary>
        public string SACAdjudicationGSA { get; set; }

        /// <summary>
        /// Column AG
        /// </summary>
        public DateTime? SACAdjudicationDate { get; set; }

        /// <summary>
        /// Column AH
        /// </summary>
        public string FinalAdjudicativeAction { get; set; }

        /// <summary>
        /// Column AI
        /// </summary>
        public DateTime? FinalAdjudicationDate { get; set; }

        /// <summary>
        /// Column AJ
        /// Might need to change to a boolean
        /// </summary>
        public string LessThan3YearsInUS { get; set; }

        /// <summary>
        /// Column AK
        /// </summary>
        public DateTime? DateOfEntry { get; set; }

        /// <summary>
        ///     Holds pers_status 
        /// </summary>
        public string Status { get; set; }
    }
}