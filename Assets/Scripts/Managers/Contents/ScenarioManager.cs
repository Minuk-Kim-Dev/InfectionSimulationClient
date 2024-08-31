using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.AI;
using Whisper;
using static Define;

public class ScenarioManager
{
    public int CompleteCount { get; set; }

    public string ScenarioName { get; set; }
    public int Progress { get; set; }
    public string Equipment { get; set; }

    public Dictionary<string, NPCController> NPCs = new Dictionary<string, NPCController>();

    GameObject _realtimeSTT;
    public GameObject RealtimeSTT
    {
        get
        {
            if (_realtimeSTT == null)
                _realtimeSTT = GameObject.Find("RealtimeSTT");

            if (_realtimeSTT == null)
                _realtimeSTT = Managers.Resource.Instantiate("System/RealtimeSTT");

            return _realtimeSTT;
        }
    }

    GameObject _scenarioAssist;
    bool _scenarioHint = false;
    public GameObject ScenarioAssist
    {
        get
        {
            if (_scenarioAssist == null)
                _scenarioAssist = GameObject.Find("ScenarioAssist");

            if (_scenarioAssist == null)
                _scenarioAssist = Managers.UI.CreateUI("ScenarioAssist");

            return _scenarioAssist;
        }
    }

    public void ScenarioAssist_HintActive()
    {
        if(_scenarioHint)
        {
            _scenarioAssist.transform.GetChild(2).gameObject.SetActive(false);
            _scenarioHint = false;
        }
        else
        {
            _scenarioAssist.transform.GetChild(2).gameObject.SetActive(true);
            _scenarioHint = true;
        }
    }

    #region 시나리오 수행 결과 저장 버퍼

    public string MyAction { get; set; }
    public bool PassSpeech { get; set; }
    public List<string> Targets { get; set; } = new List<string>();

    #endregion

    private bool _checkComplete;    //이미 Complete 패킷을 보냈는지 확인, 서버에서 시나리오는 진행시키면 (NextProcess 패킷을 받으면) false로 전환해야 함.
    public Coroutine _routine;
    bool _doingScenario = false;
    public int PopupConfirm { get; set; }   //평상시에는 0, CheckPopup에서 확인을 선택했으면 1, 취소를 선택했으면 2

    public ScenarioInfo CurrentScenarioInfo { get; set; }

    public void Init(string scenarioName)
    {
        ScenarioName = scenarioName;
        Progress = 0;
        CompleteCount = 0;
        _checkComplete = false;
        PassSpeech = false;
        CurrentScenarioInfo = Managers.Data.ScenarioData[ScenarioName][Progress];
        _scenarioAssist.transform.GetChild(2).gameObject.SetActive(_scenarioHint);

        AddNPC("환자", WaitingArea);
        AddNPC("이송요원", WaitingArea);
        AddNPC("보안요원1", WaitingArea);
        AddNPC("보안요원2", WaitingArea);
        AddNPC("미화1", WaitingArea);
        AddNPC("미화2", WaitingArea);
    }

    void Reset()
    {
        PassSpeech = false;
        MyAction = null;
        Targets.Clear();
    }

    #region 시나리오 패킷 관련 기능

    public void SendScenarioInfo(string scenarioName)
    {
        C_StartScenario scenarioPacket = new C_StartScenario();
        scenarioPacket.ScenarioName = scenarioName;
        Managers.Network.Send(scenarioPacket);
    }

    void SendComplete()
    {
        if (_checkComplete == false)
        {
            C_Complete packet = new C_Complete();
            Managers.Network.Send(packet);
            _checkComplete = true;
        }
    }

    #endregion

    #region 시나리오 진행 기능

    public void StartScenario(string scenarioName)
    {
        if(_doingScenario == false)
        {
            _doingScenario = true;
            Managers.Instance.StartCoroutine(CoScenario(scenarioName));
        }
    }

    IEnumerator CoScenarioStep(int progress)
    {
        Managers.STT.SttManager.RegisterCommand(CurrentScenarioInfo.DetailHint, CurrentScenarioInfo.Position == Managers.Object.MyPlayer.Position);
        if (Managers.Object.MyPlayer.Position == CurrentScenarioInfo.Position)
        {
            UpdateScenarioAssist($"{CurrentScenarioInfo.Hint}");
            Managers.Instance.StartCoroutine(CoCheckAction());
            yield return new WaitUntil(() => CompleteCount >= 1);

            if (CurrentScenarioInfo.Confirm != null)
            {
                GameObject go = Managers.UI.CreateUI("ScenarioCheckPopup");
                go.GetComponent<ScenarioCheckPopup>().UpdateText(CurrentScenarioInfo.Confirm);

                yield return new WaitUntil(() => PopupConfirm != 0);

                if(PopupConfirm == 1)
                {
                    PopupConfirm = 0;
                }
                else if(PopupConfirm == 2)
                {
                    PopupConfirm = 0;
                    CompleteCount = 0;
                    yield return Managers.Instance.StartCoroutine(CoScenarioStep(progress));
                    yield break;
                }
            }
        }
        else
        {
            UpdateScenarioAssist(CurrentScenarioInfo.Position + " 플레이어가 시나리오를 진행 중 입니다...");
        }
        SendComplete();

        yield return new WaitUntil(() => Progress == progress);
    }

    IEnumerator CoScenario(string scenarioName)
    {
        Managers.UI.CreateSystemPopup("PopupNotice", $"{scenarioName} 시나리오를 시작합니다.");
        yield return new WaitForSeconds(3.0f);

        Managers.UI.CreateSystemPopup("PopupNotice", $"사랑합니다.\n지금부터 신종감염병 대응 모의 훈련을 시작하고자 하오니 환자 및 보호자께서는 동요하지 마시기 바랍니다.\n모의 훈련 요원들은 지금부터 훈련을 시작하도록 하겠습니다.");
        yield return new WaitForSeconds(3.0f);

        Init(scenarioName);

        switch (scenarioName)
        {
            case "엠폭스":
                NPCs["환자"].Teleport(ObservationArea);
                NPCs["환자"].transform.rotation = Quaternion.Euler(0, -90, 0);
                Managers.UI.ChangeChatBubble(NPCs["환자"].transform, "선생님 방금 가족 중에 한명이 보건소로부터 엠폭스 확진받았다고 연락을 받아서요.\n저도 곧 보건소로부터 연락올거라고 합니다.");
                NPCs["환자"].SetState(CreatureState.Conversation);
                yield return Managers.Instance.StartCoroutine(CoScenarioStep(1));
                Managers.UI.ChangeChatBubble(NPCs["환자"].transform, "이관리 980421 입니다.\n같이 살고있어요.");
                NPCs["환자"].SetState(CreatureState.Conversation);
                yield return Managers.Instance.StartCoroutine(CoScenarioStep(2));
                yield return Managers.Instance.StartCoroutine(CoScenarioStep(3));
                yield return Managers.Instance.StartCoroutine(CoScenarioStep(4));
                yield return Managers.Instance.StartCoroutine(CoScenarioStep(5));
                yield return Managers.Instance.StartCoroutine(CoScenarioStep(6));
                yield return Managers.Instance.StartCoroutine(CoScenarioStep(7));
                yield return Managers.Instance.StartCoroutine(CoScenarioStep(8));
                yield return Managers.Instance.StartCoroutine(CoScenarioStep(9));
                yield return Managers.Instance.StartCoroutine(CoScenarioStep(10));
                yield return Managers.Instance.StartCoroutine(CoScenarioStep(11));
                yield return Managers.Instance.StartCoroutine(CoScenarioStep(12));
                NPCs["환자"].transform.position = Patientlying;

                NPCs["환자"].GetComponent<NavMeshAgent>().enabled = false;
                
                NPCs["환자"].transform.localEulerAngles = new Vector3(-90, 180, 0);
                NPCs["환자"].transform.localPosition = new Vector3(8.924f, 0.962f, 3.808f);
                NPCs["환자"].transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
                NPCs["환자"].DestoyRb();
                NPCs["환자"].transform.SetParent(GameObject.Find("move_bed").transform);
                NPCs["환자"]._positionDisplay.GetComponent<FloatingUI>().Init(GameObject.Find("move_bed").transform, y : 1.621f);

                //환자 음압격리실로 이송
                {
                    NPCs["이송요원"].Equip("ProtectedGear");
                    NPCs["보안요원1"].Teleport(Entrance);
                    NPCs["보안요원2"].Teleport(Entrance);
                    NPCs["보안요원1"].Equip("Mask");
                    NPCs["보안요원2"].Equip("Mask");
                    Managers.UI.ChangeChatBubble(NPCs["보안요원1"].transform, "격리 환자 이송     중입니다.\n통제에 따라주세요");
                    Managers.UI.ChangeChatBubble(NPCs["보안요원2"].transform, "격리 환자 이송 중입니다.\n통제에 따라주세요");
                    NPCs["이송요원"].Teleport(Entrance1);
                    NPCs["보안요원1"].SetOrder(NPCs["보안요원1"].CoGoDestination(EntranceControlPoint));
                    NPCs["이송요원"].SetOrder(NPCs["이송요원"].CoGoDestination(MovePosition));
                    NPCs["보안요원2"].SetOrder(NPCs["보안요원2"].CoFollow(NPCs["이송요원"].transform));
                    yield return new WaitUntil(() => (!NPCs["보안요원1"].IsWorking()));
                    yield return new WaitUntil(() => (!NPCs["이송요원"].IsWorking()));
                    yield return new WaitUntil(() => (NPCs["이송요원"].transform.position - MovePosition).magnitude < 2);
          
                    NPCs["이송요원"].StopOrder();
                    NPCs["보안요원2"].StopOrder();
            
                    GameObject.Find("move_bed").transform.SetParent(NPCs["이송요원"].transform);
                    NPCs["이송요원"].transform.GetChild(1).localPosition = new Vector3(0, 0, 1.2f);
                    NPCs["이송요원"].transform.GetChild(1).localEulerAngles = new Vector3(0, -90,0);
                    
                    NPCs["이송요원"].SetOrder(NPCs["이송요원"].CoGoDestination_Animation(IsolationArea,CreatureState.Push));
                    NPCs["보안요원2"].SetOrder(NPCs["보안요원2"].CoFollow(NPCs["이송요원"].transform));
                    NPCs["이송요원"].ChangeSpeed(2f);
                    NPCs["보안요원2"].ChangeSpeed(2f);
                    GameObject go = Managers.Resource.Instantiate("System/ControlSphere", NPCs["보안요원2"].transform);
                    yield return new WaitUntil(() => NPCs["보안요원2"].Place == "음압격리실");
                    NPCs["이송요원"].ResetSpeed();
                    NPCs["보안요원2"].ResetSpeed();
                    NPCs["이송요원"].StopOrder();
                    NPCs["보안요원2"].StopOrder();
                    Managers.Resource.Destroy(go);
                }
                yield return Managers.Instance.StartCoroutine(CoScenarioStep(13));
                yield return Managers.Instance.StartCoroutine(CoScenarioStep(14));
                yield return Managers.Instance.StartCoroutine(CoScenarioStep(15));
                yield return Managers.Instance.StartCoroutine(CoScenarioStep(16));
                break;
        }

        UpdateScenarioAssist("시나리오를 완료하셨습니다.");
        Managers.UI.CreateSystemPopup("PopupNotice", $"{scenarioName} 시나리오를 완료하셨습니다.");
    }

    #endregion

    #region 시나리오 진행 관련 기능

    //현재 진행된 시나리오에 대하여 다른 플레이어들이 확인할 수 있도록 말풍선, 메시지 등의 상황을 업데이트 해주는 함수
    void UpdateSituation()
    {
        switch (CurrentScenarioInfo.Action)
        {
            case "Call":
                Managers.Phone.Device.SendMessage(CurrentScenarioInfo.Position, CurrentScenarioInfo.DetailHint, CurrentScenarioInfo.Targets);
                break;
        }

        foreach(var npc in NPCs.Values)
        {
            npc.SetState(CreatureState.Idle);
        }
    }

    //서버로부터 다음 시나리오 진행도를 받으면, 시나리오 상황 업데이트 및 변수 초기화 등 실행
    public void NextProgress(int progress)
    {
        ClearNPCBubble();
        UpdateSituation();

        Progress = progress;
        CompleteCount = 0;
        _checkComplete = false;
        Reset();
        CurrentScenarioInfo = Managers.Data.ScenarioData[ScenarioName][Progress];
        _routine = null;
    }

    //화면 상단에 시나리오 진행 관련 힌트를 주는 UI 업데이트
    GameObject Hint;

    public void UpdateScenarioAssist(string state, string position = null)
    {
        if (position != null)
            ScenarioAssist.transform.GetChild(0).GetComponent<TMP_Text>().text = position;

        ScenarioAssist.transform.GetChild(1).GetComponent<TMP_Text>().text = state;

        if (Hint == null)
            Hint = Util.FindChildByName(ScenarioAssist, "Hint");

        if (CurrentScenarioInfo == null)
            return;

        Util.FindChildByName(Hint, "HintSpeech").GetComponent<TMP_Text>().text = CurrentScenarioInfo.DetailHint;
    }

    GameObject AddNPC(string position, Vector3 spawnPoint)
    {
        GameObject go = Managers.Resource.Instantiate($"Creatures/NPC/{position}");
        
        NPCController nc = go.GetComponent<NPCController>();
        nc.Position = position;
        nc.Teleport(spawnPoint);
        
        NPCs.Add(nc.Position, nc);

        return go;
    }

    void ClearNPCBubble()
    {
        foreach(var npc in NPCs.Values)
        {
            Managers.UI.InvisibleBubble(npc.transform);
        }
    }

    #endregion

    #region 시나리오 검증 관련 기능

    IEnumerator CoCheckAction()
    {
        ChangeKeyword(CurrentScenarioInfo.Keywords);
        bool complete = false;

        while (!complete)
        {
            yield return new WaitUntil(() => CheckCondition());
            complete = CheckAction();
            Reset();
        }

        CompleteCount++;
        Managers.UI.CreateSystemPopup("WarningPopup", "시나리오를 통과했습니다.");
    }

    bool CheckCondition()
    {
        if (!CheckPlace())
            return false;

        if (MyAction == null)
            return false;

        if (CurrentScenarioInfo.Action == "Tell" || CurrentScenarioInfo.Action == "Call")
            if (PassSpeech == false)
                return false;

        if (CurrentScenarioInfo.Targets.Count > 0)
            if (Targets.Count == 0)
                return false;

        return true;
    }

    void ChangeKeyword(List<string> keywords)
    {
        RealtimeSTT.GetComponent<RealtimeSTTManager>().initialPrompt = "";
        foreach (var keyword in keywords)
        {
            RealtimeSTT.GetComponent<RealtimeSTTManager>().initialPrompt += $"{keyword} ";
        }
    }

    bool CheckAction()
    {
        if (MyAction != CurrentScenarioInfo.Action)
        {
            Managers.UI.CreateSystemPopup("WarningPopup", "올바른 행동을 수행하지 않았습니다.");
            Reset();
            return false;
        }

        if (!CheckTarget())
        {
            Managers.UI.CreateSystemPopup("WarningPopup", "대상이 올바르지 않습니다.");
            Reset();
            return false;
        }

        if (!string.IsNullOrEmpty(CurrentScenarioInfo.Equipment))
        {
            if (Equipment != CurrentScenarioInfo.Equipment)
            {
                Managers.UI.CreateSystemPopup("WarningPopup", "현재 상황에 알맞게 장비를 착용/해제 하지 않았습니다.");
                Reset();
                return false;
            }
        }

        if (!(CurrentScenarioInfo.Action == "Tell" || CurrentScenarioInfo.Action == "Call"))
            return true;

        if (!PassSpeech)
        {
            Managers.UI.CreateSystemPopup("WarningPopup", "상황에 맞지 않는 대화이거나 대화 장소가 잘못되었습니다.");
            Reset();
            return false;
        }

        return true;
    }

    bool CheckTarget()
    {
        if (Targets.Count < CurrentScenarioInfo.Targets.Count)
            return false;

        foreach(var target in CurrentScenarioInfo.Targets)
        {
            if (!Targets.Contains(target))
                return false;
        }

        return true;
    }

    public bool CheckPlace()
    {
        if (CurrentScenarioInfo == null)
            return true;

        if (CurrentScenarioInfo.Position != Managers.Object.MyPlayer.Position)
            return true;

        //시나리오 진행 장소가 정해지지 않은 경우 통과
        if (string.IsNullOrEmpty(CurrentScenarioInfo.Place))
            return true;

        //장비 착용과 관련된 시나리오가 아니면 통과
        if(!(CurrentScenarioInfo.Action == "Equip" || CurrentScenarioInfo.Action == "UnEquip"))
            return true;

        //시나리오 진행 장소가 정해져있는 경우, 현재 내가 해당 장소에 있는지 확인
        if (CurrentScenarioInfo.Place == Managers.Object.MyPlayer.Place)
            return true;
        else
            return false;
    }

    public float CheckKeywords(ref string message)
    {
        if (string.IsNullOrEmpty(message))
            return 0;

        float count = 0;
        List<string> needKeywords = new List<string>();

        foreach (var keyword in CurrentScenarioInfo.Keywords)
        {
            if (message.Contains(keyword))
            {
                count += 1;
                message = message.Replace(keyword, $"<color=#0000ff>{keyword}</color>");
            }
            else
            {
                needKeywords.Add(keyword);
            }
        }

        if(needKeywords.Count > 0)
        {
            message += "\n<color=#ff0000>필요한 키워드 : ";

            foreach (var keyword in needKeywords)
            {
                message += $"{keyword} ";
            }
            message += "</color>";
        }

        return count / (float)CurrentScenarioInfo.Keywords.Count;
    }

    #endregion
}