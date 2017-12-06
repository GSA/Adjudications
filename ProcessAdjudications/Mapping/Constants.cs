
namespace Adjudications.Mapping
{
    class AdjudicationConstants
    {
        private AdjudicationConstants() { }

        /// <summary>
        /// Column C
        /// </summary>
        public const string FIRST_NAME = "First Name";

        /// <summary>
        /// Column D
        /// </summary>
        public const string MIDDLE_NAME = "Middle Name";

        /// <summary>
        /// Column B
        /// </summary>
        public const string LAST_NAME = "Last Name";

        /// <summary>
        /// Column  E
        /// </summary>
        public const string SUFFIX = "Suffix";

        /// <summary>
        /// Column  G
        /// </summary>
        public const string SSN = "SSN";

        /// <summary>
        /// Column H
        /// </summary>
        public const string INVESTIGATION_TYPE_1 = "Investigation Type";

        /// <summary>
        /// Column I
        /// </summary>
        public const string INVESTIGATION_TYPE_2 = "Investigation Type (2)";

        /// <summary>
        /// Column J
        /// </summary>
        public const string INVESTIGATION_TYPE_3 = "Investigation Type (3)";

        /// <summary>
        /// Column T
        /// </summary>
        public const string INTERIM_ADJUDICATION_1 = "Interim Adjudication";

        /// <summary>
        /// Column U
        /// </summary>
        public const string INTERIM_ADJUDICATION_2 = "Interim Adjudication (2)";

        /// <summary>
        /// Column V
        /// </summary>
        public const string INTERIM_ADJUDICATION_3 = "Interim Adjudication (3)";

        /// <summary>
        /// Column W
        /// </summary>
        public const string INTERIM_ACTION_TAKEN_IF_NOT_CLEARED_TO_EOD_1 = "Interim Action Taken If Not Cleared to EOD";

        /// <summary>
        /// Column X
        /// </summary>
        public const string INTERIM_ACTION_TAKEN_IF_NOT_CLEARED_TO_EOD_2 = "Interim Action Taken If Not Cleared to EOD (2)";

        /// <summary>
        /// Column Y
        /// </summary>
        public const string INTERIM_ACTION_TAKEN_IF_NOT_CLEARED_TO_EOD_3 = "Interim Action Taken If Not Cleared to EOD (3)";

        /// <summary>
        /// Column W - Was Column Z
        /// </summary>
        public const string INTERIM_ADJUDICATION_DATE_1 = "Interim Adjudication Date";

        /// <summary>
        /// Column X - Was Column AA
        /// </summary>
        public const string INTERIM_ADJUDICATION_DATE_2 = "Interim Adjudication Date (2)";

        /// <summary>
        /// Column Y - Was Column AB
        /// </summary>
        public const string INTERIM_ADJUDICATION_DATE_3 = "Interim Adjudication Date (3)";

        /// <summary>
        /// Column AC - Was Column AF
        /// </summary>
        public const string SAC_ADJUDICATION_GSA = "SAC Adjudication (GSA)";

        /// <summary>
        /// Column AD - Was Column AG
        /// </summary>
        public const string SAC_ADJUDICATION_DATE = "SAC Adjudication Date";

        /// <summary>
        /// Column AE - Was Column AH
        /// </summary>
        public const string FINAL_ADJUDICATIVE_ACTION = "Final Adjudicative Action";

        /// <summary>
        /// Column AF - Was Column AI
        /// </summary>
        public const string FINAL_ADJUDICATION_DATE = "Final Adjudication Date";

        /// <summary>
        /// Column AJ
        /// </summary>
        public const string LESS_THAN_3_YEARS_IN_US = "Less than 3 years in U.S.";

        /// <summary>
        /// Column AK
        /// </summary>
        public const string DATE_OF_ENTRY = "Date of Entry";
    }

    /// <summary>
    /// Constants used to set the processed statuses that are not able to be returned from the stored procedure.
    /// </summary>
    class ProcessedStatusConstants
    {
        private ProcessedStatusConstants() { }

        public const string INDETERMINABLE = "Indeterminable";
        public const string RESCINDED = "Rescinded";
        public const string NOACTION = "No Action";
        public const string PORT_OF_ENTRY = "< 3 Year Error";
    }

    class InvestigationsConstants
    {
        private InvestigationsConstants() { }

        public const string INVESTIGATION_TYPE = "Invest Type";
        public const string TYPE_ACCESS = "Type Access";
        public const string NAC = "NAC";
        public const string NACI = "NACI";
        public const string FAVORABLE = "Favorable";
    }
}