using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "JigsawSoundData", menuName = "ScriptableObjects/JigsawSoundData")]
public class JigsawSoundData : ScriptableObject
{
    [System.Serializable]
    public struct SoundEntry
    {
        public SFXId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    public List<SoundEntry> soundEntries = new List<SoundEntry>();
}