using System.Text;

namespace BigO.Types.Tests;

/// <summary>
///     Tests describing the behavior of the default <see cref="EmailAddress" /> value.
/// </summary>
public sealed class EmailAddressDefaultInstanceTests
{
    [Fact]
    public void Default_ToString_IsEmpty()
    {
        EmailAddress e = default;
        Assert.Equal(string.Empty, e.ToString());
    }

    [Fact]
    public void Default_Value_Throws_OnAccess()
    {
        EmailAddress e = default;
        Assert.Throws<InvalidOperationException>(() => _ = e.Value);
    }

    [Fact]
    public void Default_ToMailAddress_Throws()
    {
        EmailAddress e = default;
        Assert.Throws<InvalidOperationException>(() => e.ToMailAddress());
        Assert.Throws<InvalidOperationException>(() => e.ToMailAddress("name"));
        Assert.Throws<InvalidOperationException>(() => e.ToMailAddress("name", Encoding.UTF8));
    }
}