using System;

namespace DeckRogue.GodotPort.Presentation;

public sealed class AdaptiveQuality
{
	private int _frames;

	private int _slow;

	public int AutoLevel { get; private set; }

	public int Level { get; private set; }

	public string Mode { get; private set; } = "auto";

	public void RestoreAuto(int level)
	{
		AutoLevel = Math.Clamp(level, 0, 2);
	}

	public void Select(string mode, bool resetAuto = true)
	{
		Mode = mode;
		if ((mode == "auto") & resetAuto)
		{
			AutoLevel = 0;
		}
		Level = mode switch
		{
			"high" => 0, 
			"standard" => 1, 
			"low" => 2, 
			_ => AutoLevel, 
		};
		Restart();
	}

	public void Restart()
	{
		_frames = 0;
		_slow = 0;
	}

	public bool Sample(float dt, bool inBattle, bool phone)
	{
		if (Mode != "auto")
		{
			return false;
		}
		if (!phone)
		{
			bool result = Level != 0;
			Level = 0;
			return result;
		}
		if (!inBattle || Level >= 2)
		{
			return false;
		}
		_frames++;
		if (_frames <= 90)
		{
			return false;
		}
		if (dt > 0.025f)
		{
			_slow++;
		}
		if (_frames < 270)
		{
			return false;
		}
		bool flag = _slow >= 36;
		if (flag)
		{
			AutoLevel = Math.Min(2, Level + 1);
			Level = AutoLevel;
		}
		Restart();
		return flag;
	}
}
