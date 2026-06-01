using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MyBox;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Util;

[ExecuteAlways]
public class LoadMatch : MonoBehaviour
{
    [SerializeField] private GameObject[] fieldPrefab;
    [SerializeField] private bool useCustomSpawnPoint;
    [SerializeField] private Transform spawnPoint;

    [Header("Robot Selection")] [SerializeField]
    private InspectorDropdown robotSeasonSelected;
    [SerializeField] private InspectorDropdown robotSelected;

    [SerializeField] private Cameras view;

    [ConditionalField(true, nameof(isDriverStation))] [SerializeField]
    private StationNum stationNumber;
   [ConditionalField(true, nameof(isDriverStation))] [SerializeField]
    private TrackingType trackingType;
     private int selectedRobotIndex; 
     private string selectedName;
     private int selectedSeasonIndex;
     private string selectedSeasonName;
     private List<GameObject> availableRobots = new List<GameObject>();
     private List<string> availableSeasons = new List<string>();

     private bool isDriverStation() => view == Cameras.DriverStation;
    
    private GameObject _fieldHolder;
    private GameObject _activeRobot;
    private GameObject _activeCam;
    private GameObject _spawnedCamera;

    private FMS fms;

    private void OnEnable()
    {
        CheckSeasons();
        robotSeasonSelected.canBeSelected = availableSeasons;
        CheckRobots();
        robotSelected.canBeSelected = availableRobots.Select(x => x.name).ToList();
    }

    private void LateUpdate()
    {
        CheckSeasons();
        robotSeasonSelected.canBeSelected = availableSeasons;
        robotSeasonSelected.selectedIndex = selectedSeasonIndex;
        robotSeasonSelected.selectedName = selectedSeasonName;
        CheckRobots();
        robotSelected.canBeSelected = availableRobots.Select(x => x.name).ToList();
        robotSelected.selectedIndex = selectedRobotIndex;
        robotSelected.selectedName = selectedName;
    }

    private void Start()
    {
        selectedName = robotSelected.selectedName;
        selectedRobotIndex = robotSelected.selectedIndex;
        selectedSeasonIndex = robotSeasonSelected.selectedIndex;
        selectedSeasonName = robotSeasonSelected.selectedName;
        CheckRobots(); 
        ResetField();
    }
    
    private void Update()
    {
        selectedName = robotSelected.selectedName;
        selectedRobotIndex = robotSelected.selectedIndex;
        selectedSeasonIndex = robotSeasonSelected.selectedIndex;
        selectedSeasonName = robotSeasonSelected.selectedName;
        
        if (!EditorApplication.isPlayingOrWillChangePlaymode && RobotLoaded())
        {
            DeleteRobot();
        }
        if (EditorApplication.isPlaying) return;
        
        if (!CheckField())
        {
            DestroyField();
            LoadField();
        }
        
        CheckRobots(); 
    }
    
    private void LoadField()
    {
        _fieldHolder = new GameObject
        {
            name = "FieldHolder",
            transform = { position = Vector3.zero, rotation = Quaternion.identity, parent = transform },
            
        };
        Instantiate(fieldPrefab[0], Vector3.zero, Quaternion.identity, _fieldHolder.transform);
    }
    
    private bool CheckField()
    {
        if (transform.childCount == 0)
        {
            return false;
        }
        else
        {
            return _fieldHolder.transform.Find(fieldPrefab[0].name+"(Clone)");
        }
    }
    
    private void DestroyField()
    {
        if (transform.Find("FieldHolder"))
        {
            _fieldHolder = transform.Find("FieldHolder").GameObject();
            DestroyImmediate(_fieldHolder);
        }
    }

    public TrackingType GetTrackingType()
    {
        return trackingType;
    }
    
    public void ResetField()
    {
        DestroyField();
        LoadField();
        SpawnRobot();
        addCamera();
        Utils.resetParentCache();
        if (fms)
        {
            fms.Restart();
        }
    }

    public void setFMS(FMS fms)
    {
        this.fms = fms;
    }

    public GameObject getFieldHolder()
    {
        return _fieldHolder;
    }
    
    private void SpawnRobot()
    {
        if (availableRobots.Count > 0 && selectedRobotIndex >= 0 && selectedRobotIndex < availableRobots.Count)
        {
            GameObject robotToSpawn = availableRobots[selectedRobotIndex];
            Transform spawnLocation = useCustomSpawnPoint ? spawnPoint : 
                                        fms != null ? fms.defaultSpawn : 
                                                        spawnPoint;
            _activeRobot = Instantiate(robotToSpawn, spawnLocation.position, spawnLocation.rotation, _fieldHolder.transform);
            var frame = _activeRobot.GetComponent<BuildFrame>();
            var controller = frame.GetSwerveController();
            if (controller)
            {
                switch (view)
                {
                    case (Cameras.FirstPerson) :
                        controller.reversed = false;
                        controller.fieldCentric = false;
                        break;
                    case (Cameras.FirstPersonReversed) :
                        controller.reversed = true;
                        controller.fieldCentric = false;
                        break;
                    case (Cameras.ThirdPerson) :
                        controller.reversed = false;
                        controller.fieldCentric = true;
                        break;
                    case (Cameras.ReversedThirdPerson) :
                        controller.reversed = true;
                        controller.fieldCentric = true;
                        break;
                    case Cameras.DriverStation :
                        controller.reversed = false;
                        controller.fieldCentric = true;
                        break;
                }
            }
        }
    }
    
    private bool RobotLoaded()
    {
        return _activeRobot != null;
    }

    public GameObject GetRobotLoaded()
    {
        return _activeRobot;
    }
    private void DeleteRobot()
    {
        DestroyImmediate(_spawnedCamera);
        DestroyImmediate(_activeRobot);
    }
    
    private void addCamera()
    {
        string objectToLoad = "Cameras/" + view.ToString();
        _activeCam = Resources.Load(objectToLoad) as GameObject;

        var parent = _activeRobot;
        var spawnRotation = spawnPoint.gameObject;
        if (fms)
        {
            parent = view == Cameras.DriverStation ? fms.blueStationCams[(int)stationNumber] : _activeRobot;
            spawnRotation = view == Cameras.DriverStation ? fms.redStationCams[(int)stationNumber] : spawnPoint.gameObject;
        }

        _spawnedCamera = Instantiate(_activeCam, Vector3.zero, spawnRotation.transform.rotation, parent.transform);;
        _spawnedCamera.transform.localPosition = Vector3.zero;
    }

    
    public void CheckSeasons() 
    {
        string resourcesPath = Path.Combine(Application.dataPath, "Resources", "Robots");
        
        availableSeasons.Clear();
        
        if (Directory.Exists(resourcesPath))
        {
            string[] rawFolderPaths = Directory.GetDirectories(resourcesPath);

            foreach (string path in rawFolderPaths)
            {
                string folderName = Path.GetFileName(path);
                availableSeasons.Add(folderName);
            }
        }
        
        if (selectedSeasonIndex >= availableSeasons.Count)
        {
            selectedSeasonIndex = availableSeasons.Count > 0 ? availableSeasons.Count - 1 : 0;
        }
    }
    
    public void CheckRobots()
    {
        string path = "Robots/" + selectedSeasonName;
        GameObject[] loadedRobots = Resources.LoadAll<GameObject>(path);
        
        availableRobots.Clear();
        foreach (var robot in loadedRobots)
        {
            availableRobots.Add(robot);
        }
        
        if (selectedRobotIndex >= availableRobots.Count)
        {
            selectedRobotIndex = availableRobots.Count > 0 ? availableRobots.Count - 1 : 0;
        }
    }
}
