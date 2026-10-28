namespace MediaDater;

public record RenameBatch(string Directory, IReadOnlyList<FileRenameInfo> Renames);