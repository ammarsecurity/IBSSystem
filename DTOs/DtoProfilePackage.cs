namespace IBSMobile.DTOs;

public class DtoProfilePackage
{
    public int Id { get; set; }
    public int AccIndex { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal BuyCost { get; set; }
    public decimal Price { get; set; }
}
