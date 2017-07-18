
namespace Adjudications.Models
{
    class Investigation
    {
        /// <summary>
        /// Investigation Type - Used as a lookup for valid investigation types
        /// </summary>
        public string InvestigationType { get; set; }

        /// <summary>
        /// Access Type
        /// </summary>
        public string TypeAccess { get; set; }

        /// <summary>
        /// Is the investigation a NAC?
        /// </summary>
        public bool isNAC { get; set; }

        /// <summary>
        /// Is the investigation a NACI?
        /// </summary>
        public bool isNACI { get; set; }

        /// <summary>
        /// Is the investigation favorable?
        /// </summary>
        public bool isFavorable { get; set; }
    }
}