using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureMarketMvc.Models;

public sealed class Order
{
    public int Id { get; set; }

    [Required, MaxLength(32)]
    public string OrderNumber { get; set; } = string.Empty;

    public string AppUserId { get; set; } = string.Empty;
    public AppUser? AppUser { get; set; }

    public DateTime OrderedAtUtc { get; set; } = DateTime.UtcNow;

    public Address ShippingAddress { get; set; } = new();

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public OrderStatus OrderStatus { get; set; } = OrderStatus.New;

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Shipping { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Tax { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }

    [MaxLength(30)]
    public string PaymentProvider { get; set; } = "stripe";

    [MaxLength(120)]
    public string? PaymentSessionId { get; set; }

    [MaxLength(120)]
    public string? PaymentIntentId { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
