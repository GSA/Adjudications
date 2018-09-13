using Adjudications.Models;
using CsvHelper.Configuration;

namespace Adjudications.Mapping
{
    sealed class AdjudicationMapping : ClassMap<Adjudication>
    {
        /// <summary>
        /// Used to map the adjudication file to the object
        /// Mapping is done based on names of headers.
        /// By doing this if the headers are out of order the file can still be processed.
        /// </summary>
        public AdjudicationMapping()
        {
            //Name
            Map(m => m.FirstName).Name(AdjudicationConstants.FIRST_NAME);
            Map(m => m.MiddleName).Name(AdjudicationConstants.MIDDLE_NAME);
            Map(m => m.LastName).Name(AdjudicationConstants.LAST_NAME);
            Map(m => m.Suffix).Name(AdjudicationConstants.SUFFIX);
            Map(m => m.SSN).Name(AdjudicationConstants.SSN);

            //Investigation Types
            Map(m => m.InvestigationType1).Name(AdjudicationConstants.INVESTIGATION_TYPE_1);
            Map(m => m.InvestigationType2).Name(AdjudicationConstants.INVESTIGATION_TYPE_2);
            Map(m => m.InvestigationType3).Name(AdjudicationConstants.INVESTIGATION_TYPE_3);

            //Interim Adjudications
            Map(m => m.InterimAdjudication1).Name(AdjudicationConstants.INTERIM_ADJUDICATION_1);
            Map(m => m.InterimAdjudication2).Name(AdjudicationConstants.INTERIM_ADJUDICATION_2);
            Map(m => m.InterimAdjudication3).Name(AdjudicationConstants.INTERIM_ADJUDICATION_3);

            //Interim Action Taken If Not Cleared To EOD
            Map(m => m.InterimActionTakenIfNotClearedToEOD1).Name(AdjudicationConstants.INTERIM_ACTION_TAKEN_IF_NOT_CLEARED_TO_EOD_1);
            Map(m => m.InterimActionTakenIfNotClearedToEOD2).Name(AdjudicationConstants.INTERIM_ACTION_TAKEN_IF_NOT_CLEARED_TO_EOD_2);
            Map(m => m.InterimActionTakenIfNotClearedToEOD3).Name(AdjudicationConstants.INTERIM_ACTION_TAKEN_IF_NOT_CLEARED_TO_EOD_3);

            //Interim Adjudication Date
            Map(m => m.InterimAdjudicationDate1).Name(AdjudicationConstants.INTERIM_ADJUDICATION_DATE_1);
            Map(m => m.InterimAdjudicationDate2).Name(AdjudicationConstants.INTERIM_ADJUDICATION_DATE_2);
            Map(m => m.InterimAdjudicationDate3).Name(AdjudicationConstants.INTERIM_ADJUDICATION_DATE_3);

            //SAC Adjudication
            Map(m => m.SACAdjudicationGSA).Name(AdjudicationConstants.SAC_ADJUDICATION_GSA);
            Map(m => m.SACAdjudicationDate).Name(AdjudicationConstants.SAC_ADJUDICATION_DATE);

            //Final Adjudication
            Map(m => m.FinalAdjudicativeAction).Name(AdjudicationConstants.FINAL_ADJUDICATIVE_ACTION);
            Map(m => m.FinalAdjudicationDate).Name(AdjudicationConstants.FINAL_ADJUDICATION_DATE);

            //3 Year Residency
            Map(m => m.LessThan3YearsInUS).Name(AdjudicationConstants.LESS_THAN_3_YEARS_IN_US);
            Map(m => m.DateOfEntry).Name(AdjudicationConstants.DATE_OF_ENTRY);
        }
    }

    sealed class SummaryMapping : ClassMap<AdjudicationData>
    {
        /// <summary>
        /// Used to map the results to the summary object
        /// </summary>
        public SummaryMapping()
        {
            Map(m => m.FirstName);
            Map(m => m.MiddleName);
            Map(m => m.LastName);
            Map(m => m.Suffix);

            Map(m => m.InvestigationType);
            Map(x => x.InvestigationDate);
            Map(x => x.AdjudicationStatus);
            Map(x => x.EMailRequested);

            Map(m => m.ID);
            Map(m => m.Status);
        }
    }

    sealed class InvestigationMapping : ClassMap<Investigation>
    {
        /// <summary>
        /// Used to map investigation data to the investigation object
        /// </summary>
        public InvestigationMapping()
        {
            Map(m => m.InvestigationType).Index(0);
            Map(m => m.TypeAccess).Index(1);
            Map(m => m.isNAC).Index(2);
            Map(m => m.isNACI).Index(3);
            Map(m => m.isFavorable).Index(4);
        }
    }
}