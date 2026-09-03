namespace OpencodeGoWaybar.Models.Processes;

/// <summary>
/// One OpenCode process and the chain of processes that spawned it, ordered
/// nearest first and starting with the process itself.
///
/// OpenCode never owns a window of its own — it is a terminal program or, when
/// driven over ACP, a child of the editor. Its lineage is therefore the only
/// route from the process to the window a compositor can place on a workspace,
/// and the order matters: the nearest ancestor that owns a window is the one
/// the session is actually displayed in.
/// </summary>
internal sealed record OpenCodeProcessLineage(int ProcessId, IReadOnlyList<int> LineageProcessIds);
