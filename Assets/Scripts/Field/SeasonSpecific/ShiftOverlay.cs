using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShiftOverlay : GameUI
{
    
    public static float ShiftTimer;
    public static string ShiftName = "";

    private TextMeshProUGUI _matchStateLabel;
    private float _autoEndTime;
    
    protected override void Init()
    {
        ShiftTimer = 0;
        ShiftName = "";
        _autoEndTime = fms.matchTime - fms.autoTime;
        _matchStateLabel = spawnedUI.transform.Find("MatchStateLabel").GetComponent<TextMeshProUGUI>();
    }


    void Update()
    {
        UpdateOverlay();
    }
    
    private void UpdateOverlay()
    {
        if (_matchStateLabel == null) return;

        string stateText;
        switch (FMS.MatchState)
        {
            case MatchState.auto:
                var autoRemaining = FMS.MatchTimer - _autoEndTime;
                var autoSec = Mathf.CeilToInt(Mathf.Max(autoRemaining, 0f));
                stateText = $"Auto : {autoSec:D2}";
                break;
            case MatchState.finished:
                stateText = "Match Over";
                break;
            default:
                if (ShiftTimer > 0 && ShiftName.Length > 0 && RebuiltShifts.currentShift != RebuiltShifts.CurrentShift.Auto)
                {
                    int shiftSec = Mathf.CeilToInt(ShiftTimer);
                    stateText = $"{ShiftName} : {shiftSec:D2}";
                }
                else if (RebuiltShifts.currentShift == RebuiltShifts.CurrentShift.Auto)
                {
                    stateText = "Auto : 00";
                }
                else
                {
                    stateText = "";
                }
                break;
        }
        _matchStateLabel.text = stateText;
    }
}
