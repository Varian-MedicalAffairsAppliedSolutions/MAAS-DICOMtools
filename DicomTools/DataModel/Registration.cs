using FellowOakDicom;

namespace DicomTools.DataModel
{
    public class Registration : Instance
    {
        private Registration(DicomDataset dataset, DicomUID frameOfReferenceUid1, DicomUID frameOfReferenceUid2)
            : base(dataset, ModalityType.Registration)
        {
            Label = dataset.GetSingleValue<string>(DicomTag.ContentLabel);
            FrameOfReferenceUid1 = frameOfReferenceUid1;
            FrameOfReferenceUid2 = frameOfReferenceUid2;
        }

        public override string Label { get; }

        public DicomUID FrameOfReferenceUid1 { get; }

        public DicomUID FrameOfReferenceUid2 { get; }

        public static Registration Create(DicomDataset dataset)
        {
            var registrationSequence = dataset.GetSequence(DicomTag.RegistrationSequence);
            var frameOfReferenceUid1 = registrationSequence.Items[0].GetSingleValue<DicomUID>(DicomTag.FrameOfReferenceUID);
            var frameOfReferenceUid2 = registrationSequence.Items[1].GetSingleValue<DicomUID>(DicomTag.FrameOfReferenceUID);

            return new Registration(dataset, frameOfReferenceUid1, frameOfReferenceUid2);
        }
    }
}
