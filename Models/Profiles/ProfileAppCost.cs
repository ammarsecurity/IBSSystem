using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBSMobile.Models;

[Table("ProfileAppCosts")]
public class ProfileAppCost
{
    [Key]
    public int Id { get; set; }

    [Column("Profile")]
    public int ProfileId { get; set; }

    public decimal Price { get; set; }
}
