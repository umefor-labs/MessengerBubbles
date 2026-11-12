using MessengerBubbles.Core;

namespace MessengerBubbles.Tests;

public class RowTextTests
{
    [Theory]
    [InlineData("1 phút")]
    [InlineData("2 giờ")]
    [InlineData("3 ngày")]
    [InlineData("1 tuần")]
    [InlineData("5m")]
    [InlineData("2h")]
    [InlineData("Vừa xong")]
    [InlineData("Just now")]
    public void WithoutTime_DropsTimeLine(string time) =>
        Assert.Equal("An\nChào", RowText.WithoutTime($"An\nChào\n·\n{time}"));

    [Fact]
    public void WithoutTime_DropsEverythingAfterFirstDot() =>
        Assert.Equal("An\nMinh\nChào", RowText.WithoutTime("An\nMinh\nChào\n·\n5 phút\n·\nĐã gửi"));

    [Fact]
    public void Parse_IgnoresEverythingAfterFirstDot() =>
        Assert.Equal(("An", "Minh Chào"), RowText.Parse("An\nMinh\nChào\n·\n5 phút\n·\nĐã gửi"));

    [Fact]
    public void WithoutTime_DropsTimeOnSameLine() =>
        Assert.Equal("An\nChào", RowText.WithoutTime("An\nChào · 1 phút"));

    [Fact]
    public void WithoutTime_KeepsLastLineThatIsNotTime() =>
        Assert.Equal("An\nĂn cơm chưa?", RowText.WithoutTime("An\nĂn cơm chưa?"));

    [Fact]
    public void WithoutTime_DropsUnreadLabel() =>
        Assert.Equal("An\nChào", RowText.WithoutTime("An\nTin nhắn chưa đọc:\nChào\n·\n1 phút"));

    [Fact]
    public void Parse_SplitsNameAndPreview()
    {
        Assert.Equal(("An", "Chào bạn"), RowText.Parse("An\nChào bạn\n·\n1 phút"));
    }

    [Theory]
    [InlineData("Đang hoạt động")]
    [InlineData("Active now")]
    [InlineData("Hoạt động 5 phút trước")]
    [InlineData("Active 5m ago")]
    public void Parse_SkipsPresenceLabel(string label) =>
        Assert.Equal(("Mỹ Hạnh", "Đi xe"), RowText.Parse($"{label}\nMỹ Hạnh\nĐi xe\n·\n1 phút"));

    [Fact]
    public void WithoutTime_IgnoresPresenceChange() =>
        Assert.Equal(RowText.WithoutTime("An\nChào\n·\n1 phút"), RowText.WithoutTime("Đang hoạt động\nAn\nChào\n·\n1 phút"));

    [Fact]
    public void Parse_KeepsMessageThatMentionsActivity() =>
        Assert.Equal(("An", "Đang hoạt động gì đó"), RowText.Parse("An\nĐang hoạt động gì đó"));

    [Fact]
    public void Parse_Empty_DoesNotThrow()
    {
        Assert.Equal(("", ""), RowText.Parse(""));
    }

    [Theory]
    [InlineData("An\nChào\n·\n5 phút", 5)]
    [InlineData("An\nChào · 2 giờ", 120)]
    [InlineData("An\nChào\n·\n1 ngày\n·\nĐã xem", 1440)]
    [InlineData("An\nHi\n·\n3m", 3)]
    [InlineData("An\nHi\n·\n1w", 10080)]
    [InlineData("An\nChào\n·\nVừa xong", 0)]
    [InlineData("An\nHi · Just now", 0)]
    public void Age_ReadsTimeLabel(string text, int minutes) =>
        Assert.Equal(TimeSpan.FromMinutes(minutes), RowText.Age(text));

    [Theory]
    [InlineData("An\nChào")]
    [InlineData("An")]
    [InlineData("")]
    public void Age_NullWithoutTimeLabel(string text) => Assert.Null(RowText.Age(text));
}
