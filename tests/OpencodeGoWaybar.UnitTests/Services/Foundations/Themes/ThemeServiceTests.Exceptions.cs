using NSubstitute;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Brokers.Themes;
using OpencodeGoWaybar.Models.Themes.Exceptions;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.Themes;

public sealed partial class ThemeServiceTests
{
    [Fact]
    public async Task ShouldThrowThemeUnavailableExceptionIfTheStyleSheetIsInaccessibleAndLogItAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var themeBroker = Substitute.For<IWaybarThemeBroker>();
        themeBroker.ReadTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<string>>(_ => throw new UnauthorizedAccessException("denied"));

        // when and then
        await Assert.ThrowsAsync<ThemeUnavailableException>(() =>
            CreateService(themeBroker, loggingBroker)
                .RetrievePaletteAsync(CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<ThemeUnavailableException>());
    }

    [Fact]
    public async Task ShouldThrowThemeServiceExceptionIfServiceErrorOccursAndLogItAsync()
    {
        // given
        var loggingBroker = Substitute.For<ILoggingBroker>();
        var themeBroker = Substitute.For<IWaybarThemeBroker>();
        themeBroker.ReadTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<string>>(_ => throw new FormatException("unexpected"));

        // when and then
        await Assert.ThrowsAsync<ThemeServiceException>(() =>
            CreateService(themeBroker, loggingBroker)
                .RetrievePaletteAsync(CancellationToken.None).AsTask());
        await loggingBroker.Received(1).LogErrorAsync(Arg.Any<ThemeServiceException>());
    }
}
