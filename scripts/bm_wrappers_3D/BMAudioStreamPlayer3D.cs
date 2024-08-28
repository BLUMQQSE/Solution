using Godot;
using System;
[GlobalClass]
public partial class BMAudioStreamPlayer3D : AudioStreamPlayer3D, INetworkData
{
    
    public bool NetworkUpdate { get; set; } = true;
    private static readonly string _AudioData = "AD";
    private static readonly string _PlayCount = "PC";
    public AudioData AudioData { get; private set; }
    [Export]int playCount = 1;

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
        if(AudioData != null) 
            data[_AudioData].Set(AudioData.ResourcePath.RemovePath());

        data[_PlayCount].Set(playCount);
        return this.CalculateNetworkReturn(data, ignoreThisUpdateOccurred);
    }

    public void DeserializeNetworkData(JsonValue data)
    {
        if (data[_AudioData].IsValue)
        {
            if (AudioData != null)
            {
                if(AudioData.ResourcePath.RemovePath() != data[_AudioData].AsString())
                    ApplyAudioData(GD.Load<AudioData>(ReferenceManager.Instance.GetResourcePath(data[_AudioData].AsString())));
            }
            else
                ApplyAudioData(GD.Load<AudioData>(ReferenceManager.Instance.GetResourcePath(data[_AudioData].AsString())));
        }
        if(playCount < data[_PlayCount].AsInt())
        {
            int dif = data[_PlayCount].AsInt() - playCount;
            for(int i = 0; i < dif; i++)
            {
                PlaySound();
            }
        }
    }

    public void ApplyAudioData(AudioData data)
    {
        AudioData = data;
        Stream = data.AudioStream;
        MaxDb = data.MaxDB;
        MaxPolyphony = data.MaxSimultaneousInstances;
        VolumeDb = data.VolumeDB;
        AttenuationModel = data.AttenuationModel;
        Autoplay = true;
        MaxDistance = data.MaxDistance;
        PitchScale = data.PitchScale;
        UnitSize = data.UnitSize;
    }

}
