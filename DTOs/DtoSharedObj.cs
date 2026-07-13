namespace IBSMobile.DTOs
{

    public class SASAppUserResponse
    {
        public decimal userIndex { get; set; } = 0;
        public decimal customerIndex { get; set; } = 0;
        public string userId { get; set; }
        public string displayName { get; set; }
        public string mobileNumber { get; set; }
        public string? userNotes { get; set; }
        public string password { get; set; }
        public Customer? customer { get; set; }

        public int accountIndex { get; set; }
        public string accountStatus { get; set; }
        public string accountName { get; set; }

        public bool canExtendUser { get; set; }
        public bool canRefill { get; set; }
        public bool canDelete { get; set; }
        public bool canChangeAccount { get; set; }
        public bool userActive { get; set; }
        public bool userActiveManage { get; set; }

        public string onlineStatusColor { get; set; }
        public string onlineStatus { get; set; }
        public string loginFrom { get; set; }
        public string ipAddress { get; set; }
        public string callerMAC { get; set; }
        public string onlineTime { get; set; }
        public string manualExpirationDate { get; set; }
        public string lastRefill { get; set; }
        public string affiliateName { get; set; }
        public string agentName { get; set; }

        public DtoFtthOtherDetails? ftthOtherData { get; set; }
    }




}
