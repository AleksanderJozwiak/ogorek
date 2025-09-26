using UnityEngine;
using UnityEngine.Pool;
using System;
using System.Collections;
using System.Collections.Generic;

#region Manual
// THE EXMAPLE OF USE:
// TO GET OBJECT: GameObject poolObject = PoolManager.Instance.PoolMap[PoolCategory.SoundFX].Get();
// TO RELEASE OBJECT: StartCoroutine(PoolManager.Instance.ReleaseObject(PoolCategory.SoundFX, poolObject, clipLength));
#endregion

// NEW CATEGORIES ALWAYS ADD AT THE BOTTOM OF THE ENUM
public enum PoolCategory
{
    SoundFX,
    Bullets,
    Spacecrafts,
    Planets,
    Asteroids
}

[ExecuteInEditMode]
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;

    [Tooltip("Add new category via code.")]
    [SerializeField] PoolConfig[] poolConfigs; // CONTAINS CONFIGURATION FOR EACH CATEGORY.

    Dictionary<PoolCategory, IObjectPool<GameObject>> poolMap = new(); // EACH POOL IS ASSOCIATED WITH A POOL CATEGORY
    Dictionary<PoolCategory, Transform> parentMap = new(); // EACH CATEGORY HAS IT'S OWN EMPTY OBJECT AS A COINTAINER FOR THE POOL
    Dictionary<float, WaitForSecondsRealtime> delayMap = new(); // DELAY MAP DICTIONARY CONTAINS WAIT FOR SECONDS AS VALUES TO REUSE THEM WHEN KEY MATCHES

    public Dictionary<PoolCategory, IObjectPool<GameObject>> PoolMap => poolMap;

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
    
    void Start()
    {
        // CREATE POOL FOR EACH CATEGORY
        foreach (PoolCategory category in Enum.GetValues(typeof(PoolCategory)))
            {
                // GET THE PARTICULAR CONFIG ASSOCIATED WITH THE CATEGORY
                PoolConfig config = poolConfigs[(int)category];
                
                // CREATE POOL
                IObjectPool<GameObject> pool = new ObjectPool<GameObject>
                (
                    () => OnInitialize(category, config), // CREATE FUNC
                    poolObject => poolObject.SetActive(true), // ACTION ON GET
                    poolObject => poolObject.SetActive(false), // ACTION ON RELEASE
                    poolObject => Destroy(poolObject), // ACTION ON DESTROY
                    true, // COLLECTION CHECK
                    config.MinSize, // DEAFAULT CAPACITY
                    config.MaxSize // MAX SIZE
                );

                // ADD POOL TO THE DICTIONARY
                poolMap[category] = pool;
            }

        // START PREWARMING ENTRY COROUTINE
        StartCoroutine(InitializePool());
    }

    // CREATE FUNC
    GameObject OnInitialize(PoolCategory category, PoolConfig config)
    {
        GameObject poolObject = Instantiate(config.Prefab);
        parentMap.TryGetValue(category, out Transform parentTransform);
        poolObject.transform.SetParent(parentTransform);
        return poolObject;
    }

    // INVOKE THIS METHOD TO DEACTIVATE THE OBJECT AND RETURN IT TO THE POOL
    // THE CATEGORY CONATINER IS AGAIN SETTING AS THE OBJECT'S PARENT
    public IEnumerator ReleaseObject(PoolCategory category, GameObject poolObject, float delay = 0f)
    {
        poolMap.TryGetValue(category, out IObjectPool<GameObject> pool);

        if (delay != 0)
        {
            yield return delayValue(delay);
        }

        pool.Release(poolObject);

        if (poolObject.transform.parent.name != poolConfigs[(int)category].name)
        {
            poolObject.transform.SetParent(parentMap[category]);
        }

        yield return null;
    }

    // CHECK, IF THERE'S A WAIT FOR SECONDS IN THE DELAY MAP THAT REQUIRES TO THE KEY EQUAL TO THE SECONDS PARAMETER
    WaitForSecondsRealtime delayValue(float seconds)
    {
        if (!delayMap.TryGetValue(seconds, out var waitForSeconds))
        {
            waitForSeconds = new WaitForSecondsRealtime(seconds);
            delayMap[seconds] = waitForSeconds;
        }
        return waitForSeconds;
    }

    #region Prewarming
    IEnumerator InitializePool()
    {
        // WARM UP A POOL FROM EACH CATEGORY ON BY ONE
        foreach (PoolCategory category in Enum.GetValues(typeof(PoolCategory)))
        {
            yield return WarmUp(category);
        }
    }

    IEnumerator WarmUp(PoolCategory category)
    {
        if (Application.isPlaying)
        {
            PoolConfig config = poolConfigs[(int)category]; // FIND THE RIGHT CONFIG BY POOL CATEGORY

            // FIND, IF THERE'S ALREADY EXISTING THE RIGHT POOL CONTAINER
            Transform categoryTransform = null;
            foreach (Transform child in transform)
            {
                if (child.name == config.name)
                {
                    categoryTransform = child;
                    break;
                }
            }

            // CREATE THE CONTAINER FOR THE POOL IF THERE'S NONE
            if (categoryTransform == null)
            {
                GameObject categoryParent = new GameObject(config.name);
                categoryTransform = categoryParent.transform;
                categoryTransform.SetParent(this.transform);
                parentMap[category] = categoryTransform;
            }

            // CHECK, IF PREWARM OPTION IS ENABLED AND POOL HAS NOT BEEN REACH IT'S DEFAULT CAPACITY
            if (config.PreWarmPool && config.MinSize > 0)
            {
                // GET THE POOL FROM DICTIONARY
                poolMap.TryGetValue(category, out IObjectPool<GameObject> pool);

                // CREATE GAME OBJECTS FROM THE PREFAB UNTIL THE POOL REACH DEAFAULT CAPACITY
                for (int currentCount = categoryTransform.childCount; currentCount < config.MinSize; currentCount++)
                {
                    GameObject poolObject = Instantiate(config.Prefab);
                    poolObject.SetActive(false);
                    poolObject.transform.SetParent(categoryTransform.transform);
                    pool.Release(poolObject); // RELEASE THE OBJECT TO THE POOL
                    yield return null; // WAIT ONE FRAME
                }
            }
        }
    }
    #endregion

    #region Unity Editor
    // RESIZE THE POOL CONFIG ARRAY'S LENGTH BY ADDING A NEW POOL CATEGORY
    // CONFIG'S NAMES ARE SYNCHRONIZED WITH POOL CATEGORY
    #if UNITY_EDITOR
    private void OnEnable()
    {
        string[] names = Enum.GetNames(typeof(PoolCategory));
        Array.Resize(ref poolConfigs, names.Length);

        for(int i = 0; i < poolConfigs.Length; i++)
        {
            poolConfigs[i].name = names[i];
        }
    }
    #endif
    #endregion

    #region PoolConfig struct
    [Serializable]
    public struct PoolConfig
    {
        [HideInInspector] public string name;
        [SerializeField] GameObject prefab;
        [SerializeField] int minSize;
        [Tooltip("If pool reaches max size, redundant objects are destroyed.")]
        [SerializeField] int maxSize;
        [Tooltip("If box is checked, objects in the min size number are generated.")]
        [SerializeField] bool preWarmPool;

        public GameObject Prefab => prefab;
        public int MinSize => minSize;
        public int MaxSize => maxSize;
        public bool PreWarmPool => preWarmPool;
    }
    #endregion
}
