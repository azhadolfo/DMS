using System.ComponentModel.DataAnnotations;

namespace Document_Management.Models.Enums
{
    public enum Departments
    {
        Accounting,

        [Display(Name = "Credit and Collection")]
        Credit_and_Collection,

        Engineering,
        Finance,

        [Display(Name = "HR And Admin/Legal")]
        HR_And_AdminOrLegal,

        Logistics,
        Marketing,

        [Display(Name = "Management Accounting")]
        Management_Accounting,

        MIS,
        Operation,
        RCD,

        [Display(Name = "Site Dev And Acquisition")]
        Site_Dev_And_Site_Acquisition,

        [Display(Name = "Trade and Supply")]
        Trade_and_Supply,

        [Display(Name = "Training And Compliance")]
        Training_And_Compliance
    }

    public enum Companies
    {
        [Display(Name = "Bienes De Oro")]
        Bienes_De_Oro,

        [Display(Name = "Filpride Resources Inc.")]
        Filpride_Resources_Inc,

        [Display(Name = "Malayan Maritime Services Inc.")]
        Malayan_Maritime_Services_Inc,

        [Display(Name = "Mobility Group Inc.")]
        Mobility_Group_Inc,

        [Display(Name = "MCY Logistics Inc.")]
        MCY_Logistics_Inc,

        Syvill
    }

    public enum Roles
    {
        Admin,
        User,
        Validator
    }

    public enum Categories
    {
        [Display(Name = "Contracts And Agreements")]
        Contracts_And_Agreements,

        [Display(Name = "Customer Profile")]
        Customer_Profile,

        [Display(Name = "Check Vouchers")]
        Check_Vouchers,

        [Display(Name = "Daily Cashier Report")]
        Daily_Cashier_Report,

        [Display(Name = "Delivery Documents")]
        Delivery_Documents,

        [Display(Name = "Employee Data Information")]
        Employee_Data_Information,

        [Display(Name = "End Of Month Reports")]
        End_Of_Month_Reports,

        [Display(Name = "Government Agency Documents")]
        Government_Agency_Documents,

        [Display(Name = "Internal Memo")]
        Internal_Memo,

        [Display(Name = "Transaction Advices")]
        Transaction_Advices,

        Others
    }

    public enum Area
    {
        Cubao,
        Eastwood,
        Market_Market
    }

    public enum DeliverySubCategories
    {
        [Display(Name = "Delivery Receipts")]
        Delivery_Receipts,

        [Display(Name = "Receiving Report")]
        Receiving_Report,

        [Display(Name = "Sales Invoice")]
        Sales_Invoice,

        [Display(Name = "Withdrawal Certificate")]
        Withdrawal_Certificate
    }

    public enum GovernmentSubCategories
    {
        [Display(Name = "All Other Compliance")]
        All_Other_Compliances,

        Certificates,
        LOAs,
        Permits
    }
}