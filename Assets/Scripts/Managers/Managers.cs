using System;
using System.Collections;
using UnityEngine;

public class Managers : MonoBehaviour
{
    static Managers _instance;
    public static Managers Instance { get { Init(); return _instance; } }

    #region Contents
    ObjectManager _obj = new ObjectManager();
    NetworkManager _network = new NetworkManager();
    ScenarioManager _scenario = new ScenarioManager();
    PhoneManager _phone = new PhoneManager();
    ItemManager _item = new ItemManager();
    BubbleManager _bubble = new BubbleManager();

    public static ObjectManager Object { get { return Instance._obj; } }
    public static NetworkManager Network { get { return Instance._network; } }
    public static ScenarioManager Scenario { get { return Instance._scenario; } }
    public static PhoneManager Phone { get { return Instance._phone; } }
    public static ItemManager Item { get { return Instance._item; } }
    public static BubbleManager Bubble { get { return Instance._bubble; } }
   
    #endregion

    #region Core
    PoolManager _pool = new PoolManager();
    ResourceManager _resource = new ResourceManager();
    SceneManagerEx _scene = new SceneManagerEx();
    UIManager _ui = new UIManager();
    InputManager _input = new InputManager();
    DataManager _data = new DataManager();
    SoundManager _sound = new SoundManager();
    SettingManager _setting = new SettingManager();
    STTManager _stt = new STTManager();
    TTSManager _tts = new TTSManager();
    public static PoolManager Pool { get { return Instance._pool; } }
    public static ResourceManager Resource { get { return Instance._resource; } }
    public static SceneManagerEx Scene { get { return Instance._scene; } }
    public static UIManager UI { get { return Instance._ui; } }
    public static InputManager Input { get { return Instance._input; } }
    public static DataManager Data { get { return Instance._data; } }
    public static SoundManager Sound { get { return Instance._sound; } }
    public static SettingManager Setting { get { return Instance._setting; } }
    public static STTManager STT { get { return Instance._stt; } }
    public static TTSManager TTS { get { return Instance._tts; } }
    #endregion

    void Start()
    {
        Init();
    }

    void Update()
    {
        _network.Update();
    }

    static void Init()
    {
        if (_instance == null)
        {
            GameObject go = GameObject.Find("@Managers");
            if (go == null)
            {
                go = new GameObject { name = "@Managers" };
                go.AddComponent<Managers>();
            }

            DontDestroyOnLoad(go);
            _instance = go.GetComponent<Managers>();

            _instance._pool.Init();
            _instance._data.Init();

            try
            {
                _instance._network.Init();
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }
    }

    public static void Clear()
    {
        Scene.Clear();
        Pool.Clear();
    }
}