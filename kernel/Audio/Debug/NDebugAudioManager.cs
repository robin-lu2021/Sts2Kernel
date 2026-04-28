namespace MegaCrit.Sts2.Core.Audio.Debug;

public sealed class NDebugAudioManager
{
	public static NDebugAudioManager Instance { get; } = new NDebugAudioManager();

	private int _nextId;

	public int Play(string streamName, float volume = 1f, PitchVariance variance = PitchVariance.None)
	{
		return _nextId++;
	}

	public void StopAll()
	{
	}

	public void Stop(int id, float fadeTime = 0.5f)
	{
	}

	public void SetMasterAudioVolume(float linearVolume)
	{
	}

	public void SetSfxAudioVolume(float linearVolume)
	{
	}
}
