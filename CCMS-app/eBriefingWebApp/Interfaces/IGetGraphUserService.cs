using eBriefingWebApp.Models;
using System.Threading.Tasks;

namespace eBriefingWebApp.Interfaces
{
    public interface IGetGraphUserService
    {
        Task<UserAccount> GetAccountInfo(string userId);
    }
}