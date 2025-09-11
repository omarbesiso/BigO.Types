namespace BigO.Types.Tests;

public class EmailAddressComparisonTests
{
    [Fact]
    public void CompareTo_OrdersByAddressThenDisplayName_NullFirst()
    {
        var a = EmailAddress.From("a@example.com"); // null display
        var b = EmailAddress.From("a@example.com", "Bob");
        var c = EmailAddress.From("b@example.com", "Alice");

        Assert.True(a.CompareTo(b) < 0); // null display sorts before non-null for the same address
        Assert.True(b.CompareTo(c) < 0); // a@example.com < b@example.com
        Assert.True(c.CompareTo(a) > 0);
    }

    [Fact]
    public void Sorting_IsDeterministic_AndConsistentWithEquality()
    {
        var items = new List<EmailAddress>
        {
            EmailAddress.From("B@example.com", "Zed"),
            EmailAddress.From("a@example.com"),
            EmailAddress.From("b@example.com", "Ann"),
            EmailAddress.From("A@example.com", "Bob")
        };

        items.Sort();

        Assert.Equal("a@example.com", items[0].Address);
        Assert.Null(items[0].DisplayName);

        Assert.Equal("a@example.com", items[1].Address);
        Assert.Equal("Bob", items[1].DisplayName);

        Assert.Equal("b@example.com", items[2].Address);
        Assert.Equal("Ann", items[2].DisplayName);

        Assert.Equal("b@example.com", items[3].Address);
        Assert.Equal("Zed", items[3].DisplayName);
    }
}