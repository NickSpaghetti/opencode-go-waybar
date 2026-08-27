using System.ComponentModel;
using System.Diagnostics;
using NSubstitute;
using OpencodeGoWaybar.Brokers.Loggings;
using OpencodeGoWaybar.Brokers.Processes;
using OpencodeGoWaybar.Models.Configurations;
using OpencodeGoWaybar.Services.Foundations.Processes;
using OpencodeGoWaybar.Models.Processes.Exceptions;
using Xunit;

namespace OpencodeGoWaybar.UnitTests.Services.Foundations.Processes;

/// <summary>
/// The process broker hands over native <see cref="Process"/> objects, which cannot be
/// constructed with a chosen name — a native model that leaks by design (The Standard 1.6,
/// 1.7.1). The "an opencode process is running" branch is therefore exercised through the
/// override here and against the real process table in ProcessBrokerTests; every other
/// branch is covered below.
/// </summary>
public sealed partial class ProcessServiceTests
{

    private static ProcessService CreateService(
        IProcessBroker broker,
        ILoggingBroker loggingBroker,
        bool? processPresentOverride = null) =>
        new(
            broker,
            loggingBroker,
            new OpenCodeGoOptions { ProcessPresentOverride = processPresentOverride });
}
