namespace MediaDater;

internal record RenameBatch(string Directory, IReadOnlyList<FileRenameInfo> Renames);