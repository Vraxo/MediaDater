namespace LocalSendRenamer;

public record RenameBatch(string Directory, IReadOnlyList<FileRenameInfo> Renames);