using System;
using Godot;

namespace DeckRogue.GodotPort.Core;

public sealed class GodotSessionFiles : ISessionFiles
{
	private readonly int _ownerThread = System.Environment.CurrentManagedThreadId;

	private void RequireThread()
	{
		if (System.Environment.CurrentManagedThreadId != _ownerThread)
		{
			throw new InvalidOperationException("Godot file IO must stay on the main thread.");
		}
	}

	public bool Exists(string path)
	{
		RequireThread();
		return FileAccess.FileExists(path);
	}

	public string Read(string path)
	{
		RequireThread();
		using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			throw new InvalidOperationException($"Cannot read {path}: {FileAccess.GetOpenError()}");
		}
		return file.GetAsText();
	}

	public void Write(string path, string text)
	{
		RequireThread();
		using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
		if (file == null)
		{
			throw new InvalidOperationException($"Cannot write {path}: {FileAccess.GetOpenError()}");
		}
		file.StoreString(text);
		file.Flush();
		if (file.GetError() != Error.Ok)
		{
			throw new InvalidOperationException($"Cannot finish {path}: {file.GetError()}");
		}
	}

	public void Move(string from, string to)
	{
		RequireThread();
		Error error = DirAccess.RenameAbsolute(from, to);
		if (error != Error.Ok)
		{
			throw new InvalidOperationException($"Cannot rotate save {from}: {error}");
		}
	}

	public void Delete(string path)
	{
		RequireThread();
		Error error = DirAccess.RemoveAbsolute(path);
		if (error != Error.Ok)
		{
			throw new InvalidOperationException($"Cannot remove {path}: {error}");
		}
	}

	public void EnsureDirectory(string path)
	{
		RequireThread();
		Error error = DirAccess.MakeDirRecursiveAbsolute(path);
		if (error != Error.Ok && error != Error.AlreadyExists)
		{
			throw new InvalidOperationException($"Cannot create {path}: {error}");
		}
	}
}
