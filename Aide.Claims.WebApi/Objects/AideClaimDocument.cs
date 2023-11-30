namespace Aide.Claims.WebApi.Objects
{
    /// <summary>
    /// This class is a simplified version of ClaimProbatoryDocument.
    /// </summary>
    public class AideClaimDocument
    {
        /// <summary>
        /// This is the ID of the probatory document (see ProbatoryDocument.Id).
        /// </summary>
        public int ProbatoryDocumentId { get; set; }

        /// <summary>
        /// This is the name of the probatory document (see ProbatoryDocument.Name).
        /// </summary>
        public string ProbatoryDocumentName { get; set; }

        /// <summary>
        /// This is the ID of the media file (see Media.Id).
        /// </summary>
        public int? MediaId { get; set; }

        /// <summary>
        /// This is the group ID of the probatory document (see ClaimProbatoryDocument.GroupId).
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// This is NOT a line item ID but a line item number. It only applies when the group ID is equals to 3-Cristal.
        /// For every item in the order it will be a set of images in group ID 3=Cristal.
        /// </summary>
        public int? ClaimItemId { get; set; }
    }
}
