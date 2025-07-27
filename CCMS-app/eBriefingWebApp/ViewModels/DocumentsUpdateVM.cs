using System;
using System.Linq;

namespace eBriefingWebApp.ViewModels
{
    public class DocumentsUpdateVM
    {
        public static bool IsCrewAcknowledged(string StaffNumber)
        {
            CSMEntities db = new CSMEntities();

            try
            {
                var getLastVersion = db.Doc_Versions.OrderByDescending(a => a.DueDate).FirstOrDefault();

                var chkCrewAcknowledged = db.Doc_Crew_Log.Where(a => a.StaffNumber == StaffNumber && a.VersionId == getLastVersion.Id).FirstOrDefault();

                if (chkCrewAcknowledged == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {

            }

            return false;
        }
    }
}