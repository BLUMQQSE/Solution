using Godot;
using System;

public partial class BMAudioStreamPlayer : AudioStreamPlayer, INetworkData
{
        
    public bool NetworkUpdate { get; set; } = true;
    public AudioData AudioData { get; private set; }
    [Export] int playCount = 1;

    public void PlaySound()
    {
        playCount++;
        Play();
    }

    public JsonValue SerializeNetworkData(bool forceReturn = false, bool ignoreThisUpdateOccurred = false)
    {
        if (!this.ShouldUpdate(forceReturn))
            return null;
        JsonValue data = new JsonValue();
        if (AudioData != null)
            data["AD"].Set(AudioData.ResourcePath.RemovePath());

        data["PC"].Set(playCount);

        return this.CalculateNetworkReturn(data, ignoreThisUpdateOccurred);
    }

    public void DeserializeNetworkData(JsonValue data)
    {
        if (data["AD"].IsValue)
        {
            if (AudioData != null)
            {
                if (AudioData.ResourcePath.RemovePath() != data["AD"].AsString())
                    ApplyAudioData(GD.Load<AudioData>(ReferenceManager.Instance.GetResourcePath(data["AD"].AsString())));
            }
            else
                ApplyAudioData(GD.Load<AudioData>(ReferenceManager.Instance.GetResourcePath(data["AD"].AsString())));
        }
        if (playCount < data["PC"].AsInt())
        {
            int dif = data["PC"].AsInt() - playCount;
            for (int i = 0; i < dif; i++)
            {
                PlaySound();
            }
        }
    }

    public void ApplyAudioData(AudioData data)
    {
        AudioData = data;
        Stream = data.AudioStream;
        VolumeDb = data.VolumeDB;
        MaxPolyphony = data.MaxSimultaneousInstances;
        Autoplay = true;
        PitchScale = data.PitchScale;
    }

}
