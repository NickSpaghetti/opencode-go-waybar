using Xunit;
using YamlDotNet.Serialization;
using OpencodeGoWaybar.Models.Usages;

namespace OpencodeGoWaybar.UnitTests.Contracts;

public class OpenApiContractTests
{
    private static readonly string ContractPath = Path.Combine(
        FindRepoRoot(),
        "contracts",
        "opencode-go-usage.openapi.yaml");

    [Fact]
    public void ShouldPublishTheContractFile()
    {
        // given
        Assert.True(
            File.Exists(ContractPath),
            $"OpenAPI contract is missing at {ContractPath}.");
    }

    [Fact]
    public void ShouldParseTheContractAsValidYaml()
    {
        // given
        File.Exists(ContractPath);
        var yaml = File.ReadAllText(ContractPath);
        var deserializer = new DeserializerBuilder().Build();
        var document = deserializer.Deserialize<object>(yaml);
        // then
        Assert.NotNull(document);
    }

    [Fact]
    public void ShouldDeclareTheUsageEndpointInTheContract()
    {
        // given
        File.Exists(ContractPath);
        var yaml = File.ReadAllText(ContractPath);
        // then
        Assert.Contains("/v1/usage", yaml);
        Assert.Contains("bearerAuth", yaml);
        Assert.Contains("UsageResponse", yaml);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "OpencodeGoWaybar.sln")))
        {
            current = current.Parent;
        }

        if (current is null)
        {
            throw new InvalidOperationException("Could not locate repository root from " + AppContext.BaseDirectory);
        }

        return current.FullName;
    }
}