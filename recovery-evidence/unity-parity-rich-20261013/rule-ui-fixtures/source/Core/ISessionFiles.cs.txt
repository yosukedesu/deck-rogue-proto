namespace DeckRogue.GodotPort.Core;

public interface ISessionFiles
{
	bool Exists(string path);

	string Read(string path);

	void Write(string path, string text);

	void Move(string from, string to);

	void Delete(string path);

	void EnsureDirectory(string path);
}
