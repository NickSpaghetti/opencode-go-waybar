using Xunit;
using YamlDotNet.Serialization;

namespace OpencodeGoWaybar.UnitTests.Contracts;

public class OpenApiContractTests
{
    private static readonly string ContractPath = Path.Combine(
        FindRepoRoot(),
        "contracts",
        "opencode-go-usage.openapi.yaml");

    [Fact]
    public void ContractFileExists()
    {
        Assert.True(
            File.Exists(ContractPath),
            $"OpenAPI contract is missing at {ContractPath}.");
    }

    [Fact]
    public void ContractParsesAsValidYaml()
    {
        File.Exists(ContractPath);
        var yaml = File.ReadAllText(ContractPath);
        var deserializer = new DeserializerBuilder().Build();
        var document = deserializer.Deserialize<object>(yaml);
        Assert.NotNull(document);
    }

    [Fact]
    public void ContractDeclaresTheUsageEndpoint()
    {
        File.Exists(ContractPath);
        var yaml = File.ReadAllText(ContractPath);
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