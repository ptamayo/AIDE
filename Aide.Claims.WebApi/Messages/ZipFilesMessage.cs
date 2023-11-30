namespace Aide.Hangfire.Common.Messages
{
    public class ZipFilesMessage
    {
        public string[] Files { get; set; }
        public string OutputFolder { get; set; }
        public string OutputFilename { get; set; }
        public string CallbackTypeId { get; set; }
        public string Callback { get; set; }
        public object Metadata { get; set; }
    }
}
