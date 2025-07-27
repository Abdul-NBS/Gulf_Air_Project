namespace eBriefingWebApp.Models
{
    public class UserAccount
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string GivenName { get; set; }
        public string JobTitle { get; set; }
        public string OnPremisesSamAccountName { get; set; }
        public string OnPremisesUserPrincipalName { get; set; }
        public string Surname { get; set; }
        public string UserPrincipalName { get; set; }
    }
}