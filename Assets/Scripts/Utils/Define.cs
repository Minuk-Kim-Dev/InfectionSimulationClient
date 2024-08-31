using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Define
{
    #region Voice

    public class VoiceBuffer<T>
    {
        private T[] _buffer;
        private int _writePos;
        private int _readPos;
        private int _bufferSize;
        private int _count;

        public VoiceBuffer(int size)
        {
            _bufferSize = size;
            _buffer = new T[_bufferSize];
            _writePos = 0;
            _readPos = 0;
            _count = 0;
        }

        public void Write(T item)
        {
            _buffer[_writePos] = item;
            _writePos = (_writePos + 1) % _bufferSize;

            if (_count == _bufferSize)
            {
                _readPos = (_readPos + 1) % _bufferSize;
            }
            else
            {
                _count++;
            }
        }

        public void Write(T[] items)
        {
            foreach (var item in items)
            {
                Write(item);
            }
        }

        public T Read()
        {
            if (_count == 0)
            {
                return default;
            }

            T item = _buffer[_readPos];
            _readPos = (_readPos + 1) % _bufferSize;
            _count--;

            return item;
        }

        public void Read(T[] output, int length)
        {
            if (_count < length)
                return;

            for (int i = 0; i < length; i++)
            {
                output[i] = Read();
            }
        }

        public int Count { get { return _count; } }
        public bool IsEmpty { get { return _count == 0; } }
        public bool IsFull { get { return _count == _bufferSize; } }
        public int ReadPos { get { return _readPos; } }
    }

    public static int VoiceFrequency = 16000;
    public static int VoiceChannel = 2;

    public static float[] ConvertMonoToStereo(float[] monoSamples)
    {
        float[] stereoSamples = new float[monoSamples.Length * 2];
        for (int i = 0; i < monoSamples.Length; i++)
        {
            stereoSamples[i * 2] = monoSamples[i];
            stereoSamples[i * 2 + 1] = monoSamples[i];
        }
        return stereoSamples;
    }

    public static float[] ConvertStereoToMono(float[] stereoSamples)
    {
        int monoLength = stereoSamples.Length / 2;
        float[] monoSamples = new float[monoLength];

        for (int i = 0; i < monoLength; i++)
        {
            monoSamples[i] = (stereoSamples[i * 2] + stereoSamples[i * 2 + 1]) / 2;
        }

        return monoSamples;
    }

    #endregion

    public static readonly Vector3 Entrance = new Vector3(-4, 0, 18);
    public static readonly Vector3 WaitingArea = new Vector3(-35, 0, 15);
    public static readonly Vector3 ObservationArea = new Vector3(11.5f, 0, 0);
    public static readonly Vector3 IsolationArea = new Vector3(22, 0, -44);
    public static readonly Vector3 EntranceControlPoint = new Vector3(-5, 0, 13);
    public static readonly Vector3 CorridorControlPoint = new Vector3(-5, 0, -16);
    public static readonly Vector3 Station = new Vector3(-12, 0, -9);

    //추가 08.07 최우진
    public static readonly Vector3 Patientlying = new Vector3(100, 0.7812495f, 100f);
    public static readonly Vector3 Entrance1 = new Vector3(-5, 0, 18);
    public static readonly Vector3 MovePosition = new Vector3(9.6f, 0, 2.32f);
   

    public static readonly string[] PhoneAddress =
    {
        "응급센터",
        "응급의학과",
        "감염관리팀",
        "영상의학팀",
        "감염병대응센터",
        "감염재난대책본부",
        "이송팀",
        "보안팀",
        "미화팀",
        "원무팀",
        "진단검사의학팀"
    };

    public interface IJob
    {
        void Execute();
    }

    //인수가 없는 작업
    public class Job : IJob
    {
        Action _action;

        public Job(Action action)
        {
            _action = action;
        }

        public void Execute()
        {
            _action.Invoke();
        }
    }

    //인수가 1개인 작업
    public class Job<T1> : IJob
    {
        Action<T1> _action;
        T1 _t1;

        public Job(Action<T1> action, T1 t1)
        {
            _action = action;
            _t1 = t1;
        }

        public void Execute()
        {
            _action.Invoke(_t1);
        }
    }

    //인수가 2개인 작업
    public class Job<T1, T2> : IJob
    {
        Action<T1, T2> _action;
        T1 _t1;
        T2 _t2;

        public Job(Action<T1, T2> action, T1 t1, T2 t2)
        {
            _action = action;
            _t1 = t1;
            _t2 = t2;
        }

        public void Execute()
        {
            _action.Invoke(_t1, _t2);
        }
    }

    public enum Sound
    {
        Bgm,
        Effect,
        MaxCount
    }

    public class JScenarioInfo
    {
        public int Progress { get; set; }
        public string Place { get; set; }
        public string Position { get; set; }
        public string Equipment { get; set; }
        public string Action { get; set; }
        public string Keywords { get; set; }
        public string Targets { get; set; }
        public string DetailHint { get; set; }
        public string Hint { get; set; }
        public string Confirm { get; set; }
    }

    public class ScenarioInfo
    {
        public int Progress { get; set; }
        public string Place { get; set; }
        public string Position { get; set; }
        public string Equipment { get; set; }
        public string Action { get; set; }
        public List<string> Keywords { get; set; } = new List<string>();
        public List<string> Targets { get; set; } = new List<string>();
        public string DetailHint { get; set; }
        public string Hint { get; set; }
        public string Confirm { get; set; }
    }

    public class ItemInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public ItemInfo(string name, string description)
        {
            this.Name = name;
            this.Description = description;
        }
    }

    public static readonly Dictionary<string, ItemInfo> ItemInfoDict = new Dictionary<string, ItemInfo>()
    {
        {"Mask", new ItemInfo("마스크", "먼지, 바이러스, 세균 등 공기 중의 유해물질이나 감염으로부터 착용자를 보호")},
        {"ProtectedGear", new ItemInfo("4종 보호구", "감염 및 오염으로부터 의료진을 보호하기 위해\n마스크, 장갑, 가운, 고글(또는 페이스쉴드)로 구성된 개인 보호장비")},
        {"ShoeCover", new ItemInfo("덧신", "병원 내 감염 예방과 위생 유지, 환자 보호를 위해 사용")},
        {"Goggle", new ItemInfo("고글", "눈을 통한 감염을 방지하기 위해 착용하는 보호 장비")}
    };

    public enum Scene
    {
        Unknown,
        Game,
        Lobby,
        Login
    }

    public enum PlayerState
    {
        None,
        UsingPhone,
        UsingSetting
    }
}