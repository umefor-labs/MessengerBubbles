using MessengerBubbles.Core;

namespace MessengerBubbles.Tests;

public class TextsTests
{
    [Theory]
    [InlineData("vi-VN", AppLanguage.Vietnamese)]
    [InlineData("vi", AppLanguage.Vietnamese)]
    [InlineData("en-US", AppLanguage.English)]
    [InlineData("fr-FR", AppLanguage.English)]
    [InlineData("", AppLanguage.English)]
    public void DefaultFor_PicksVietnameseOnlyForVietnamese(string culture, AppLanguage expected) =>
        Assert.Equal(expected, Texts.DefaultFor(culture));

    [Fact]
    public void For_ReturnsThatLanguage()
    {
        Assert.Same(Texts.English, Texts.For(AppLanguage.English));
        Assert.Same(Texts.Vietnamese, Texts.For(AppLanguage.Vietnamese));
    }

    [Fact]
    public void EveryTextIsFilledInBothLanguages()
    {
        foreach (var p in typeof(Texts).GetProperties().Where(p => p.PropertyType == typeof(string)))
        {
            Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(Texts.English)), p.Name);
            Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(Texts.Vietnamese)), p.Name);
        }
    }
}
