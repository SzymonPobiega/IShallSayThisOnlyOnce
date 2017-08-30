using NServiceBus;

namespace Messages
{
    public class ItemAdded : IEvent
    {
        public string OrderId { get; set; }
        public Filling Filling { get; set; }
        public int Quantity { get; set; }
    }
}