using MayNho.Domain;

namespace MayNho.Unit;

public class TitleTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsBlankTitles(string? value) =>
        Assert.Throws<ArgumentException>(() => Title.Required(value));

    [Fact]
    public void TrimsVietnameseTitle() =>
        Assert.Equal("Nộp báo cáo", Title.Required("  Nộp báo cáo  "));

    [Fact]
    public void AcceptsMaximumLength() => Assert.Equal(200, Title.Required(new string('x', 200)).Length);

    [Fact]
    public void RejectsTooLongTitle() =>
        Assert.Throws<ArgumentException>(() => Title.Required(new string('x', 201)));

    [Fact]
    public void ListTitleHasSeparateLimit() =>
        Assert.Throws<ArgumentException>(() => Title.Required(new string('x', 81), 80));
}
