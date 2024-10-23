namespace DeliveryService.Entities;

public class Order
{
    public Guid OrderId { get; set; }
    public double Weight { get; set; }
    public string Area { get; set; }
    public DateTime DeliveryTime { get; set; }
}