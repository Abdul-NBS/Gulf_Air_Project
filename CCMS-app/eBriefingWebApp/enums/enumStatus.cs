using System.ComponentModel;

namespace eBriefingWebApp.enums
{
    public enum enumStatus
    {
        [Description("Added")]
        Added,
        [Description("Updated")]
        Updated,
        [Description("Exist")]
        Exist,
        [Description("Deleted")]
        Deleted,
        [Description("Error")]
        Error,
        [Description("Not Found")]
        NotFound
    }
}