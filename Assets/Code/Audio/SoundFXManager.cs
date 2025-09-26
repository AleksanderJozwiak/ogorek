using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;
using System;

//NEW SOUND TYPES ALWAYS ADD AT THE BOTTOM OF THE LIST
public enum SoundType
{
    SucceedParry
}

[ExecuteInEditMode]
public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager Instance;
    public AudioMixerGroup mixerGroup;

    [SerializeField] AudioSource soundFXObject;
    [Tooltip("Add new sound types via code.")]
    [SerializeField] SoundList[] soundList;

    AudioSource audioSource;

    void Awake()
    {
        if (Application.isPlaying)
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            DontDestroyOnLoad(gameObject);
        }
    }

    public void PlaySound2D(SoundType soundType, Transform spawnTransform, float volume = 1f)
    {
        // CHOOSE RANDOM AUDIO CLIP DROM LIST ASSIGNED TO TYPE
        AudioClip audioClip = Instance.soundList[(int)soundType].Sounds[UnityEngine.Random.Range(0, Instance.soundList[(int)soundType].Sounds.Length)];

        // GET OBJECT FROM THE POOL
        GameObject poolObject = PoolManager.Instance.PoolMap[PoolCategory.SoundFX].Get();

        // GET REFERENCE TO THE AUDIO SOURCE
        AudioSource audioSource = poolObject.GetComponent<AudioSource>();

        // ASSIGN AUDIO CLIP
        audioSource.clip = audioClip;

        // ASSIGN GROUP OF SOUND
        audioSource.outputAudioMixerGroup = mixerGroup;

        // ASSIGN VOLUME
        audioSource.volume = volume;

        // PLAY SOUND
        audioSource.Play();

        // GET LENGTH OF SFX CLIP
        float clipLength = audioSource.clip.length;

        // RETURN THE OBJECT TO THE POOL AFTER IT IS DONE PLAYING
        StartCoroutine(PoolManager.Instance.ReleaseObject(PoolCategory.SoundFX, poolObject, clipLength));

        Debug.Log($"[SFX] Odtwarzam: {soundList[(int)soundType].name} na {spawnTransform.name}");
    }

    public void PlaySound3D(SoundType soundType, Transform spawnTransform, float volume = 1f, float minDistance = 0.5f, float maxDistance = 5f)
    {
        // CHOOSE RANDOM AUDIO CLIP DROM LIST ASSIGNED TO TYPE
        AudioClip audioClip = Instance.soundList[(int)soundType].Sounds[UnityEngine.Random.Range(0, Instance.soundList[(int)soundType].Sounds.Length)];

        // GET OBJECT FROM THE POOL
        GameObject poolObject = PoolManager.Instance.PoolMap[PoolCategory.SoundFX].Get();

        // GET REFERENCE TO THE AUDIO SOURCE
        AudioSource audioSource = poolObject.GetComponent<AudioSource>();

        // ASSIGN AUDIO CLIP
        audioSource.clip = audioClip;

        // ASSIGN GROUP OF SOUND
        audioSource.outputAudioMixerGroup = mixerGroup;

        // ASSIGN VOLUME
        audioSource.volume = volume;

        // TURN SPATIAL BLEND INTO 3D
        audioSource.spatialBlend = 1f;

        // TURN OFF DOPPLER EFFECT
        audioSource.dopplerLevel = 0f;

        // CHANGE ROLLOFF MODE TO CUSTOM
        audioSource.rolloffMode = AudioRolloffMode.Custom;

        // ASSIGN KEY FRAMES TO CUSTOM CURVE
        AnimationCurve customCurve = new AnimationCurve(
            new Keyframe(0f, volume),
            new Keyframe(1f, 0f)
        );

        // ASSIGN MIN DISTANCE AT WHICH SPATIAL BLEND CHANGES TO 2D
        audioSource.minDistance = minDistance;

        // ASSIGN MAX DISTANCE FROM AUDIO SOURCE
        audioSource.maxDistance = maxDistance;

        // PLAY SOUND
        audioSource.Play();

        // GET LENGTH OF SFX CLIP
        float clipLength = audioSource.clip.length;

        // RETURN THE OBJECT TO THE POOL AFTER IT IS DONE PLAYING
        StartCoroutine(PoolManager.Instance.ReleaseObject(PoolCategory.SoundFX, poolObject, clipLength));

        Debug.Log($"[SFX] Odtwarzam: {soundList[(int)soundType].name} na {spawnTransform.name}");
    }

    public void ChooseRandom(SoundType soundType, AudioSource audioSource)
    {
        // CHOOSE RANDOM AUDIO CLIP FROM LIST ASSIGNED TO TYPE
        AudioClip audioClip = Instance.soundList[(int)soundType].Sounds[UnityEngine.Random.Range(0, Instance.soundList[(int)soundType].Sounds.Length)];

        // ASSIGN AUDIO CLIP
        audioSource.clip = audioClip;
    }

#if UNITY_EDITOR
    private void OnEnable()
    {
        string[] names = Enum.GetNames(typeof(SoundType));
        Array.Resize(ref soundList, names.Length);

        for(int i = 0; i < soundList.Length; i++)
        {
            soundList[i].name = names[i];
        }
    }
#endif

    [Serializable]
    public struct SoundList
    {
        [HideInInspector] public string name;
        [SerializeField] AudioClip[] sounds;

        public AudioClip[] Sounds => sounds;
    }
}
