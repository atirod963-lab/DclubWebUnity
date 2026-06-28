using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Dclub/Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [Serializable]
    public class BGMEntry
    {
        public BGMId id;
        public AudioClip clip;
    }

    [Serializable]
    public class SFXEntry
    {
        public SFXId id;
        public AudioClip clip;
    }

    [Header("Background Music")]
    public List<BGMEntry> bgmTracks = new List<BGMEntry>();

    [Header("Sound Effects")]
    public List<SFXEntry> sfxClips = new List<SFXEntry>();

    public AudioClip GetBGM(BGMId id)
    {
        if (id == BGMId.None)
            return null;

        for (int i = 0; i < bgmTracks.Count; i++)
        {
            if (bgmTracks[i].id == id)
                return bgmTracks[i].clip;
        }

        return null;
    }

    public AudioClip GetSFX(SFXId id)
    {
        if (id == SFXId.None)
            return null;

        for (int i = 0; i < sfxClips.Count; i++)
        {
            if (sfxClips[i].id == id)
                return sfxClips[i].clip;
        }

        return null;
    }
}
