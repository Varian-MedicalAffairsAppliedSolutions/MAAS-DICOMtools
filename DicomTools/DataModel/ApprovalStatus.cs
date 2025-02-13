namespace DicomTools.DataModel
{
    public enum ApprovalStatus
    {
        Approved,   //Reviewer recorded that object met an implied criterion
        UnApproved, // No review of object has been recorded
        Rejected,   //Reviewer recorded that object failed to meet an implied criterion
        Unknown
    }

    public static class ApprovalStatusExtensions
    {
        public static ApprovalStatus FromDicomValue(string statusAsString)
        {
            switch (statusAsString)
            {
                case "APPROVED": return ApprovalStatus.Approved;
                case "UNAPPROVED": return ApprovalStatus.UnApproved;
                case "REJECTED": return ApprovalStatus.Rejected;
            }

            return ApprovalStatus.Unknown;
        }
    }
}
