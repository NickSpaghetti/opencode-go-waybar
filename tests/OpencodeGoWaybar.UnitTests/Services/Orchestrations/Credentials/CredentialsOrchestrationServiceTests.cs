using NSubstitute;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Services.Foundations.Secrets;
using OpencodeGoWaybar.Services.Foundations.OpenCodeAuth;
using OpencodeGoWaybar.Services.Orchestrations.Credentials;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Orchestrations.Credentials;

public sealed class CredentialsOrchestrationServiceTests
{
    [Fact]
    public void ShouldPreferTheConfiguredKeyWhenSourceIsAuto()
    {
        // given
        var service = CreateService(ApiKeySource.Auto, configured: "sk-configured", fromAuthFile: "sk-auth-file");

        // then
        Assert.Equal("sk-configured", service.RetrieveSecrets().ApiKey);
    }

    [Fact]
    public void ShouldFallBackToTheAuthFileWhenSourceIsAutoAndNoKeyIsConfigured()
    {
        // given
        var service = CreateService(ApiKeySource.Auto, configured: null, fromAuthFile: "sk-auth-file");

        // then
        Assert.Equal("sk-auth-file", service.RetrieveSecrets().ApiKey);
    }

    [Fact]
    public void ShouldTreatABlankConfiguredKeyAsAbsentWhenSourceIsAuto()
    {
        // given
        var service = CreateService(ApiKeySource.Auto, configured: "   ", fromAuthFile: "sk-auth-file");

        // then
        Assert.Equal("sk-auth-file", service.RetrieveSecrets().ApiKey);
    }

    [Fact]
    public void ShouldNotConsultTheAuthFileWhenSourceIsEnvironment()
    {
        // given
        var authService = Substitute.For<IOpenCodeAuthService>();
        authService.RetrieveApiKey().Returns("sk-auth-file");

        var service = CreateService(ApiKeySource.Environment, configured: null, authService: authService);

        // then
        Assert.Null(service.RetrieveSecrets().ApiKey);
        authService.DidNotReceive().RetrieveApiKey();
    }

    [Fact]
    public void ShouldIgnoreTheConfiguredKeyWhenSourceIsAuthFile()
    {
        // given
        var service = CreateService(ApiKeySource.AuthFile, configured: "sk-configured", fromAuthFile: "sk-auth-file");

        // then
        Assert.Equal("sk-auth-file", service.RetrieveSecrets().ApiKey);
    }

    private static CredentialsOrchestrationService CreateService(
        ApiKeySource source,
        string? configured,
        string? fromAuthFile = null,
        IOpenCodeAuthService? authService = null)
    {
        var secretsService = Substitute.For<ISecretsService>();
        secretsService.RetrieveSecrets()
            .Returns(new OpenCodeGoSecrets { ApiKey = configured });

        if (authService is null)
        {
            authService = Substitute.For<IOpenCodeAuthService>();
            authService.RetrieveApiKey().Returns(fromAuthFile);
        }

        return new CredentialsOrchestrationService(
            secretsService,
            authService,
            new OpenCodeGoOptions { ApiKeySource = source });
    }
}
