using System.ComponentModel;

namespace Aide.Hangfire.Domain.Enumerations
{
    public enum EnumDocumentOrientationId
    {
        [Description("Not Applicable")]
        NA = 0,

        [Description("Portrait")]
        Portrait = 1,

        [Description("Landscape")]
        Landscape = 2
    }
}
