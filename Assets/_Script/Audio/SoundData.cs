using UnityEngine;

/// <summary>
/// Categorizes audio items as background music or sound effects.
/// </summary>
public enum SoundType
{
    Music,
    SFX
}

/// <summary>
/// Serializable data structure representing a single sound entry in AudioManager.
/// Allows setting custom sound ID, volume, pitch randomization, and loop settings in Unity Inspector.
/// </summary>
[System.Serializable]
public class Sound
{
    [Tooltip("Unique Identifier for this sound. Used when calling AudioManager.Instance.PlaySFX('ID') or PlayMusic('ID').")]
    public string soundID;

    [Tooltip("The audio clip file (.wav, .mp3, etc.).")]
    public AudioClip audioFile;

    [Tooltip("Type of sound (Music or SFX). Determines routing to AudioMixer groups.")]
    public SoundType soundType = SoundType.SFX;

    [Tooltip("Volume multiplier for this specific sound (0.0 to 1.0).")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Tooltip("If true, a random pitch offset will be applied every time this sound is played.")]
    public bool useRandomPitch = false;

    [Tooltip("Random pitch range offset. Pitch will vary between (basePitch - pitchRandomRange) and (basePitch + pitchRandomRange).")]
    [Range(0f, 0.5f)]
    public float pitchRandomRange = 0.1f;

    [Tooltip("Base pitch multiplier (1.0 = normal pitch).")]
    [Range(0.1f, 3f)]
    public float basePitch = 1f;

    [Tooltip("Should this audio clip loop automatically?")]
    public bool loop = false;

    /// <summary>
    /// Calculates final pitch applying random variation if useRandomPitch is enabled.
    /// </summary>
    public float GetCalculatedPitch()
    {
        if (!useRandomPitch || pitchRandomRange <= 0f)
        {
            return basePitch;
        }

        float minPitch = Mathf.Max(0.1f, basePitch - pitchRandomRange);
        float maxPitch = basePitch + pitchRandomRange;
        return Random.Range(minPitch, maxPitch);
    }
}
