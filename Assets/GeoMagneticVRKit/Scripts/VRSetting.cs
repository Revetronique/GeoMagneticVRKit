using UnityEngine;
using System.Collections;

public class VRSetting : MonoBehaviour {

    static float UNIT_DIST_MM = 0.001f;

    [SerializeField]
    GameObject[] vrCamera;

    [SerializeField, Tooltip("VR表示をさせるかどうか")]
    bool vrMode = true;

    /// <summary>
    /// 瞳孔間距離(mm)
    /// </summary>
    [SerializeField, Range(52, 78), Tooltip("瞳孔間距離(mm)")]
    float pupilDist = 63.5f;

    /// <summary>
    /// カメラの歪み度合
    /// </summary>
    [SerializeField, Range(0.1f, 2.0f), Tooltip("FOVの歪み補正度合")]
    float fovRadius = 1.69f;

    /// <summary>
    /// 瞳孔間距離(mm)
    /// </summary>
    public float PupilDistance { get { return pupilDist; } }

    // Use this for initialization
    void Start () {

        SetVRMode(vrMode);

    }
    
    public void SetVRMode(bool useVR)
    {
        if (useVR)
        {
            SetPupilDistance(pupilDist);
            SetFovRadius(fovRadius);
        }
        else
        {
            foreach (GameObject cam in vrCamera)
            {
                cam.GetComponent<BarrelDistortion>().enabled = false;
            }
        }
    }

    public void SetPupilDistance(float dist)
    {
        float d = dist / 2 * UNIT_DIST_MM;

        Vector3 pos = new Vector3(-d, 0, 0);
        vrCamera[0].transform.position = pos;
        pos = new Vector3(d, 0, 0);
        vrCamera[1].transform.position = pos;
    }

    public void SetFovRadius(float dist)
    {
        fovRadius = dist;
        foreach (GameObject cam in vrCamera)
        {
            BarrelDistortion barrel = cam.GetComponent<BarrelDistortion>();
            barrel.enabled = true;
            barrel.FOV_Radians = fovRadius;
        }
    }

}
