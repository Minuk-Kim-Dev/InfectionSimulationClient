using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeywordManager
{
    GUIKeywordPanel _panel;

    public void OpenGUIKeyword()
    {
        if (Managers.Scenario.CurrentScenarioInfo == null)
        {
            Managers.UI.CreateSystemPopup("WarningPopup", "시나리오가 시작되지 않았습니다.", UIManager.NoticeType.Warning);
            return;
        }

        if (Managers.Scenario.CurrentScenarioInfo.Position != Managers.Object.MyPlayer.Position)
        {
            Managers.UI.CreateSystemPopup("WarningPopup", "시나리오를 수행할 차례가 아닙니다.", UIManager.NoticeType.None);
            return;
        }

        GameObject go = Managers.UI.CreateUI("GUIKeyword/GUIKeywordPanel");
        go.GetComponent<GUIKeywordPanel>().UpdateUI();
        _panel = go.GetComponent<GUIKeywordPanel>();
        Managers.Object.MyPlayer.State = CreatureState.Conversation;
    }

    public void CloseGUIKeyword()
    {
        _panel.ResetUIs();
        Managers.UI.DestroyUI(_panel.gameObject);
        _panel = null;
        Managers.Object.MyPlayer.State = CreatureState.Idle;
    }

    public void CheckRemainKeywords()
    {
        if (_panel == null)
            return;

        if (!_panel.CheckRemainKeywords())
            return;

        Managers.Scenario.PassSpeech = true;

        C_Talk talkPacket = new C_Talk();
        talkPacket.Message = Managers.Scenario.CurrentScenarioInfo.OriginalSentence;
        talkPacket.TTSSelf = true;
        Managers.Network.Send(talkPacket);

        CloseGUIKeyword();
    }

    public void Clear()
    {
        _panel = null;
    }
}