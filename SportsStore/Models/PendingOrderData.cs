namespace SportsStore.Models {

    /// <summary>
    /// Serializable data for an order pending Stripe payment. Stored in session during checkout redirect.
    /// </summary>
    public class PendingOrderData {
        public string? Name { get; set; }
        public string? Line1 { get; set; }
        public string? Line2 { get; set; }
        public string? Line3 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public string? Country { get; set; }
        public bool GiftWrap { get; set; }
        public List<PendingOrderLine> Lines { get; set; } = new();
    }

    public class PendingOrderLine {
        public long ProductID { get; set; }
        public int Quantity { get; set; }
    }
}
