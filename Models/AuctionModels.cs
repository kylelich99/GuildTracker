using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GuildTracker.Models;

public class AuctionItemType
{
    public string Name { get; set; } = string.Empty;
    public int MaxPerPlayer { get; set; } = 1;
}

public class AuctionEvent
{
    public string EventName { get; set; } = string.Empty;
    public List<AuctionItemAllocation> Items { get; set; } = new();
}

public class AuctionItemAllocation
{
    public string ItemName { get; set; } = string.Empty;
    public int TotalAvailable { get; set; }
}

public class AuctionResult
{
    [BsonId]
    public ObjectId Id { get; set; }
    public DateTime WeekStart { get; set; }
    public string EventName { get; set; } = string.Empty;
    public List<AuctionDistribution> Distributions { get; set; } = new();
    public List<AuctionItemAllocation> Quantities { get; set; } = new();
}

public class AuctionDistribution
{
    public string MemberId { get; set; } = string.Empty;
    public string MemberIGN { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
